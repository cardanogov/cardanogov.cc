import { Component, OnInit, AfterViewInit, ViewChild, ElementRef, ChangeDetectorRef } from '@angular/core';
import { NbIconModule, NbCardModule, NbTableModule } from '@nebular/theme';
import { Chart, ChartConfiguration, ChartType, ChartTypeRegistry, registerables } from 'chart.js';
import { MatDialog } from '@angular/material/dialog';

import { ChartComponent } from '../../shared/components/chart/chart.component';
import { ModalComponent } from '../../shared/components/modal/modal.component';
import { CardSkeletonComponent } from '../../shared/components/skeleton/card-skeleton.component';
import { CommonModule } from '@angular/common';
import { CommitteeCardComponent } from './committee-card/committee-card.component';
import { EpochService } from '../../core/services/epoch.service';
import { CommitteeInfoResponse, CommitteeService } from '../../core/services/committee.service';
import { VotingService } from '../../core/services/voting.service';

// Register Chart.js components
Chart.register(...registerables);

interface VoteData {
  vote: string;
  epoch_no: number;
  committee_id: string;
  committee_name?: string;
}

interface CcInfo {
  key: string;
  value: string;
  cc_hot: string | null;
  members: number;
  link: string;
  endEpoch: number;
  endDate: string;
}

@Component({
  selector: 'app-cc',
  standalone: true,
  imports: [
    NbIconModule,
    NbCardModule,
    ChartComponent,
    NbTableModule,
    CardSkeletonComponent,
    CommonModule,
    CommitteeCardComponent
  ],
  templateUrl: './cc.component.html',
  styleUrl: './cc.component.scss'
})
export class CcComponent implements OnInit, AfterViewInit {
  @ViewChild('constitutionCommitteeChart') constitutionCommitteeChart!: ElementRef;
  @ViewChild('ccMembersByRegionChart') ccMembersByRegionChart!: ElementRef;
  @ViewChild('voteStaticsChart', { static: true }) voteStaticsChart!: ElementRef<HTMLCanvasElement>;

  public isLoading = true;
  public charts: { [key: string]: Chart } = {};
  private chartInitialized = false;

  totalGroups = 7;
  totalMembers = 31;
  currentEpoch = 0;
  currentEpochStart = 0;
  expiredEpoch = 0;
  expiredEpochDate = '';
  committeeInfo: CommitteeInfoResponse[] = [];

  // Pagination properties
  currentPage = 1;
  totalPages = 2;
  displayedPages: number[] = [1, 2];

