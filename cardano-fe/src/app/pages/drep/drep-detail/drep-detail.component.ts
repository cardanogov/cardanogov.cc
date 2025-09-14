import { CommonModule } from '@angular/common';
import {
  AfterViewInit,
  ChangeDetectorRef,
  Component,
  ElementRef,
  inject,
  OnInit,
  ViewChild,
} from '@angular/core';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { Router, RouterModule } from '@angular/router';
import {
  NbBadgeModule,
  NbCardModule,
  NbIconModule,
  NbToastrService,
} from '@nebular/theme';
import { ChartComponent } from '../../../shared/components/chart/chart.component';
import { ChartSkeletonComponent } from '../../../shared/components/skeleton/chart-skeleton.component';
import {
  TableColumn,
  TableComponent,
} from '../../../shared/components/table/table.component';
import { ModalComponent } from '../../../shared/components/modal/modal.component';
import {
  Chart,
  ChartConfiguration,
  ChartType,
  registerables,
  ChartTypeRegistry,
} from 'chart.js';
import { ActivatedRoute } from '@angular/router';
import {
  DrepCardDataByIdResponse,
  DrepDetailsVotingPowerResponse,
  DrepInfoResponse,
  DrepService,
  DrepVoteInfoResponse,
  References,
} from '../../../core/services/drep.service';
import { Observable, of, catchError, map, forkJoin } from 'rxjs';
import { EpochService } from '../../../core/services/epoch.service';
import { Vote } from '../../../core/services/proposal.service';
import { TableSkeletonComponent } from '../../../shared/components/skeleton/table-skeleton.component';
import { DrepDetailModalComponent } from './drep-detail-modal/drep-detail-modal.component';
import { CardSkeletonComponent } from '../../../shared/components/skeleton/card-skeleton.component';
import { formatValue, truncateMiddle } from '../../../core/helper/format.helper';
import { TableModalComponent } from '../../../shared/table-modal/table-modal.component';
import { Pool } from '../../../core/services/pool.service';

Chart.register(...registerables);

interface VoteInfo {
  proposal_id: string;
  proposal_name: string;
  proposal_type: string;
  vote: Vote;
  meta_url: string;
  date_time_ago: number;
}

interface Registration {
  date_time_ago: number;
  resgistrationGivenName: string;
  action: 'updated' | 'registered' | 'deregistered';
  meta_url: string;
}

interface Delegation {
  time_ago: number;
  stake_address: string;
  amount: number;
}

interface DrepCardDataWithReferences extends DrepCardDataByIdResponse {
  ref?: References[];
}

@Component({
  selector: 'app-drep-detail',
  imports: [
    CommonModule,
    RouterModule,
    NbIconModule,
    NbCardModule,
    NbBadgeModule,
    MatDialogModule,
    ChartComponent,
    ChartSkeletonComponent,
    TableComponent,
    TableSkeletonComponent,
    CardSkeletonComponent,
  ],
  templateUrl: './drep-detail.component.html',
  styleUrl: './drep-detail.component.scss',
})
export class DrepDetailComponent implements OnInit, AfterViewInit {
  route = inject(ActivatedRoute);
  private router = inject(Router);
  drepId = this.route.snapshot.params['id'];

  @ViewChild('votingPowerChart') votingPowerChart!: ElementRef;

  public isLoading = true;
  public charts: { [key: string]: Chart } = {};
  private chartInitialized = false;

  voteInfoColumns: TableColumn[] = [
    { key: 'proposal_name', title: 'Title' },
    { key: 'vote', title: 'Vote' },
    { key: 'proposal_type', title: 'Type' },
    { key: 'date_time_ago', title: 'Date' },
    { key: 'meta_url', title: 'URL' },
  ];

  registrationColumns: TableColumn[] = [
    { key: 'date_time_ago', title: 'Date' },
    { key: 'resgistrationGivenName', title: 'Name' },
    { key: 'action', title: 'Action' },
    { key: 'meta_url', title: 'URL' },
  ];

  delegationColumns: TableColumn[] = [
    { key: 'time_ago', title: 'Date' },
    { key: 'stake_address', title: 'Stake Address' },
    { key: 'amount', title: 'Amount' },
  ];

  // Pagination properties
  voteCurrentPage = 1;
  registrationCurrentPage = 1;
  delegationCurrentPage = 1;
  itemsPerPage = 4;
  delegationItemsPerPage = 17;

