import {
  Component,
  OnInit,
  ViewChild,
  ElementRef,
  AfterViewInit,
  ChangeDetectorRef,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import {
  Chart,
  ChartConfiguration,
  ChartType,
  ChartTypeRegistry,
  registerables,
} from 'chart.js';
import { ChartSkeletonComponent } from '../../shared/components/skeleton/chart-skeleton.component';
import { NbCardModule } from '@nebular/theme';
import { ChartComponent } from '../../shared/components/chart/chart.component';
import { MatDialog } from '@angular/material/dialog';
import { ModalComponent } from '../../shared/components/modal/modal.component';
import { TextModalComponent } from '../../shared/components/modal/text-modal.component';
import {
  GovernanceActionsStatisticsByEpochResponse,
  GovernanceActionsStatisticsResponse,
  ProposalService,
  ProposalStatsResponse,
  ProposalInfoResponse,
  GovernanceActionResponse,
} from '../../core/services/proposal.service';
import { CardSkeletonComponent } from '../../shared/components/skeleton/card-skeleton.component';
import {
  CombineService,
  GovernanceParametersResponse,
  ParticipateInVotingResponse,
} from '../../core/services/combine.service';
import { MembershipDataResponse } from '../../core/services/combine.service';
import { combineLatest, finalize, merge } from 'rxjs';
import { IconComponent } from '../../shared/components/icon/icon.component';
import {
  DrepDelegatorsResponse,
  DrepPoolVotingThresholdResponse,
  DrepService,
  TotalDrepResponse,
} from '../../core/services/drep.service';
import {
  StakeService,
  TotalStakeResponse,
} from '../../core/services/stake.service';
import {
  TreasuryDataResponse,
  TreasuryResponse,
  TreasuryService,
  TreasuryVolatilityResponse,
  TreasuryWithdrawalsResponse,
} from '../../core/services/treasury.service';
import {
  formatDynamicDecimals,
  formatToTrillion,
  formatValue,
  roundToTwoDecimals,
} from '../../core/helper/format.helper';
import {
  createDonutChartConfig,
  createLineChartConfig,
  createBarChartConfig,
  ChartDataWithMeta,
} from '../../core/helper/chart.helper';
import {
  AdaStatisticsPercentageResponse,
  AdaStatisticsResponse,
  PoolService,
} from '../../core/services/pool.service';
import { EpochService } from '../../core/services/epoch.service';
import { filterEpochs } from '../../core/helper/filter.helper';
import { NgApexchartsModule } from 'ng-apexcharts';
import {
  ApexAxisChartSeries,
  ApexChart,
  ApexFill,
  ApexStroke,
  ApexXAxis,
  ApexDataLabels,
  ApexYAxis,
  ApexGrid,
  ApexMarkers,
  ApexTitleSubtitle,
  ApexTooltip,
  ApexLegend,
} from 'ng-apexcharts';
import { ApexModalComponent } from '../../shared/components/apex-modal/apex-modal.component';

// Register Chart.js components
Chart.register(...registerables);

export type ChartOptions = {
  series: ApexAxisChartSeries;
  chart: ApexChart;
  dataLabels: ApexDataLabels;
  stroke: ApexStroke;
  xaxis: ApexXAxis;
  yaxis: ApexYAxis;
  fill: ApexFill;
  markers: ApexMarkers;
  title: ApexTitleSubtitle;
  tooltip: ApexTooltip;
  grid: ApexGrid;
  colors: string[];
  legend: ApexLegend;
};

export interface CardInfo {
  title: string;
  value: string;
  change: number;
  detail: string;
}

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    ChartSkeletonComponent,
    NbCardModule,
    ChartComponent,
    CardSkeletonComponent,
    IconComponent,
    NgApexchartsModule,
  ],
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.scss'],
})
export class DashboardComponent implements OnInit, AfterViewInit {
  @ViewChild('activeVotesChart') activeVotesChart!: ElementRef;
  @ViewChild('stakeChart') stakeChart!: ElementRef;
  @ViewChild('treasuryCardChart') treasuryCardChart!: ElementRef;
  @ViewChild('drepsVotingChart') drepsVotingChart!: ElementRef;
  @ViewChild('spoVotingChart') spoVotingChart!: ElementRef;
  @ViewChild('constitutionalCommitteeChart')
  constitutionalCommitteeChart!: ElementRef;
  @ViewChild('participationChart') participationChart!: ElementRef;
  @ViewChild('walletAddressStatsChart') walletAddressStatsChart!: ElementRef;
  @ViewChild('governanceActionsChart') governanceActionsChart!: ElementRef;
  @ViewChild('governanceActionsByEpochChart')
  governanceActionsByEpochChart!: ElementRef;
  @ViewChild('drepVotingThresholdChart') drepVotingThresholdChart!: ElementRef;
  @ViewChild('poolVotingThresholdChart') poolVotingThresholdChart!: ElementRef;
  @ViewChild('adaStatsChart') adaStatsChart!: ElementRef;
  @ViewChild('adaStatsPercentageChart') adaStatsPercentageChart!: ElementRef;
  @ViewChild('treasuryChart') treasuryChart!: ElementRef;
  @ViewChild('networkGroupChart') networkGroupChart!: ElementRef;
  @ViewChild('economicGroupChart') economicGroupChart!: ElementRef;
  @ViewChild('technicalGroupChart') technicalGroupChart!: ElementRef;
  @ViewChild('governanceGroupChart') governanceGroupChart!: ElementRef;

  public isLoadingCard = true;
  public isLoadingChart = true;
  public isGroupSpo = true;
  public isGroupAdaStatsPercentage = true;
  public charts: { [key: string]: Chart } = {};
  private chartInitialized = false;
  public cardInfo: CardInfo[] = [];

  // Constitutional Committee chart toggle
  public chartDisplayNewCCs = true; // Default to new CCs

  // New Constitution Committee data
  private newCcData = {
    labels: [
      'Cardano Atlantic Council',
      'Tingvard',
      'Eastern Cardano Council',
      'KtorZ',
      'Ace Alliance',
      'Cardano Japan Council',
      'Phil_uplc'
    ],
    data: [6, 6, 7, 1, 5, 5, 1],
    colors: [
      '#7FD6C2', // Light turquoise
      '#005F89', // Dark blue
      '#3E7EFF', // Bright blue
      '#CCCCCC', // Light gray
      '#00A4B8', // Turquoise
      '#8C88CD', // Purple
      '#D9D9D9'  // Light gray
    ]
  };

  // Old Constitution Committee data
  private oldCcData = {
    labels: [
      'Cardano Atlantic Council',
      'Cardano Japan',
      'Eastern Cardano Council',
      'Input Output Global',
      'Cardano Foundation',
      'EMURGO',
      'Intersect'
    ],
    data: [1, 1, 1, 1, 1, 1, 1],
    colors: [
      '#7FD6C2', // Light turquoise
      '#005F89', // Dark blue
      '#3E7EFF', // Bright blue
      '#CCCCCC', // Light gray
      '#00A4B8', // Turquoise
      '#8C88CD', // Purple
      '#000000'  // Black
    ]
  };

  governanceAction: ProposalStatsResponse = {
    totalProposals: 0,
    approvedProposals: 0,
    approvalRate: 0,
    percentage_change: 0,
    difference: 0,
  };

  membershipData: MembershipDataResponse = {
    total_stake_addresses: 0,
    total_pool: 0,
    total_drep: 0,
    total_committee: 0,
  };

  totalStakeNumbers: TotalDrepResponse = {
    total_active: 0,
    total_no_confidence: 0,
    total_abstain: 0,
    total_register: 0,
    chart_stats: [],
  };

  totalStake: TotalStakeResponse = {
    totalADA: 0,
    totalSupply: 0,
    chartStats: [],
  };

  treasuryData: TreasuryDataResponse | any = {
    treasury: 0,
    treasuryValue: 0,
    totalWithdrawals: 0,
    totalWithdrawalsValue: 0,
    chartStats: [],
    percentWithdrawals: 0,
  };

  public chartApexOptions: ChartOptions = {
    series: [],
    chart: {
      type: 'area',
    },
    dataLabels: {
      enabled: false,
    },
    stroke: {
      curve: 'smooth',
      width: 2,
    },
    xaxis: {
      categories: [],
    },
    yaxis: {
      labels: {
        style: {
          colors: '#666',
        },
      },
    },
    fill: {
      type: 'gradient',
      gradient: {
        shadeIntensity: 1,
        opacityFrom: 0.8,
        opacityTo: 0.5,
        stops: [0, 90, 100],
      },
    },
    title: {
      text: '',
    },
    tooltip: {} as ApexTooltip,
    grid: {} as ApexGrid,
    markers: {} as ApexMarkers,
    colors: ['#A6F6FF', '#3E7EFF', '#238DB4'],
    legend: {
      position: 'top',
      horizontalAlign: 'right',
      floating: true,
      offsetY: -25,
      offsetX: -5,
    },
  };
  public ischartApexOptionsLoaded = false;

  public chartTreasuryVolatilityOptions: ChartOptions = {
    series: [],
    chart: {
      type: 'area',
    },
    dataLabels: {
      enabled: false,
    },
    stroke: {
      curve: 'smooth',
      width: 2,
    },
    xaxis: {
      categories: [],
    },
    yaxis: {
      labels: {
        style: {
          colors: '#666',
        },
      },
    },
    fill: {
      type: 'gradient',
      gradient: {
        shadeIntensity: 1,
        opacityFrom: 0.8,
        opacityTo: 0.5,
        stops: [0, 90, 100],
      },
    },
    title: {
      text: '',
    },
    tooltip: {} as ApexTooltip,
    grid: {} as ApexGrid,
    markers: {} as ApexMarkers,
    colors: ['rgba(40, 120, 144, 0.8)', '#66C2A5'],
    legend: {
      show: true,
      position: 'top',
      horizontalAlign: 'center',
      floating: false,
      offsetY: -10,
      offsetX: 0,
      fontSize: '12px',
      fontWeight: 400,
      itemMargin: {
        horizontal: 15,
        vertical: 5,
      },
    },
  };
  public isTreasuryVolatilityLoaded = false;

  proposalLiveInfo: ProposalInfoResponse[] = [];
  governanceActionExpired: GovernanceActionResponse = {
    total_proposals: 0,
    approved_proposals: 0,
    percentage_change: 0,
    proposal_info: [],
  };

  constructor(
    private cdr: ChangeDetectorRef,
    private router: Router,
    private dialog: MatDialog,
    private proposalService: ProposalService,
    private combineService: CombineService,
    private drepService: DrepService,
    private stakeService: StakeService,
    private treasuryService: TreasuryService,
    private poolService: PoolService,
    private epochService: EpochService
  ) {}

  ngOnInit(): void {
    this.isLoadingCard = true;
    this.isLoadingChart = true;
    this.getCardsData();
    this.proposalService.getProposalLive().subscribe({
      next: (response) => {
        this.proposalLiveInfo = response;
      },
      error: (err) => {
        console.error('Error fetching live proposals:', err);
      },
    });
    this.proposalService.getProposalExpired().subscribe({
      next: (response) => {
        this.governanceActionExpired = response;
      },
      error: (err) => {
        console.error('Error fetching expired proposals:', err);
      },
    });
  }

  ngAfterViewInit(): void {
    // We'll initialize charts after data is loaded
    // This method is now just a placeholder
  }

