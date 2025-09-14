import { Injectable } from '@angular/core';
import { Observable, map } from 'rxjs';
import { ApiService } from './api.service';
import { CacheService } from './cache.service';

export interface Proposal {
  id?: string;
  title?: string;
  description?: string;
  status?: string;
  start_date?: string;
  end_date?: string;
  voting_power?: number;
  total_votes?: number;
  yes_votes?: number;
  no_votes?: number;
  abstain_votes?: number;
  created_by?: string;
  created_at?: string;
  updated_at?: string;
}

export interface ProposalResponse {
  data?: Proposal[];
  total?: number;
  page?: number;
  page_size?: number;
}

export interface ProposalStatsResponse {
  totalProposals?: number;
  approvedProposals?: number;
  approvalRate?: number;
  percentage_change?: number;
  difference?: number;
  live_proposals?: number;
  expired_proposals?: number;
  enacted_proposals?: number;
}

export interface VoterCounts {
  yes?: number;
  no?: number;
  abstain?: number;
  abstainAlways?: number;
}

export interface TotalVoter {
  drep?: VoterCounts;
  spo?: VoterCounts;
  cc?: VoterCounts;
}

export interface ProposalInfoResponse {
  proposalId?: string;
  proposedEpoch?: number;
  expiration?: number;
  imageUrl?: string;
  title?: string;
  proposalType?: string;
  abstract_?: string;
  hash?: string;
  status?: string;
  motivation?: string;
  rationale?: string;
  anchorLink?: string;
  supportLink?: string;
  timeLine?: string;
  time?: string;
  deposit?: number;
  startTime?: string;
  endTime?: string;
  totalVoter?: TotalVoter;
  drepYesVotes?: number;
  drepYesPct?: number;
  drepNoVotes?: number;
  drepNoPct?: number;
  drepActiveNoVotePower?: number;
  drepNoConfidence?: number;
  drepAbstainAlways?: number;
  drepAbstainActive?: number;
  poolYesVotes?: number;
  poolYesPct?: number;
  poolNoVotes?: number;
  poolNoPct?: number;
  poolActiveNoVotePower?: number;
  poolNoConfidence?: number;
  poolAbstainAlways?: number;
  poolAbstainActive?: number;
  committeeYesPct?: number;
  param_proposal?: string;
  block_time?:number;
}

export interface GovernanceActionResponse {
  total_proposals?: number;
  approved_proposals?: number;
  percentage_change?: number;
  proposal_info?: ProposalInfoResponse[];
}

export enum ProposalType {
  ParameterChange = 'ParameterChange',
  HardForkInitiation = 'HardForkInitiation',
  TreasuryWithdrawals = 'TreasuryWithdrawals',
  NoConfidence = 'NoConfidence',
  NewCommittee = 'NewCommittee',
  NewConstitution = 'NewConstitution',
  InfoAction = 'InfoAction',
}

export interface ProposalVotingSummaryResponse {
  proposal_type?: string;
  epoch_no?: number;
  drep_yes_votes_cast?: number;
  drep_active_yes_vote_power?: string;
  drep_yes_vote_power?: string;
  drep_yes_pct?: number;
  drep_no_votes_cast?: number;
  drep_active_no_vote_power?: string;
  drep_no_vote_power?: string;
  drep_no_pct?: number;
  drep_abstain_votes_cast?: number;
  drep_active_abstain_vote_power?: string;
  drep_always_no_confidence_vote_power?: string;
  drep_always_abstain_vote_power?: string;
  pool_yes_votes_cast?: number;
  pool_active_yes_vote_power?: string;
  pool_yes_vote_power?: string;
  pool_yes_pct?: number;
  pool_no_votes_cast?: number;
  pool_active_no_vote_power?: string;
  pool_no_vote_power?: string;
  pool_no_pct?: number;
  pool_abstain_votes_cast?: number;
  pool_active_abstain_vote_power?: string;
  pool_passive_always_abstain_votes_assigned?: number;
  pool_passive_always_abstain_vote_power?: string;
  pool_passive_always_no_confidence_votes_assigned?: number;
  pool_passive_always_no_confidence_vote_power?: string;
  committee_yes_votes_cast?: number;
  committee_yes_pct?: number;
  committee_no_votes_cast?: number;
  committee_no_pct?: number;
  committee_abstain_votes_cast?: number;
}

export interface GovernanceActionsStatisticsResponse {
  statistics?: { [key: string]: number };
}

export interface GovernanceActionsStatisticsByEpochResponse {
  statistics_by_epoch?: { [key: string]: { [key: string]: number } };
}

export enum VoterRole {
  Drep = 'DRep',
  Pool = 'SPO',
  CC = 'ConstitutionalCommittee',
}

export enum Vote {
  Yes = 'Yes',
  No = 'No',
  Abstain = 'Abstain',
}

export interface ProposalVotingResponse {
  block_time?: string;
  voter_role?: string;
  voter_id?: string;
  voter_hex?: string;
  voter_has_script?: boolean;
  vote?: string;
  meta_url?: string;
  meta_hash?: string;
  proposal_type?: string;
  proposal_id?: string;
}

export interface ProposalVotesResponse {
  totalVotesResult?: number;
  voteInfo?: ProposalVotes[] | any;
}