  delegationAmountSort: 'asc' | 'desc' | 'default' = 'default';

  // Computed properties for paginated data
  get paginatedVoteData(): VoteInfo[] {
    if (!this.voteData?.length) return [];
    const start = (this.voteCurrentPage - 1) * this.itemsPerPage;
    return this.voteData.slice(start, start + this.itemsPerPage);
  }

  get paginatedRegistrationData(): Registration[] {
    const start = (this.registrationCurrentPage - 1) * this.itemsPerPage;
    const end = start + this.itemsPerPage;
    return this.registrationData.slice(start, end);
  }

  get paginatedDelegationData(): Delegation[] {
    let data = [...this.delegationData];
    if (this.delegationAmountSort === 'asc') {
      data.sort((a, b) => a.amount - b.amount);
    } else if (this.delegationAmountSort === 'desc') {
      data.sort((a, b) => b.amount - a.amount);
    } else {
      // mặc định: theo thời gian tăng dần
      data.sort((a, b) => a.time_ago - b.time_ago);
    }
    const start = (this.delegationCurrentPage - 1) * this.delegationItemsPerPage;
    const end = start + this.delegationItemsPerPage;
    return data.slice(start, end);
  }

  voteTotalItems = 0;
  registrationTotalItems = 0;
  delegationTotalItems = 0;
  totalDelegators = 0;
  voteData: VoteInfo[] = [];
  registrationData: Registration[] = [];
  delegationData: Delegation[] = [];
  drepInfo: DrepInfoResponse | undefined;
  cardData: DrepCardDataWithReferences | undefined;
  activeUntil?: Date | null = null;
  voteInfo: DrepVoteInfoResponse[] = [];
  visibleReferenceCount = 0; // Show 3 icons, rest go in the funnel
  showAllReferences = false;

  // Getter cho ngày Registration (dạng Date)
  get registrationDate(): Date | null {
    if (this.cardData?.registrationDate) {
      // Nếu registrationDate là string hoặc số, chuyển sang Date
      return new Date(this.cardData.registrationDate);
    }
    return null;
  }

  // Getter cho ngày Active Until (đã có sẵn là this.activeUntil)
  get activeUntilDate(): Date | null {
    return this.activeUntil ?? null;
  }

  // Getter cho trạng thái active
  get isActive(): boolean {
    return !!this.drepInfo?.active;
  }

  // Property cho phần trăm tiến trình thời gian
  tenureProgressPercent: number = 0;

  private calculateTenureProgress(): void {
    const reg = this.registrationDate;
    const until = this.activeUntilDate;
    if (!reg || !until) {
      this.tenureProgressPercent = 0;
      return;
    }
    const now = new Date();
    if (!this.isActive) {
      this.tenureProgressPercent = 100;
      return;
    }
    if (now <= reg) {
      this.tenureProgressPercent = 0;
      return;
    }
    if (now >= until) {
      this.tenureProgressPercent = 100;
      return;
    }
    const total = until.getTime() - reg.getTime();
    const elapsed = now.getTime() - reg.getTime();
    if (total <= 0) {
      this.tenureProgressPercent = 100;
      return;
    }
    this.tenureProgressPercent = Math.min(100, Math.max(0, (elapsed / total) * 100));
  }

  get visibleReferences() {
    return (
      this.cardData?.ref?.slice(0, this.visibleReferenceCount) || []
    );
  }

  get overflowReferences() {
    return this.cardData?.ref?.slice(this.visibleReferenceCount) || [];
  }

  toggleShowAllReferences() {
    this.showAllReferences = !this.showAllReferences;
  }

  constructor(
    private cdr: ChangeDetectorRef,
    private dialog: MatDialog,
    private drepService: DrepService,
    private epochService: EpochService,
    private toastr: NbToastrService
  ) {}

  ngOnInit(): void {
    this.isLoading = true;

    // Initialize data
    this.isValidId(this.drepId).subscribe((isValid) => {
      if (!isValid) {
        // Handle invalid ID - you might want to redirect or show an error
        console.error('Invalid DREP ID');
        this.router.navigate(['/dreps']);
        return;
      }

      // Load all data first
      this.loadTableData();
      this.loadDrepCardData();
    });
  }

  ngAfterViewInit(): void {
    // Wait for the next tick to ensure the view is initialized
    setTimeout(() => {
      this.initializeChartsAfterDataLoad();
    }, 500);
  }

