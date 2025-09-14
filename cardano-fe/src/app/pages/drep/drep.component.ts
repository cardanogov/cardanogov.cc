import {
  AfterViewInit,
  ChangeDetectorRef,
  Component,
  ElementRef,
  OnInit,
  ViewChild,
} from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { CardSkeletonComponent } from '../../shared/components/skeleton/card-skeleton.component';
import { ChartComponent } from '../../shared/components/chart/chart.component';
import {
  Chart,
  ChartConfiguration,
  ChartType,
  ChartTypeRegistry,
  registerables,
} from 'chart.js';
import {
  createDonutChartConfig,
  ChartDataWithMeta,
} from '../../core/helper/chart.helper';
import { ModalComponent } from '../../shared/components/modal/modal.component';
import { MatDialog } from '@angular/material/dialog';
import {
  TableColumn,
  TableComponent,
} from '../../shared/components/table/table.component';
import { NbIconModule, NbToastrService } from '@nebular/theme';
import {
  DrepCardDataResponse,
  DrepDelegatorsResponse,
  DrepListResponse,
  DrepNewRegisterResponse,
  DrepService,
  DrepsVotingPowerResponse,
  DrepVotingPowerHistoryResponse,
} from '../../core/services/drep.service';
import { Router, RouterModule } from '@angular/router';
import {
  formatValue,
  formatBlockTime,
  truncateMiddle,
} from '../../core/helper/format.helper';
import {
  CombineService,
  ParticipateInVotingResponse,
} from '../../core/services/combine.service';
import { debounceTime, distinctUntilChanged, Subject } from 'rxjs';
import {
  trigger,
  state,
  style,
  animate,
  transition,
} from '@angular/animations';
import { TableModalComponent } from '../../shared/table-modal/table-modal.component';
import { TimeAgoPipe } from '../../shared/pipes/time-ago.pipe';

Chart.register(...registerables);

@Component({
  selector: 'app-drep',
  standalone: true,
  imports: [
    CommonModule,
    CardSkeletonComponent,
    ChartComponent,
    TableComponent,
    NbIconModule,
    RouterModule,
    TimeAgoPipe,
  ],
  providers: [DatePipe],
  templateUrl: './drep.component.html',
  styleUrl: './drep.component.scss',

})
export class DrepComponent implements OnInit, AfterViewInit {
  // Chart
  @ViewChild('drepStatistics', { static: true })
  private drepStatistics!: ElementRef<HTMLCanvasElement>;
  private drepStatisticsChart!: Chart;
  @ViewChild('drepMembers', { static: true })
  private drepMembers!: ElementRef<HTMLCanvasElement>;
  private drepMembersChart!: Chart;
  @ViewChild('delegateAddresses', { static: true })
  private delegateAddresses!: ElementRef<HTMLCanvasElement>;
  private delegateAddressesChart!: Chart;
  @ViewChild('drepVotingPower', { static: true })
  private drepVotingPower!: ElementRef<HTMLCanvasElement>;
  private drepVotingPowerChart!: Chart;

  public charts: { [key: string]: Chart } = {};
  private chartInitialized = false;

  // Loading states
  isLoadingCards = true;
  isLoadingTable = true;
  isLoadingCharts = true;
  isLoadingDrepList = true;
  isLoadingCarousel = true;

  // Card
  totalDRepsChange = 0;
  totalDelegatorsChange = 0;
  totalLiveStakeChange = 0;

  // Table
  drepLeaderboardColumns: TableColumn[] = [
    { key: 'givenName', title: 'DRep ID' },
    { key: 'amount', title: 'Voting Power' },
    { key: 'percentage', title: 'Vote Percentage' },
  ];

  drepLeaderboardData: DrepVotingPowerHistoryResponse[] = [];
  drepLeaderboardTotalPercentage: number = 0;
  newDreps: DrepNewRegisterResponse[] = [];

  drepCardData: DrepCardDataResponse = {
    dreps: 0,
    drepsChange: '0',
    totalDelegatedDrep: 0,
    totalDelegatedDrepChange: '0',
    currentTotalActive: 0,
    totalActiveChange: '0',
  };

  drepListData: DrepListResponse = {
    total_dreps: 0,
    drep_info: [],
  };

