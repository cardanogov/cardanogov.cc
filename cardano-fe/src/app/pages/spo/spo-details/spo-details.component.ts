import {
  Component,
  OnInit,
  AfterViewInit,
  ViewChild,
  ElementRef,
  ChangeDetectorRef,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterModule } from '@angular/router';
import {
  Chart,
  ChartConfiguration,
  ChartType,
  ChartTypeRegistry,
  registerables,
} from 'chart.js';
import {
  NbIconModule,
  NbCardModule,
  NbBadgeModule,
  NbToastrService,
} from '@nebular/theme';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';

import { ChartComponent } from '../../../shared/components/chart/chart.component';
import { ChartSkeletonComponent } from '../../../shared/components/skeleton/chart-skeleton.component';
import { CardSkeletonComponent } from '../../../shared/components/skeleton/card-skeleton.component';
import {
  TableColumn,
  TableComponent,
} from '../../../shared/components/table/table.component';
import { ModalComponent } from '../../../shared/components/modal/modal.component';
import { PoolInfo, PoolService } from '../../../core/services/pool.service';
import {
  formatValue,
  truncateMiddle,
} from '../../../core/helper/format.helper';
import { TableModalComponent } from '../../../shared/table-modal/table-modal.component';
import { IntroduceModalComponent } from './introduce-modal/introduce-modal.component';

// Register Chart.js components
Chart.register(...registerables);

interface VoteInfo {
  proposal_id: string;
  proposal_name: string;
  proposal_type: string;
  date_time_ago: number;
  number: number;
  url: string;
}

interface Registration {
  date_time_ago: number;
  title: string;
  url: string;
}

interface Delegation {
  time_ago: number;
  id: string;
  amount: number;
}

@Component({
  selector: 'app-spo-details',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    NbIconModule,
    NbCardModule,
    NbBadgeModule,
    MatDialogModule,
    ChartComponent,
    ChartSkeletonComponent,
    CardSkeletonComponent,
    TableComponent,
  ],
  templateUrl: './spo-details.component.html',
  styleUrls: ['./spo-details.component.scss'],
})
export class SpoDetailsComponent implements OnInit, AfterViewInit {
  @ViewChild('votingPowerChart') votingPowerChart!: ElementRef;
  poolId: string | null = null;

  public isLoading = true;
  public charts: { [key: string]: Chart } = {};
  private chartInitialized = false;

  voteInfoColumns: TableColumn[] = [
    { key: 'proposal_name', title: 'Title' },
    { key: 'proposal_type', title: 'Type' },
    { key: 'number', title: 'Vote' },
    { key: 'date_time_ago', title: 'Date' },
    { key: 'url', title: 'URL' },
  ];

  registrationColumns: TableColumn[] = [
    { key: 'date_time_ago', title: 'Date' },
    { key: 'title', title: 'Name' },
    { key: 'url', title: 'Transaction' },
  ];

  delegationColumns: TableColumn[] = [
    { key: 'time_ago', title: 'Date' },
    { key: 'id', title: 'Stake Address' },
    { key: 'amount', title: 'Amount' },
  ];

  // Pagination properties
  voteCurrentPage = 1;
  registrationCurrentPage = 1;
  delegationCurrentPage = 1;
  itemsPerPage = 4;
  delegationItemsPerPage = 17;

  delegationAmountSort: 'asc' | 'desc' | 'default' = 'default';
  private currentTime = Date.now();
  private timeInterval: any;

  // Computed properties for paginated data
  get paginatedVoteData(): VoteInfo[] {
    const start = (this.voteCurrentPage - 1) * this.itemsPerPage;
    const end = start + this.itemsPerPage;
    return this.voteData.slice(start, end);
  }

  get paginatedRegistrationData(): Registration[] {
    const start = (this.registrationCurrentPage - 1) * this.itemsPerPage;
    const end = start + this.itemsPerPage;
    return this.registrationData.slice(start, end);
  }

  get paginatedDelegationData(): Delegation[] {
    return this.delegationData;
  }

  voteTotalItems = 0;
  registrationTotalItems = 0;
  delegationTotalItems = 0;
  votingPower = 0;
  votingPowerChange = 0;
  voteData: VoteInfo[] = [];
  registrationData: Registration[] = [];
  delegationData: Delegation[] = [];
  poolInfo!: PoolInfo;
  isLoadingDelegation = false;