export interface ProposalVotes {
  block_time?: string;
  name?: string;
  voter_role?: string;
  voting_power?: number;
  vote?: string;
  voter_id?: string;
}

export interface ProposalActionTypeResponse {
  proposal_id?: string;
  proposal_type?: string;
  meta_json?: any;
}

@Injectable({
  providedIn: 'root',
})
export class ProposalService extends CacheService {
  constructor(private masterApiService: ApiService) {
    super();
  }

  /**
   * Get all governance actions
   * @returns Observable of GovernanceActionResponse
   */
  getProposals(): Observable<GovernanceActionResponse> {
    const cacheKey = 'proposals';
    const source$ = this.masterApiService
      .get<{ data: GovernanceActionResponse }>(`proposals`)
      .pipe(map((response) => response.data));
    return this.getCachedData<GovernanceActionResponse>(cacheKey, source$);
  }

  getProposalExpired(): Observable<GovernanceActionResponse> {
    const cacheKey = 'proposalsExpired';
    const source$ = this.masterApiService
      .get<{ data: GovernanceActionResponse }>(`proposals?isLive=false`)
      .pipe(map((response) => response.data));
    return this.getCachedData<GovernanceActionResponse>(cacheKey, source$);
  }

  getProposalLive(): Observable<ProposalInfoResponse[]> {
    const cacheKey = 'proposalsLive';
    const source$ = this.masterApiService
      .get<{ data: GovernanceActionResponse }>(`proposals?isLive=true`)
      .pipe(map((response) => response.data.proposal_info ?? []));
    return this.getCachedData<ProposalInfoResponse[]>(cacheKey, source$);
  }

  getProposalVotingSummary(
    govId: string
  ): Observable<ProposalVotingSummaryResponse> {
    const cacheKey = `proposalVotingSummary-${govId}`;
    const source$ = this.masterApiService
      .get<{ data: ProposalVotingSummaryResponse }>(
        `proposal_voting_summary/${govId}`
      )
      .pipe(map((response) => response.data));
    return this.getCachedData<ProposalVotingSummaryResponse>(cacheKey, source$);
  }

  getProposalActionType(): Observable<ProposalActionTypeResponse[]> {
    const cacheKey = 'proposalActionType';
    const source$ = this.masterApiService
      .get<{ data: ProposalActionTypeResponse[] }>(`proposal_action_type`)
      .pipe(map((response) => response.data));
    return this.getCachedData<ProposalActionTypeResponse[]>(cacheKey, source$);
  }

  getProposalStats(): Observable<ProposalStatsResponse> {
    const cacheKey = 'proposalStats';
    const source$ = this.masterApiService
      .get<{ data: ProposalStatsResponse }>(`proposal_stats`)
      .pipe(map((response) => response.data));
    return this.getCachedData<ProposalStatsResponse>(cacheKey, source$);
  }

  getGovernanceActionsStatistics(): Observable<GovernanceActionsStatisticsResponse> {
    const cacheKey = 'governanceActionsStatistics';
    const source$ = this.masterApiService
      .get<{ data: GovernanceActionsStatisticsResponse }>(
        `governance_actions_statistics`
      )
      .pipe(map((response) => response.data));
    return this.getCachedData<GovernanceActionsStatisticsResponse>(
      cacheKey,
      source$
    );
  }

  getGovernanceActionsStatisticsByEpoch(): Observable<GovernanceActionsStatisticsByEpochResponse> {
    const cacheKey = 'governanceActionsStatisticsByEpoch';
    const source$ = this.masterApiService
      .get<{ data: GovernanceActionsStatisticsByEpochResponse }>(
        `governance_actions_statistics_by_epoch`
      )
      .pipe(map((response) => response.data));
    return this.getCachedData<GovernanceActionsStatisticsByEpochResponse>(
      cacheKey,
      source$
    );
  }

  getProposalVotes(
    proposalId: string,
    page: number,
    filter?: string,
    search?: string
  ): Observable<ProposalVotesResponse> {
    let url = `proposal_votes/${proposalId}?page=${page}`;
    if (filter) {
      url += `&filter=${filter}`;
    }
    if (search) {
      url += `&search=${search}`;
    }
    const cacheKey = `proposalVotes-${proposalId}-p${page}-f${filter || ''}-s${
      search || ''
    }`;
    const source$ = this.masterApiService
      .get<{ data: ProposalVotesResponse }>(url)
      .pipe(map((response) => response.data));
    return this.getCachedData<ProposalVotesResponse>(cacheKey, source$);
  }

  getProposalExpiredById(id: string): Observable<GovernanceActionResponse> {
    const cacheKey = `proposalExpiredById-${id}`;
    const source$ = this.masterApiService
      .get<{ data: GovernanceActionResponse }>(`proposal_expired_detail/${id}`)
      .pipe(map((response) => response.data));
    return this.getCachedData<GovernanceActionResponse>(cacheKey, source$);
  }

  getProposalLiveById(id: string): Observable<ProposalInfoResponse[]> {
    const cacheKey = `proposalLiveById-${id}`;
    const source$ = this.masterApiService
      .get<{ data: ProposalInfoResponse[] }>(`proposal_live_detail/${id}`)
      .pipe(map((response) => response.data));
    return this.getCachedData<ProposalInfoResponse[]>(cacheKey, source$);
  }
}
