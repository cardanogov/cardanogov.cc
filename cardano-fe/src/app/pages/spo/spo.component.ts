import {
  Component,
  OnInit,
  AfterViewInit,
  ViewChild,
  ElementRef,
  ChangeDetectorRef,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MatDialog } from '@angular/material/dialog';
import {
  Chart,
  ChartConfiguration,
  ChartType,
  ChartTypeRegistry,
} from 'chart.js';
import { NbIconModule, NbToastrService } from '@nebular/theme';
import { merge } from 'rxjs';
import { finalize } from 'rxjs/operators';
import { debounceTime, distinctUntilChanged, Subject } from 'rxjs';

import { ChartComponent } from '../../shared/components/chart/chart.component';
import { ModalComponent } from '../../shared/components/modal/modal.component';
import {
  ProposalService,
  ProposalStatsResponse,
} from '../../core/services/proposal.service';
import {
  CombineService,
  MembershipDataResponse,
  ParticipateInVotingResponse,
} from '../../core/services/combine.service';
import { EpochService } from '../../core/services/epoch.service';
import {
  AdaStatisticsResponse,
  Pool,
  PoolResponse,
  PoolService,
} from '../../core/services/pool.service';
import { CurrentEpochResponse, EpochInfoResponse } from '../../core/services/epoch.service';
import { ChartSkeletonComponent } from '../../shared/components/skeleton/chart-skeleton.component';
import { divideAndTruncate, formatValue, MILLION, truncateMiddle } from '../../core/helper/format.helper';
import { CardSkeletonComponent } from '../../shared/components/skeleton/card-skeleton.component';
import {
  ChartDataWithMeta,
  createDonutChartConfig,
} from '../../core/helper/chart.helper';
import {
  DrepDelegatorsResponse,
  DrepService,
  References,
} from '../../core/services/drep.service';

interface PoolWithReferences extends Pool {
  ref?: References[];
}

@Component({
  selector: 'app-spo',
  standalone: true,
  imports: [
    NbIconModule,
    ChartSkeletonComponent,
    ChartComponent,
    CommonModule,
    RouterModule,
    CardSkeletonComponent,
  ],
  templateUrl: './spo.component.html',
  styleUrl: './spo.component.scss',
})
export class SpoComponent implements OnInit, AfterViewInit {
  @ViewChild('spoVotingChart') spoVotingChart!: ElementRef;
  @ViewChild('stakedStatisticsChart') stakedStatisticsChart!: ElementRef;
  @ViewChild('spoStatisticsChart') spoStatisticsChart!: ElementRef;
  @ViewChild('stakeAddressesStatisticsChart')
  stakeAddressesStatisticsChart!: ElementRef;

  public isLoading = true;
  public isLoadingList = true;
  public isGroupSpo = true;
  public charts: { [key: string]: Chart } = {};
  private chartInitialized = false;
  public currentEpoch: CurrentEpochResponse[] = [];
  public currentEpochNo = 0;
  public epochInfo: EpochInfoResponse | null = null;
  activeStake = '0';
  prevActiveStake = '0'; // Add this line
  activeStakeChange = 0; // Add this line
  totalSupply = 0;
  totalSupplyPercent = 0;
  truncateSupply = 0;
  truncateStake = 0;

  governanceAction: ProposalStatsResponse = {
    totalProposals: 0,
    approvedProposals: 0,
    approvalRate: 0,
    percentage_change: 0,
  };

  membershipData: MembershipDataResponse = {
    total_stake_addresses: 0,
    total_pool: 0,
    total_drep: 0,
    total_committee: 0,
  };

  public currentPage = 1;
  public pageSize = 12;
  public totalItems = 0;
  pools: PoolWithReferences[] = [];
  totalPools = 0;
  currentStatus: 'active' | 'inactive' | null = null;
  searchTerm = '';
  private searchSubject = new Subject<string>();
  Math = Math; // Make Math available in template