  private initializeChartsAfterDataLoad(): void {
    // First, set loading to false to render the template
    this.isLoading = false;
    this.cdr.detectChanges();

    // Then initialize charts in the next change detection cycle
    setTimeout(() => {
      try {
        if (!this.chartInitialized && this.votingPowerChart?.nativeElement) {
          this.initializeCharts();
          this.chartInitialized = true;
          this.cdr.detectChanges();
        }
      } catch (error) {
        console.error('Error initializing charts:', error);
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    }, 100);
  }

  private loadDrepCardData(): void {
    this.drepService.getDrepCardDataById(this.drepId).subscribe({
      next: (data) => {
        this.cardData = data;
        this.cardData.ref = data.references ? JSON.parse(data.references) : [];
        this.calculateTenureProgress();
      },
      error: (error) => {
        console.error('Error loading DREP card data:', error);
      },
    });
  }

  private isValidId(id: string): Observable<boolean> {
    if (!id || id.trim() === '') {
      console.error('Invalid DREP ID: Empty ID provided');
      this.router.navigate(['/dreps']);
      return of(false);
    }

    return this.drepService.getDrepInfo(id).pipe(
      map((res) => {
        if (!res ) {
          console.error('No DREP info found for ID:', id);
          this.router.navigate(['/dreps']);
          return false;
        }
        this.drepInfo = res;

        const expiresEpochNo = this.drepInfo.expires_epoch_no;
        if (!expiresEpochNo) {
          this.activeUntil = null;
          this.calculateTenureProgress();
        }
        else {
          this.epochService.getCurrentEpochInfo().subscribe((data) => {
            if (data && data.epoch_no) {
              const currentEpochNo = data.epoch_no;
              const currentEpochStart = data.start_time || 0;
              const epochsUntilExpiration = expiresEpochNo - currentEpochNo;
              const daysUntilExpiration = epochsUntilExpiration * 5;
              const expirationDate = new Date(currentEpochStart * 1000);
              expirationDate.setDate(
                expirationDate.getDate() + daysUntilExpiration
              );
              this.activeUntil = expirationDate;
              this.calculateTenureProgress();
            } else {
              this.activeUntil = null;
              this.calculateTenureProgress();
            }
          });
        }

        return true;
      }),
      catchError((error) => {
        console.error('Error validating DREP ID:', error);
        this.router.navigate(['/dreps']);
        return of(false);
      })
    );
  }

  private loadTableData(): void {
    forkJoin({
      voteInfo: this.drepService.getDrepVoteInfo(this.drepId),
      registrationTable: this.drepService.getDrepRegistrationTable(this.drepId),
      delegationTable: this.drepService.getDrepDelegatorsTable(this.drepId),
    }).subscribe({
      next: (results) => {
        // Process vote info
        let mappedVotes: VoteInfo[] = [];
        if (
          results.voteInfo &&
          'drepVotesData' in results.voteInfo &&
          Array.isArray(results.voteInfo.drepVotesData)
        ) {
          mappedVotes = results.voteInfo.drepVotesData.map((item) => ({
            proposal_id: item.proposal_id || '',
            proposal_name: item.proposal_title || '',
            vote: Object.values(Vote).includes(item.vote as Vote)
              ? item.vote
              : Vote.Abstain,
            meta_url: item.meta_url || '',
            proposal_type: item.proposal_type || '',
            date_time_ago: +item.block_time || 0,
          }));
        } else if (Array.isArray(results.voteInfo)) {
          mappedVotes = results?.voteInfo?.map((item) => ({
            proposal_id: item.proposal_id || '',
            proposal_name: item.proposal_title || '',
            vote: Object.values(Vote).includes(item.vote as Vote)
              ? item.vote
              : Vote.Abstain,
            meta_url: item.meta_url || '',
            proposal_type: item.proposal_type || '',
            date_time_ago: item.block_time || 0,
          })) as VoteInfo[];
        }
        this.voteData = mappedVotes;
        this.voteTotalItems = this.voteData.length;
        this.voteCurrentPage = 1;

        // Process registration data
        this.registrationData = results?.registrationTable?.map((item) => ({
          date_time_ago: item.block_time || 0,
          resgistrationGivenName: item.given_name || '',
          action: item.action || '',
          meta_url: item.meta_url,
        })) as Registration[];
        this.registrationTotalItems = this.registrationData.length;
        this.registrationCurrentPage = 1;

        // Process delegation data
        if (results.delegationTable && results.delegationTable.delegation_data) {
          this.delegationData = results.delegationTable?.delegation_data?.map(
            (item) => ({
              time_ago: item.block_time || 0,
              stake_address: item.stake_address || '',
              amount: item.amount || 0,
            })) as Delegation[];
          this.delegationTotalItems = this.delegationData.length;
          this.totalDelegators = results.delegationTable.total_delegators || 0;
        }

        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: (error) => {
        console.error('Error loading table data:', error);
        this.voteData = [];
        this.registrationData = [];
        this.delegationData = [];
        this.voteTotalItems = 0;
        this.registrationTotalItems = 0;
        this.delegationTotalItems = 0;
        this.totalDelegators = 0;
        this.isLoading = false;
        this.cdr.detectChanges();
      },
    });
  }

  private initializeCharts(): void {
    if (!this.votingPowerChart?.nativeElement) {

      return;
    }
    this.initVotingPowerChart();
  }

  private initVotingPowerChart(): void {
    if (!this.votingPowerChart?.nativeElement) {
      return;
    }

    this.drepService.getDrepDetailsVotingPower(this.drepId).subscribe({
      next: (data: DrepDetailsVotingPowerResponse[]) => {
        if (!this.votingPowerChart?.nativeElement) {
          return;
        }

        const labels = data.map((item) => item.epoch_no);

        const config: ChartConfiguration = {
          type: 'line' as ChartType,
          data: {
            labels,
            datasets: [
              {
                label: 'Voting Power',
                data: data.map((item) => item.amount || 0),
                borderColor: '#0086AD',
                backgroundColor: 'rgba(0, 134, 173, 0.1)',
                fill: true,
                tension: 0.4,
                borderWidth: 2,
                pointRadius: (ctx: { dataIndex: number }) => {
                  const index = ctx.dataIndex;
                  return index === data.length - 1
                    ? 4
                    : labels[index] && labels[index] % 5 === 2
                    ? 4
                    : 0;
                },
                pointBackgroundColor: '#0086AD',
              },
            ],
          },
          options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
              legend: {
                display: false,
              },
            },
            scales: {
              y: {
                beginAtZero: true,
                grid: {
                  color: 'rgba(0, 0, 0, 0.05)',
                },
                ticks: {
                  callback: (value: string | number) => {
                    const numValue = Math.abs(Number(value));
                    return formatValue(numValue).toString();
                  },
                  font: {
                    size: 12,
                  },
                },
              },
              x: {
                grid: {
                  display: false,
                },
                ticks: {
                  autoSkip: false,
                  callback: function (value, index, ticks) {
                    const lastIndex = labels.length - 1;
                    if (
                      index === 0 ||
                      index === lastIndex ||
                      labels[index] && labels[index] % 5 === 2
                    ) {
                      return labels[index];
                    }
                    return '';
                  },
                  font: {
                    size: 12,
                  },
                },
              },
            },
          },
        };

        try {
          if (this.charts['votingPowerChart']) {
            this.charts['votingPowerChart'].destroy();
          }
          this.charts['votingPowerChart'] = new Chart(
            this.votingPowerChart.nativeElement,
            config
          );
          this.cdr.detectChanges();
        } catch (error) {
          console.error('Error initializing voting power chart:', error);
        }
      },
      error: (error) => {
        console.error('Error loading voting power data:', error);
      },
    });
  }