  // Pagination state
  currentPage = 1;
  itemsPerPage = 12;
  totalPages = 1;
  currentStatus: 'active' | 'inactive' | null = null;
  searchTerm = '';
  displayedPages: number[] = [];
  private searchSubject = new Subject<string>();


  constructor(
    private dialog: MatDialog,
    private cdr: ChangeDetectorRef,
    private drepService: DrepService,
    private router: Router,
    private combineService: CombineService,
    private toastr: NbToastrService
  ) {
    this.searchSubject.pipe(distinctUntilChanged()).subscribe((term) => {
      this.currentPage = 1; // Reset to page 1 when search changes
      this.searchTerm = term;
      this.getDrepList();
    });
  }

  ngOnInit(): void {
    this.loadInitialData();
    this.getDrepCardData();
    this.getDrepList();
    this.getDrepNewRegister();

  }

  ngAfterViewInit(): void {
    this.initializeData();
  }

  getDrepCardData(): void {
    this.drepService.getDrepCardData().subscribe({
      next: (data) => {
        this.drepCardData = data;
        // Format the number for display while keeping the original number value
        this.drepCardData.totalDelegatedDrep = Number(data.totalDelegatedDrep);
        this.isLoadingCards = false;
        this.cdr.detectChanges();
      },
      error: (error) => {
        console.error('Error loading DRep card data:', error);
        this.isLoadingCards = false;
        this.cdr.detectChanges();
      },
    });
  }

  private loadInitialData(): void {
    this.isLoadingCards = true;
    this.isLoadingTable = true;
    this.isLoadingCharts = true;

    // Load table data
    this.getTop10DrepVotingPower();

    // Simulate loading for cards (replace with actual API call)
    setTimeout(() => {
      this.isLoadingCards = false;
      this.cdr.detectChanges();
    }, 1000);

    // Charts will be initialized in initializeData
  }

  getTop10DrepVotingPower(): void {
    this.isLoadingTable = true;
    this.drepService.getTop10DrepVotingPower().subscribe({
      next: (data) => {
        this.drepLeaderboardData = data || [];
        this.drepLeaderboardTotalPercentage =
          data && data.length > 0
            ? Number(
                this.drepLeaderboardData
                  .reduce((acc, curr) => acc + (curr.percentage || 0), 0)
                  .toFixed(2)
              )
            : 0;
        this.isLoadingTable = false;
        this.cdr.detectChanges();
      },
      error: (error) => {
        console.error('Error loading DRep leaderboard data:', error);
        this.drepLeaderboardData = [];
        this.drepLeaderboardTotalPercentage = 0;
        this.isLoadingTable = false;
        this.cdr.detectChanges();
      },
    });
  }

  getDrepNewRegister(): void {
    this.isLoadingCarousel = true;
    this.drepService.getDrepNewRegister().subscribe({
      next: (data) => {
        this.newDreps = data.map((item) => ({
          block_time: item.block_time || 0,
          drep_id: item.drep_id || '',
          action: item.action || '',
        }));

        this.isLoadingCarousel = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Error loading DRep new register data:', err);
        this.isLoadingCarousel = false;
        this.cdr.detectChanges();
      },
    });
  }

  private initializeData(): void {
    this.initializeChartsAfterDataLoad();
  }

  private initializeChartsAfterDataLoad(): void {
    setTimeout(() => {
      if (!this.chartInitialized) {
        this.initializeCharts();
        this.chartInitialized = true;
        this.isLoadingCharts = false;
        this.cdr.detectChanges();
      }
    }, 500);
  }

  private initializeCharts(): void {
    if (this.drepStatistics?.nativeElement) {
      this.initDrepStatisticsChart();
    }
    if (this.drepMembers?.nativeElement) {
      this.initDrepMembersChart();
    }
    if (this.delegateAddresses?.nativeElement) {
      this.initDelegateAddressesChart();
    }
    if (this.drepVotingPower?.nativeElement) {
      this.initDrepVotingPowerChart();
    }
  }