  constructor(
    private cdr: ChangeDetectorRef,
    private dialog: MatDialog,
    private proposalService: ProposalService,
    private combineService: CombineService,
    private epochService: EpochService,
    private poolService: PoolService,
    private drepService: DrepService,
    private toastr: NbToastrService
  ) {
    this.searchSubject
      .pipe(distinctUntilChanged())
      .subscribe((term) => {
        this.searchTerm = term;
        this.currentPage = 1; // Reset to page 1 when searching
        this.loadPools();
      });
  }

  ngOnInit(): void {
    this.getCardData();
    this.loadPools();
  }

  ngAfterViewInit(): void {
    this.getChartData();
  }

  getCardData(): void {
    this.epochService.getCurrentEpoch().subscribe((data) => {
      this.currentEpoch = data;
      this.currentEpochNo = this.currentEpoch[0]?.epoch_no || 0;
      this.getEpochInfo(this.currentEpochNo);
      // Fetch previous epoch's info
      if (this.currentEpochNo > 0) {
        this.getPrevEpochInfo(this.currentEpochNo - 1);
      }
    });
  }

  getEpochInfo(epochNo: number): void {
    this.epochService.getEpochInfo(epochNo).subscribe((data) => {
      this.epochInfo = data;
      this.activeStake = this.epochInfo?.active_stake
        ? formatValue(+this.epochInfo.active_stake / 1e6)
        : '0';

      const stake = this.epochInfo?.active_stake ? this.epochInfo.active_stake : '0';
      this.truncateStake = divideAndTruncate(stake, MILLION.toString(), 0);
      if (this.truncateSupply > 0) {
        this.totalSupplyPercent = parseFloat(((this.truncateStake / this.truncateSupply) * 100).toFixed(2));
      }
      // Calculate change if previous stake is available
      if (this.prevActiveStake !== '0') {
        const prev = Number(this.prevActiveStake.replace(/[^\d.-]/g, ''));
        const curr = Number(this.activeStake.replace(/[^\d.-]/g, ''));
        if (prev > 0) {
          this.activeStakeChange = ((curr - prev) / prev) * 100;
        } else {
          this.activeStakeChange = 0;
        }
      }
    });

    this.poolService.getTotals(epochNo).subscribe((data) => {
      this.totalSupply = data[0]?.supply
        ? parseFloat(data[0].supply)
        : 0;
      this.truncateSupply = divideAndTruncate(this.totalSupply.toString(), MILLION.toString(), 0);
      if (this.truncateStake > 0) {
        this.totalSupplyPercent = parseFloat(((this.truncateStake / this.truncateSupply) * 100).toFixed(2));
      }
    });
  }

  // Add this method to fetch previous epoch's activeStake
  getPrevEpochInfo(epochNo: number): void {
    this.epochService.getEpochInfo(epochNo).subscribe((data) => {
      const prevEpochInfo = data;
      this.prevActiveStake = prevEpochInfo?.active_stake
        ? formatValue(+prevEpochInfo.active_stake / 1e6)
        : '0';
      // If current activeStake is already loaded, calculate change
      if (this.activeStake !== '0') {
        const prev = Number(this.prevActiveStake.replace(/[^\d.-]/g, ''));
        const curr = Number(this.activeStake.replace(/[^\d.-]/g, ''));
        if (prev > 0) {
          this.activeStakeChange = ((curr - prev) / prev) * 100;
        } else {
          this.activeStakeChange = 0;
        }
      }
    });
  }

  formatValueHtml(value: number) {
    if (value) return formatValue(value);
    return 0;
  }

  public getChartData(): void {

    merge(
      this.proposalService.getProposalStats(),
      this.combineService.getMembershipData()
    )
      .pipe(
        finalize(() => {

          this.isLoading = false;
        })
      )
      .subscribe({
        next: (data) => {
          if ('totalProposals' in data) {
            this.governanceAction = data;
          } else if ('total_stake_addresses' in data) {
            this.membershipData = data;
          }
          this.initializeChartsAfterDataLoad();
        },
        error: (err: any) => {
          console.error('Error fetching data:', err);
          this.isLoading = false;
          this.cdr.detectChanges();
        },
      });
  }