  get lastActivityPercent(): number {
    if (
      !this.poolInfo?.status?.registration ||
      !this.poolInfo?.status?.last_activity
    )
      return 0;
    const reg =
      this.poolInfo.status.registration > 0
        ? this.poolInfo.status.registration * 1000
        : 0;
    const last =
      this.poolInfo.status.last_activity > 0
        ? this.poolInfo.status.last_activity * 1000
        : 0;
    if (this.currentTime === reg) return 100;
    return Math.max(0, Math.min(100, ((last - reg) / (this.currentTime - reg)) * 100));
  }

  constructor(
    private cdr: ChangeDetectorRef,
    private dialog: MatDialog,
    private route: ActivatedRoute,
    private poolService: PoolService,
    private toastr: NbToastrService
  ) {
    this.poolId = this.route.snapshot.paramMap.get('id');
    if (this.poolId) {
      this.poolService.getPoolInfo(this.poolId).subscribe((pool) => {
        this.poolInfo = pool;
        this.voteData = pool.vote_info
          ?.filter((item) => item.proposal_type)
          .map((vote) => ({
            proposal_id: vote.proposal_id || '',
            proposal_name: vote.title,
            proposal_type: vote.proposal_type,
            date_time_ago: vote.block_time,
            number: vote.vote || 0,
            url: vote.meta_url,
          })) as VoteInfo[];
        this.registrationData = pool.registration
          ?.filter((item) => item.block_time && item.block_time > 0)
          .map((reg) => ({
            date_time_ago: reg.block_time,
            title: reg.ticker || '',
            url: reg.meta_url,
          })) as Registration[];

        this.voteTotalItems = this.voteData.length;
        this.registrationTotalItems = this.registrationData.length;
        this.voteCurrentPage = 1;
        this.registrationCurrentPage = 1;
        this.delegationCurrentPage = 1;
        if (pool.voting_power?.length && pool.voting_power.length > 1) {
          this.votingPower = pool.voting_power[0].amount || 0;
          this.votingPowerChange =
            (((pool.voting_power[0].amount || 0) -
              (pool.voting_power[1].amount || 0)) /
              (pool.voting_power[1].amount || 0)) *
            100;
        } else {
          this.votingPower = pool.voting_power?.length
            ? pool.voting_power[0].amount || 0
            : 0;
        }

        this.isLoading = false;
        this.cdr.detectChanges();
        this.initializeCharts(); // Call initializeCharts after loading is complete

        // Load initial delegation data
        this.loadDelegationData();
      });
    }
  }

  ngOnInit(): void {
    // Initialize data
    setTimeout(() => {
      this.isLoading = false;
      this.cdr.detectChanges();
      this.initializeCharts(); // Call initializeCharts after loading is complete
    }, 1000);

    // Update current time every second to keep the progress bar accurate
    this.updateCurrentTime();
  }

  ngAfterViewInit(): void {
    // Initial attempt to initialize charts
    if (!this.isLoading) {
      this.initializeCharts();
    }
  }

  formatValue(value: number): string {
    return formatValue(value);
  }

  private updateCurrentTime(): void {
    this.timeInterval = setInterval(() => {
      this.currentTime = Date.now();
      this.cdr.detectChanges();
    }, 1000);
  }

  ngOnDestroy(): void {
    if (this.timeInterval) {
      clearInterval(this.timeInterval);
    }
  }

  private initializeCharts(): void {
    if (
      !this.chartInitialized &&
      !this.isLoading &&
      this.votingPowerChart?.nativeElement
    ) {
      this.initVotingPowerChart();
      this.chartInitialized = true;
      this.cdr.detectChanges();
    }
  }

