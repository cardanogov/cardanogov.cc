import {
  ChangeDetectorRef,
  Component,
  ElementRef,
  OnInit,
  ViewChild,
} from '@angular/core';
import {
  Chart,
  ChartConfiguration,
  ChartTypeRegistry,
  registerables,
} from 'chart.js';
import { DrepService } from '../../../core/services/drep.service';
import { CommitteeService } from '../../../core/services/committee.service';
import { MatDialog } from '@angular/material/dialog';
import { Router, ActivatedRoute } from '@angular/router';
import {
  TableColumn,
  TableComponent,
} from '../../../shared/components/table/table.component';
import {
  ProposalInfoResponse,
  ProposalService,
  ProposalVotes,
  TotalVoter,
  VoterRole,
} from '../../../core/services/proposal.service';
import {
  formatValue,
  truncateMiddle,
} from '../../../core/helper/format.helper';
import { CardInfo } from '../../dashboard/dashboard.component';
import { ModalComponent } from '../../../shared/components/modal/modal.component';
import {
  NbAccordionModule,
  NbButtonModule,
  NbCardModule,
  NbIconModule,
  NbToastrService,
} from '@nebular/theme';
import { ChartComponent } from '../../../shared/components/chart/chart.component';
import { CommonModule } from '@angular/common';
import { SkeletonComponent } from '../../../shared/components/skeleton/skeleton.component';
import { Subject } from 'rxjs';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';
import { EpochService } from '../../../core/services/epoch.service';
import { MarkdownModule } from 'ngx-markdown';
import { HasKeyPipe } from '../../../shared/pipes/has-key.pipe';

// Register Chart.js components
Chart.register(...registerables);

@Component({
  selector: 'app-activity-details',
  imports: [
    NbCardModule,
    TableComponent,
    ChartComponent,
    NbButtonModule,
    NbIconModule,
    NbAccordionModule,
    CommonModule,
    SkeletonComponent,
    MarkdownModule,
    HasKeyPipe,
  ],
  templateUrl: './activity-details.component.html',
  styleUrl: './activity-details.component.scss',
})
export class ActivityDetailsComponent implements OnInit {
  proposalDetails: ProposalInfoResponse = {} as ProposalInfoResponse;
  drepApprovalThreshold: any;
  drepApprovalThresholdVote: number = 0;
  drepApprovalThresholdPct: number = 0;
  poolApprovalThreshold: any;
  poolApprovalThresholdVote: number = 0;
  poolApprovalThresholdPct: number = 0;
  totalVoter?: TotalVoter | null = null;
  drepTotalStakeDetails: any;
  spoTotalStakeDetails: any;
  abstract: string = '';
  motivation: string = '';
  rationale: string = '';
  anchorLink: string = '';
  totalStakeNumbers: number = 0;
  totalStake: number = 0;
  drepStakeNotVote: number = 0;
  spoStakeNotVote: number = 0;
  abstainPctDrep: number = 0;
  abstainPctSpo: number = 0;
  supportLink: any;
  isLiveProposal: boolean = false;
  isLoadingData: boolean = true;
  // rationaleHtml: string = '';
  // abstractHtml: string = '';
  // motivationHtml: string = '';

  private lastProposalId: string | null = null;

  protected readonly voteData: ProposalVotes[] = [];
  protected cardInfo: CardInfo[] = [];
  protected currentPage = 1;
  protected itemsPerPage = 10;
  protected totalItems = 0;
  protected activeFilters = new Set<string>();
  protected filteredData: ProposalVotes[] = [];
  protected allVotes: ProposalVotes[] = [];
  searchText = '';