  // New Constitution Committee data (Page 1)
  protected newCcInfo: CcInfo[] = [
    {
      "key": "Cardano Atlantic Council",
      "value": "cc_cold1zv6fu40c86d0yjqnum9ndr0k4qxn39gm9ge5mlxly6q42kqmjmzyj",
      "cc_hot": "cc_hot1qvr7p6ms588athsgfd0uez5m9rlhwu3g9dt7wcxkjtr4hhsq6ytv2",
      "members": 6,
      "link": "https://github.com/Cardano-Atlantic-Council",
      "endEpoch": 653,
      "endDate": "06/09/2026"
    },
    {
      "key": "Tingvard",
      "value": "cc_cold1zvvcpkl3443ykr94gyp4nddtzngqs4sejjnv9dk98747cqqeatx67",
      "cc_hot": "cc_hot1qdjx6xe6e9zk3fpzk6rakmz84n0cf8ckwjvz4e8e5j2tuscr7ckq4",
      "members": 6,
      "link": "https://github.com/Tingvard-cc/",
      "endEpoch": 726,
      "endDate": "01/09/2027"
    },
    {
      "key": "Eastern Cardano Council",
      "value": "cc_cold1zwz2a08a8cqdp7r6lyv0cj67qqf47sr7x7vf8hm705ujc6s4m87eh",
      "cc_hot": "cc_hot1qvh20fuwhy2dnz9e6d5wmzysduaunlz5y9n8m6n2xen3pmqqvyw8v",
      "members": 7,
      "link": "https://x.com/EasternCardano",
      "endEpoch": 726,
      "endDate": "01/09/2027"
    },
    {
      "key": "KtorZ",
      "value": "cc_cold1ztwq6mh5jkgwk6yq559qptw7zavkumtk7u2e2uh6rlu972slkt0rz",
      "cc_hot": "cc_hot1qfj0jatguuhl0cqrtd96u7asszssa3h6uhq08q0dgqzn5jgjfy0l0",
      "members": 1,
      "link": "https://github.com/KtorZ/",
      "endEpoch": 653,
      "endDate": "06/09/2026"
    },
    {
      "key": "Ace Alliance",
      "value": "cc_cold1zwt49epsdedwsezyr5ssvnmez96v3d3xrxdcu7j9l8srk3g5xu74h",
      "cc_hot": "cc_hot1qdc65ke6jfq2q25fcn3g89tea30tvrzpptc2tw6g8cdc7pqtmus0y",
      "members": 5,
      "link": "https://x.com/AceAlliance_CC/",
      "endEpoch": 726,
      "endDate": "01/09/2027"
    },
    {
      "key": "Cardano Japan Council",
      "value": "cc_cold1zwwv8uu8vgl5tkhx569hp94sctjq8krqr2pdcspzr6k5rcsxw2az4",
      "cc_hot": null,
      "members": 5,
      "link": "https://x.com/Cardanojp_icc",
      "endEpoch": 653,
      "endDate": "06/09/2026"
    },
    {
      "key": "Phil_uplc",
      "value": "cc_cold1zgf5jdusmxcrfqapu8ngf6j04u0wfzjc7sp9wnnlyfr0f4q68as9w",
      "cc_hot": null,
      "members": 1,
      "link": "https://x.com/phil_uplc",
      "endEpoch": 653,
      "endDate": "06/09/2026"
    }
  ]

  // Old Interim Constitution Committee data (Page 2)
  protected oldCcInfo: CcInfo[] = [
    {
      "key": "Intersect",
      "value": "cc_hot1qwzuglw5hx3wwr5gjewerhtfhcvz64s9kgam2fgtrj2t7eqs00fzv",
      "cc_hot": "cc_hot1qwzuglw5hx3wwr5gjewerhtfhcvz64s9kgam2fgtrj2t7eqs00fzv",
      "members": 1,
      "link": "https://intersectmbo.org/",
      "endEpoch": 0,
      "endDate": "expiredEpochDate"
    },
    {
      "key": "Input Output Global",
      "value": "cc_hot1qv7fa08xua5s7qscy9zct3asaa5a3hvtdc8sxexetcv3unq7cfkq5",
      "cc_hot": "cc_hot1qv7fa08xua5s7qscy9zct3asaa5a3hvtdc8sxexetcv3unq7cfkq5",
      "members": 1,
      "link": "https://iohk.io/",
      "endEpoch": 0,
      "endDate": "expiredEpochDate"
    },
    {
      "key": "Cardano Foundation",
      "value": "cc_hot1qdnedkra2957t6xzzwygdgyefd5ctpe4asywauqhtzlu9qqkttvd9",
      "cc_hot": "cc_hot1qdnedkra2957t6xzzwygdgyefd5ctpe4asywauqhtzlu9qqkttvd9",
      "members": 1,
      "link": "https://cardanofoundation.org/",
      "endEpoch": 0,
      "endDate": "expiredEpochDate"
    },
    {
      "key": "Emurgo",
      "value": "cc_hot1q0wzkpcxzzfs4mf4yk6yx7d075vqtyx2tnxsr256he6gnwq6yfy5w",
      "cc_hot": "cc_hot1q0wzkpcxzzfs4mf4yk6yx7d075vqtyx2tnxsr256he6gnwq6yfy5w",
      "members": 1,
      "link": "https://emurgo.io/",
      "endEpoch": 0,
      "endDate": "expiredEpochDate"
    },
    {
      "key": "Cardano Japan Council",
      "value": "cc_hot1qdqp9j44qfnwlkx9h78kts8hvee4ycc7czrw0xl4lqhsw4gcxgkpt",
      "cc_hot": "cc_hot1qdqp9j44qfnwlkx9h78kts8hvee4ycc7czrw0xl4lqhsw4gcxgkpt",
      "members": 5,
      "link": "https://x.com/Cardanojp_icc",
      "endEpoch": 0,
      "endDate": "expiredEpochDate"
    },
    {
      "key": "Eastern Cardano Council",
      "value": "cc_hot1qvh20fuwhy2dnz9e6d5wmzysduaunlz5y9n8m6n2xen3pmqqvyw8v",
      "cc_hot": "cc_hot1qvh20fuwhy2dnz9e6d5wmzysduaunlz5y9n8m6n2xen3pmqqvyw8v",
      "members": 6,
      "link": "https://x.com/EasternCardano",
      "endEpoch": 0,
      "endDate": "expiredEpochDate"
    },
    {
      "key": "Cardano Atlantic Council",
      "value": "cc_hot1qvr7p6ms588athsgfd0uez5m9rlhwu3g9dt7wcxkjtr4hhsq6ytv2",
      "cc_hot": "cc_hot1qvr7p6ms588athsgfd0uez5m9rlhwu3g9dt7wcxkjtr4hhsq6ytv2",
      "members": 6,
      "link": "https://x.com/CardanoAtlantic",
      "endEpoch": 0,
      "endDate": "expiredEpochDate"
    }
  ]