  // Charts
  private initDrepStatisticsChart(): void {
    this.drepService.getDrepVotingPowerHistory().subscribe({
      next: (data) => {
        const chartData: ChartDataWithMeta[] = data.map((item) => ({
          title: item.givenName || '',
          total: item.amount || 0,
          percentage: item.percentage || 0,
        }));
        // Create donut chart configuration
        const config = createDonutChartConfig(
          chartData,
          {
            cutout: '60%',
            rotation: 0,
            circumference: 360,
            spacing: 1,
            borderWidth: 1,
            centerText: {
              title: chartData[0].title,
              subtitle: chartData[0].percentage + '%',
              amount: '₳' + formatValue(chartData[0].total),
            },
          },
          false,
          true,
          true
        );

        // Ensure proper chart dimensions
        if (config.options) {
          config.options.responsive = true;
          config.options.maintainAspectRatio = false;
        }

        try {
          if (this.charts['drepStatistics']) {
            this.charts['drepStatistics'].destroy();
          }
          this.charts['drepStatistics'] = new Chart(
            this.drepStatistics.nativeElement,
            config
          );
        } catch (error) {
          console.error('Error initializing DReps Voting Chart:', error);
        }
      },
      error: (error) => {
        console.error('Error fetching DReps voting data:', error);
      },
    });
  }