  private initVotingPowerChart(): void {
    const config: ChartConfiguration = {
      type: 'line' as ChartType,
      data: {
        labels:
          this.poolInfo?.voting_power
            ?.map((item) => item.epoch_no)
            .sort((a, b) => (a || 0) - (b || 0)) || [],
        datasets: [
          {
            label: 'Voting Power',
            data:
              this.poolInfo?.voting_power
                ?.map((item) => item.amount)
                .sort((a, b) => (a || 0) - (b || 0)) || ([] as any),
            borderColor: '#0086AD',
            backgroundColor: 'rgba(0, 134, 173, 0.1)',
            fill: true,
            tension: 0.4,
            borderWidth: 2,
            pointRadius: 2,
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
              font: {
                size: 12,
              },
              callback: (value) => {
                return formatValue(+value);
              },
            },
          },
          x: {
            grid: {
              display: false,
            },
            ticks: {
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
    } catch (error) {
      console.error('Error initializing Voting Power Chart:', error);
    }
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
    tableType: 'vote' | 'registration' | 'delegation'
  ): void {
    switch (tableType) {
      case 'vote':
        this.voteCurrentPage = event.page;
        break;
      case 'registration':
        this.registrationCurrentPage = event.page;
        break;
      case 'delegation':
        this.delegationCurrentPage = event.page;
        this.loadDelegationData();
        break;
    }
  }

  copyToClipboard(value: string) {
    if (!value) return;
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

  truncateMiddle(value: string): string {
    return truncateMiddle(value);
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
        data: this.registrationData,
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
    // Load all delegation data for the modal
    if (this.poolId) {
      this.poolService.getPoolDelegation(
        this.poolId,
        1,
        1000, // Load a large number to get all data
        this.delegationAmountSort === 'default' ? 'block_time' : 'amount',
        this.delegationAmountSort === 'asc' ? 'asc' : 'desc'
      ).subscribe(
        (response) => {
          const allDelegationData = response.items
            ?.filter((s) => s.block_time && s.block_time > 0)
            .map((del) => ({
              time_ago: del.block_time,
              id: del.stake_address,
              amount: del.amount,
            })) as Delegation[] || [];

          this.dialog.open(TableModalComponent, {
            width: '90vw',
            height: '90vh',
            maxWidth: 'none',
            panelClass: 'fullscreen-modal',
            data: {
              title: 'Delegation',
              columns: this.delegationColumns,
              data: allDelegationData,
              text: 'Delegators ' + (response.total || 0),
            },
          });
        },
        (error) => {
          console.error('Error loading delegation data for modal:', error);
          this.toastr.danger('Failed to load delegation data', 'Error');
        }
      );
    }
  }

  onAmountSort() {
    if (this.delegationAmountSort === 'default') {
      this.delegationAmountSort = 'asc';
    } else if (this.delegationAmountSort === 'asc') {
      this.delegationAmountSort = 'desc';
    } else {
      this.delegationAmountSort = 'default';
    }

    // Reset to first page and reload data
    this.delegationCurrentPage = 1;
    this.loadDelegationData();
  }

  onIntroduceExpand() {
    this.dialog.open(IntroduceModalComponent, {
      width: '90vw',
      height: '90vh',
      maxWidth: 'none',
      panelClass: 'fullscreen-modal',
      data: {
        title: 'Introduce',
        data: this.poolInfo,
      },
    });
  }

  private loadDelegationData(): void {
    if (this.poolId) {
      this.isLoadingDelegation = true;

      // Determine sort parameters based on current sort state
      let sortBy = 'amount';
      let sortOrder = 'desc';

      if (this.delegationAmountSort === 'asc') {
        sortOrder = 'asc';
      } else if (this.delegationAmountSort === 'default') {
        sortBy = 'block_time';
        sortOrder = 'asc';
      }

      this.poolService.getPoolDelegation(
        this.poolId,
        this.delegationCurrentPage,
        this.delegationItemsPerPage,
        sortBy,
        sortOrder
      ).subscribe(
        (response) => {
          this.delegationData = response.items
            ?.filter((s) => s.block_time && s.block_time > 0)
            .map((del) => ({
              time_ago: del.block_time,
              id: del.stake_address,
              amount: del.amount,
            })) as Delegation[] || [];
          this.delegationTotalItems = response.total || 0;
          this.isLoadingDelegation = false;
          this.cdr.detectChanges();
        },
        (error) => {
          console.error('Error loading delegation data:', error);
          this.isLoadingDelegation = false;
          this.cdr.detectChanges();
        }
      );
    }
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