  constructor(
    private cdr: ChangeDetectorRef,
    private dialog: MatDialog,
    private epochService: EpochService,
    private committeeService: CommitteeService,
    private votingService: VotingService
  ) { }

  ngOnInit(): void {
    this.loadData();
  }

  ngAfterViewInit(): void {
    if (!this.isLoading) {
      this.initializeCharts();
    }
  }

  private loadData(): void {
    this.isLoading = true;
    this.committeeService.getCommitteeInfo().subscribe({
      next: (data) => {
        this.committeeInfo = data;
        if (!data || !data[0] || !data[0].members || !data[0].members[0]) {
          this.isLoading = false;
          this.cdr.detectChanges();
          return;
        }

        this.expiredEpoch = data[0].members[0].expiration_epoch || 0;

        // After getting committee info, get epoch info
        this.epochService.getCurrentEpochInfo().subscribe({
          next: (epochData) => {
            if (!epochData) {
              this.isLoading = false;
              this.cdr.detectChanges();
              return;
            }

            this.currentEpoch = epochData.epoch_no || 0;
            this.currentEpochStart = epochData.start_time || 0;
            this.calculateExpirationDate();

            // Initialize charts and get vote data after all data is loaded
            this.initializeCharts();
            this.getChartData();
          },
          error: (error) => {
            console.error('Error fetching epoch info:', error);
            this.isLoading = false;
            this.cdr.detectChanges();
          }
        });
      },
      error: (error) => {
        console.error('Error fetching committee info:', error);
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  private calculateExpirationDate(): void {
    if (!this.currentEpochStart || !this.expiredEpoch) {
      return;
    }

    const epochsUntilExpiration = this.expiredEpoch - this.currentEpoch;
    const daysUntilExpiration = epochsUntilExpiration * 5;

    const expirationDate = new Date(this.currentEpochStart * 1000);
    expirationDate.setDate(expirationDate.getDate() + daysUntilExpiration);

    const options: Intl.DateTimeFormatOptions = {
      timeZone: 'UTC',
      day: '2-digit',
      month: '2-digit',
      year: 'numeric'
    };

    this.expiredEpochDate = expirationDate.toLocaleDateString('en-GB', options);
    this.isLoading = false;
    this.cdr.detectChanges();
  }

  private initializeCharts(): void {
    if (!this.isLoading) {
      if (this.constitutionCommitteeChart?.nativeElement) {
        this.initConstitutionCommitteeChart();
      }
      if (this.ccMembersByRegionChart?.nativeElement) {
        this.initCCMembersByRegionChart();
      }

      this.chartInitialized = true;
      this.cdr.detectChanges();
    }
  }

  getChartData() {
    this.isLoading = true;
    this.votingService.getVoteList().subscribe({
      next: (data) => {
        if (!data || !Array.isArray(data)) {
          this.isLoading = false;
          this.cdr.detectChanges();
          return;
        }

        if (data.length === 0) {
          this.initVoteStatsChart([], [], []);
          this.isLoading = false;
          this.cdr.detectChanges();
          return;
        }

        const yesVotes = data.filter(vote => vote && vote.vote === 'Yes' && vote.voter_role === 'ConstitutionalCommittee')
          .map(vote => ({
            ...vote,
            committee_name: this.getCommitteeNameByVoterId(vote.voter_id || '')
          }));
        const abstainVotes = data.filter(vote => vote && vote.vote === 'Abstain' && vote.voter_role === 'ConstitutionalCommittee')
          .map(vote => ({
            ...vote,
            committee_name: this.getCommitteeNameByVoterId(vote.voter_id || '')
          }));
        const noVotes = data.filter(vote => vote && vote.vote === 'No' && vote.voter_role === 'ConstitutionalCommittee')
          .map(vote => ({
            ...vote,
            committee_name: this.getCommitteeNameByVoterId(vote.voter_id || '')
          }));

        this.initVoteStatsChart(yesVotes, abstainVotes, noVotes);
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: (error) => {
        console.error('Error fetching vote data:', error);
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  private initVoteStatsChart(yesVotes: any[], abstainVotes: any[], noVotes: any[]): void {
    // Ensure canvas is available before chart creation
    const canvas = this.voteStaticsChart?.nativeElement as HTMLCanvasElement | undefined;
    if (!canvas) {
      console.warn('Vote stats canvas not ready');
      return;
    }

    // If the canvas has no layout size yet, retry shortly (Chart.js may compute 0x0 and render nothing)
    const rect = canvas.getBoundingClientRect();
    if (!rect.width || !rect.height) {
      setTimeout(() => this.initVoteStatsChart(yesVotes, abstainVotes, noVotes), 300);
      return;
    }

    // Sync canvas pixel size with its CSS box to avoid blurry/blank rendering
    canvas.width = rect.width;
    canvas.height = rect.height;

    // Group votes by epoch
    const groupVotesByEpoch = (votes: any[]) => {
      if (!votes || votes.length === 0) return [];

      const grouped = votes.reduce((acc, vote) => {
        if (!acc[vote.epoch_no]) {
          acc[vote.epoch_no] = {
            count: 0,
            committees: new Set()
          };
        }
        acc[vote.epoch_no].count++;
        acc[vote.epoch_no].committees.add(vote.committee_name);
        return acc;
      }, {});

      return Object.entries(grouped).map(([epoch, data]: [string, any]) => ({
        x: parseInt(epoch),
        y: 2,
        r: data.count * 5,
        committees: Array.from(data.committees)
      }));
    };

    const yesData = groupVotesByEpoch(yesVotes);
    const abstainData = groupVotesByEpoch(abstainVotes).map(point => ({ ...point, y: 1 }));
    const noData = groupVotesByEpoch(noVotes).map(point => ({ ...point, y: 0 }));

    const allPoints = [...yesData, ...abstainData, ...noData] as Array<{x:number;y:number;r:number}>;
    const hasData = allPoints.length > 0;
    const xValues = allPoints.map(p => p.x);
    const xMin = hasData ? Math.min(...xValues) : Math.max(0, (this.currentEpoch || 0) - 10);
    const xMax = hasData ? Math.max(...xValues) : (this.currentEpoch || 0) + 1;

    console.log('VoteStats data points:', { yesData, abstainData, noData, xMin, xMax, currentEpoch: this.currentEpoch });

    const config: ChartConfiguration = {
      type: 'bubble' as ChartType,
      data: {
        datasets: [
          {
            label: 'Yes',
            data: yesData,
            backgroundColor: '#00E396',
            borderColor: 'transparent',
            parsing: false
          },
          {
            label: 'Abstain',
            data: abstainData,
            backgroundColor: '#FFFF00',
            borderColor: 'transparent',
            parsing: false
          },
          {
            label: 'No',
            data: noData,
            backgroundColor: '#FF4560',
            borderColor: 'transparent',
            parsing: false
          }
        ]
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        scales: {
          x: {
            type: 'linear',
            min: xMin,
            max: xMax,
            grid: {
              display: true,
              color: 'rgba(0, 0, 0, 0.1)'
            },
            ticks: {
              color: '#666',
              padding: 10,
              callback: (value) => Number(value)
            }
          },
          y: {
            min: -0.5,
            max: 2.5,
            grid: {
              display: true,
              color: 'rgba(0, 0, 0, 0.1)'
            },
            ticks: {
              color: '#666',
              padding: 20,
              stepSize: 1,
              callback: (value) => {
                const labels: string[] = ['No', 'Abstain', 'Yes'];
                const idx = Number(value);
                return Number.isInteger(idx) ? (labels[idx] || '') : '';
              }
            }
          }
        },
        plugins: {
          legend: {
            position: 'top',
            labels: {
              usePointStyle: true,
              padding: 20
            }
          },
          tooltip: {
            enabled: true,
            backgroundColor: 'rgba(255, 255, 255, 0.9)',
            titleColor: '#000',
            bodyColor: '#666',
            borderColor: '#ddd',
            borderWidth: 1,
            padding: 10,
            callbacks: {
              label: function (this: CcComponent, context: any) {
                const value = context.raw.r / 5;
                const label = context.dataset.label;
                const committees = context.raw.committees;
                return [
                  `${label}: ${value}`,
                  `Epoch: ${context.raw.x}`,
                  `Committees: ${committees.join(', ')}`
                ];
              }.bind(this)
            }
          }
        }
      }
    };

    try {
      if (this.charts['voteStatsChart']) {
        this.charts['voteStatsChart'].destroy();
      }

      const ctx = this.voteStaticsChart.nativeElement.getContext('2d');
      if (!ctx) {
        console.error('2D context not available for voteStaticsChart');
        return;
      }
      this.charts['voteStatsChart'] = new Chart(ctx, config as any);
    } catch (error) {
      console.error('Error initializing Vote Statistics Chart:', error);
    }
  }

  private initConstitutionCommitteeChart(): void {
    const config: ChartConfiguration = {
      type: 'doughnut' as ChartType,
      data: {
        labels: [
          'Cardano Atlantic Council', 'Tingvard', 'Eastern Cardano Council', 'KtorZ', 'Ace Alliance', 'Cardano Japan Council', 'Phil_uplc'
        ],
        datasets: [
          {
            data: [5, 5, 5, 5, 5, 5, 5],
            backgroundColor: [
              '#9BEDF6',
              '#14B8A6',
              '#0086AD',
              '#3B82F6',
              '#6366F1',
              '#70A5B1',
              '#D9D9D9'
            ],
            borderWidth: 2
          }
        ]
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
          legend: {
            position: 'top',
            labels: {
              padding: 10,
              usePointStyle: true,
              pointStyle: 'circle',
              font: {
                size: 12
              }
            }
          },
          tooltip: {
            enabled: true,
            callbacks: {
              label: function(context) {
                return context.label;
              }
            }
          }
        }
      }
    };

    try {
      if (this.charts['constitutionCommitteeChart']) {
        this.charts['constitutionCommitteeChart'].destroy();
      }

      this.charts['constitutionCommitteeChart'] = new Chart(
        this.constitutionCommitteeChart.nativeElement,
        config
      );
    } catch (error) {
      console.error('Error initializing Constitution Committee Chart:', error);
    }
  }

  private initCCMembersByRegionChart(): void {
    const config: ChartConfiguration = {
      type: 'pie' as ChartType,
      data: {
        labels: ['Asia', 'North America', 'Oceania', 'Europe', 'South America', 'Unknown'],
        datasets: [
          {
            data: [8, 12, 4, 3, 2, 2],
            backgroundColor: [
              '#9BEDF6',
              '#14B8A6',
              '#D9D9D9',
              '#FFFF00',
              '#0086AD',
              '#3B82F6'
            ],
            borderColor: '#ffffff',
            borderWidth: 2,
            hoverOffset: 4
          }
        ]
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
          legend: {
            display: true,
            position: 'top',
            labels: {
              usePointStyle: true,
              padding: 20,
              font: {
                size: 12
              }
            }
          },
          tooltip: {
            enabled: true,
            backgroundColor: 'rgba(255, 255, 255, 0.9)',
            titleColor: '#000',
            bodyColor: '#666',
            borderColor: '#ddd',
            borderWidth: 1,
            padding: 10,
            boxPadding: 4,
            callbacks: {
              label: (context: any) => {
                const value = context.formattedValue;
                const total = context.dataset.data.reduce((a: number, b: number) => a + b, 0);
                const percentage = Math.round((value / total) * 100);
                return `${context.label}: ${value} (${percentage}%)`;
              }
            }
          }
        }
      }
    };

    try {
      if (this.charts['ccMembersByRegionChart']) {
        this.charts['ccMembersByRegionChart'].destroy();
      }

      this.charts['ccMembersByRegionChart'] = new Chart(
        this.ccMembersByRegionChart.nativeElement,
        config
      );
    } catch (error) {
      console.error('Error initializing CC Members by Region Chart:', error);
    }
  }

  openModal(chartKey: string, chartTitle: string): void {
    // Get the source chart's configuration
    const sourceChart = this.charts[chartKey];
    const chartType = (sourceChart.config as ChartConfiguration<keyof ChartTypeRegistry>).type;

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
        createDiagonalPattern: createDiagonalPattern
      },
    });
  }

  // Pagination methods
  goToPage(page: number): void {
    if (page >= 1 && page <= this.totalPages && page !== this.currentPage) {
      this.currentPage = page;
      this.cdr.detectChanges();
    }
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

  get currentCcInfo() {
    return this.currentPage === 1 ? this.newCcInfo : this.oldCcInfo;
  }

  get allCcInfo() {
    return [...this.newCcInfo, ...this.oldCcInfo];
  }

  get currentPageTitle() {
    return this.currentPage === 1 ? 'Constitution Committee' : 'Interim Constitution Committee';
  }

  calculateTenure(endEpoch: number): string {
    if (!endEpoch || !this.currentEpoch) return 'N/A';

    const epochsRemaining = endEpoch - this.currentEpoch;
    const daysRemaining = epochsRemaining * 5;

    if (daysRemaining <= 0) return 'Expired';

    const months = Math.floor(daysRemaining / 30);
    const days = daysRemaining % 30;

    if (months > 0) {
      return `${months} month${months > 1 ? 's' : ''} ${days} day${days > 1 ? 's' : ''}`;
    } else {
      return `${days} day${days > 1 ? 's' : ''}`;
    }
  }

  getEndDate(ccInfo: CcInfo): string {
    if (ccInfo.endDate === 'expiredEpochDate') {
      return this.expiredEpochDate;
    }
    return ccInfo.endDate;
  }

  getCcLogo(ccKey: string): string {
    const logoMap: { [key: string]: string } = {
      'Cardano Atlantic Council': 'assets/icons/cc/atlantic-council.png',
      'Tingvard': 'assets/icons/cc/tingvard.jpg',
      'Eastern Cardano Council': 'assets/icons/cc/eastern-council.png',
      'KtorZ': 'assets/icons/cc/ktorz.png',
      'Ace Alliance': 'assets/icons/cc/ace-alliance.jpg',
      'Cardano Japan Council': 'assets/icons/cc/japan-council.png',
      'Phil_uplc': 'assets/icons/cc/phil_uplc.jpg',
      'Intersect': 'assets/icons/cc/intersect.png',
      'Input Output Global': 'assets/icons/cc/iog.png',
      'Cardano Foundation': 'assets/icons/cc/foundation.png',
      'Emurgo': 'assets/icons/cc/emurgo.png'
    };
    return logoMap[ccKey] || 'assets/icons/cc/default.png';
  }

  trackByCcKey(index: number, cc: CcInfo): string {
    return cc.key;
  }

  getCommitteeNameByVoterId(voterId: string): string {
    // Check in new CC info (using cc_cold_id or cc_hot_id)
    const newCc = this.newCcInfo.find(cc => cc.value === voterId || cc.cc_hot === voterId);
    if (newCc) {
      return newCc.key;
    }

    // Check in old CC info (using cc_hot_id)
    const oldCc = this.oldCcInfo.find(cc => cc.value === voterId);
    if (oldCc) {
      return oldCc.key;
    }

    return 'Unknown Committee';
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
