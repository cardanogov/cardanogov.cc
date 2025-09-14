import {
  Component,
  ElementRef,
  OnInit,
  ViewChild,
  ChangeDetectorRef,
  HostListener,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { MatDialog } from '@angular/material/dialog';
import {
  Chart,
  ChartConfiguration,
  ChartType,
  ChartTypeRegistry,
  registerables,
} from 'chart.js';
import annotationPlugin from 'chartjs-plugin-annotation';
import { finalize, Subject, debounceTime, distinctUntilChanged } from 'rxjs';

import { ChartComponent } from '../../shared/components/chart/chart.component';
import {
  TableColumn,
  TableComponent,
} from '../../shared/components/table/table.component';
import { ModalComponent } from '../../shared/components/modal/modal.component';
import {
  VoteStatisticResponse,
  VotingHistoryResponse,
  VotingService,
} from '../../core/services/voting.service';
import {
  divideAndTruncate,
  formatRepresentative,
  formatType,
  formatValue,
  MILLION,
  truncateMiddle,
} from '../../core/helper/format.helper';
import { CardSkeletonComponent } from '../../shared/components/skeleton/card-skeleton.component';
import {
  ProposalType,
  Vote,
  VoterRole,
} from '../../core/services/proposal.service';
import {
  AllocationResponse,
  CombineService,
} from '../../core/services/combine.service';
import {
  VennDiagramComponent,
  VennDiagramConfig,
  VennSet,
} from '../../shared/components/venn-diagram/venn-diagram.component';
import { VennModalComponent } from '../../shared/components/venn-modal/venn-modal.component';
import { NbIconModule } from '@nebular/theme';

Chart.register(...registerables, annotationPlugin);

interface VotingData {
  date: string;
  type: string;
  role: VoterRole;
  representative: string;
  power: string;
  vote: Vote;
  proposalId: string;
}

interface VotingHistory {
  date: string;
  type: ProposalType;
  role: VoterRole;
  representative: string;
  power: string;
  vote: Vote;
  proposalId: string;
}

export interface AllocationData {
  maxSupply: number;
  circulatingSupply: number;
  staking: number;
  delegate: number;
  power: number;
}

@Component({
  selector: 'app-voting',
  standalone: true,
  imports: [
    CommonModule,
    ChartComponent,
    TableComponent,
    CardSkeletonComponent,
    VennDiagramComponent,
    NbIconModule,
  ],
  templateUrl: './voting.component.html',
  styleUrl: './voting.component.scss',
})
export class VotingComponent implements OnInit {
  @ViewChild('voteStatistics', { static: true })
  private voteStatistics!: ElementRef<HTMLCanvasElement>;
  @ViewChild('voteStatistics2', { static: true })
  private voteStatistics2!: ElementRef<HTMLCanvasElement>;
  @ViewChild('adaAllocation', { static: true })
  private adaAllocation!: ElementRef<HTMLCanvasElement>;
  @ViewChild('votingParticipation', { static: true })
  private votingParticipation!: ElementRef<HTMLCanvasElement>;

  private voteStatisticsChart!: Chart;
  private voteStatistics2Chart!: Chart;
  private adaAllocationChart!: Chart;
  private votingParticipationChart!: Chart;

  public charts: { [key: string]: Chart } = {};
  private chartInitialized = false;

  protected isLoading = true;

  // Card data
  protected currentRegister: string = '0';
  protected registerChange: number = 0;
  protected registerRate: number = 0;
  protected abstainAmount: string = '0';
  protected abstainChange: number = 0;
  protected currentStake: string = '0';
  protected stakeChange: number = 0;
  protected currentSuplly: string = '0';
  protected supplyChange: number = 0;
  protected supplyRate: number = 0;

  // Vote History
  protected votingHistoryData: VotingHistory[] = [];
  protected currentPage = 1;
  protected itemsPerPage = 10;
  protected totalItems = 0;
  protected activeFilters = new Set<string>();
  protected filteredData: VotingData[] = [];
  protected displayData: VotingData[] = [];
  public isTableLoading = false;
  searchText = '';

  public voteStatistic1Data: VoteStatisticResponse[] = [];
  public isLoadingCharts = false;

  protected readonly tableColumns: TableColumn[] = [
    { key: 'date', title: 'Date', hasAlert: true },
    { key: 'name', title: 'Representative', hasAlert: true },
    { key: 'role', title: 'Role', hasAlert: true },
    { key: 'power', title: 'Power', hasAlert: true },
    { key: 'vote', title: 'Vote', hasAlert: true },
    { key: 'type', title: 'Type', hasAlert: true },
  ];

  protected readonly tableFilters = [
    { value: 'All', label: 'All' },
    { value: 'Yes', label: 'Yes' },
    { value: 'No', label: 'No' },
    { value: 'Abstain', label: 'Abstain' },
    { value: 'ConstitutionalCommittee', label: 'CC' },
    { value: 'SPO', label: 'SPO' },
    { value: 'DRep', label: 'DRep' },
  ];

  private searchSubject = new Subject<string>();

  VennSet: VennSet[] = [];

  constructor(
    private readonly dialog: MatDialog,
    private readonly votingService: VotingService,
    private readonly router: Router,
    private readonly cdr: ChangeDetectorRef,
    private readonly combineService: CombineService
  ) {
    this.searchSubject.pipe(distinctUntilChanged()).subscribe((term) => {
      this.currentPage = 1;
      const activeFilterArray = Array.from(this.activeFilters);

      // If "All" is selected, send undefined to show all data
      if (activeFilterArray.includes('All')) {
        this.loadVoteHistory(this.currentPage, undefined, term);
      } else {
        const filterQuery =
          activeFilterArray.length > 0
            ? activeFilterArray.join(',')
            : undefined;
        this.loadVoteHistory(this.currentPage, filterQuery, term);
      }
    });
  }

  ngOnInit(): void {
    this.initializeData();
  }

  @HostListener('window:resize')
  onResize(): void {
    // Debounce resize events to avoid excessive chart redraws
    setTimeout(() => {
      if (this.chartInitialized) {
        this.resizeCharts();
      }
    }, 250);
  }

  private resizeCharts(): void {
    // Resize Chart.js charts
    Object.values(this.charts).forEach(chart => {
      if (chart) {
        chart.resize();
      }
    });
  }

  private initializeData(): void {
    this.isLoading = true;
    this.isLoadingCharts = true;

    this.getCardInfo();
    this.loadVoteHistory(1, undefined, this.searchText);

    this.initializeChartsAfterDataLoad();
  }

  // Cards
  private getCardInfo(): void {
    this.isLoading = true;

    this.votingService
      .getVotingCardInfo()
      .pipe(
        finalize(() => {
          this.isLoading = false;
          this.cdr.detectChanges();
        })
      )
      .subscribe({
        next: (cardInfo) => {
          this.currentRegister = formatValue(cardInfo.currentRegister || 0);
          this.registerChange = parseFloat(cardInfo.registerChange || '0');
          this.registerRate = parseFloat(cardInfo.registerRate || '0');
          this.abstainAmount = formatValue(cardInfo.abstainAmount || 0);
          this.abstainChange = parseFloat(cardInfo.abstainChange || '0');
          this.currentStake = formatValue(cardInfo.currentStake || 0);
          this.stakeChange = parseFloat(cardInfo.stakeChange || '0');
          this.currentSuplly = formatValue(cardInfo.currentSuplly || 0);
          this.supplyChange = parseFloat(cardInfo.supplyChange || '0');
          this.supplyRate = parseFloat(cardInfo.supplyRate || '0');
        },
        error: (err) => {
          console.error('Error fetching voting card info:', err);
        },
      });
  }

  protected delegation(): void {
    this.router.navigate(['/more']);
  }

  private initializeChartsAfterDataLoad(): void {
    this.isLoadingCharts = false;
    this.cdr.detectChanges();

    // Then initialize charts in the next change detection cycle
    setTimeout(() => {
      try {
        if (!this.chartInitialized) {
          this.initializeCharts();
          this.chartInitialized = true;
          this.isLoadingCharts = false;
          this.cdr.detectChanges();
        }
      } catch (error) {
        console.error('Error initializing charts:', error);
        this.isLoadingCharts = false;
        this.cdr.detectChanges();
      }
    }, 1500); // Increased timeout to 500ms to ensure DOM is fully ready
  }

  private initializeCharts(): void {
    if (this.voteStatistics?.nativeElement) {
      this.initDrepStatisticsChart();
    }
    if (this.voteStatistics2?.nativeElement) {
      this.initDrepStatistics2Chart();
    }

    if (this.votingParticipation?.nativeElement) {
      this.initVotingParticipationChart();
    }

    this.initAllocationChart();
  }

  // Charts
  // Vote Statistics 1
  private initDrepStatisticsChart(): void {
    this.votingService.getVoteStatisticDrepSpo().subscribe({
      next: (data) => {
        const yesData = data.map((d) =>
          d.sum_yes_voting_power?.reduce(
            (sum, item) => sum + (item.power || 0),
            0
          )
        );
        const noData = data.map((d) =>
          d.sum_no_voting_power?.reduce(
            (sum, item) => sum + (item.power || 0),
            0
          )
        );

        const config: ChartConfiguration = {
          type: 'bar' as ChartType,
          data: {
            labels: data.map((data) => data.epoch_no),
            datasets: [
              {
                label: 'Yes',
                data: yesData.map((value) =>
                  divideAndTruncate(
                    value?.toString() || '0',
                    MILLION.toString(),
                    1
                  )
                ),
                backgroundColor: '#4CAF50',
                borderColor: '#4CAF50',
                borderWidth: 1,
              },
              {
                label: 'No',
                data: noData.map(
                  (value) =>
                    -divideAndTruncate(
                      value?.toString() || '0',
                      MILLION.toString(),
                      1
                    )
                ),
                backgroundColor: '#F44336',
                borderColor: '#F44336',
                borderWidth: 1,
              },
            ],
          },
          options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
              legend: { display: false },
              tooltip: {
                callbacks: {
                  label: (context: any) => {
                    const value = context.raw;
                    const numValue = Math.abs(Number(value));
                    let formattedValue = '';
                    if (numValue >= 1e15)
                      formattedValue = (numValue / 1e15).toFixed(2) + 'Q';
                    else if (numValue >= 1e12)
                      formattedValue = (numValue / 1e12).toFixed(2) + 'T';
                    else if (numValue >= 1e9)
                      formattedValue = (numValue / 1e9).toFixed(2) + 'B';
                    else if (numValue >= 1e6)
                      formattedValue = (numValue / 1e6).toFixed(2) + 'M';
                    else if (numValue >= 1e3)
                      formattedValue = (numValue / 1e3).toFixed(2) + 'K';
                    else formattedValue = numValue.toString();

                    // Find the corresponding epoch from the label
                    const epoch = context.chart.data.labels[context.dataIndex];

                    return [`Epoch: ${epoch}`, `Power: ₳ ${formattedValue}`];
                  },
                },
              },
            },
            scales: {
              x: {
                stacked: true,
              },
              y: {
                beginAtZero: false,
                stacked: true,
                ticks: {
                  callback: function (value: number | string) {
                    const numValue = Number(value);
                    if (Math.abs(numValue) >= 1e15)
                      return (numValue / 1e15).toFixed(2) + 'Q';
                    if (Math.abs(numValue) >= 1e12)
                      return (numValue / 1e12).toFixed(2) + 'T';
                    if (Math.abs(numValue) >= 1e9)
                      return (numValue / 1e9).toFixed(2) + 'B';
                    if (Math.abs(numValue) >= 1e6)
                      return (numValue / 1e6).toFixed(2) + 'M';
                    if (Math.abs(numValue) >= 1e3)
                      return (numValue / 1e3).toFixed(2) + 'K';
                    return numValue.toString();
                  },
                },
              },
            },
          },
        };

        try {
          if (this.charts['voteStatistics']) {
            this.charts['voteStatistics'].destroy();
          }
          this.charts['voteStatistics'] = new Chart(
            this.voteStatistics.nativeElement,
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

  // Vote Statistics 2
  private initDrepStatistics2Chart(): void {
    this.votingService.getVoteStatisticDrepSpo().subscribe({
      next: (data) => {
        const yesData = data.flatMap((d) =>
          d.sum_yes_voting_power?.map((item) => ({
            x: item.epoch_no || 0,
            y: divideAndTruncate(
              item.power?.toString() || '0',
              MILLION.toString(),
              1
            ),
            r: parseFloat(
              (
                (divideAndTruncate(
                  item.power?.toString() || '0',
                  MILLION.toString(),
                  1
                ) /
                  1e8) *
                3
              ).toFixed()
            ),
            id: item.id,
            power: item.power,
            type: 'time',
            epoch: d.epoch_no,
            name: item.name,
          }))
        );

        const noData = data.flatMap((d) =>
          d.sum_no_voting_power?.map((item) => ({
            x: item.epoch_no || 0,
            y: -divideAndTruncate(
              item.power?.toString() || '0',
              MILLION.toString(),
              1
            ),
            r: parseFloat(
              (
                (divideAndTruncate(
                  item.power?.toString() || '0',
                  MILLION.toString(),
                  1
                ) /
                  1e8) *
                3
              ).toFixed()
            ),
            id: item.id,
            power: item.power,
            type: 'time',
            epoch: d.epoch_no,
            name: item.name,
          }))
        );

        const datasets = [
          {
            label: 'Positive Votes',
            data: yesData,
            backgroundColor: '#4CAF50',
          },
          {
            label: 'Negative Votes',
            data: noData,
            backgroundColor: '#F44336',
          },
        ];

        const config: ChartConfiguration = {
          type: 'bubble' as ChartType,
          data: {
            labels: data.map((data) => data.epoch_no),
            datasets: datasets as any,
          },
          options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
              legend: { display: false },
              tooltip: {
                callbacks: {
                  label: (context: any) => {
                    const dataPoint = context.raw;
                    if (
                      !dataPoint ||
                      dataPoint.id === undefined ||
                      dataPoint.power === undefined ||
                      dataPoint.x === undefined // Ensure x (timestamp) exists
                    ) {
                      return [];
                    }

                    return [
                      `Epoch: ${dataPoint.epoch}`,
                      `ID: ${truncateMiddle(dataPoint.id)}`,
                      `Power: ₳ ${formatValue(
                        divideAndTruncate(
                          dataPoint.power.toString(),
                          MILLION.toString(),
                          1
                        )
                      )}`,
                      `Name: ${dataPoint.name}`,
                    ];
                  },
                },
              },
            },
            scales: {
              x: {
                display: true, // Keep display true
                type: 'linear', // Change type back to linear for manual tick formatting
                ticks: {
                  // padding: 10, // Keep padding if needed
                  callback: function (
                    value: number | string
                  ): string | string[] | number {
                    return value;
                  },
                  stepSize: 5,
                },
              },
              y: {
                ticks: {
                  callback: (value: string | number) => {
                    const numValue = Math.abs(Number(value));
                    if (Math.abs(numValue) >= 1e15)
                      return (numValue / 1e15).toFixed(2) + 'Q';
                    if (Math.abs(numValue) >= 1e12)
                      return (numValue / 1e12).toFixed(2) + 'T';
                    if (Math.abs(numValue) >= 1e9)
                      return (numValue / 1e9).toFixed(2) + 'B';
                    if (Math.abs(numValue) >= 1e6)
                      return (numValue / 1e6).toFixed(2) + 'M';
                    if (Math.abs(numValue) >= 1e3)
                      return (numValue / 1e3).toFixed(2) + 'K';
                    return value.toString();
                  },
                },
              },
            },
          },
        };

        try {
          if (this.charts['voteStatistics2']) {
            this.charts['voteStatistics2'].destroy();
          }
          this.charts['voteStatistics2'] = new Chart(
            this.voteStatistics2.nativeElement,
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

  private initAllocationChart(): void {
    this.combineService.getAllocation().subscribe({
      next: (data: AllocationResponse) => {
        const percenterage = (value: number, total: number): number => {
          return total > 0 ? parseFloat(((value / total) * 100).toFixed(2)) : 0;
        };

        const supplyPercentage = percenterage(
          data.circulatingSupply || 0,
          data.total || 0
        );
        const stakingPercentage = percenterage(
          data.adaStaking || 0,
          data.total || 0
        );
        const delegationPercentage = percenterage(
          data.delegation || 0,
          data.total || 0
        );
        const totalActivePercentage = percenterage(
          data.totalActive || 0,
          data.total || 0
        );

        this.VennSet = [
          {
            id: 1,
            label: 'DReps Voting Power',
            color: '#26C6DA', // teal
            count: totalActivePercentage,
            description: 'Active voting power in governance',
          },
          {
            id: 2,
            label: 'ADA Delegation',
            color: '#FFCA28', // amber
            count: delegationPercentage,
            description: 'Delegated ADA tokens',
          },
          {
            id: 3,
            label: 'ADA Staking',
            color: '#9CCC65', // light green
            count: stakingPercentage,
            description: 'Staked ADA tokens',
          },
          {
            id: 4,
            label: 'Circulating Supply',
            color: '#FF7043', // deep orange
            count: supplyPercentage,
            description: 'Total circulating ADA',
          },
          {
            id: 5,
            label: 'Max Supply',
            color: '#5C6BC0', // indigo
            count: 100,
            description: 'Maximum ADA supply',
          },
        ];

        this.cdr.detectChanges();
      },
      error: (error) => {
        console.error('Error fetching allocation data:', error);
      },
    });
  }
  // Voting Participation Chart
  private initVotingParticipationChart(): void {
    this.votingService.getVoteParticipationIndex().subscribe({
      next: (value) => {
        const chartEl = this.votingParticipation?.nativeElement;
        const ctx = chartEl ? chartEl.getContext('2d') : null;
        if (!ctx) return;

        const chartWidth = chartEl.getBoundingClientRect().width;
        const gradient = ctx.createLinearGradient(0, 0, chartWidth, 0);
        gradient.addColorStop(0, '#FF4560'); // Red
        gradient.addColorStop(0.5, '#FFD700'); // Yellow
        gradient.addColorStop(1, '#32CD32'); // Green

        const data = {
          labels: ['0', '20', '40', '60', '80', '100'],
          datasets: [
            {
              data: [100],
              backgroundColor: [gradient],
              borderWidth: 0,
              cutout: '65%',
              circumference: 180,
              rotation: 270,
            },
          ],
        };

        const gaugePlugin = {
          id: 'gaugePlugin',
          afterDatasetsDraw(chart: any) {
            const { ctx } = chart;
            ctx.save();

            // Enable anti-aliasing for smoother rendering
            ctx.imageSmoothingEnabled = true;
            ctx.imageSmoothingQuality = 'high';

            const meta = chart.getDatasetMeta(0);
            const x = meta.data[0].x;
            const y = meta.data[0].y;
            const score = value;
            const angle = Math.PI + (1 / 100) * score * Math.PI;
            const outerRadius = meta.data[0].outerRadius;
            const innerRadius = meta.data[0].innerRadius;

            // Descriptive Labels
            const descriptiveLabels = ['Minimal', 'Limited', 'Moderate', 'Strong', 'Optimal'];
            const labelRadius = innerRadius + (outerRadius - innerRadius) / 2;
            ctx.font = '16px sans-serif';
            ctx.fillStyle = 'white';
            ctx.textAlign = 'center';
            descriptiveLabels.forEach((label, i) => {
              const segmentAngle = Math.PI / descriptiveLabels.length;
              const labelAngle = Math.PI + segmentAngle * (i + 0.5);
              const labelX = x + Math.cos(labelAngle) * labelRadius;
              const labelY = y + Math.sin(labelAngle) * labelRadius;
              ctx.fillText(label, labelX, labelY);
            });

            // Tick Marks
            ctx.strokeStyle = '#444';
            ctx.lineWidth = 2;
            data.labels.forEach((_, i) => {
              const segmentAngle = Math.PI / (data.labels.length - 1);
              const tickAngle = Math.PI + segmentAngle * i;
              const startX = x + Math.cos(tickAngle) * innerRadius;
              const startY = y + Math.sin(tickAngle) * innerRadius;
              const endX = x + Math.cos(tickAngle) * outerRadius;
              const endY = y + Math.sin(tickAngle) * outerRadius;
              ctx.beginPath();
              ctx.moveTo(startX, startY);
              ctx.lineTo(endX, endY);
              ctx.stroke();
            });

            // Determine color based on score
            let needleColor = '#444';
            if (score <= 33) needleColor = '#FF4560';
            else if (score <= 66) needleColor = '#FFD700';
            else needleColor = '#32CD32';

            // Needle
            ctx.translate(x, y);
            ctx.rotate(angle);
            ctx.beginPath();
            ctx.moveTo(0, -14); // Doubled base width
            const needleLength = innerRadius + (outerRadius - innerRadius) / 2;
            ctx.lineTo(needleLength, 0); // Points to the middle of the arc
            ctx.lineTo(0, 14); // Doubled base width
            ctx.closePath();
            ctx.fillStyle = '#444';
            ctx.fill();
            ctx.strokeStyle = needleColor; // Dynamic border color
            ctx.lineWidth = 2; // Thicker border
            ctx.stroke();
            ctx.rotate(-angle);

            // Dynamic Pivot and Value
            let pivotColor = '#444';
            if (score <= 33) pivotColor = '#FF4560';
            else if (score <= 66) pivotColor = '#FFD700';
            else pivotColor = '#32CD32';

            ctx.beginPath();
            ctx.arc(0, 0, 25, 0, 2 * Math.PI);
            ctx.fillStyle = pivotColor;
            ctx.fill();

            ctx.font = 'bold 20px sans-serif';
            ctx.fillStyle = 'white';
            ctx.textAlign = 'center';
            ctx.textBaseline = 'middle';
            ctx.fillText(`${score}`, 0, 0);
            ctx.translate(-x, -y);

            // Numerical Labels with Dynamic Color
            const numLabelRadius = outerRadius + 20;
            const getColorForValue = (v: number) => {
              if (v <= 50) {
                const p = v / 50;
                const r = Math.round(255 + (255 - 255) * p);
                const g = Math.round(69 + (215 - 69) * p);
                const b = Math.round(96 + (0 - 96) * p);
                return `rgb(${r},${g},${b})`;
              } else {
                const p = (v - 50) / 50;
                const r = Math.round(255 + (50 - 255) * p);
                const g = Math.round(215 + (205 - 215) * p);
                const b = Math.round(0 + (50 - 0) * p);
                return `rgb(${r},${g},${b})`;
              }
            };

            data.labels.forEach((label, i) => {
              const segmentAngle = Math.PI / (data.labels.length - 1);
              const labelAngle = Math.PI + segmentAngle * i;
              const labelX = x + Math.cos(labelAngle) * numLabelRadius;
              const labelY = y + Math.sin(labelAngle) * numLabelRadius;

              ctx.font = 'bold 16px sans-serif';
              ctx.fillStyle = getColorForValue(parseInt(label));
              ctx.textAlign = 'center';
              ctx.fillText(label, labelX, labelY);
            });

            ctx.restore();
          },
        };

        const config = {
          type: 'doughnut' as ChartType,
          data,
          options: {
            responsive: true,
            maintainAspectRatio: false,
            aspectRatio: 2,
            layout: {
              padding: {
                top: 10,
                bottom: 5,
                left: 15,
                right: 15
              }
            },
            plugins: {
              legend: { display: false },
              tooltip: { enabled: false },
            },
          },
          plugins: [gaugePlugin],
        };

        try {
          if (this.charts['votingParticipation']) {
            this.charts['votingParticipation'].destroy();
          }
          this.charts['votingParticipation'] = new Chart(
            this.votingParticipation.nativeElement,
            config as ChartConfiguration
          );
        } catch (error) {
          console.error('Error initializing Voting Participation Chart:', error);
        }
      },
      error: (error) => {
        console.error('Error fetching voting participation data:', error);
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

  onExpandAllocation(sets: VennSet[]): void {
    this.dialog
      .open(VennModalComponent, {
        width: '90vw', // Chiều rộng 90% viewport width
        height: '90vh', // Chiều cao 90% viewport height
        maxWidth: '99vw', // Giới hạn chiều rộng tối đa
        maxHeight: '99vh', // Giới hạn chiều cao tối đa
        data: {
          sets,
        },
        panelClass: 'venn-modal', // Thêm class tùy chỉnh nếu cần
      })
      .afterClosed()
      .subscribe(() => {
        // Modal closed
      });
  }

  // Vote History
  private loadVoteHistory(
    pageNumber: number,
    filter?: string,
    searchText?: string
  ): void {
    this.isTableLoading = true;
    this.votingService
      .getVotingHistory(pageNumber, filter, searchText)
      .pipe(
        finalize(() => {
          this.cdr.detectChanges();
        })
      )
      .subscribe({
        next: (data: VotingHistoryResponse) => {
          this.totalItems = data.totalVote || 0;
          this.votingHistoryData =
            data.filteredVoteInfo?.map((item) => ({
              date: item.block_time || '',
              type: formatType(
                item.proposal_type?.toString() || ''
              ) as ProposalType,
              name: item.name || '',
              role: item.voter_role as VoterRole,
              representative: formatRepresentative(item.voter_id || ''),
              power: formatValue(item.amount || 0),
              vote: item.vote as Vote,
              proposalId: item.voter_id || '',
              epoch: item.epoch_no || 0,
            })) || [];

          this.displayData = this.votingHistoryData;
          this.isTableLoading = false;
        },
        error: (err) => {
          console.error('Error fetching voting history:', err);
          this.isTableLoading = false;
        },
      });
  }

  protected onFilterChange(filters: Set<string>): void {
    this.activeFilters = filters;
    this.currentPage = 1;

    const activeFilterArray = Array.from(this.activeFilters);

    // If "All" is selected, send undefined to show all data
    if (activeFilterArray.includes('All')) {
      this.loadVoteHistory(this.currentPage, undefined, this.searchText);
    } else {
      const filterQuery =
        activeFilterArray.length > 0 ? activeFilterArray.join(',') : undefined;
      this.loadVoteHistory(this.currentPage, filterQuery, this.searchText);
    }
  }

  protected onSearchChange(searchTerm: string): void {
    this.searchText = searchTerm;
    this.searchSubject.next(searchTerm);
  }

  protected onPageChange(event: { page: number; itemsPerPage: number }): void {
    this.currentPage = event.page;
    // Construct filterQuery from this.activeFilters to ensure pagination respects current filters
    const activeFilterArray = Array.from(this.activeFilters);

    // If "All" is selected, send undefined to show all data
    if (activeFilterArray.includes('All')) {
      this.loadVoteHistory(this.currentPage, undefined, this.searchText);
    } else {
      const filterQuery =
        activeFilterArray.length > 0 ? activeFilterArray.join(',') : undefined;
      this.loadVoteHistory(this.currentPage, filterQuery, this.searchText);
    }
  }

  protected get registerToSupplyPercent(): number {
    const register = parseFloat(
      this.currentRegister.toString().replace(/,/g, '')
    );
    const supply = parseFloat(this.currentSuplly.toString().replace(/,/g, ''));
    if (!register || !supply) return 0;
    return (register / supply) * 100;
  }

  protected get abstainToRegisterPercent(): number {
    const abstain = parseFloat(this.abstainAmount.toString().replace(/,/g, ''));
    const register = parseFloat(
      this.currentRegister.toString().replace(/,/g, '')
    );
    if (!abstain || !register) return 0;
    return (abstain / register) * 100;
  }

  protected get abstainRate(): string {
    const abstain = parseFloat(this.abstainAmount.toString().replace(/,/g, ''));
    const register = parseFloat(
      this.currentRegister.toString().replace(/,/g, '')
    );
    if (!abstain || !register) return '0';
    return ((abstain / register) * 100).toFixed(2);
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