  public getCardsData(): void {
    combineLatest([
      this.proposalService.getProposalStats(),
      this.combineService.getMembershipData(),
      this.drepService.getTotalStakeNumbers(),
      this.stakeService.getTotalStake(),
      this.treasuryService.getTreasuryData(),
      this.combineService.getCurrentAdaPrice(),
    ])
      .pipe(
        finalize(() => {
          this.isLoadingCard = false;
        })
      )
      .subscribe({
        next: ([
          governanceAction,
          membershipData,
          totalStakeNumbers,
          totalStake,
          treasuryData,
          adaPrice,
        ]) => {
          this.governanceAction = governanceAction;
          this.membershipData = membershipData;
          this.totalStakeNumbers = totalStakeNumbers;
          this.totalStake = totalStake;
          const { treasury, total_withdrawals, chart_stats } = treasuryData;
          this.treasuryData = {
            treasuryValue: roundToTwoDecimals((+treasury * adaPrice) / 10 ** 9),
            totalWithdrawalsValue: Math.round(
              (+total_withdrawals * adaPrice) / 10 ** 6
            ),
            chartStats: chart_stats?.map((d) =>
              roundToTwoDecimals((d * adaPrice) / 10 ** 9)
            ),
            treasury: formatValue(+treasury),
            totalWithdrawals: formatValue(+total_withdrawals),
            percentWithdrawals: roundToTwoDecimals(
              (+total_withdrawals / (+treasury + +total_withdrawals)) * 100
            ),
          };

          // Now that data is loaded, initialize charts
          this.initializeChartsAfterDataLoad();
        },
        error: (err: any) => {
          console.error('Error fetching data:', err);
          this.isLoadingCard = false;
          this.cdr.detectChanges();
        },
      });
  }

  private initializeChartsAfterDataLoad(): void {
    // First, set loading to false to render the template
    this.isLoadingChart = false;
    this.cdr.detectChanges();

    // Then initialize charts in the next change detection cycle
    setTimeout(() => {
      this.initializeCharts();
    });
  }

  private initializeCharts(): void {
    try {
      // Check if chart elements exist before initializing

      if (!this.activeVotesChart?.nativeElement) {
        console.error('activeVotesChart element not found');
      } else {
        this.initActiveVotesChart();
      }

      if (!this.stakeChart?.nativeElement) {
        console.error('stakeChart element not found');
      } else {
        this.initStakeChart();
      }

      if (!this.treasuryCardChart?.nativeElement) {
        console.error('treasuryCardChart element not found');
      } else {
        this.initTreasuryCardChart();
      }

      if (!this.drepsVotingChart?.nativeElement) {
        console.error('drepsVotingChart element not found');
      } else {
        this.initDrepsVotingChart();
      }

      if (!this.spoVotingChart?.nativeElement) {
        console.error('spoVotingChart element not found');
      } else {
        this.initSpoVotingChart();
      }

      if (!this.constitutionalCommitteeChart?.nativeElement) {
        console.error('constitutionalCommitteeChart element not found');
      } else {
        this.initConstitutionalCommitteeChart();
      }

      if (!this.participationChart?.nativeElement) {
        console.error('participationChart element not found');
      } else {
        this.initParticipationChart();
      }

      if (!this.walletAddressStatsChart?.nativeElement) {
        console.error('walletAddressStatsChart element not found');
      } else {
        this.initWalletAddressStatsChart();
      }

      if (!this.governanceActionsChart?.nativeElement) {
        console.error('governanceActionsChart element not found');
      } else {
        this.initGovernanceActionsChart();
      }

      if (!this.governanceActionsByEpochChart?.nativeElement) {
        console.error('governanceActionsByEpochChart element not found');
      } else {
        this.initGovernanceActionsByEpochChart();
      }

      if (
        !this.drepVotingThresholdChart?.nativeElement &&
        !this.poolVotingThresholdChart?.nativeElement
      ) {
        console.error(
          'drepVotingThresholdChart, poolVotingThresholdChart element not found'
        );
      } else {
        this.initDrepPoolVotingThresholdChart();
      }

      // if (!this.adaStatsChart?.nativeElement) {
      //   console.error('adaStatsChart element not found');
      // } else {
      //   this.initAdaStatsChart();
      // }

      if (!this.adaStatsPercentageChart?.nativeElement) {
        console.error('adaStatsPercentageChart element not found');
      } else {
        this.initAdaStatsPercentageChart();
      }

      if (!this.treasuryChart?.nativeElement) {
        console.error('treasuryChart element not found');
      } else {
        this.initTreasuryChart();
      }

      this.initTreasuryVolatilityChart();

      if (
        !this.networkGroupChart?.nativeElement &&
        !this.economicGroupChart?.nativeElement &&
        !this.technicalGroupChart?.nativeElement &&
        !this.governanceGroupChart?.nativeElement
      ) {
        console.error(
          'networkGroupChart, economicGroupChart, technicalGroupChart, governanceGroupChart element not found'
        );
      } else {
        this.initGovernanceParametersChart();
      }

      this.initApexChart();
    } catch (error) {
      console.error('Error in initializeCharts:', error);
      throw error;
    }
  }

  private initActiveVotesChart(): void {
    const data = this.totalStakeNumbers.chart_stats?.reverse(); // Reverse the data to show the latest first
    const config = createLineChartConfig(data || []);

    if (this.charts['activeVotes']) {
      this.charts['activeVotes'].destroy();
    }

    this.charts['activeVotes'] = new Chart(
      this.activeVotesChart.nativeElement,
      config
    );
  }

  private initStakeChart(): void {
    const data = this.totalStake.chartStats.reverse(); // Reverse the data to show the latest first
    const config = createLineChartConfig(data);

    this.charts['stake'] = new Chart(this.stakeChart.nativeElement, config);
  }

  private initTreasuryCardChart(): void {
    const data = this.treasuryData.chartStats.reverse(); // Reverse the data to show the latest first
    const config = createLineChartConfig(data);

    this.charts['treasuryCard'] = new Chart(
      this.treasuryCardChart.nativeElement,
      config
    );
  }