  private initializeChartsAfterDataLoad(): void {
    this.isLoading = false;
    this.cdr.detectChanges();

    setTimeout(() => {
      if (!this.chartInitialized) {
        this.initializeCharts();
        this.chartInitialized = true;
        this.cdr.detectChanges();
      }
    }, 500);
  }

  private initializeCharts(): void {
    if (this.spoVotingChart?.nativeElement) {
      this.initSpoVotingChart();
    }
    if (this.stakedStatisticsChart?.nativeElement) {
      this.initStakedStatisticsChart();
    }
    if (this.spoStatisticsChart?.nativeElement) {
      this.initSpoStatisticsChart();
    }
    if (this.stakeAddressesStatisticsChart?.nativeElement) {
      this.initStakeAddressesStatisticsChart();
    }
  }

  private initSpoVotingChart(): void {
    this.poolService.getSpoVotingPowerHistory().subscribe({
      next: (data) => {
        const chartData: ChartDataWithMeta[] = data.map((item) => ({
          title: item.ticker || 'N/A',
          total: item.active_stake || 0,
          percentage: Number(item.percentage?.toFixed ? item.percentage?.toFixed(2) : Math.round((item.percentage || 0) * 100) / 100),
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
          config.options.aspectRatio = 1;
        }

        if (this.charts['spoVoting']) {
          this.charts['spoVoting'].destroy();
        }
        this.charts['spoVoting'] = new Chart(
          this.spoVotingChart.nativeElement,
          config
        );
      },
      error: (error: any) => {
        console.error('Error fetching SPO voting data:', error);
      },
    });
  }

  groupSpoVotingPower() {
    this.poolService.getSpoVotingPowerHistory().subscribe({
      next: (data) => {
        let result: ChartDataWithMeta[] = [];
        if (this.isGroupSpo) {
          const grouped: {
            [key: string]: { title: string; total: number; percentage: number };
          } = {};

          data.forEach((item) => {
            if (item.group) {
              if (!grouped[item.group]) {
                grouped[item.group] = {
                  title: item.group,
                  total: 0,
                  percentage: 0,
                };
              }
              grouped[item.group].total += item.active_stake || 0;
              grouped[item.group].percentage += item.percentage || 0;
            } else {
              // Giữ nguyên item không có group
              result.push({
                title: item.ticker || 'N/A',
                total: item.active_stake || 0,
                percentage: item.percentage || 0,
              });
            }
          });

          // Thêm các group đã gộp vào kết quả
          Object.values(grouped).forEach((groupItem) => {
            groupItem.percentage = Math.trunc(groupItem.percentage * 100) / 100;
            result.push(groupItem);
          });

          result.sort((a, b) => b.percentage - a.percentage);
        } else {
          result = data.map((item) => ({
            title: item.ticker || 'N/A',
            total: item.active_stake || 0,
            percentage: item.percentage || 0,
          }));
        }

        this.isGroupSpo = !this.isGroupSpo;

        // Create donut chart configuration
        const config = createDonutChartConfig(
          result,
          {
            cutout: '60%',
            rotation: 0,
            circumference: 360,
            spacing: 1,
            borderWidth: 1,
            centerText: {
              title: result[0].title,
              subtitle: result[0].percentage + '%',
              amount: '₳' + formatValue(result[0].total),
            },
          },
          false,
          true,
          true
        );

        // Ensure proper chart dimensions
        if (config.options) {
          config.options.maintainAspectRatio = false;
          config.options.responsive = true;
        } else {
          config.options = { responsive: true } as any;
        }

        try {
          if (this.charts['spoVoting']) {
            this.charts['spoVoting'].destroy();
          }
          this.charts['spoVoting'] = new Chart(
            this.spoVotingChart.nativeElement,
            config
          );
        } catch (error) {
          console.error('Error initializing SPO Voting Chart:', error);
        }
      },
      error: (error: any) => {
        console.error('Error fetching SPO voting data:', error);
      },
    });
  }

  private initStakedStatisticsChart(): void {
    this.poolService.getAdaStatistics().subscribe({
      next: (data: AdaStatisticsResponse) => {
        const labels = data.pool_result?.map((item) => item.epoch_no) || [];

        const config: ChartConfiguration = {
          type: 'line' as ChartType,
          data: {
            labels,
            datasets: [
              {
                label: 'ADA staking',
                data: data.pool_result?.map((item) => +(item.total_active_stake || 0)) || [],
                borderColor: '#0086AD',
                backgroundColor: 'rgba(0, 134, 173, 0.1)',
                fill: true,
                tension: 0.4,
                borderWidth: 2,
                pointRadius: (ctx: { dataIndex: number }) => {
                  const index = ctx.dataIndex;
                  const lastIndex = labels.length - 1;
                  if (
                    index === 0 ||
                    index === lastIndex ||
                    (labels[index] || 0) % 5 === 2
                  ) {
                    return 4; // Larger radius for first, last, and every 5th epoch
                  }
                  return 0; // Smaller radius for other points
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
              x: {
                display: true,
                grid: {
                  display: false,
                },
                ticks: {
                  autoSkip: false,
                  callback: function (value, index, ticks) {
                    const lastIndex = labels.length - 1;
                    // Always show first, last, and every 5th epoch starting from 2
                    if (
                      index === 0 ||
                      index === lastIndex ||
                      (labels[index] || 0) % 5 === 2
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
              y: {
                beginAtZero: true,
                grid: {
                  color: 'rgba(0, 0, 0, 0.05)',
                },
                ticks: {
                  font: {
                    size: 12,
                  },
                  callback: function (value: number | string) {
                    return formatValue(Number(value));
                  },
                },
              },
            },
          },
        };

        if (this.charts['stakedStatistics']) {
          this.charts['stakedStatistics'].destroy();
        }
        this.charts['stakedStatistics'] = new Chart(
          this.stakedStatisticsChart.nativeElement,
          config
        );
      },
    });
  }

  private initSpoStatisticsChart(): void {
    this.combineService.getParticipateInVoting().subscribe({
      next: (data: ParticipateInVotingResponse) => {
        // Extract epoch numbers for x-axis labels
        const labels = data.pool?.map((item) => item.epoch_no) || [];

        // Prepare datasets
        const poolData = data.pool?.map((item) => item.total) || [];

        const config: ChartConfiguration = {
          type: 'line' as ChartType,
          data: {
            labels,
            datasets: [
              {
                label: 'ADA staking',
                data: poolData as any,
                borderColor: '#0086AD',
                backgroundColor: 'rgba(0, 134, 173, 0.1)',
                fill: true,
                tension: 0.4,
                borderWidth: 2,
                pointRadius: (ctx: { dataIndex: number }) => {
                  const index = ctx.dataIndex;
                  const lastIndex = labels.length - 1;
                  if (
                    index === 0 ||
                    index === lastIndex ||
                    (labels[index] || 0) % 5 === 2
                  ) {
                    return 4; // Larger radius for first, last, and every 5th epoch
                  }
                  return 0; // Smaller radius for other points
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
              x: {
                display: true,
                grid: {
                  display: false,
                },
                ticks: {
                  autoSkip: false,
                  callback: function (value, index, ticks) {
                    const lastIndex = labels.length - 1;
                    // Always show first, last, and every 5th epoch starting from 2
                    if (
                      index === 0 ||
                      index === lastIndex ||
                      (labels[index] || 0) % 5 === 2
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
            },
          },
        };

        if (this.charts['spoStatistics']) {
          this.charts['spoStatistics'].destroy();
        }
        this.charts['spoStatistics'] = new Chart(
          this.spoStatisticsChart.nativeElement,
          config
        );
      },
    });
  }

  private initStakeAddressesStatisticsChart(): void {
    this.drepService.getDrepDelegators().subscribe({
      next: (data: DrepDelegatorsResponse) => {
        // Extract data from the response
        const liveDelegators = data.live_delegators?.map(
          (item) => item.live_delegators || 0
        ) || [];
        // Create labels based on the data length
        const labels = data.delegators?.map((item) => item.epoch_no) || [];

        const config: ChartConfiguration = {
          type: 'line' as ChartType,
          data: {
            labels,
            datasets: [
              {
                label: 'Total Delegator Addresses',
                data: liveDelegators,
                borderColor: '#0086AD',
                backgroundColor: 'rgba(0, 134, 173, 0.1)',
                fill: true,
                tension: 0.4,
                borderWidth: 2,
                pointRadius: (ctx: { dataIndex: number }) => {
                  const index = ctx.dataIndex;
                  const lastIndex = labels.length - 1;
                  if (
                    index === 0 ||
                    index === lastIndex ||
                    labels[index] % 5 === 2
                  ) {
                    return 4; // Larger radius for first, last, and every 5th epoch
                  }
                  return 0; // Smaller radius for other points
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
              x: {
                display: true,
                grid: {
                  display: false,
                },
                ticks: {
                  autoSkip: false,
                  callback: function (value, index, ticks) {
                    const lastIndex = labels.length - 1;
                    // Always show first, last, and every 5th epoch starting from 2
                    if (
                      index === 0 ||
                      index === lastIndex ||
                      labels[index] % 5 === 2
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
              y: {
                beginAtZero: true,
                grid: {
                  color: 'rgba(0, 0, 0, 0.05)',
                },
                ticks: {
                  callback: function(value: number | string) {
                    return formatValue(Number(value));
                  },
                  font: {
                    size: 12,
                  },
                }
              },
            },
          },
        };

        if (this.charts['stakeAddressesStatistics']) {
          this.charts['stakeAddressesStatistics'].destroy();
        }
        this.charts['stakeAddressesStatistics'] = new Chart(
          this.stakeAddressesStatisticsChart.nativeElement,
          config
        );
      },
      error: (error) => {
        console.error('Error fetching wallet address stats data:', error);
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
      },
    });
  }

  getStatusBadge(status: string): string {
    switch (status.toLowerCase()) {
      case 'active':
        return 'success';
      case 'inactive':
        return 'danger';
      default:
        return 'basic';
    }
  }

  loadPools() {
    this.isLoadingList = true;
    this.poolService
      .getPools(
        this.currentPage,
        this.searchTerm,
        this.currentStatus || undefined
      )
      .subscribe({
        next: (response: PoolResponse) => {
          this.pools = response.items?.map((item) => ({
            ...item,
            ref: item.references ? JSON.parse(item.references) : [],
          })) || [];
          this.totalPools = response.total || 0;
          this.isLoadingList = false;
        },
        error: (error) => {
          console.error('Error loading pools:', error);
          this.isLoadingList = false;
        },
      });
  }

  copyToClipboard(element: HTMLElement) {
    const text = element.dataset['textCopy'] || '';
    if (!text || text.trim() === '') {
      this.toastr.danger('No value to copy!', 'Error');
      return;
    }
    navigator.clipboard
      .writeText(text)
      .then(() => {
        this.toastr.success('Copied to clipboard!', 'Success');
      })
      .catch((err) => {
        this.toastr.danger('No value to copy!', 'Error');
      });
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

  onPageChange(page: number) {
    this.currentPage = page;
    this.loadPools();
  }

  onStatusChange(status: 'active' | 'inactive') {
    this.currentStatus = status === this.currentStatus ? null : status;
    this.currentPage = 1;
    this.loadPools();
  }

  onSearch(event: string) {
    this.searchSubject.next(event);
  }

  get totalPages(): number {
    return Math.ceil(this.totalPools / this.pageSize);
  }

  get pageRange(): number[] {
    const range = [];
    const maxPages = 3;
    let start = Math.max(1, this.currentPage - Math.floor(maxPages / 2));
    let end = Math.min(this.totalPages, start + maxPages - 1);

    if (end - start + 1 < maxPages) {
      start = Math.max(1, end - maxPages + 1);
    }

    for (let i = start; i <= end; i++) {
      range.push(i);
    }

    return range;
  }

  truncateMiddle(value: string): string {
    return truncateMiddle(value);
  }
}