  openModal(chartKey: string, chartTitle: string): void {
    // Get the source chart's configuration
    const sourceChart = this.charts[chartKey];
    const chartType = (
      sourceChart.config as ChartConfiguration<keyof ChartTypeRegistry>
    ).type;

    // Create structured copies of data (safe to clone)
    // But preserve the original structure of options and plugins
    this.dialog.open(ModalComponent, {
      width: '90vw',
      height: '90vh',
      maxWidth: 'none',
      panelClass: 'fullscreen-modal',
      data: {
        title: chartTitle,
        // Pass the complete original chart object to preserve all references
        originalChart: sourceChart,
        chartKey: chartKey,
        type: chartType,
        createDiagonalPattern: createDiagonalPattern,
      },
    });
  }

  onPageChange(
    event: { page: number; itemsPerPage: number },
    type: 'vote' | 'registration' | 'delegation'
  ): void {
    switch (type) {
      case 'vote':
        this.voteCurrentPage = event.page;
        break;
      case 'registration':
        this.registrationCurrentPage = event.page;
        break;
      case 'delegation':
        this.delegationCurrentPage = event.page;
        break;
    }

    // Force change detection
    this.cdr.detectChanges();
  }

  copyToClipboard(value: string | undefined) {
    if (!value || value.trim() === '') {
      this.toastr.danger('No value to copy!', 'Error');
      return;
    }
    navigator.clipboard.writeText(value).then(
      () => {
        this.toastr.success('Copied to clipboard!', 'Success');
      },
      (err) => {
        this.toastr.danger('Failed to copy!', 'Error');
        console.error('Could not copy text: ', err);
      }
    );
  }