  private initDrepMembersChart(): void {
    this.combineService.getParticipateInVoting().subscribe({
      next: (data: ParticipateInVotingResponse) => {
        // Extract epoch numbers for x-axis labels
        const epochs = data.pool?.map((item) => item.epoch_no) || [];
        const drepData = data.drep?.map((item) => item.dreps) || [];

        const config: ChartConfiguration = {
          type: 'line' as ChartType,
          data: {
            labels: epochs,
            datasets: [
              {
                label: 'DRep Members',
                data: drepData as any,
                borderColor: '#0086AD',
                backgroundColor: 'rgba(0, 134, 173, 0.1)',
                fill: true,
                tension: 0.4,
                borderWidth: 2,
                pointRadius: (ctx: { dataIndex: number }) => {
                  const index = ctx.dataIndex;
                  return index && index === drepData.length - 1
                    ? 4
                    : epochs[index] && epochs[index] % 5 === 2
                    ? 4
                    : 0;
                },
                pointHoverRadius: 4,
                pointBackgroundColor: '#0086AD',
                pointBorderColor: '#0086AD',
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
                },
              },
              x: {
                grid: {
                  display: false,
                },
                ticks: {
                  autoSkip: false,
                  callback: function (value, index, ticks) {
                    const lastIndex = epochs.length - 1;
                    // Always show first, last, and every 5th epoch starting from 2
                    if (
                      index === 0 ||
                      index === lastIndex ||
                      (epochs[index] && epochs[index] % 5 === 2)
                    ) {
                      return epochs[index];
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

        if (this.charts['drepMembers']) {
          this.charts['drepMembers'].destroy();
        }

        this.charts['drepMembers'] = new Chart(
          this.drepMembers.nativeElement,
          config
        );
      },
    });
  }

  private initDelegateAddressesChart(): void {
    this.drepService.getDrepDelegators().subscribe({
      next: (data: DrepDelegatorsResponse) => {
        const liveDelegators =
          data.live_delegators?.map((item) => item.live_delegators) || [];
        const labels = data.delegators?.map((item) => item.epoch_no) || [];

        const config: ChartConfiguration = {
          type: 'line' as ChartType,
          data: {
            labels: labels,
            datasets: [
              {
                label: 'DRep Members',
                data: liveDelegators as any,
                borderColor: '#0086AD',
                backgroundColor: 'rgba(0, 134, 173, 0.1)',
                fill: true,
                tension: 0.4,
                borderWidth: 2,
                pointRadius: (ctx: { dataIndex: number }) => {
                  const index = ctx.dataIndex;
                  return index === liveDelegators.length - 1
                    ? 4
                    : index % 5 === 2
                    ? 4
                    : 0;
                },
                pointHoverRadius: 4,
                pointBackgroundColor: '#0086AD',
                pointBorderColor: '#0086AD',
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
                    // Always show first, last, and every 5th epoch starting from 2
                    if (index === 0 || index === lastIndex || index % 5 === 2) {
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

        if (this.charts['delegateAddresses']) {
          this.charts['delegateAddresses'].destroy();
        }

        this.charts['delegateAddresses'] = new Chart(
          this.delegateAddresses.nativeElement,
          config
        );
      },
      error: (error) => {
        console.error('Error fetching wallet address stats data:', error);
      },
    });
  }

  private initDrepVotingPowerChart(): void {
    this.drepService.getDrepsVotingPower().subscribe({
      next: (data: DrepsVotingPowerResponse) => {
        const labels = data.abstain_data?.map((item) => item.epoch_no) || [];
        const drepData =
          data.total_drep_data?.map((item) => item.voting_power) || ([] as any);
        const noConfidentData = data.no_confident_data?.map(
          (item) => item.voting_power
        );
        const abstainData =
          data.abstain_data?.map((item) => item.voting_power) || [];

        const config: ChartConfiguration = {
          type: 'line' as ChartType,
          data: {
            labels: labels,
            datasets: [
              {
                label: 'DRep',
                data: drepData,
                borderColor: '#0086AD',
                backgroundColor: 'rgba(0, 134, 173, 0.1)',
                fill: true,
                tension: 0.4,
                borderWidth: 2,
                pointRadius: (ctx: { dataIndex: number }) => {
                  const index = ctx.dataIndex;
                  return index === drepData.length - 1
                    ? 4
                    : index % 5 === 2
                    ? 4
                    : 0;
                },
                pointHoverRadius: 4,
                pointBackgroundColor: '#0086AD',
                pointBorderColor: '#0086AD',
              },
              {
                label: 'Abstain',
                data: abstainData,
                borderColor: '#FFD600',
                backgroundColor: 'rgba(255, 214, 0, 0.1)',
                fill: true,
                tension: 0.4,
                borderWidth: 2,
                pointRadius: (ctx: { dataIndex: number }) => {
                  const index = ctx.dataIndex;
                  return index === abstainData.length - 1
                    ? 4
                    : index % 5 === 2
                    ? 4
                    : 0;
                },
                pointHoverRadius: 4,
                pointBackgroundColor: '#FFD600',
                pointBorderColor: '#FFD600',
              },
              {
                label: 'No Confident',
                data: noConfidentData,
                borderColor: '#FF3B30',
                backgroundColor: 'rgba(255, 59, 48, 0.1)',
                fill: true,
                tension: 0.4,
                borderWidth: 2,
                pointRadius: (ctx: { dataIndex: number }) => {
                  const index = ctx.dataIndex;
                  return noConfidentData && index === noConfidentData.length - 1
                    ? 4
                    : index % 5 === 2
                    ? 4
                    : 0;
                },
                pointHoverRadius: 4,
                pointBackgroundColor: '#FF3B30',
                pointBorderColor: '#FF3B30',
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
                  callback: function (value: number | string) {
                    return formatValue(Number(value));
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
                    // Always show first, last, and every 5th epoch starting from 2
                    if (index === 0 || index === lastIndex || index % 5 === 2) {
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

        if (this.charts['drepVotingPower']) {
          this.charts['drepVotingPower'].destroy();
        }

        this.charts['drepVotingPower'] = new Chart(
          this.drepVotingPower.nativeElement,
          config
        );
      },
    });
  }

  registration(): void {
    this.router.navigate(['/more']);
  }

  getDrepList(): void {
    this.isLoadingDrepList = true;
    this.drepService
      .getDrepList(this.currentPage, this.searchTerm, this.currentStatus)
      .subscribe({
        next: (data) => {
          this.drepListData = data;
          this.drepListData.drep_info = data.drep_info?.map((item) => ({
            ...item,
            contact:
              typeof item.contact === 'string'
                ? JSON.parse(item.contact)
                : item.contact || [],
          }));
          this.totalPages = Math.ceil(
            (data.total_dreps || 0) / this.itemsPerPage
          );
          this.updateDisplayedPages();
          this.isLoadingDrepList = false;
        },
        error: (error) => {
          console.error('Error fetching DRep list:', error);
          this.isLoadingDrepList = false;
          this.toastr.danger('Failed to load DRep list', 'Error');
        },
      });
  }

  updateDisplayedPages(): void {
    const maxVisiblePages = 3;
    let pages: number[] = [];

    if (this.totalPages <= maxVisiblePages) {
      // If total pages is less than or equal to max visible pages, show all pages
      pages = Array.from({ length: this.totalPages }, (_, i) => i + 1);
    } else {
      // Always include first page
      pages.push(1);

      if (this.currentPage <= 2) {
        // If current page is near the start
        pages.push(2);
        pages.push(3);
        if (this.totalPages > 3) {
          pages.push(-1); // Represents ellipsis
          pages.push(this.totalPages);
        }
      } else if (this.currentPage >= this.totalPages - 1) {
        // If current page is near the end
        pages.push(-1);
        pages.push(this.totalPages - 2);
        pages.push(this.totalPages - 1);
        pages.push(this.totalPages);
      } else {
        // Current page is in the middle
        pages.push(-1);
        pages.push(this.currentPage);
        pages.push(-1);
        pages.push(this.totalPages);
      }
    }

    this.displayedPages = pages;
  }

  goToPage(page: number): void {
    if (page >= 1 && page <= this.totalPages && page !== this.currentPage) {
      this.currentPage = page;
      this.getDrepList();
      this.updateDisplayedPages();
    }
  }

  get startItem(): number {
    return (this.currentPage - 1) * this.itemsPerPage + 1;
  }

  get endItem(): number {
    const end = this.currentPage * this.itemsPerPage;
    return Math.min(end, this.drepListData?.total_dreps || 0);
  }

  nextPage(): void {
    if (this.currentPage < this.totalPages) {
      this.goToPage(this.currentPage + 1);
    }
  }

  previousPage(): void {
    if (this.currentPage > 1) {
      this.goToPage(this.currentPage - 1);
    }
  }

  onStatusChange(status: 'active' | 'inactive') {
    this.currentPage = 1; // Reset to page 1 when filter changes
    this.currentStatus = status === this.currentStatus ? null : status;
    this.getDrepList();
  }

  onSearch(event: string) {
    this.searchSubject.next(event);
  }

  formatValue(value: any): string {
    if (value === null || value === undefined) {
      return '';
    }

    return formatValue(value);
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

  getUri(ref: any): string {
    if (!ref || !ref.uri) return '#';

    // Lấy giá trị uri, xử lý cả chuỗi và đối tượng
    let uriValue: string = '';
    if (typeof ref.uri === 'string') {
      uriValue = ref.uri.toLowerCase();
    } else if (typeof ref.uri === 'object' && ref.uri['@value']) {
      uriValue = ref.uri['@value'].toLowerCase();
    } else {
      return '#'; // Nếu không phải chuỗi hoặc không có '@value', trả về default
    }

    return uriValue;
  }

  getReferenceType(ref: any): string {
    if (!ref || !ref.uri) return 'default';

    // Lấy giá trị uri, xử lý cả chuỗi và đối tượng
    let uriValue: string = '';
    if (typeof ref.uri === 'string') {
      uriValue = ref.uri.toLowerCase();
    } else if (typeof ref.uri === 'object' && ref.uri['@value']) {
      uriValue = ref.uri['@value'].toLowerCase();
    } else {
      return 'default'; // Nếu không phải chuỗi hoặc không có '@value', trả về default
    }

    // Kiểm tra các loại tham chiếu dựa trên uri
    if (uriValue.includes('twitter.com') || uriValue.includes('x.com'))
      return 'Twitter';
    if (uriValue.includes('github.com')) return 'Github';
    if (uriValue.includes('t.me')) return 'Telegram';
    if (uriValue.includes('discord.gg') || uriValue.includes('discord.com'))
      return 'Discord';
    if (uriValue.includes('youtube.com') || uriValue.includes('youtu.be'))
      return 'Youtube';
    if (uriValue.includes('linkedin.com')) return 'Linkedin';
    if (uriValue.includes('linktr.ee')) return 'Linktree';

    // Kiểm tra label để xác định Website
    if (
      ref.label &&
      typeof ref.label === 'object' &&
      ref.label['@value'] === 'Website'
    ) {
      return 'Website';
    }

    return 'default';
  }

  viewProfile(drepId: string): void {
    this.router.navigate(['/dreps', drepId]);
  }

  truncateMiddle(value: string): string {
    return truncateMiddle(value);
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



  formatDate(dateString: string): string {
    if (!dateString) return 'N/A';
    const [date] = dateString.split(' ');
    return date;
  }

  onLeaderboardExpand() {
    this.dialog.open(TableModalComponent, {
      width: '90vw',
      height: '90vh',
      maxWidth: 'none',
      panelClass: 'fullscreen-modal',
      data: {
        title: 'Delegation',
        columns: this.drepLeaderboardColumns,
        data: this.drepLeaderboardData,
        text:
          'Voting Power Percentage: ' +
          this.drepLeaderboardTotalPercentage +
          '%',
      },
    });
  }

  get repeatedDreps(): DrepNewRegisterResponse[] {
    return [...this.newDreps, ...this.newDreps];
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