  private initDrepsVotingChart(): void {
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
              subtitle: formatDynamicDecimals(chartData[0].percentage) + '%',
              amount: '₳' + formatValue(chartData[0].total),
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
          if (this.charts['drepsVoting']) {
            this.charts['drepsVoting'].destroy();
          }
          this.charts['drepsVoting'] = new Chart(
            this.drepsVotingChart.nativeElement,
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

  private initSpoVotingChart(): void {
    this.poolService.getSpoVotingPowerHistory().subscribe({
      next: (data) => {
        const chartData: ChartDataWithMeta[] = data.map((item) => ({
          title: item.ticker || '',
          total: item.active_stake || 0,
          percentage: Number(
            (item.percentage || 0).toFixed(2) ||
              Math.round((item.percentage || 0) * 100) / 100
          ),
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
                title: item.ticker || '',
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
            title: item.ticker || '',
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

  groupAdaStatsPercentage() {

    if (!this.isGroupAdaStatsPercentage) {
      // Khi false, vẽ lại chart mặc định
      this.initAdaStatsPercentageChart();
      return;
    }
    // Khi true, vẽ chart group như hiện tại
    this.poolService.getAdaStatisticsPercentage().subscribe({
      next: (data: AdaStatisticsPercentageResponse) => {
        // Store real ring data for center text
        let stakingPercent = +formatDynamicDecimals(parseFloat(data.circulating_supply || '')
            ? (parseFloat(data.ada_staking || '') || 0) / parseFloat(data.circulating_supply || '') * 100
            : 0)

        let votingPercent = +formatDynamicDecimals(parseFloat(data.ada_staking || '')
            ? (parseFloat(data.ada_register_to_vote || '') || 0) /
                parseFloat(data.ada_staking || '') * 100
            : 0)

        let abstainPercent = +formatDynamicDecimals(parseFloat(data.ada_register_to_vote || '')
            ? (parseFloat(data.ada_abstain || '') || 0) /
                parseFloat(data.ada_register_to_vote || '') * 100
            : 0)

        const ringData = [
          {
            label: 'Circulating supply (ADA)',
            percentage: '100%',
            value: data.circulating_supply,
            display: 'Circulating supply (ADA)',
            percentText: `100% of Total Supply`,
          },
          {
            label: 'Staking (ADA)',
            percentage: stakingPercent,
            value: data.ada_staking,
            display: 'Staking (ADA)',
            percentText: `${stakingPercent}% of Circulating Supply`,
          },
          {
            label: 'Voting Power (ADA)',
            percentage: votingPercent,
            value: data.ada_register_to_vote,
            display: 'Voting Power (ADA)',
            percentText: `${votingPercent}% of Staking`,
          },
          {
            label: 'Abstain (ADA)',
            percentage: abstainPercent,
            value: data.ada_abstain,
            display: 'Abstain (ADA)',
            percentText: `${abstainPercent}% of Voting Power`,
          },
        ];
        let hoveredRingIndex: number | null = null;
        let selectedRingIndex: number | null = null;
        const originalRingDatas = [
          [
            data.circulating_supply_percentage || 0,
            100 - (data.circulating_supply_percentage || 0),
          ],
          [
            stakingPercent || 0,
            100 - (stakingPercent || 0),
          ],
          [
            votingPercent || 0,
            100 - (votingPercent || 0),
          ],
          [
            abstainPercent || 0,
            100 - (abstainPercent || 0),
          ],
        ];
        const config: ChartConfiguration<'doughnut'> = {
          type: 'doughnut',
          data: {
            labels: [
              'Circulating supply (ADA)',
              'Staking (ADA)',
              'Voting Power (ADA)',
              'Abstain (ADA)',
            ],
            datasets: [
              {
                label: 'Circulating supply (ADA)',
                data: [...originalRingDatas[0]],
                backgroundColor: ['#0086AD', '#F5F7FA'],
                borderWidth: 0,
                weight: 4,
              },
              {
                label: 'spacer1',
                data: [1, 0],
                backgroundColor: ['rgba(0,0,0,0)', 'rgba(0,0,0,0)'],
                borderWidth: 0,
                weight: 1.5,
                hoverBackgroundColor: ['rgba(0,0,0,0)', 'rgba(0,0,0,0)'],
                hoverBorderColor: ['rgba(0,0,0,0)', 'rgba(0,0,0,0)'],
              },
              {
                label: 'Staking (ADA)',
                data: [...originalRingDatas[1]],
                backgroundColor: ['#3B82F6', '#F5F7FA'],
                borderWidth: 0,
                weight: 4,
              },
              {
                label: 'spacer2',
                data: [1, 0],
                backgroundColor: ['rgba(0,0,0,0)', 'rgba(0,0,0,0)'],
                borderWidth: 0,
                weight: 1.5,
                hoverBackgroundColor: ['rgba(0,0,0,0)', 'rgba(0,0,0,0)'],
                hoverBorderColor: ['rgba(0,0,0,0)', 'rgba(0,0,0,0)'],
              },
              {
                label: 'Voting Power (ADA)',
                data: [...originalRingDatas[2]],
                backgroundColor: ['#9BEDF6', '#F5F7FA'],
                borderWidth: 0,
                weight: 4,
              },
              {
                label: 'spacer3',
                data: [1, 0],
                backgroundColor: ['rgba(0,0,0,0)', 'rgba(0,0,0,0)'],
                borderWidth: 0,
                weight: 1.5,
                hoverBackgroundColor: ['rgba(0,0,0,0)', 'rgba(0,0,0,0)'],
                hoverBorderColor: ['rgba(0,0,0,0)', 'rgba(0,0,0,0)'],
              },
              {
                label: 'Abstain (ADA)',
                data: [...originalRingDatas[3]],
                backgroundColor: ['#808080', '#F5F7FA'],
                borderWidth: 0,
                weight: 4,
              },
            ],
          },
          options: {
            responsive: true,
            maintainAspectRatio: false,
            layout: {
              padding: 20,
            },
            plugins: {
              legend: {
                onClick: (event, legendItem, legend) => {
                  const chart = legend.chart;
                  const idx =
                    legendItem.index !== undefined ? legendItem.index : null;
                  if (selectedRingIndex === idx) {
                    // Toggle off: show all rings
                    selectedRingIndex = null;
                    [0, 2, 4, 6].forEach((datasetIndex, i) => {
                      chart.data.datasets[datasetIndex].data = [
                        ...originalRingDatas[i],
                      ];
                    });
                  } else {
                    // Show only the selected ring
                    selectedRingIndex = idx;
                    [0, 2, 4, 6].forEach((datasetIndex, i) => {
                      if (i === selectedRingIndex) {
                        chart.data.datasets[datasetIndex].data = [
                          ...originalRingDatas[i],
                        ];
                      } else {
                        chart.data.datasets[datasetIndex].data = [0, 100];
                      }
                    });
                  }
                  hoveredRingIndex = selectedRingIndex;
                  chart.update();
                },
                display: true,
                position: 'top',
                labels: {
                  usePointStyle: true,
                  padding: 20,
                  font: { size: 14 },
                  pointStyle: 'rect',
                  generateLabels: (chart) => {
                    // Only real rings: 0, 2, 4, 6
                    const datasets = chart.data.datasets;
                    const labels = chart.data.labels as string[];
                    return [0, 2, 4, 6].map((datasetIndex, i) => {
                      const dataset = datasets[datasetIndex];
                      // backgroundColor can be string or array, ensure array
                      let color = '#ccc';
                      if (Array.isArray(dataset.backgroundColor)) {
                        color = dataset.backgroundColor[0] as string;
                      } else if (typeof dataset.backgroundColor === 'string') {
                        color = dataset.backgroundColor;
                      }
                      return {
                        text: String(labels?.[i] ?? ''),
                        fillStyle: color,
                        strokeStyle: color,
                        index: i,
                        datasetIndex,
                        pointStyle: 'rect',
                        hidden: false,
                      };
                    });
                  },
                },
              },
              tooltip: {
                enabled: true,
                filter: function (context: any) {
                  // Only show tooltip for real rings (even dataset indexes)
                  return context.datasetIndex % 2 === 0;
                },
                callbacks: {
                  label: function (context: any) {
                    // Only show tooltip for real rings (even dataset indexes)
                    const datasetIndex = context.datasetIndex;
                    if (datasetIndex % 2 === 0) {
                      const ringIdx = datasetIndex / 2;
                      const d = ringData[ringIdx];
                      return `${d.display} ${formatValue(d?.value, 2)}B - ${
                        d.percentText
                      }`;
                    }
                    return '';
                  },
                  title: function () {
                    return '';
                  },
                },
                displayColors: false,
              },
            },
            elements: {
              arc: {
                borderWidth: 0,
              },
            },
            onHover: (event: any, chartElements: any[]) => {
              if (chartElements && chartElements.length > 0) {
                const datasetIndex = chartElements[0].datasetIndex;
                // Only real rings (even indexes)
                if (datasetIndex % 2 === 0) {
                  hoveredRingIndex = datasetIndex / 2;
                } else {
                  hoveredRingIndex = null;
                }
              } else {
                hoveredRingIndex = null;
              }
              // Redraw chart to update center text
              if (this.charts['adaStatsPercentage']) {
                this.charts['adaStatsPercentage'].update();
              }
            },
          },
          plugins: [
            {
              id: 'centerText',
              afterDraw: (chart: any) => {
                const { ctx, chartArea } = chart;
                if (!chartArea) return;
                const isTooltipVisible =
                  chart.tooltip && chart.tooltip.opacity > 0;
                ctx.save();
                ctx.globalAlpha = 1; // Làm mờ khi có tooltip

                ctx.font = 'bold 18px Arial';
                ctx.textAlign = 'center';
                ctx.textBaseline = 'middle';
                ctx.fillStyle = '#666';
                let title = 'Circulating supply';
                let value = formatValue(data?.circulating_supply, 2) + 'B';
                let percentText = ringData[0].percentText;
                if (
                  hoveredRingIndex !== null &&
                  hoveredRingIndex >= 0 &&
                  hoveredRingIndex < ringData.length
                ) {
                  // When hovering: show full label (with ' (ADA)')
                  title = ringData[hoveredRingIndex].display.replace(
                    ' (ADA)',
                    ''
                  );
                  value = formatValue(ringData[hoveredRingIndex]?.value, 2) + 'B';
                  percentText = ringData[hoveredRingIndex].percentText;
                  ctx.font = 'bold 20px Arial';
                  ctx.fillText(
                    title,
                    (chartArea.left + chartArea.right) / 2,
                    (chartArea.top + chartArea.bottom) / 2 - 15
                  );
                  ctx.font = 'bold 18px Arial';
                  ctx.fillText(
                    value,
                    (chartArea.left + chartArea.right) / 2,
                    (chartArea.top + chartArea.bottom) / 2 + 15
                  );
                  ctx.font = 'bold 15px Arial';
                  ctx.fillText(
                    percentText,
                    (chartArea.left + chartArea.right) / 2,
                    (chartArea.top + chartArea.bottom) / 2 + 40
                  );
                } else {
                  // Default: Circulating supply (no percentage, no ' (ADA)')
                  title = ringData[0].display.replace(' (ADA)', '');
                  value = formatValue(ringData[0].value, 2) + 'B';
                  percentText = ringData[0].percentText;
                  ctx.font = 'bold 20px Arial';
                  ctx.fillText(
                    title,
                    (chartArea.left + chartArea.right) / 2,
                    (chartArea.top + chartArea.bottom) / 2 - 15
                  );
                  ctx.font = 'bold 18px Arial';
                  ctx.fillText(
                    value,
                    (chartArea.left + chartArea.right) / 2,
                    (chartArea.top + chartArea.bottom) / 2 + 15
                  );
                  ctx.font = 'bold 15px Arial';
                  ctx.fillText(
                    percentText,
                    (chartArea.left + chartArea.right) / 2,
                    (chartArea.top + chartArea.bottom) / 2 + 40
                  );
                }
                ctx.restore();
              },
            },
          ],
        };
        if (this.charts['adaStatsPercentage']) {
          this.charts['adaStatsPercentage'].destroy();
        }
        this.charts['adaStatsPercentage'] = new Chart(
          this.adaStatsPercentageChart.nativeElement,
          config
        );
      },
    });

     this.isGroupAdaStatsPercentage = !this.isGroupAdaStatsPercentage;
  }

  private initConstitutionalCommitteeChart(): void {
    const chartData = this.getConstitutionalCommitteeChartData();

    const config: ChartConfiguration<'doughnut'> = {
      type: 'doughnut',
      data: {
        labels: chartData.labels,
        datasets: [
          {
            data: chartData.data,
            backgroundColor: chartData.colors,
          },
        ],
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
          legend: {
            position: 'top',
            labels: {
              padding: 20,
              color: '#000000',
            },
          },
          tooltip: {
            enabled: true,
            callbacks: {
              label: (context) => {
                return '';
              },
            },
          },
        },
        cutout: '70%',
      },
    };

    try {
      if (this.charts['constitutionalCommittee']) {
        this.charts['constitutionalCommittee'].destroy();
      }
      this.charts['constitutionalCommittee'] = new Chart(
        this.constitutionalCommitteeChart.nativeElement,
        config
      );
    } catch (error) {
      console.error(
        'Error initializing Constitutional Committee Chart:',
        error
      );
    }
  }

  private initParticipationChart(): void {
    this.combineService.getParticipateInVoting().subscribe({
      next: (data: ParticipateInVotingResponse) => {
        // Extract epoch numbers for x-axis labels
        const epochs = data.pool?.map((item) => item.epoch_no) || [];

        // Prepare datasets
        const poolData = data.pool?.map((item) => item.total) || [];
        const drepData = data.drep?.map((item) => item.dreps) || [];
        const committeeData = Array(epochs.length).fill(
          data.committee?.[0] || 0
        );

        const config: ChartConfiguration = {
          type: 'line',
          data: {
            labels: epochs,
            datasets: [
              {
                label: 'DReps',
                data: drepData,
                borderColor: '#28EEB1',
                backgroundColor: '#28EEB1',
                fill: false,
                tension: 0.4,
                pointRadius: (ctx: { dataIndex: number }) => {
                  const index = ctx.dataIndex;
                  return index === drepData.length - 1
                    ? 4
                    : epochs[index] && epochs[index] % 5 === 2
                    ? 4
                    : 0;
                },
                pointHoverRadius: 4,
                pointBackgroundColor: '#28EEB1',
                pointBorderColor: '#28EEB1',
              },
              {
                label: 'SPO',
                data: poolData,
                borderColor: '#70A5B1',
                backgroundColor: '#70A5B1',
                fill: false,
                tension: 0.4,
                pointRadius: (ctx: { dataIndex: number }) => {
                  const index = ctx.dataIndex;
                  return index === poolData.length - 1
                    ? 4
                    : epochs[index] && epochs[index] % 5 === 2
                    ? 4
                    : 0;
                },
                pointHoverRadius: 4,
                pointBackgroundColor: '#70A5B1',
                pointBorderColor: '#70A5B1',
              },
              {
                label: 'CC',
                data: committeeData,
                borderColor: '#9BEDF6',
                backgroundColor: '#9BEDF6',
                fill: false,
                tension: 0.4,
                pointRadius: (ctx: { dataIndex: number }) => {
                  const index = ctx.dataIndex;
                  return index === committeeData.length - 1
                    ? 4
                    : epochs[index] && epochs[index] % 5 === 2
                    ? 4
                    : 0;
                },
                pointHoverRadius: 4,
                pointBackgroundColor: '#9BEDF6',
                pointBorderColor: '#9BEDF6',
              },
            ],
          },
          options: {
            responsive: true,
            maintainAspectRatio: false,
            layout: {
              padding: {
                top: 20,
                right: 20,
                bottom: 20,
                left: 20,
              },
            },
            plugins: {
              legend: {
                position: 'top',
                align: 'center',
                labels: {
                  usePointStyle: true,
                  pointStyle: 'rect',
                  padding: 20,
                },
              },
            },
            scales: {
              x: {
                display: true,
                title: {
                  display: false,
                  // text: 'Epoch',
                  // font: {
                  //   size: 16,
                  //   weight: 'bold',
                  // },
                },
                grid: {
                  display: true,
                  color: '#eee',
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
                },
              },
              y: {
                type: 'linear',
                display: true,
                position: 'left',
                title: {
                  display: false,
                  // text: 'ID',
                  // font: {
                  //   size: 16,
                  //   weight: 'bold',
                  // },
                },
                min: 0,
                max: 3000,
                ticks: {
                  stepSize: 500,
                },
                grid: {
                  color: '#eee',
                },
              },
            },
          },
        };

        this.charts['participationVoting'] = new Chart(
          this.participationChart.nativeElement,
          config
        );
      },
    });
  }

  private initWalletAddressStatsChart(): void {
    this.drepService.getDrepDelegators().subscribe({
      next: (data: DrepDelegatorsResponse) => {
        // Extract data from the response
        const delegators = data.delegators?.map((item) => item.delegator) || [];
        const liveDelegators =
          data.live_delegators?.map((item) => item.live_delegators) || [];
        const amounts = data.amounts?.map((item) => item.amount) || [];
        // Create labels based on the data length
        const labels = data.delegators?.map((item) => item.epoch_no) || [];

        const config: ChartConfiguration = {
          type: 'line',
          data: {
            labels: labels,
            datasets: [
              {
                label: 'Total Stake Addresses',
                data: delegators,
                borderColor: '#28EEB1',
                backgroundColor: '#28EEB1',
                fill: false,
                tension: 0.4,
                pointRadius: (ctx: { dataIndex: number }) => {
                  const index = ctx.dataIndex;
                  return index === delegators.length - 1
                    ? 4
                    : index % 5 === 2
                    ? 4
                    : 0;
                },
                pointHoverRadius: 4,
                pointBackgroundColor: '#28EEB1',
                pointBorderColor: '#28EEB1',
              },
              {
                label: 'Total Drep Delegator Addresses',
                data: liveDelegators,
                borderColor: '#70A5B1',
                backgroundColor: '#70A5B1',
                fill: false,
                tension: 0.4,
                pointRadius: (ctx: { dataIndex: number }) => {
                  const index = ctx.dataIndex;
                  return index === liveDelegators.length - 1
                    ? 4
                    : index % 5 === 2
                    ? 4
                    : 0;
                },
                pointHoverRadius: 4,
                pointBackgroundColor: '#70A5B1',
                pointBorderColor: '#70A5B1',
              },
              {
                label: 'Total Wallet Addresses',
                data: amounts,
                borderColor: '#9BEDF6',
                backgroundColor: '#9BEDF6',
                fill: false,
                tension: 0.4,
                pointRadius: (ctx: { dataIndex: number }) => {
                  const index = ctx.dataIndex;
                  return index === amounts.length - 1
                    ? 4
                    : index % 5 === 2
                    ? 4
                    : 0;
                },
                pointHoverRadius: 4,
                pointBackgroundColor: '#9BEDF6',
                pointBorderColor: '#9BEDF6',
              },
            ],
          },
          options: {
            responsive: true,
            maintainAspectRatio: false,
            layout: {
              padding: {
                top: 20,
                right: 20,
                bottom: 20,
                left: 20,
              },
            },
            plugins: {
              legend: {
                position: 'top',
                align: 'center',
                labels: {
                  usePointStyle: true,
                  pointStyle: 'rect',
                  padding: 20,
                },
              },
            },
            scales: {
              x: {
                title: {
                  display: true,
                  text: 'Epoch',
                  font: {
                    size: 16,
                    weight: 'bold',
                  },
                },
                display: true,
                grid: {
                  display: true,
                  color: '#eee',
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
                },
              },
              y: {
                type: 'linear',
                display: true,
                position: 'left',
                min: 0,
                ticks: {
                  stepSize: 1000,
                  callback: function (value: number | string) {
                    return formatValue(Number(value));
                  },
                },
                grid: {
                  color: '#eee',
                },
              },
            },
          },
        };

        this.charts['walletStats'] = new Chart(
          this.walletAddressStatsChart.nativeElement,
          config
        );
      },
      error: (error) => {
        console.error('Error fetching wallet address stats data:', error);
      },
    });
  }

  private initGovernanceActionsChart(): void {
    this.proposalService.getGovernanceActionsStatistics().subscribe({
      next: (data: GovernanceActionsStatisticsResponse) => {
        // Map API keys to chart labels and order
        const labels = [
          'Information',
          'Constitution update',
          'Protocol parameter change',
          'Treasury withdrawal',
          'Hard Fork',
          'No Confidence',
          'New Committee',
        ];
        const dataMap: { [key: string]: number } = {
          Information: data.statistics?.['InfoAction'] ?? 0,
          'Constitution update': data.statistics?.['NewConstitution'] ?? 0,
          'Protocol parameter change':
            data.statistics?.['ParameterChange'] ?? 0,
          'Treasury withdrawal': data.statistics?.['TreasuryWithdrawals'] ?? 0,
          'Hard Fork': data.statistics?.['HardForkInitiation'] ?? 0,
          'No Confidence': data.statistics?.['NoConfidence'] ?? 0,
          'New Committee': data.statistics?.['NewCommittee'] ?? 0,
        };
        const values = labels.map((label) => dataMap[label]);

        const backgrounds = [
          '#3FE0C5', // Information
          '#B6F3FB', // Constitution update
          '#2B8CA9', // Protocol parameter change
          '#4B7BFF', // Treasury withdrawal
          '#6B6BFF', // Hard Fork
          '#7DB6C2', // No Confidence
          '#E6E6E6', // New Committee
        ];

        const paired = labels.map((label, index) => [
          label,
          values[index],
          backgrounds[index],
        ]);

        // Sắp xếp theo giá trị giảm dần
        paired.sort((a: any, b: any) => b[1] - a[1]);

        // Tách lại thành hai mảng
        const sortedLabels = paired.map((pair) => pair[0]);
        const sortedValues = paired.map((pair) => +pair[1]);
        const sortedBackgrounds = paired.map((pair) => pair[2] + '');

        const config: ChartConfiguration = {
          type: 'bar' as ChartType,
          data: {
            labels: sortedLabels,
            datasets: [
              {
                label: 'Actions',
                data: sortedValues,
                backgroundColor: sortedBackgrounds,
                barThickness: 8,
                borderRadius: [
                  {
                    topLeft: 50,
                    topRight: 0,
                    bottomLeft: 50,
                    bottomRight: 0,
                  },
                ],
                borderSkipped: false,
              },
            ],
          },
          options: {
            indexAxis: 'y',
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
              legend: {
                display: false,
              },
              tooltip: {
                enabled: true,
                callbacks: {
                  label: function (context: { parsed: { x: number } }) {
                    return `${context.parsed.x}`;
                  },
                },
                backgroundColor: 'rgba(0, 0, 0, 0.8)',
                titleColor: '#fff',
                bodyColor: '#fff',
                padding: 10,
                displayColors: false,
              } as any,
            },
            scales: {
              x: {
                beginAtZero: true,
                grid: {
                  display: false,
                },
                ticks: {
                  stepSize: 1,
                  color: '#666',
                  font: {
                    size: 14,
                  },
                },
                offset: true,
              },
              y: {
                grid: {
                  display: false,
                },
                ticks: {
                  color: '#888',
                  font: {
                    size: 16,
                    weight: 'bold',
                  },
                },
              },
            },
          },
        };
        if (this.charts['governanceActions']) {
          this.charts['governanceActions'].destroy();
        }
        this.charts['governanceActions'] = new Chart(
          this.governanceActionsChart.nativeElement,
          config
        );
      },
    });
  }

  private initGovernanceActionsByEpochChart(): void {
    this.proposalService.getGovernanceActionsStatisticsByEpoch().subscribe({
      next: (keyData: GovernanceActionsStatisticsByEpochResponse) => {
        let data = keyData.statistics_by_epoch || {};
        // Extract epochs and sort them numerically
        const epochs = Object.keys(data)
          .map(Number)
          .sort((a, b) => a - b);
        const labels = epochs.map((e) => e.toString());
        // Action types and their colors (order and color as in the image)
        const actionTypes = [
          {
            key: 'TreasuryWithdrawals',
            label: ['Treasury', 'withdrawal'],
            color: '#4B7BFF',
          },
          {
            key: 'ParameterChange',
            label: ['Protocol', 'parameter', 'change'],
            color: '#2B8CA9',
          },
          {
            key: 'NewConstitution',
            label: ['Constitution', 'update'],
            color: '#B6F3FB',
          },
          { key: 'InfoAction', label: 'Information', color: '#3FE0C5' },
          {
            key: 'NewCommittee',
            label: ['New', 'Committee'],
            color: '#E6E6E6',
          },
          {
            key: 'HardForkInitiation',
            label: ['Hard', 'Fork'],
            color: '#6B6BFF',
          },
          {
            key: 'NoConfidence',
            label: ['No', 'Confidence'],
            color: '#7DB6C2',
          },
        ];
        // Build datasets for each action type
        const datasets = actionTypes.map((type) => ({
          type: 'bar' as ChartType,
          label: type.label,
          data: epochs.map((epoch) => data[epoch]?.[type.key] ?? 0),
          backgroundColor: type.color,
          stack: 'actions',
          order: 2,
          barPercentage: 0.8,
          categoryPercentage: 0.6,
        }));

        // Initialize cumulative sum
        let cumulativeSum = 0;

        // Object to store cumulative sums for each epoch
        const result: any = {};

        // Sort epochs numerically
        const totalGovernanceActionsByEpoch = Object.keys(data).sort(
          (a, b) => Number(a) - Number(b)
        );

        // Calculate cumulative sum for each epoch
        totalGovernanceActionsByEpoch.forEach((epoch) => {
          const actions = data?.[epoch] || {};

          // Sum all values in the current epoch
          const epochSum = Object.values(actions).reduce(
            (sum, value) => sum + value,
            0
          );

          // Add to cumulative sum
          cumulativeSum += epochSum;

          // Store the cumulative sum for this epoch
          result[epoch] = cumulativeSum;
        });

        datasets.push({
          type: 'line',
          label: 'Total',
          data: totalGovernanceActionsByEpoch.map((epoch) => result[epoch]),
          borderColor: '#222',
          fill: false,
          tension: 0.4,
          order: 1,
          yAxisID: 'y1',
          pointRadius: 4,
          pointBackgroundColor: '#222',
        } as any);
        const config: ChartConfiguration = {
          type: 'bar' as ChartType,
          data: {
            labels,
            datasets: datasets as any,
          },
          options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
              legend: {
                position: 'right',
                labels: {
                  usePointStyle: true,
                  padding: 20,
                  pointStyle: 'rect',
                  font: { size: 10 },
                  boxWidth: 20,
                },
              },
              tooltip: {
                mode: 'index',
                intersect: false,
              },
            },
            scales: {
              x: {
                stacked: true,
                grid: { color: '#E0E0E0' },
                ticks: { color: '#666', font: { size: 14 } },
              },
              y: {
                stacked: true,
                beginAtZero: true,
                grid: { color: '#E0E0E0' },
                ticks: { color: '#666', font: { size: 14 }, stepSize: 1 },
                min: 0,
                max: (Math.floor(cumulativeSum / 5) / 2) * 5,
                title: { display: false },
              },
              y1: {
                position: 'right',
                beginAtZero: true,
                grid: { drawOnChartArea: false },
                ticks: { color: '#222', font: { size: 14 }, stepSize: 5 },
                display: true,
              },
            },
          },
        };
        if (this.charts['governanceByEpoch']) {
          this.charts['governanceByEpoch'].destroy();
        }
        this.charts['governanceByEpoch'] = new Chart(
          this.governanceActionsByEpochChart.nativeElement,
          config
        );
      },
    });
  }

  private initDrepPoolVotingThresholdChart(): void {
    this.drepService.getDrepVotingThreshold().subscribe({
      next: (data: DrepPoolVotingThresholdResponse) => {
        const drepLabels = [
          'Motion No Confidence',
          'Committee Normal',
          'Committee No Confidence',
          'HF Initiation',
          'Update Constitution',
          'Network Param Voting',
          'Economic Param Voting',
          'Technical Param Voting',
          'Gov Param Voting',
          'Treasury Withdrawal',
        ];
        const drepThreshold = [
          data.motion_no_confidence,
          data.committee_normal,
          data.committee_no_confidence,
          data.hard_fork_initiation,
          data.update_to_constitution,
          data.network_param_voting,
          data.economic_param_voting,
          data.technical_param_voting,
          data.governance_param_voting,
          data.treasury_withdrawal,
        ];

        const placeholderValues = drepThreshold.map(
          (value) => 100 - (value || 0)
        );

        const options = {
          indexAxis: 'y',
          responsive: true,
          maintainAspectRatio: false,
          plugins: {
            legend: { display: false },
            tooltip: {
              enabled: true,
              filter: function (tooltipItem: any) {
                return tooltipItem.datasetIndex === 0; // Only show tooltip for data bars (index 1)
              },
              callbacks: {
                label: function (context: { parsed: { x: number } }) {
                  return `${context.parsed.x}%`;
                },
              },
              backgroundColor: 'rgba(0, 0, 0, 0.8)',
              titleColor: '#fff',
              bodyColor: '#fff',
              padding: 10,
              displayColors: false,
            } as any,
          },
          scales: {
            x: {
              stacked: true,
              grid: {
                display: false,
                color: '#bbb',
              },
              ticks: {
                color: '#666',
                font: { size: 14 },
                stepSize: 10,
                callback: function (value: any) {
                  return value;
                },
              },
              offset: true,
            },
            y: {
              stacked: true,
              grid: { display: false },
              ticks: {
                color: '#888',
                font: { size: 16, weight: 'bold' },
                padding: 10,
              },
            },
          },
          layout: {
            padding: {
              left: 0,
              right: 20,
              top: 10,
              bottom: 10,
            },
          },
        };

        const borderRadius = [
          {
            topLeft: 50,
            topRight: 0,
            bottomLeft: 50,
            bottomRight: 0,
          },
        ];

        const drepConfig: ChartConfiguration = {
          type: 'bar' as ChartType,
          data: {
            labels: drepLabels,
            datasets: [
              {
                data: drepThreshold.map((value) => value || 0),
                backgroundColor: '#2DEBCF',
                barThickness: 8,
                categoryPercentage: 1.0,
                barPercentage: 1.0,
                borderRadius,
                borderSkipped: false,
              },
              {
                data: placeholderValues,
                backgroundColor: '#D3D3D3', // Gray color for placeholder
                barThickness: 8,
                categoryPercentage: 1.0,
                barPercentage: 1.0,
              },
            ],
          },
          options: options as any,
        };
        if (this.charts['drepVotingThreshold']) {
          this.charts['drepVotingThreshold'].destroy();
        }
        this.charts['drepVotingThreshold'] = new Chart(
          this.drepVotingThresholdChart.nativeElement,
          drepConfig
        );

        const poolLabels = [
          'Motion No Confidence',
          'Committee Normal',
          'Committee No Confidence',
          'HF Initiation',
        ];
        const poolThreshold = [
          data.pool_motion_no_confidence,
          data.pool_committee_normal,
          data.pool_committee_no_confidence,
          data.pool_hard_fork_initiation,
        ];

        const poolPlaceholderValues =
          poolThreshold?.map((value) => (value ? 100 - value : 0)) || [];

        const poolConfig: ChartConfiguration = {
          type: 'bar' as ChartType,
          data: {
            labels: poolLabels,
            datasets: [
              {
                data: poolThreshold.map((value) => value || 0),
                backgroundColor: '#2DEBCF',
                borderWidth: 0,
                barThickness: 8,
                categoryPercentage: 1.0,
                barPercentage: 1.0,
                borderRadius,
                borderSkipped: false,
              },
              {
                data: poolPlaceholderValues,
                backgroundColor: '#D3D3D3', // Gray color for placeholder
                borderWidth: 0,
                barThickness: 8,
                categoryPercentage: 1.0,
                barPercentage: 1.0,
              },
            ],
          },
          options: options as any,
        };
        if (this.charts['poolVotingThreshold']) {
          this.charts['poolVotingThreshold'].destroy();
        }
        this.charts['poolVotingThreshold'] = new Chart(
          this.poolVotingThresholdChart.nativeElement,
          poolConfig
        );
      },
    });
  }

  // private initAdaStatsChart(): void {
  //   this.poolService.getAdaStatistics().subscribe({
  //     next: (data: AdaStatisticsResponse) => {
  //       const labels = data.poolResult.map((item) => item.epoch_no);
  //       const config: ChartConfiguration = {
  //         type: 'line' as ChartType,
  //         data: {
  //           labels,
  //           datasets: [
  //             {
  //               label: 'ADA register to vote',
  //               data: data.drepResult.map((item) => +item.amount),
  //               backgroundColor: 'rgba(166, 246, 255, 0.7)', // light blue
  //               borderColor: 'rgba(166, 246, 255, 1)',
  //               fill: true,
  //               pointRadius: 0,
  //               borderWidth: 0,
  //             },
  //             {
  //               label: 'ADA staking',
  //               data: data.poolResult.map((item) => +item.total_active_stake),
  //               backgroundColor: 'rgba(62, 126, 255, 0.7)', // blue
  //               borderColor: 'rgba(62, 126, 255, 1)',
  //               fill: true,
  //               pointRadius: 0,
  //               borderWidth: 0,
  //             },
  //             {
  //               label: 'Circulating supply',
  //               data: data.supplyResult.map((item) => +item.supply),
  //               backgroundColor: 'rgba(35, 141, 180, 0.7)', // teal
  //               borderColor: 'rgba(35, 141, 180, 1)',
  //               fill: true,
  //               pointRadius: 0,
  //               borderWidth: 0,
  //             },
  //           ],
  //         },
  //         options: {
  //           responsive: true,
  //           maintainAspectRatio: false,
  //           plugins: {
  //             legend: {
  //               display: true,
  //               position: 'top',
  //               labels: {
  //                 usePointStyle: true,
  //                 padding: 20,
  //                 font: { size: 14 },
  //               },
  //             },
  //             tooltip: {
  //               enabled: true,
  //               mode: 'index',
  //               intersect: false,
  //             },
  //           },
  //           scales: {
  //             x: {
  //               stacked: true,
  //               title: { display: false },
  //               grid: { display: false },
  //               ticks: {
  //                 autoSkip: false,
  //                 callback: function (value, index, ticks) {
  //                   const lastIndex = labels.length - 1;
  //                   // Always show first, last, and every 5th epoch starting from 2
  //                   if (
  //                     index === 0 ||
  //                     index === lastIndex ||
  //                     labels[index] % 5 === 2
  //                   ) {
  //                     return labels[index];
  //                   }
  //                   return '';
  //                 },
  //               },
  //             },
  //             y: {
  //               grid: { color: '#E0E0E0' },
  //               ticks: {
  //                 callback: function (
  //                   tickValue: string | number,
  //                   index: number,
  //                   ticks: any[]
  //                 ) {
  //                   return formatValue(Number(tickValue), 0);
  //                 },
  //                 font: { size: 14 },
  //               },
  //             },
  //           },
  //           elements: {
  //             line: { borderWidth: 0 },
  //           },
  //         },
  //       };

  //       if (this.charts['adaStats']) {
  //         this.charts['adaStats'].destroy();
  //       }
  //       this.charts['adaStats'] = new Chart(
  //         this.adaStatsChart.nativeElement,
  //         config
  //       );
  //     },
  //   });
  // }

  private initAdaStatsPercentageChart(): void {
    this.poolService.getAdaStatisticsPercentage().subscribe({
      next: (data: AdaStatisticsPercentageResponse) => {
        const ringData = [
          {
            label: 'Circulating supply (ADA)',
            percentage: data.circulating_supply_percentage,
            value: data.circulating_supply,
            display: 'Circulating supply (ADA)',
          },
          {
            label: 'Staking (ADA)',
            percentage: data.ada_staking_percentage,
            value: data.ada_staking,
            display: 'Staking (ADA)',
          },
          {
            label: 'Voting Power (ADA)',
            percentage: data.ada_register_to_vote_percentage,
            value: data.ada_register_to_vote,
            display: 'Voting Power (ADA)',
          },
          {
            label: 'Abstain (ADA)',
            percentage: data.ada_abstain_percentage,
            value: data.ada_abstain,
            display: 'Abstain (ADA)',
          },
        ];
        let hoveredRingIndex: number | null = null;
        let selectedRingIndex: number | null = null;
        const originalRingDatas = [
          [
            data.circulating_supply_percentage || 0,
            100 - (data.circulating_supply_percentage || 0),
          ],
          [
            data.ada_staking_percentage || 0,
            100 - (data.ada_staking_percentage || 0),
          ],
          [
            data.ada_register_to_vote_percentage || 0,
            100 - (data.ada_register_to_vote_percentage || 0),
          ],
          [
            data.ada_abstain_percentage || 0,
            100 - (data.ada_abstain_percentage || 0),
          ],
        ];
        const config: ChartConfiguration<'doughnut'> = {
          type: 'doughnut',
          data: {
            labels: [
              'Circulating supply (ADA)',
              'Staking (ADA)',
              'Voting Power (ADA)',
              'Abstain (ADA)',
            ],
            datasets: [
              {
                label: 'Circulating supply (ADA)',
                data: [...originalRingDatas[0]],
                backgroundColor: ['#0086AD', '#F5F7FA'],
                borderWidth: 0,
                weight: 4,
              },
              {
                label: 'spacer1',
                data: [1, 0],
                backgroundColor: ['rgba(0,0,0,0)', 'rgba(0,0,0,0)'],
                borderWidth: 0,
                weight: 1.5,
                hoverBackgroundColor: ['rgba(0,0,0,0)', 'rgba(0,0,0,0)'],
                hoverBorderColor: ['rgba(0,0,0,0)', 'rgba(0,0,0,0)'],
              },
              {
                label: 'Staking (ADA)',
                data: [...originalRingDatas[1]],
                backgroundColor: ['#3B82F6', '#F5F7FA'],
                borderWidth: 0,
                weight: 4,
              },
              {
                label: 'spacer2',
                data: [1, 0],
                backgroundColor: ['rgba(0,0,0,0)', 'rgba(0,0,0,0)'],
                borderWidth: 0,
                weight: 1.5,
                hoverBackgroundColor: ['rgba(0,0,0,0)', 'rgba(0,0,0,0)'],
                hoverBorderColor: ['rgba(0,0,0,0)', 'rgba(0,0,0,0)'],
              },
              {
                label: 'Voting Power (ADA)',
                data: [...originalRingDatas[2]],
                backgroundColor: ['#9BEDF6', '#F5F7FA'],
                borderWidth: 0,
                weight: 4,
              },
              {
                label: 'spacer3',
                data: [1, 0],
                backgroundColor: ['rgba(0,0,0,0)', 'rgba(0,0,0,0)'],
                borderWidth: 0,
                weight: 1.5,
                hoverBackgroundColor: ['rgba(0,0,0,0)', 'rgba(0,0,0,0)'],
                hoverBorderColor: ['rgba(0,0,0,0)', 'rgba(0,0,0,0)'],
              },
              {
                label: 'Abstain (ADA)',
                data: [...originalRingDatas[3]],
                backgroundColor: ['#808080', '#F5F7FA'],
                borderWidth: 0,
                weight: 4,
              },
            ],
          },
          options: {
            responsive: true,
            maintainAspectRatio: false,
            layout: {
              padding: 20,
            },
            plugins: {
              legend: {
                onClick: (event, legendItem, legend) => {
                  const chart = legend.chart;
                  const idx =
                    legendItem.index !== undefined ? legendItem.index : null;
                  if (selectedRingIndex === idx) {
                    // Toggle off: show all rings
                    selectedRingIndex = null;
                    [0, 2, 4, 6].forEach((datasetIndex, i) => {
                      chart.data.datasets[datasetIndex].data = [
                        ...originalRingDatas[i],
                      ];
                    });
                  } else {
                    // Show only the selected ring
                    selectedRingIndex = idx;
                    [0, 2, 4, 6].forEach((datasetIndex, i) => {
                      if (i === selectedRingIndex) {
                        chart.data.datasets[datasetIndex].data = [
                          ...originalRingDatas[i],
                        ];
                      } else {
                        chart.data.datasets[datasetIndex].data = [0, 100];
                      }
                    });
                  }
                  hoveredRingIndex = selectedRingIndex;
                  chart.update();
                },
                display: true,
                position: 'top',
                labels: {
                  usePointStyle: true,
                  padding: 20,
                  font: { size: 14 },
                  pointStyle: 'rect',
                  generateLabels: (chart) => {
                    // Only real rings: 0, 2, 4, 6
                    const datasets = chart.data.datasets;
                    const labels = chart.data.labels as string[];
                    return [0, 2, 4, 6].map((datasetIndex, i) => {
                      const dataset = datasets[datasetIndex];
                      // backgroundColor can be string or array, ensure array
                      let color = '#ccc';
                      if (Array.isArray(dataset.backgroundColor)) {
                        color = dataset.backgroundColor[0] as string;
                      } else if (typeof dataset.backgroundColor === 'string') {
                        color = dataset.backgroundColor;
                      }
                      return {
                        text: String(labels?.[i] ?? ''),
                        fillStyle: color,
                        strokeStyle: color,
                        index: i,
                        datasetIndex,
                        pointStyle: 'rect',
                        hidden: false,
                      };
                    });
                  },
                },
              },
              tooltip: {
                position: 'nearest',
                enabled: true,
                filter: function (context: any) {
                  // Only show tooltip for real rings (even dataset indexes)
                  return context.datasetIndex % 2 === 0;
                },
                callbacks: {
                  label: function (context: any) {
                    // Only show tooltip for real rings (even dataset indexes)
                    const datasetIndex = context.datasetIndex;
                    if (datasetIndex % 2 === 0) {
                      const ringIdx = datasetIndex / 2;
                      const d = ringData[ringIdx];
                      return `${d.display} ${formatValue(d.value, 2)}B - ${
                        d.percentage
                      }% of Max Supply`;
                    }
                    return '';
                  },
                  title: function () {
                    return '';
                  },
                },
                displayColors: false,
              },
            },
            elements: {
              arc: {
                borderWidth: 0,
              },
            },
            onHover: (event: any, chartElements: any[]) => {
              if (chartElements && chartElements.length > 0) {
                const datasetIndex = chartElements[0].datasetIndex;
                // Only real rings (even indexes)
                if (datasetIndex % 2 === 0) {
                  hoveredRingIndex = datasetIndex / 2;
                } else {
                  hoveredRingIndex = null;
                }
              } else {
                hoveredRingIndex = null;
              }
              // Redraw chart to update center text
              if (this.charts['adaStatsPercentage']) {
                this.charts['adaStatsPercentage'].update();
              }
            },
          },
          plugins: [
            {
              id: 'centerText',
              afterDraw: (chart: any) => {
                const { ctx, chartArea } = chart;
                if (!chartArea) return;
                const isTooltipVisible =
                  chart.tooltip && chart.tooltip.opacity > 0;
                ctx.save();
                ctx.globalAlpha = isTooltipVisible ? 0.3 : 1; // Làm mờ khi có tooltip

                ctx.font = 'bold 18px Arial';
                ctx.textAlign = 'center';
                ctx.textBaseline = 'middle';
                ctx.fillStyle = '#666';
                let title = 'Circulating supply';
                let value = formatValue(data.circulating_supply, 2) + 'B';
                if (
                  hoveredRingIndex !== null &&
                  hoveredRingIndex >= 0 &&
                  hoveredRingIndex < ringData.length
                ) {
                  // When hovering: show full label (with ' (ADA)')
                  title = ringData[hoveredRingIndex].display;
                  value =
                    formatValue(ringData[hoveredRingIndex].value, 2) + 'B';
                  let percentage = '';
                  if (hoveredRingIndex === 0) {
                    percentage = data.circulating_supply_percentage + '%';
                  } else {
                    percentage = ringData[hoveredRingIndex].percentage + '%';
                  }
                  ctx.font = 'bold 20px Arial';
                  ctx.fillText(
                    title,
                    (chartArea.left + chartArea.right) / 2,
                    (chartArea.top + chartArea.bottom) / 2 - 15
                  );
                  ctx.font = 'bold 18px Arial';
                  ctx.fillText(
                    value,
                    (chartArea.left + chartArea.right) / 2,
                    (chartArea.top + chartArea.bottom) / 2 + 15
                  );
                  ctx.font = 'bold 15px Arial';
                  ctx.fillText(
                    percentage,
                    (chartArea.left + chartArea.right) / 2,
                    (chartArea.top + chartArea.bottom) / 2 + 40
                  );
                } else {
                  // Default: Circulating supply (no percentage, no ' (ADA)')
                  title = ringData[0].display.replace(' (ADA)', '');
                  value = formatValue(ringData[0].value, 2) + 'B';
                  ctx.font = 'bold 20px Arial';
                  ctx.fillText(
                    title,
                    (chartArea.left + chartArea.right) / 2,
                    (chartArea.top + chartArea.bottom) / 2 - 15
                  );
                  ctx.font = 'bold 18px Arial';
                  ctx.fillText(
                    value,
                    (chartArea.left + chartArea.right) / 2,
                    (chartArea.top + chartArea.bottom) / 2 + 15
                  );
                }
                ctx.restore();
              },
            },
          ],
        };
        if (this.charts['adaStatsPercentage']) {
          this.charts['adaStatsPercentage'].destroy();
        }
        this.charts['adaStatsPercentage'] = new Chart(
          this.adaStatsPercentageChart.nativeElement,
          config
        );
      },
    });
  }

  private initTreasuryChart(): void {
    this.treasuryService.getTreasuryWithdrawals().subscribe({
      next: (data: TreasuryWithdrawalsResponse[]) => {
        // Sort by epoch_no ascending
        const sorted = [...data].sort(
          (a, b) => (a.epoch_no || 0) - (b.epoch_no || 0)
        );
        const labels = sorted.map((item) => (item.epoch_no || 0).toString());
        const localLabels = labels; // for closure in tick callback
        // Datasets: bar for amount, line for withdrawals
        const withdrawals = sorted.map((item) => item.amount || 0);
        const cumulativeSums = withdrawals.reduce(
          (acc: number[], amount: number, index: number) => {
            const sum = (acc[index - 1] || 0) + amount;
            return [...acc, sum];
          },
          []
        );
        const datasets = [
          {
            type: 'bar' as ChartType,
            label: 'Treasury Withdrawals (ADA)',
            data: withdrawals,
            backgroundColor: '#2B8CA9',
            order: 2,
            barPercentage: 0.8,
            categoryPercentage: 0.6,
          },
          {
            type: 'line',
            label: 'Total Treasury Withdrawals (ADA)',
            data: cumulativeSums,
            borderColor: '#000000',
            fill: false,
            tension: 0.4,
            order: 1,
            yAxisID: 'y1',
            pointRadius: 4,
            pointBackgroundColor: '#000000',
          } as any,
        ];
        const config: ChartConfiguration = {
          type: 'bar' as ChartType,
          data: {
            labels,
            datasets: datasets as any,
          },
          options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
              legend: {
                position: 'top',
                labels: {
                  usePointStyle: true,
                  padding: 20,
                  pointStyle: 'rect',
                  font: { size: 12 },
                  boxWidth: 20,
                },
              },
              tooltip: {
                mode: 'index',
                intersect: false,
              },
            },
            scales: {
              x: {
                grid: { color: '#E0E0E0' },
                ticks: {
                  color: '#666',
                  font: { size: 12 },
                  maxRotation: 45,
                  minRotation: 0,
                },
                title: {
                  display: true,
                  text: 'Epoch',
                  font: {
                    size: 16,
                    weight: 'bold',
                  },
                },
              },
              y: {
                beginAtZero: true,
                grid: { color: '#E0E0E0' },
                ticks: {
                  color: '#666',
                  font: { size: 14 },
                  callback: function (value, index, ticks) {
                    return formatValue(Number(value), 1);
                  },
                },
                title: { display: false },
              },
              y1: {
                beginAtZero: true,
                position: 'right',
                grid: { drawOnChartArea: false },
                ticks: {
                  color: '#666',
                  font: { size: 14 },
                  callback: function (value, index, ticks) {
                    return formatValue(Number(value), 1);
                  },
                },
                display: true,
              },
            },
          },
        };
        if (this.charts['treasury']) {
          this.charts['treasury'].destroy();
        }
        this.charts['treasury'] = new Chart(
          this.treasuryChart.nativeElement,
          config
        );
      },
    });
  }

  private initTreasuryVolatilityChart(): void {
    this.treasuryService.getTreasuryVolatility().subscribe({
      next: (data: TreasuryResponse) => {
        const epochs = data.volatilities.map((item) => item.epoch_no || 0);
        const treasuryData = data.volatilities.map(
          (item) => item.treasury || 0
        );
        const withdrawalsData = data.withdrawals.map(
          (item) => item.amount || 0
        );

        this.chartTreasuryVolatilityOptions = {
          series: [
            {
              name: 'Total Treasury (ADA)',
              data: treasuryData,
            },
            {
              name: 'Total Treasury Withdrawals (ADA)',
              data: withdrawalsData,
            },
          ],
          chart: {
            type: 'area',
            stacked: false,
            zoom: {
              enabled: false,
            },
            toolbar: {
              show: false,
            },
            height: 350,
          },
          dataLabels: {
            enabled: false,
          },
          stroke: {
            curve: 'smooth',
          },
          xaxis: {
            categories: epochs,
            labels: {
              style: {
                colors: '#666',
              },
              rotate: -45,
              rotateAlways: false,
              hideOverlappingLabels: true,
              formatter: (value: string | number) => {
                return Math.floor(Number(value)).toString();
              },
            },
            type: 'numeric',
            tickAmount: 10,
          },
          yaxis: {
            labels: {
              style: {
                colors: '#666',
              },
              formatter: (value: number) => {
                return formatValue(value, 1);
              },
            },
          },
          fill: {
            type: 'gradient',
            gradient: {
              shadeIntensity: 1,
              opacityFrom: 0.8,
              opacityTo: 0.5,
              stops: [0, 90, 100],
            },
          },
          title: {
            text: '',
          },
          tooltip: {
            theme: 'dark',
            y: {
              formatter: (value: number) => {
                return formatValue(value, 2) + ' ₳';
              },
            },
            x: {
              formatter: (value: string | number) => {
                return `Epoch: ${value}`;
              },
              show: true,
            },
          } as ApexTooltip,
          grid: {
            borderColor: '#f1f1f1',
            strokeDashArray: 4,
          } as ApexGrid,
          markers: {
            size: 0,
            hover: {
              size: 6,
            },
          },
          colors: ['rgba(40, 120, 144, 0.8)', '#66C2A5'],
          legend: {
            show: true,
            position: 'top',
            horizontalAlign: 'center',
            floating: false,
            offsetY: -10,
            offsetX: 0,
            fontSize: '12px',
            fontWeight: 400,
            itemMargin: {
              horizontal: 15,
              vertical: 5,
            },
          },
        };

        this.isTreasuryVolatilityLoaded = true;
      },
      error: (error) => {
        this.isTreasuryVolatilityLoaded = false;
        console.error('Error fetching Treasury Volatility data:', error);
      },
    });
  }

  private initGovernanceParametersChart(): void {
    this.combineService.getGovernanceParameters().subscribe({
      next: (data: GovernanceParametersResponse[]) => {
        // Lọc dữ liệu sử dụng filterEpochs
        const networkFields: (keyof GovernanceParametersResponse)[] = [
          'max_block_size',
          'max_tx_size',
          'max_bh_size',
          'max_val_size',
          'max_tx_ex_mem',
          'max_tx_ex_steps',
          'max_block_ex_mem',
          'max_block_ex_steps',
          'max_collateral_inputs',
        ];

        const economicFields: (keyof GovernanceParametersResponse)[] = [
          'min_fee_a',
          'min_fee_b',
          'key_deposit',
          'pool_deposit',
          'monetary_expand_rate',
          'treasury_growth_rate',
          'min_pool_cost',
          'coins_per_utxo_size',
          'price_mem',
          'price_step',
        ];

        const technicalFields: (keyof GovernanceParametersResponse)[] = [
          'influence',
          'max_epoch',
          'optimal_pool_count',
          'cost_models',
          'collateral_percent',
        ];

        const governanceFields: (keyof GovernanceParametersResponse)[] = [
          'gov_action_lifetime',
          'gov_action_deposit',
          'drep_deposit',
          'drep_activity',
          'committee_min_size',
          'committee_max_term_length',
        ];

        // Lọc dữ liệu riêng cho từng biểu đồ
        const networkFilteredData = filterEpochs(data, [networkFields], 7);
        const economicFilteredData = filterEpochs(data, [economicFields], 7);
        const technicalFilteredData = filterEpochs(data, [technicalFields], 7);
        const governanceFilteredData = filterEpochs(
          data,
          [governanceFields],
          7
        );

        const labels = {
          network: networkFilteredData.map((item) => `${item.epoch_no}`),
          economic: economicFilteredData.map((item) => `${item.epoch_no}`),
          technical: technicalFilteredData.map((item) => `${item.epoch_no}`),
          governance: governanceFilteredData.map((item) => `${item.epoch_no}`),
        };

        // Hàm tạo interleavedLabels và labelEpochs cho từng group
        function buildInterleavedLabels(epochLabels: string[]): string[] {
          const result: string[] = [];
          const epochNums = epochLabels.map(Number);
          for (let i = 0; i < epochLabels.length; i++) {
            result.push(epochLabels[i]);
            if (i < epochLabels.length - 1) {
              if (epochNums[i + 1] - epochNums[i] > 1) {
                result.push('...');
              }
            }
          }
          return result;
        }
        function buildLabelEpochs(epochLabels: string[]): number[] {
          return epochLabels.map(Number);
        }

        // Network
        const networkInterleavedLabels = buildInterleavedLabels(labels.network);
        const networkLabelEpochs = buildLabelEpochs(labels.network);
        const networkParameters: {
          key: keyof GovernanceParametersResponse;
          label: string;
          color: string;
        }[] = [
          { key: 'max_block_size', label: 'Max Block Size', color: '#FF6B82' },
          { key: 'max_tx_size', label: 'Max Tx Size', color: '#FFD966' },
          { key: 'max_bh_size', label: 'Max BH Size', color: '#FF9F40' },
          { key: 'max_val_size', label: 'Max Val Size', color: '#66C2A5' },
          { key: 'max_tx_ex_mem', label: 'Max Tx Ex Mem', color: '#8DA0CB' },
          {
            key: 'max_tx_ex_steps',
            label: 'Max Tx Ex Steps',
            color: '#B3B3E6',
          },
          {
            key: 'max_block_ex_mem',
            label: 'Max Block Ex Mem',
            color: '#F28CB1',
          },
          {
            key: 'max_block_ex_steps',
            label: 'Max Block Ex Steps',
            color: '#FFE599',
          },
          {
            key: 'max_collateral_inputs',
            label: 'Max Collateral Inputs',
            color: '#FFB266',
          },
        ];

        // Economic
        const economicInterleavedLabels = buildInterleavedLabels(
          labels.economic
        );
        const economicLabelEpochs = buildLabelEpochs(labels.economic);
        const economicParameters: {
          key: keyof GovernanceParametersResponse;
          label: string;
          color: string;
        }[] = [
          { key: 'min_fee_a', label: 'Min Fee A', color: '#FF6B82' },
          { key: 'min_fee_b', label: 'Min Fee B', color: '#FFD966' },
          { key: 'key_deposit', label: 'Key Deposit', color: '#FF9F40' },
          { key: 'pool_deposit', label: 'Pool Deposit', color: '#66C2A5' },
          {
            key: 'monetary_expand_rate',
            label: 'Monetary Expand Rate',
            color: '#8DA0CB',
          },
          {
            key: 'treasury_growth_rate',
            label: 'Treasury Growth Rate',
            color: '#B3B3E6',
          },
          { key: 'min_pool_cost', label: 'Min Pool Cost', color: '#F28CB1' },
          {
            key: 'coins_per_utxo_size',
            label: 'Coins Per UTXO Size',
            color: '#FFE599',
          },
          { key: 'price_mem', label: 'Price Memory', color: '#FFB266' },
          { key: 'price_step', label: 'Price Step', color: '#85D1B2' },
        ];

        // Tính kích thước cost_models cho từng epoch
        const costModelsSizes = technicalFilteredData.map((item) => {
          try {
            let obj;
            if (typeof item.cost_models === 'string') {
              obj = JSON.parse(item.cost_models);
            } else if (
              item.cost_models &&
              typeof item.cost_models === 'object'
            ) {
              obj = item.cost_models;
            } else {

              return 0;
            }

            // Kiểm tra nội dung và tính tổng số phần tử
            if (obj && typeof obj === 'object' && !Array.isArray(obj)) {
              let totalSize = 0;
              for (const key in obj) {
                if (obj.hasOwnProperty(key)) {
                  const value = obj[key];
                  if (Array.isArray(value)) {
                    totalSize += value.length; // Đếm số phần tử trong mảng
                  } else {
                    totalSize += 1; // Nếu không phải mảng, tính như một phần tử
                  }
                }
              }
              return totalSize > 0 ? totalSize : 0;
            } else {

              return 0;
            }
          } catch (e) {
            console.error('Error parsing cost_models:', e, item.cost_models);
            return 0;
          }
        });

        // Tìm min/max để scale
        const minSize = Math.min(...costModelsSizes.filter((x) => x > 0)) || 1; // Tránh chia cho 0
        const maxSize = Math.max(...costModelsSizes);

        // Hàm scale về khoảng [500, 1000]
        function scaleSize(size: number) {
          if (maxSize === minSize) return 500;
          const scaledValue =
            500 + ((size - minSize) / (maxSize - minSize)) * 500;
          return Math.max(500, Math.min(1000, scaledValue)); // Giới hạn trong [500, 1000]
        }

        // Technical
        const technicalInterleavedLabels = buildInterleavedLabels(
          labels.technical
        );
        const technicalLabelEpochs = buildLabelEpochs(labels.technical);
        const technicalParameters: {
          key: keyof GovernanceParametersResponse;
          label: string;
          color: any;
        }[] = [
          { key: 'influence', label: 'Influence', color: '#36A2EB' },
          { key: 'max_epoch', label: 'Max Epoch', color: '#FF6384' },
          {
            key: 'optimal_pool_count',
            label: 'Optimal Pool Count',
            color: '#FFCE56',
          },
          {
            key: 'cost_models',
            label: 'Cost Models',
            color: createDiagonalPattern('green'),
          },
          {
            key: 'collateral_percent',
            label: 'Collateral Percent',
            color: '#9966FF',
          },
        ];

        // Governance
        const governanceInterleavedLabels = buildInterleavedLabels(
          labels.governance
        );
        const governanceLabelEpochs = buildLabelEpochs(labels.governance);
        const governanceParameters: {
          key: keyof GovernanceParametersResponse;
          label: string;
          color: string;
        }[] = [
          {
            key: 'gov_action_lifetime',
            label: 'Gov Action Validity',
            color: '#36A2EB',
          },
          {
            key: 'gov_action_deposit',
            label: 'Gov Action Deposit',
            color: '#FF6384',
          },
          { key: 'drep_deposit', label: 'DRep Deposit', color: '#FFCE56' },
          { key: 'drep_activity', label: 'DRep Activity', color: '#4BC0C0' },
          {
            key: 'committee_min_size',
            label: 'Committee Min Size',
            color: '#9966FF',
          },
          {
            key: 'committee_max_term_length',
            label: 'Committee Max Term Length',
            color: '#FF9F40',
          },
        ];

        // Hàm chèn null vào giữa các giá trị dataset nếu epoch cách nhau > 1
        function interleaveNullsByEpoch(
          arr: number[],
          epochs: number[]
        ): (number | null)[] {
          const result: (number | null)[] = [];
          for (let i = 0; i < arr.length; i++) {
            result.push(arr[i]);
            if (i < arr.length - 1) {
              if (epochs[i + 1] - epochs[i] > 1) {
                result.push(null);
              }
            }
          }
          return result;
        }

        // Network
        const networkDatasets = networkParameters.map((param) => ({
          label: param.label,
          data: interleaveNullsByEpoch(
            networkFilteredData.map((item) => Number(item[param.key])),
            networkLabelEpochs
          ),
          backgroundColor: param.color,
          borderColor: param.color,
          borderWidth: 1,
        }));

        // Economic
        const economicDatasets = economicParameters.map((param) => ({
          label: param.label,
          data: interleaveNullsByEpoch(
            economicFilteredData.map((item) => Number(item[param.key])),
            economicLabelEpochs
          ),
          backgroundColor: param.color,
          borderColor: param.color,
          borderWidth: 1,
        }));

        // Technical
        const technicalDatasets = technicalParameters.map((param) => ({
          label: param.label,
          data: interleaveNullsByEpoch(
            technicalFilteredData.map((item, idx) =>
              param.key === 'cost_models'
                ? scaleSize(costModelsSizes[idx])
                : Number(item[param.key])
            ),
            technicalLabelEpochs
          ),
          backgroundColor: param.color,
          borderColor: param.color,
          borderWidth: 1,
        }));

        // Governance
        const governanceDatasets = governanceParameters.map((param) => ({
          label: param.label,
          data: interleaveNullsByEpoch(
            governanceFilteredData.map((item) => Number(item[param.key])),
            governanceLabelEpochs
          ),
          backgroundColor: param.color,
          borderColor: param.color,
          borderWidth: 1,
        }));

        // Sửa lại createChartConfig để nhận thêm tham số labels
        const createChartConfig = (
          datasets: any[],
          chartElement: ElementRef,
          chartKey: string,
          chartLabels: string[]
        ) => {
          const config: ChartConfiguration = {
            type: 'bar' as ChartType,
            data: {
              labels: chartLabels,
              datasets: datasets,
            },
            options: {
              responsive: true,
              maintainAspectRatio: false,
              plugins: {
                legend: {
                  position: 'top',
                },
                tooltip: {
                  callbacks: {
                    label: function (context) {
                      // If this is the technicalGroup chart and the dataset label is 'Cost Models', show 'Click to View'
                      if (chartKey === 'technicalGroup') {

                        if (context.dataset.label === 'Cost Models') {
                          return 'Click to View';
                        }
                        if (context.dataset.label === 'Max Epoch') {
                          return `${context.dataset.label}: ${formatValue(
                            Number(context.raw),
                            0
                          )} epoch`;
                        }
                      }

                      if (chartKey === 'networkGroup') {
                        if (
                          context.dataset.label === 'Max Block Size' ||
                          context.dataset.label === 'Max Tx Size' ||
                          context.dataset.label === 'Max Val Size' ||
                          context.dataset.label === 'Max BH Size'
                        )
                          return `${context.dataset.label}: ${formatValue(
                            Number(context.raw),
                            0
                          )} bytes`;
                        else {
                          let unit = '';
                          switch (context.dataset.label) {
                            case 'Max Tx Ex Mem':
                              unit = 'execution memory';
                              break;
                            case 'Max Tx Ex Steps':
                            case 'Max Block Ex Steps':
                              unit = 'execution steps';
                              break;
                            case 'Max Block Ex Mem':
                              unit = 'execution memory';
                          }

                          return `${context.dataset.label}: ${formatValue(
                            Number(context.raw),
                            0
                          )} ${unit}`;
                        }
                      }

                      if (chartKey === 'economicGroup') {
                        let unit = '';
                        switch (context.dataset.label) {
                          case 'Min Fee A':
                          case 'Min Fee B':
                          case 'Key Deposit':
                          case 'Pool Deposit':
                          case 'Min Pool Cost':
                            unit = '₳';
                            break;
                          case 'Monetary Expand Rate':
                          case 'Treasury Growth Rate':
                            unit = '%';
                            break;
                        }

                        if (unit) {
                          return `${context.dataset.label}: ${formatValue(
                            Number(context.raw),
                            0
                          )} ${unit}`;
                        }
                      }

                      if (chartKey === 'governanceGroup') {
                        let unit = '';
                        switch (context.dataset.label) {
                          case 'Gov Action Deposit':
                          case 'DRep Deposit':
                            unit = '₳';
                            break;
                          case 'Gov Action Validity':
                          case 'DRep Activity':
                          case 'Committee Max Term Length':
                            unit = 'epoch';
                            break;
                          case 'Committee Min Size':
                            unit = 'constitutional committee';
                            break;
                        }

                        if (unit) {
                          return `${context.dataset.label}: ${formatValue(
                            Number(context.raw),
                            0
                          )} ${unit}`;
                        }
                      }

                      return `${context.dataset.label}: ${formatValue(
                        Number(context.raw),
                        0
                      )}`;
                    },
                  },
                },
              },
              scales: {
                y: {
                  type: 'logarithmic',
                  ticks: {
                    callback: function (value) {
                      return formatValue(Number(value), 0);
                    },
                  },
                },
                x: {
                  ticks: {
                    autoSkip: false,
                  },
                },
              },
            },
          };

          if (this.charts[chartKey]) {
            this.charts[chartKey].destroy();
          }
          this.charts[chartKey] = new Chart(chartElement.nativeElement, config);
        };

        // Khởi tạo từng chart với labels riêng
        createChartConfig(
          networkDatasets,
          this.networkGroupChart,
          'networkGroup',
          networkInterleavedLabels
        );
        createChartConfig(
          economicDatasets,
          this.economicGroupChart,
          'economicGroup',
          economicInterleavedLabels
        );
        createChartConfig(
          technicalDatasets,
          this.technicalGroupChart,
          'technicalGroup',
          technicalInterleavedLabels
        );
        createChartConfig(
          governanceDatasets,
          this.governanceGroupChart,
          'governanceGroup',
          governanceInterleavedLabels
        );

        // Add click event for cost_models column in technicalGroupChart
        if (
          this.technicalGroupChart?.nativeElement &&
          this.charts['technicalGroup']
        ) {
          const chart = this.charts['technicalGroup'];
          this.technicalGroupChart.nativeElement.onclick = (
            event: MouseEvent
          ) => {
            const points = chart.getElementsAtEventForMode(
              event,
              'nearest',
              { intersect: true },
              true
            );
            if (points.length > 0) {
              const firstPoint = points[0];
              const datasetIndex = firstPoint.datasetIndex;
              const dataIndex = firstPoint.index;
              // Find the cost_models dataset index
              const costModelsIndex = technicalParameters.findIndex(
                (p) => p.key === 'cost_models'
              );
              if (datasetIndex === costModelsIndex) {
                // Lấy label tại vị trí click
                const clickedLabel = technicalInterleavedLabels[dataIndex];
                // Tìm index của label này trong technicalFilteredData
                const realIndex = technicalFilteredData.findIndex(
                  (item) => String(item.epoch_no) === clickedLabel
                );
                // Lấy cost_models đúng
                const costModelsRaw =
                  realIndex !== -1
                    ? technicalFilteredData[realIndex]?.cost_models
                    : undefined;
                let parsed: any = {};
                try {
                  parsed =
                    typeof costModelsRaw === 'string'
                      ? JSON.parse(costModelsRaw)
                      : costModelsRaw;
                } catch (e) {
                  parsed = { error: 'Invalid JSON' };
                }
                this.dialog.open(TextModalComponent, {
                  width: '50vw',
                  height: '90vh',
                  maxWidth: 'none',
                  panelClass: 'fullscreen-modal',
                  data: {
                    title: 'Cost Models',
                    costModels: parsed,
                    dynamic: true,
                  },
                });
              }
            }
          };
        }
      },
    });
  }

  private initApexChart(): void {
    this.poolService.getAdaStatistics().subscribe({
      next: (data: AdaStatisticsResponse) => {
        const epochs = data.pool_result?.map((item) => item.epoch_no || 0);
        const amount = data.drep_result?.map(
          (item) => (item.amount && +item.amount) || 0
        );
        const stake = data.pool_result?.map(
          (item) => (item.total_active_stake && +item.total_active_stake) || 0
        );
        const supply = data.supply_result?.map(
          (item) => (item.supply && +item.supply) || 0
        );

        this.chartApexOptions = {
          series: [
            {
              name: 'ADA register to vote',
              data: amount || [],
            },
            {
              name: 'ADA staking',
              data: stake || [],
            },
            {
              name: 'Circulating supply',
              data: supply || [],
            },
          ],
          chart: {
            type: 'area',
            stacked: false,
            zoom: {
              enabled: false,
            },
            toolbar: {
              show: false,
            },
          },
          dataLabels: {
            enabled: false,
          },
          stroke: {
            curve: 'smooth',
          },
          xaxis: {
            categories: epochs,
            labels: {
              style: {
                colors: '#666',
              },
              rotate: -45,
              rotateAlways: false,
              hideOverlappingLabels: true,
              formatter: (value: string | number) => {
                return Math.floor(Number(value)).toString();
              },
            },
            type: 'numeric',
            tickAmount: 10,
          },
          yaxis: {
            labels: {
              style: {
                colors: '#666',
              },
              formatter: (value: number) => {
                return formatValue(value, 0);
              },
            },
          },
          fill: {
            type: 'gradient',
            gradient: {
              shadeIntensity: 1,
              opacityFrom: 0.8,
              opacityTo: 0.5,
              stops: [0, 90, 100],
            },
          },
          title: {
            text: '',
          },
          tooltip: {
            theme: 'dark',
            y: {
              formatter: (value: number) => {
                return formatValue(value, 2) + ' ₳';
              },
            },
            x: {
              formatter: (value: string | number) => {
                return `Epoch: ${value}`;
              },
              show: true,
            },
          } as ApexTooltip,
          grid: {
            borderColor: '#f1f1f1',
            strokeDashArray: 4,
          } as ApexGrid,
          markers: {
            size: 0,
            hover: {
              size: 6,
            },
          },
          colors: ['#A6F6FF', '#3E7EFF', '#238DB4'],
          legend: {
            position: 'top',
            horizontalAlign: 'right',
            floating: true,
            offsetY: -25,
            offsetX: -5,
          },
        };

        this.ischartApexOptionsLoaded = true;
      },
      error: (error) => {
        this.ischartApexOptionsLoaded = false;
        console.error('Error fetching ADA statistics data:', error);
      },
    });
  }

  onExpandADAStatitics(chartOptions: ChartOptions): void {
    const dialogChartOptions = { ...chartOptions };
    dialogChartOptions.chart = { ...dialogChartOptions.chart, height: 700 };

    this.dialog
      .open(ApexModalComponent, {
        width: '90vw', // Chiều rộng 90% viewport width
        height: '90vh', // Chiều cao 90% viewport height
        maxWidth: '99vw', // Giới hạn chiều rộng tối đa
        maxHeight: '99vh', // Giới hạn chiều cao tối đa
        data: {
          title: 'ADA Statistics',
          chartApexOptions: dialogChartOptions,
        },
        panelClass: 'venn-modal', // Thêm class tùy chỉnh nếu cần
      })
      .afterClosed()
      .subscribe((result) => {

      });
  }

  onExpandTreasuryVolatility(chartOptions: ChartOptions): void {
    const dialogChartOptions = { ...chartOptions };
    dialogChartOptions.chart = { ...dialogChartOptions.chart, height: 700 };

    this.dialog
      .open(ApexModalComponent, {
        width: '90vw', // Chiều rộng 90% viewport width
        height: '90vh', // Chiều cao 90% viewport height
        maxWidth: '99vw', // Giới hạn chiều rộng tối đa
        maxHeight: '99vh', // Giới hạn chiều cao tối đa
        data: {
          title: 'Treasury Volatility',
          chartApexOptions: dialogChartOptions,
        },
        panelClass: 'venn-modal', // Thêm class tùy chỉnh nếu cần
      })
      .afterClosed()
      .subscribe((result) => {

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

  get approvalRate(): number {
    const total = this.governanceAction?.totalProposals || 0;
    if (total === 0) return 0;
    return ((this.governanceAction?.approvedProposals || 0) / total) * 100;
  }

  getConstitutionalCommitteeChartData() {
    if (this.chartDisplayNewCCs) {
      return this.newCcData;
    } else {
      return this.oldCcData;
    }
  }

  toggleConstitutionalCommitteeChart(): void {
    this.chartDisplayNewCCs = !this.chartDisplayNewCCs;
    this.initConstitutionalCommitteeChart();
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