  protected ccInfo = [
    {
      key: 'Cardano Atlantic Council',
      value: 'cc_hot1qvr7p6ms588athsgfd0uez5m9rlhwu3g9dt7wcxkjtr4hhsq6ytv2',
      cc_cold: 'cc_cold1zv6fu40c86d0yjqnum9ndr0k4qxn39gm9ge5mlxly6q42kqmjmzyj',
      vote: '',
      color: '',
      imageUrl: 'assets/icons/cc/atlantic-council.png',
    },
    {
      key: 'Tingvard',
      value: 'cc_hot1qdjx6xe6e9zk3fpzk6rakmz84n0cf8ckwjvz4e8e5j2tuscr7ckq4',
      cc_cold: 'cc_cold1zvvcpkl3443ykr94gyp4nddtzngqs4sejjnv9dk98747cqqeatx67',
      vote: '',
      color: '',
      imageUrl: 'assets/icons/cc/tingvard.jpg',
    },
    {
      key: 'Eastern Cardano Council',
      value: 'cc_hot1qvh20fuwhy2dnz9e6d5wmzysduaunlz5y9n8m6n2xen3pmqqvyw8v',
      cc_cold: 'cc_cold1zwz2a08a8cqdp7r6lyv0cj67qqf47sr7x7vf8hm705ujc6s4m87eh',
      vote: '',
      color: '',
      imageUrl: 'assets/icons/cc/eastern-council.png',
    },
    {
      key: 'KtorZ',
      value: 'cc_hot1qfj0jatguuhl0cqrtd96u7asszssa3h6uhq08q0dgqzn5jgjfy0l0',
      cc_cold: 'cc_cold1ztwq6mh5jkgwk6yq559qptw7zavkumtk7u2e2uh6rlu972slkt0rz',
      vote: '',
      color: '',
      imageUrl: 'assets/icons/cc/ktorz.png',
    },
    {
      key: 'Ace Alliance',
      value: 'cc_hot1qdc65ke6jfq2q25fcn3g89tea30tvrzpptc2tw6g8cdc7pqtmus0y',
      cc_cold: 'cc_cold1zwt49epsdedwsezyr5ssvnmez96v3d3xrxdcu7j9l8srk3g5xu74h',
      vote: '',
      color: '',
      imageUrl: 'assets/icons/cc/ace-alliance.jpg',
    },
    {
      key: 'Cardano Japan Council',
      value: null,
      cc_cold: 'cc_cold1zwwv8uu8vgl5tkhx569hp94sctjq8krqr2pdcspzr6k5rcsxw2az4',
      vote: '',
      color: '',
      imageUrl: 'assets/icons/cc/japan-council.png',
    },
    {
      key: 'Phil_uplc',
      value: null,
      cc_cold: 'cc_cold1zgf5jdusmxcrfqapu8ngf6j04u0wfzjc7sp9wnnlyfr0f4q68as9w',
      vote: '',
      color: '',
      imageUrl: 'assets/icons/cc/phil_uplc.jpg',
    },
  ];