  onInfoExpand(): void {
    this.dialog.open(DrepDetailModalComponent, {
      width: '90vw',
      height: '90vh',
      maxWidth: 'none',
      panelClass: 'fullscreen-modal',
      data: {
        title: 'Info',
        cardData: this.cardData,
      },
    });
  }

  onRegistrationExpand() {
    this.dialog.open(TableModalComponent, {
      width: '90vw',
      height: '90vh',
      maxWidth: 'none',
      panelClass: 'fullscreen-modal',
      data: {
        title: 'Registration',
        columns: this.registrationColumns,
        data: this.registrationData
      },
    });
  }

  onVotesInfoExpand() {
    this.dialog.open(TableModalComponent, {
      width: '90vw',
      height: '90vh',
      maxWidth: 'none',
      panelClass: 'fullscreen-modal',
      data: {
        title: 'Votes info',
        columns: this.voteInfoColumns,
        data: this.voteData,
      },
    });
  }

  onDelegationExpand() {
    this.dialog.open(TableModalComponent, {
      width: '90vw',
      height: '90vh',
      maxWidth: 'none',
      panelClass: 'fullscreen-modal',
      data: {
        title: 'Delegation',
        columns: this.delegationColumns,
        data: this.delegationData,
        text: 'Delegators ' + this.delegationTotalItems,
      },
    });
  }

  onAmountSort() {
    if (this.delegationAmountSort === 'default') {
      this.delegationAmountSort = 'asc';
    } else if (this.delegationAmountSort === 'asc') {
      this.delegationAmountSort = 'desc';
    } else {
      this.delegationAmountSort = 'default';
    }
  }

  getReferenceType(ref: any): string {
    if (!ref?.uri || typeof ref.uri !== 'string') return 'default';
    const uri = ref.uri.toLowerCase();
    if (uri.includes('twitter.com') || uri.includes('x.com')) return 'Twitter';
    if (uri.includes('github.com')) return 'Github';
    if (uri.includes('t.me')) return 'Telegram';
    if (uri.includes('discord.gg') || uri.includes('discord.com'))
      return 'Discord';
    if (uri.includes('youtube.com') || uri.includes('youtu.be'))
      return 'Youtube';
    if (uri.includes('linkedin.com')) return 'Linkedin';
    if (uri.includes('linktr.ee')) return 'Linktree';
    return 'default';
  }

  truncateMiddle(value: string | undefined): string {
    if (!value) return '';
    return truncateMiddle(value);
  }

  formatValue(value: number | undefined): string {
    if (!value) return '';
    return formatValue(value);
  }

  formatDate(dateString: string | undefined): string {
    console.log(dateString)
    if (!dateString) return 'N/A';
    const date = new Date(dateString);
    if (isNaN(date.getUTCSeconds())) return 'N/A';
    const day = String(date.getUTCDate()).padStart(2, '0');
    const month = String(date.getUTCMonth() + 1).padStart(2, '0');
    const year = date.getUTCFullYear();
    const hour = date.getUTCHours();
    const min = date.getUTCMinutes();
    return `${day}/${month}/${year} ${hour}:${min}`;
  }
}

// Đảm bảo hàm createDiagonalPattern tồn tại trước khi dùng trong technicalParameters
const createDiagonalPattern = (color: string): CanvasPattern => {
  const shape = document.createElement('canvas');
  shape.width = 10;
  shape.height = 10;
  const c = shape.getContext('2d')!;
  c.strokeStyle = color;
  c.lineWidth = 2;
  c.beginPath();
  c.moveTo(2, 0);
  c.lineTo(10, 8);
  c.stroke();
  c.beginPath();
  c.moveTo(0, 8);
  c.lineTo(2, 10);
  c.stroke();
  return c.createPattern(shape, 'repeat')!;
};