  protected readonly tableColumns: TableColumn[] = [
    { key: 'block_time', title: 'Date' },
    { key: 'name', title: 'Representative' },
    { key: 'voter_role', title: 'Role' },
    { key: 'amount', title: 'Voting Power' },
    { key: 'vote', title: 'Vote' },
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

  @ViewChild('voteAnalysisChart') voteAnalysisChart!: ElementRef;
  public charts: { [key: string]: Chart } = {};
  private chartInitialized = false;
  public isTableLoading = false;

  private searchSubject = new Subject<string>();
  public timelineProgressPercentValue = 0;

  constructor(
    private drepService: DrepService,
    private committeeService: CommitteeService,
    private epochService: EpochService,
    private cdr: ChangeDetectorRef,
    private dialog: MatDialog,
    private router: Router,
    private route: ActivatedRoute,
    private proposalService: ProposalService,
    private toastr: NbToastrService
  ) {
    this.searchSubject.pipe(distinctUntilChanged()).subscribe((term) => {
      this.currentPage = 1;
      const proposalId =
        this.proposalDetails?.proposalId || this.lastProposalId;
      if (typeof proposalId === 'string' && proposalId) {
        const activeFilterArray = Array.from(this.activeFilters);

        // If "All" is selected, send undefined to show all data
        if (activeFilterArray.includes('All')) {
          this.getProposalVotes(proposalId, this.currentPage, undefined, term);
        } else {
          const filterQuery =
            activeFilterArray.length > 0
              ? activeFilterArray.join(',')
              : undefined;
          this.getProposalVotes(
            proposalId,
            this.currentPage,
            filterQuery,
            term
          );
        }
      }
    });
  }

  ngOnInit(): void {
    this.isLoadingData = true;
    this.route.queryParams.subscribe((params) => {
      this.isLiveProposal = params['isLive'] === 'true';
    });
    this.route.params.subscribe((params) => {
      const proposalId = params['id'];
      if (proposalId) {
        this.lastProposalId = proposalId;
        this.loadProposalData(proposalId);
      }
    });
  }

  parseDateTime(dateStr: string): number {
    if (!dateStr) return 0;
    const [date, time] = dateStr.split(' ');
    const [day, month, year] = date.split('/').map(Number);
    const [hour, minute] = time.split(':').map(Number);
    return new Date(year, month - 1, day, hour, minute).getTime();
  }

  updateTimelineProgress() {
    if (!this.proposalDetails?.startTime || !this.proposalDetails?.endTime) {
      this.timelineProgressPercentValue = 0;
      return;
    }
    const start = this.parseDateTime(this.proposalDetails.startTime);
    const end = this.parseDateTime(this.proposalDetails.endTime);
    const now = Date.now();

    if (now <= start) this.timelineProgressPercentValue = 0;
    else if (now >= end) this.timelineProgressPercentValue = 100;
    else
      this.timelineProgressPercentValue = ((now - start) / (end - start)) * 100;
  }

  private loadProposalData(proposalId: string): void {
    if (!proposalId || proposalId.trim() === '') {
      console.error('Invalid proposal ID');
      this.router.navigate(['/activity']);
      return;
    }

    if (this.isLiveProposal) {
      this.proposalService.getProposalLiveById(proposalId).subscribe({
        next: (response) => {
          this.onViewDetails(response[0]);
        },
        error: (err) => {
          console.error('Error fetching live proposal:', err);
          this.router.navigate(['/activity']);
        },
      });
    } else {
      this.proposalService.getProposalExpiredById(proposalId).subscribe({
        next: (response) => {
          const proposal = response.proposal_info?.find(
            (p) => p.proposalId === proposalId
          );
          if (proposal) {
            this.onViewDetails(proposal);
          } else {
            console.error('Proposal not found');
            this.router.navigate(['/activity']);
          }
        },
        error: (err) => {
          console.error('Error fetching expired proposal:', err);
          this.router.navigate(['/activity']);
        },
      });
    }
  }

  async onViewDetails(proposalDetails: ProposalInfoResponse) {
    this.isLoadingData = false;

    // Set proposal details first
    this.proposalDetails = proposalDetails;
    this.updateTimelineProgress();

    // Set basic data that doesn't depend on APIs
    this.totalVoter = proposalDetails.totalVoter;
    this.drepTotalStakeDetails = {
      yesPct: proposalDetails.drepYesPct || 0,
      yesVotes: proposalDetails.drepYesVotes || 0,
      noPct: proposalDetails.drepNoPct || 0,
      noVotes: proposalDetails.drepNoVotes || 0,
      activeNoVotePower: proposalDetails.drepActiveNoVotePower || 0,
      noConfidence: proposalDetails.drepNoConfidence || 0,
      abstainAlways: proposalDetails.drepAbstainAlways || 0,
      abstainActive: proposalDetails.drepAbstainActive || 0,
    };
    this.spoTotalStakeDetails = {
      yesPct: proposalDetails.poolYesPct || 0,
      yesVotes: proposalDetails.poolYesVotes || 0,
      noPct: proposalDetails.poolNoPct || 0,
      noVotes: proposalDetails.poolNoVotes || 0,
      activeNoVotePower: proposalDetails.poolActiveNoVotePower || 0,
      noConfidence: proposalDetails.poolNoConfidence || 0,
      abstainAlways: proposalDetails.poolAbstainAlways || 0,
      abstainActive: proposalDetails.poolAbstainActive || 0,
    };
    this.abstract = proposalDetails.abstract_ || '';
    this.motivation = proposalDetails.motivation || '';
    this.rationale = proposalDetails.rationale || '';
    this.anchorLink = proposalDetails.anchorLink || '';
    this.supportLink = JSON.parse(proposalDetails.supportLink || '{}') || '';

    // Load approval thresholds first
    const epochNo = proposalDetails.proposedEpoch?.toString() || '';
    const proposalType = proposalDetails.proposalType || '';

    this.drepService
      .getDrepTotalStakeApprovalThreshold(epochNo, proposalType)
      .subscribe({
        next: (response) => {
          if (response != null) {
            this.drepApprovalThreshold = response.drepTotalStake
              ? response.drepTotalStake
              : '--';
            this.poolApprovalThreshold = response.poolTotalStake
              ? response.poolTotalStake
              : '--';

            // After approval thresholds are loaded, load stake data and calculate positions
            this.loadStakeDataAndCalculatePositions(proposalDetails);
          } else {
            this.drepApprovalThreshold = '--';
            this.poolApprovalThreshold = '--';
            this.loadStakeDataAndCalculatePositions(proposalDetails);
          }
        },
        error: (err) => {
          console.error('Error fetching approval thresholds:', err);
          this.drepApprovalThreshold = '--';
          this.poolApprovalThreshold = '--';
          this.loadStakeDataAndCalculatePositions(proposalDetails);
        },
      });

    // Load committee votes
    this.ccInfo.forEach((item) => {
      if (item.value && item.value != '') {
        this.committeeService.getCommitteeVotes(item.value).subscribe({
          next: (response) => {
            item.vote =
              response.find((r) => r.proposal_id === proposalDetails.proposalId)
                ?.vote ?? 'Not voted';
            switch (item.vote) {
              case 'Yes':
                item.color = 'vote-yes';
                break;
              case 'No':
                item.color = 'vote-no';
                break;
              case 'Abstain':
                item.color = 'vote-abstain';
                break;
              default:
                item.color = '';
            }
          },
          error: (err) => {
            console.error(`Error fetching votes for ${item.key}:`, err);
          },
        });
      }else{
        item.vote = 'Not authorized';
      }
    });

    // Load proposal votes
    if (this.proposalDetails?.proposalId) {
      this.lastProposalId = this.proposalDetails.proposalId;
      this.currentPage = 1;
      this.getProposalVotes(
        this.lastProposalId,
        this.currentPage,
        undefined,
        this.searchText
      );
    }

    // Initialize chart after data is loaded
    setTimeout(() => {
      this.initializeCharts(proposalDetails);
    }, 0);
  }

  private loadStakeDataAndCalculatePositions(
    proposalDetails: ProposalInfoResponse
  ) {
    // Load DRep stake data
    this.drepService
      .getDrepEpochSummary(proposalDetails.expiration?.toString() || '')
      .subscribe({
        next: (response) => {
          if (response != null) {
            this.totalStakeNumbers = response;
            this.drepStakeNotVote = parseFloat(
              (
                proposalDetails.drepNoVotes ||
                0 -
                  (proposalDetails.drepActiveNoVotePower || 0) -
                  (proposalDetails.drepNoConfidence || 0)
              ).toFixed(2)
            );
            this.abstainPctDrep =
              ((proposalDetails.drepAbstainAlways || 0) /
                ((proposalDetails.drepNoVotes || 0) +
                  (proposalDetails.drepYesVotes || 0))) *
              100;

            // Calculate DRep approval threshold position
            if (
              (proposalDetails.drepYesPct || 0) >= 0 ||
              (proposalDetails.drepNoPct || 0) >= 0
            ) {
              // Only consider Yes + No votes (exclude Abstain) for percentage calculation
              const totalVotedStake =
                (proposalDetails.drepYesVotes || 0) +
                (proposalDetails.drepNoVotes || 0);
              const approvalThreshold =
                parseFloat(this.drepApprovalThreshold) || 0;
              const approvalThresholdStake =
                (totalVotedStake * approvalThreshold) / 100;

              // Calculate percentage position based on Yes + No votes only
              if (totalVotedStake > 0) {
                this.drepApprovalThresholdPct =
                  (approvalThresholdStake / totalVotedStake) * 100;
              } else {
                this.drepApprovalThresholdPct = approvalThreshold;
              }

              this.drepApprovalThresholdVote = parseFloat(
                approvalThresholdStake.toFixed(2)
              );

              // Debug log
              console.log('DRep Debug:', {
                totalVotedStake,
                approvalThreshold: approvalThreshold,
                approvalThresholdStake,
                approvalThresholdPct: this.drepApprovalThresholdPct,
                yesVotes: proposalDetails.drepYesVotes,
                noVotes: proposalDetails.drepNoVotes,
                yesPct: proposalDetails.drepYesPct,
                noPct: proposalDetails.drepNoPct,
              });
            }
          }
        },
        error: (err) => {
          console.error('Error fetching DRep stake data:', err);
        },
      });

    // Load SPO stake data
    this.epochService
      .getEpochInfoSpo(proposalDetails.expiration || 0)
      .subscribe({
        next: (response) => {
          if (response != null) {
            this.totalStake = response;
            this.spoStakeNotVote = parseFloat(
              (
                (proposalDetails.poolNoVotes || 0) -
                (proposalDetails.poolActiveNoVotePower || 0) -
                (proposalDetails.poolNoConfidence || 0)
              ).toFixed(2)
            );
            this.abstainPctSpo =
              ((proposalDetails.poolAbstainAlways || 0) /
                ((proposalDetails.poolNoVotes || 0) +
                  (proposalDetails.poolYesVotes || 0))) *
              100;

            // Calculate SPO approval threshold position
            if (
              (proposalDetails.poolYesPct || 0) >= 0 ||
              (proposalDetails.poolNoPct || 0) >= 0
            ) {
              // Only consider Yes + No votes (exclude Abstain) for percentage calculation
              const totalVotedStake =
                (proposalDetails.poolYesVotes || 0) +
                (proposalDetails.poolNoVotes || 0);
              const approvalThreshold =
                parseFloat(this.poolApprovalThreshold) || 0;
              const approvalThresholdStake =
                (totalVotedStake * approvalThreshold) / 100;

              // Calculate percentage position based on Yes + No votes only
              if (totalVotedStake > 0) {
                this.poolApprovalThresholdPct =
                  (approvalThresholdStake / totalVotedStake) * 100;
              } else {
                this.poolApprovalThresholdPct = approvalThreshold;
              }

              this.poolApprovalThresholdVote = parseFloat(
                approvalThresholdStake.toFixed(2)
              );

              // Debug log
              console.log('SPO Debug:', {
                totalVotedStake,
                approvalThreshold: approvalThreshold,
                approvalThresholdStake,
                approvalThresholdPct: this.poolApprovalThresholdPct,
                yesVotes: proposalDetails.poolYesVotes,
                noVotes: proposalDetails.poolNoVotes,
                yesPct: proposalDetails.poolYesPct,
                noPct: proposalDetails.poolNoPct,
              });
            }
          }
        },
        error: (err) => {
          console.error('Error fetching SPO stake data:', err);
        },
      });
  }

  getProposalVotes(
    proposalId: string | null,
    page: number,
    filter?: string,
    searchText?: string
  ) {
    if (!proposalId) {
      return;
    }
    this.isTableLoading = true;
    this.proposalService
      .getProposalVotes(proposalId, page, filter, searchText)
      .subscribe({
        next: (response) => {
          response.voteInfo?.forEach((item: any) => {
            item.amount = item.voting_power;
          });
          this.filteredData = response.voteInfo || [];
          this.totalItems = response.totalVotesResult || 0;
          this.isTableLoading = false;
          this.cdr.detectChanges();
        },
        error: (err) => {
          console.error('Error fetching proposal votes:', err);
          this.isTableLoading = false;
        },
      });
  }

  // details
  private initializeCharts(proposalDetails: any): void {
    if (!this.voteAnalysisChart?.nativeElement) {
      return;
    }

    const ctx = this.voteAnalysisChart.nativeElement.getContext('2d');
    if (!ctx) {
      return;
    }
    const { proposalType } = proposalDetails;
    const noSPO = [
      'InfoAction',
      'HardForkInitiation',
      'NewCommittee',
      'NoConfidence',
      'ParameterChange',
    ].includes(proposalType)
      ? 0
      : 100;
    const noCC = ['NoConfidence', 'NewCommittee'].includes(proposalType)
      ? 100
      : 0;

    const data = {
      labels: ['CC', 'Drep', 'No CC', 'SPO', 'No SPO'],
      datasets: [
        {
          label: 'Vote',
          data: [
            proposalDetails.committeeYesPct,
            proposalDetails.drepYesPct,
            noCC,
            proposalDetails.poolYesPct,
            noSPO,
          ],
          fill: true,
          backgroundColor: 'rgba(144, 238, 144, 0.2)',
          borderColor: 'rgba(144, 238, 144, 1)',
          pointBackgroundColor: 'rgba(144, 238, 144, 1)',
          pointBorderColor: '#fff',
          pointHoverBackgroundColor: '#fff',
          pointHoverBorderColor: 'rgba(144, 238, 144, 1)',
        },
      ],
    };

    const blueLinePlugin = {
      id: 'blueLinePlugin',
      afterDatasetDraw(chart: Chart, args: any, options: any) {
        if (args.index !== 0) return;
        const { ctx, chartArea, data } = chart;
        const meta = chart.getDatasetMeta(0);
        ctx.save();
        ctx.strokeStyle = '#0074D9';
        ctx.lineWidth = 4;
        ctx.beginPath();
        meta.data.forEach((point: any, i: number) => {
          const { x, y } = point.getProps(['x', 'y'], true);
          if (i === 0) {
            ctx.moveTo(x, y);
          } else {
            ctx.lineTo(x, y);
          }
        });
        ctx.closePath();
        ctx.stroke();
        ctx.restore();
      },
    };

    const config: ChartConfiguration = {
      type: 'radar',
      data: data,
      options: {
        responsive: true,
        maintainAspectRatio: false,
        scales: {
          r: {
            beginAtZero: true,
            max: 100,
            ticks: {
              stepSize: 10,
              display: false,
            },
            grid: {
              color: 'rgba(0, 0, 0, 0.1)',
            },
            angleLines: {
              color: 'rgba(0, 0, 0, 0.1)',
            },
            pointLabels: {
              font: {
                size: 16,
                family: 'Arial',
                weight: 'bold',
              },
              color: '#222',
            },
          },
        },
        plugins: {
          legend: {
            display: false,
          },
        },
      },
      plugins: [blueLinePlugin],
    };

    try {
      if (this.charts['voteAnalysisChart']) {
        this.charts['voteAnalysisChart'].destroy();
      }

      this.charts['voteAnalysisChart'] = new Chart(ctx, config);
      this.chartInitialized = true;
      this.cdr.detectChanges();
    } catch (error) {
      console.error('Error initializing Vote Statistics Chart:', error);
    }
  }

  get totalVoterCount(): number {
    const cc = this.totalVoter?.cc;
    const drep = this.totalVoter?.drep;
    const spo = this.totalVoter?.spo;

    return (
      (cc?.yes || 0) +
      (cc?.no || 0) +
      (cc?.abstain || 0) +
      (drep?.yes || 0) +
      (drep?.no || 0) +
      (drep?.abstain || 0) +
      (spo?.yes || 0) +
      (spo?.no || 0) +
      (spo?.abstain || 0)
    );
  }

  protected onPageChange(event: { page: number; itemsPerPage: number }): void {
    this.currentPage = event.page;
    const proposalId = this.proposalDetails?.proposalId || this.lastProposalId;

    const activeFilterArray = Array.from(this.activeFilters);

    // If "All" is selected, send undefined to show all data
    if (activeFilterArray.includes('All')) {
      this.getProposalVotes(
        proposalId,
        this.currentPage,
        undefined,
        this.searchText
      );
    } else {
      const filterQuery =
        activeFilterArray.length > 0 ? activeFilterArray.join(',') : undefined;
      this.getProposalVotes(
        proposalId,
        this.currentPage,
        filterQuery,
        this.searchText
      );
    }
  }

  protected onSearchChange(searchTerm: string): void {
    this.searchText = searchTerm;
    this.searchSubject.next(searchTerm);
  }

  protected onFilterChange(filters: Set<string>): void {
    this.activeFilters = filters;
    this.currentPage = 1;
    const proposalId = this.proposalDetails?.proposalId || this.lastProposalId;

    const activeFilterArray = Array.from(this.activeFilters);

    // If "All" is selected, send undefined to show all data
    if (activeFilterArray.includes('All')) {
      this.getProposalVotes(
        proposalId,
        this.currentPage,
        undefined,
        this.searchText
      );
    } else {
      const filterQuery =
        activeFilterArray.length > 0 ? activeFilterArray.join(',') : undefined;
      this.getProposalVotes(
        proposalId,
        this.currentPage,
        filterQuery,
        this.searchText
      );
    }
  }

  onVotingClick(): void {
    this.router.navigate(['/voting']);
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

  milestonePercent(
    aPercent: number,
    yesPct: number,
    noPct: number,
    abstainPct: number
  ): number {
    const sumBC = yesPct + noPct;
    const total = sumBC + abstainPct;
    if (total === 0 || sumBC === 0) return 0; // tránh chia cho 0, a khi đó không có ý nghĩa
    return aPercent * (sumBC / total);
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

  formatValue(value: any): string {
    if (value === null || value === undefined) {
      return '';
    }

    return formatValue(value, 0);
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
