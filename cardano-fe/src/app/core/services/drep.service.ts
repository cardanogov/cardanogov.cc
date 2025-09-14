import { Injectable } from '@angular/core';
import { Observable, map } from 'rxjs';
import { ApiService } from './api.service';
import { Vote } from './proposal.service';
import { HttpParams } from '@angular/common/http';
import { CacheService } from './cache.service';

export interface TotalDrepResponse {
  total_active?: number;
  total_no_confidence?: number;
  total_abstain?: number;
  total_register?: number;
  chart_stats?: number[];
}

export interface DrepInfoResponse {
  drep_id?: string;
  hex?: string;
  has_script?: boolean;
  registered?: boolean;
  deposit?: any;
  active?: boolean;
  expires_epoch_no?: number;
  amount?: string;
  meta_url?: any;
  meta_hash?: any;
}

export interface DrepVotingPowerHistoryResponse {
  givenName?: string;
  drep_id?: string;
  amount?: number;
  percentage?: number;
}

export interface DrepDelegatorsResponse {
  delegators?: any[];
  live_delegators?: any[];
  amounts?: any[];
}

export interface DrepPoolVotingThresholdResponse {
  motion_no_confidence?: number;
  committee_normal?: number;
  committee_no_confidence?: number;
  hard_fork_initiation?: number;
  update_to_constitution?: number;
  network_param_voting?: number;
  economic_param_voting?: number;
  technical_param_voting?: number;
  governance_param_voting?: number;
  treasury_withdrawal?: number;
  pool_motion_no_confidence?: number;
  pool_committee_normal?: number;
  pool_committee_no_confidence?: number;
  pool_hard_fork_initiation?: number;
}

export interface DrepCardDataResponse {
  dreps?: number;
  drepsChange?: string;
  totalDelegatedDrep?: number;
  totalDelegatedDrepChange?: string;
  currentTotalActive?: number;
  totalActiveChange?: string;
}

export interface DrepCardDataByIdResponse {
  givenName?: string;
  votingPower?: number;
  previousVotingPower?: number;
  votingPowerChange?: number;
  image?: any;
  objectives?: string;
  motivations?: string;
  qualifications?: string;
  references?: string;
  registrationDate?: string;
}

export interface References {
  uri?: any;
  type?: string;
  label?: string;
}

export interface DrepVoteInfoResponse {
  proposal_id?: string;
  proposal_title?: string;
  proposal_type?: string;
  vote?: Vote;
  meta_url?: string;
  block_time?: string;
}

export interface DrepDelegationResponse {
  total_delegators?: number;
  delegation_data?: DrepDelegationTableResponse[];
}

export interface DrepDelegationTableResponse {
  stake_address?: string;
  block_time?: number;
  amount?: number;
}

export interface DrepRegistrationTableResponse {
  block_time?: number;
  given_name?: string;
  action?: string;
  meta_url?: string;
}

export interface DrepDetailsVotingPowerResponse {
  epoch_no?: number;
  amount?: number;
}

export interface DrepListResponse {
  total_dreps?: number;
  drep_info?: DrepList[];
}

export interface DrepList {
  name?: string;
  drep_id?: string;
  voting_power?: number;
  delegators?: number;
  active_until?: string;
  image?: string;
  times_voted?: number;
  status?: string;
  contact?: any[];
}

export interface VotingPowerData {
  epoch_no?: number;
  voting_power?: number;
}

export interface DrepsVotingPowerResponse {
  abstain_data?: VotingPowerData[];
  no_confident_data?: VotingPowerData[];
  total_drep_data?: VotingPowerData[];
}

export interface DrepNewRegisterResponse {
  block_time?: number;
  drep_id?: string;
  action?: string;
}

@Injectable({
  providedIn: 'root',
})
export class DrepService extends CacheService {
  constructor(private masterApiService: ApiService) {
    super();
  }

  /**
   * Get all account list
   * @returns Observable of Account
   */
  getTotalDrep(): Observable<number> {
    const cacheKey = 'totalDrep';
    const source$ = this.masterApiService
      .get<{ data: number }>(`total_drep`)
      .pipe(map((response) => response.data));
    return this.getCachedData<number>(cacheKey, source$);
  }

  /**
   * Get total stake numbers
   * @returns Observable of number
   */
  getTotalStakeNumbers(): Observable<TotalDrepResponse> {
    const cacheKey = 'totalStakeNumbers';
    const source$ = this.masterApiService
      .get<{ data: TotalDrepResponse }>(`total_stake_numbers`)
      .pipe(map((response) => response.data));
    return this.getCachedData<TotalDrepResponse>(cacheKey, source$);
  }

  getDrepInfo(drepIds: string): Observable<DrepInfoResponse> {
    const cacheKey = `drepInfo-${drepIds}`;
    const source$ = this.masterApiService
      .get<{ data: DrepInfoResponse }>(`drep_info/${drepIds}`)
      .pipe(map((response) => response.data));
    return this.getCachedData<DrepInfoResponse>(cacheKey, source$);
  }

  getDrepVotingPowerHistory(): Observable<DrepVotingPowerHistoryResponse[]> {
    const cacheKey = 'drepVotingPowerHistory';
    const source$ = this.masterApiService
      .get<{ data: DrepVotingPowerHistoryResponse[] }>(`drep_voting_power_history`)
      .pipe(map((response) => response.data));
    return this.getCachedData<DrepVotingPowerHistoryResponse[]>(cacheKey, source$);
  }

  getDrepDelegators(): Observable<DrepDelegatorsResponse> {
    const cacheKey = 'drepDelegators';
    const source$ = this.masterApiService
      .get<{ data: DrepDelegatorsResponse }>(`total_wallet_stastics`)
      .pipe(map((response) => response.data));
    return this.getCachedData<DrepDelegatorsResponse>(cacheKey, source$);
  }

  getDrepVotingThreshold(): Observable<DrepPoolVotingThresholdResponse> {
    const cacheKey = 'drepVotingThreshold';
    const source$ = this.masterApiService
      .get<{ data: DrepPoolVotingThresholdResponse }>(`drep_and_pool_voting_threshold`)
      .pipe(map((response) => response.data));
    return this.getCachedData<DrepPoolVotingThresholdResponse>(cacheKey, source$);
  }

  getTop10DrepVotingPower(): Observable<DrepVotingPowerHistoryResponse[]> {
    const cacheKey = 'top10DrepVotingPower';
    const source$ = this.masterApiService
      .get<{ data: DrepVotingPowerHistoryResponse[] }>(`top_10_drep_voting_power`)
      .pipe(map((response) => response.data));
    return this.getCachedData<DrepVotingPowerHistoryResponse[]>(cacheKey, source$);
  }

  getDrepCardData(): Observable<DrepCardDataResponse> {
    const cacheKey = 'drepCardData';
    const source$ = this.masterApiService
      .get<{ data: DrepCardDataResponse }>(`drep_card_data`)
      .pipe(map((response) => response.data));
    return this.getCachedData<DrepCardDataResponse>(cacheKey, source$);
  }

  getDrepCardDataById(drepId: string): Observable<DrepCardDataByIdResponse> {
    const cacheKey = `drepCardDataById-${drepId}`;
    const source$ = this.masterApiService
      .get<{ data: DrepCardDataByIdResponse }>(`drep_card_data_by_id/${drepId}`)
      .pipe(map((response) => response.data));
    return this.getCachedData<DrepCardDataByIdResponse>(cacheKey, source$);
  }

  getDrepVoteInfo(drepId: string): Observable<DrepVoteInfoResponse[]> {
    const cacheKey = `drepVoteInfo-${drepId}`;
    const source$ = this.masterApiService
      .get<{ data: DrepVoteInfoResponse[] }>(`drep_vote_info/${drepId}`)
      .pipe(map((response) => response.data));
    return this.getCachedData<DrepVoteInfoResponse[]>(cacheKey, source$);
  }

  getDrepDelegatorsTable(drepId: string): Observable<DrepDelegationResponse> {
    const cacheKey = `drepDelegatorsTable-${drepId}`;
    const source$ = this.masterApiService
      .get<{ data: DrepDelegationResponse }>(`drep_delegation/${drepId}`)
      .pipe(map((response) => response.data));
    return this.getCachedData<DrepDelegationResponse>(cacheKey, source$);
  }

  getDrepRegistrationTable(
    drepId: string
  ): Observable<DrepRegistrationTableResponse[]> {
    const cacheKey = `drepRegistrationTable-${drepId}`;
    const source$ = this.masterApiService
      .get<{ data: DrepRegistrationTableResponse[] }>(`drep_registration/${drepId}`)
      .pipe(map((response) => response.data));
    return this.getCachedData<DrepRegistrationTableResponse[]>(cacheKey, source$);
  }

  getDrepDetailsVotingPower(
    drepId: string
  ): Observable<DrepDetailsVotingPowerResponse[]> {
    const cacheKey = `drepDetailsVotingPower-${drepId}`;
    const source$ = this.masterApiService.get<{ data: DrepDetailsVotingPowerResponse[] }>(
        `drep_details_voting_power/${drepId}`
      )
      .pipe(
        map((response) => response.data)
      );
    return this.getCachedData<DrepDetailsVotingPowerResponse[]>(cacheKey, source$);
  }

  getDrepList(
    page: number = 1,
    search?: string,
    status?: 'active' | 'inactive' | null
  ): Observable<DrepListResponse> {
    let params = new HttpParams().set('page', page);

    if (status !== undefined && status !== null) {
      params = params.set('status', status);
    }

    if (search) {
      params = params.set('search', search);
    }

    const cacheKey = `drepList-p${page}-s${search || ''}-st${status || ''}`;
    const source$ = this.masterApiService
      .get<{ data: DrepListResponse }>(`drep_list`, params)
      .pipe(map((response) => response.data));
    return this.getCachedData<DrepListResponse>(cacheKey, source$);
  }

  getDrepsVotingPower(): Observable<DrepsVotingPowerResponse> {
    const cacheKey = 'drepsVotingPower';
    const source$ = this.masterApiService
      .get<{ data: DrepsVotingPowerResponse }>(`dreps_voting_power`)
      .pipe(map((response) => response.data));
    return this.getCachedData<DrepsVotingPowerResponse>(cacheKey, source$);
  }

  getDrepTotalStakeApprovalThreshold(
    epochNo: string,
    proposalType: string
  ): Observable<any> {
    const cacheKey = `drepTotalStakeApprovalThreshold-${epochNo}-${proposalType}`;
    const source$ = this.masterApiService.get<{ data: any }>(
        `drep_total_stake_approval_threshold/${epochNo}/${proposalType}`
      )
      .pipe(
        map((response) => response.data)
      );
    return this.getCachedData<any>(cacheKey, source$);
  }

  getDrepNewRegister(): Observable<DrepNewRegisterResponse[]> {
    const cacheKey = 'drepNewRegister';
    const source$ = this.masterApiService
      .get<{ data: DrepNewRegisterResponse[] }>(`dreps_new_register`)
      .pipe(map((response) => response.data));
    return this.getCachedData<DrepNewRegisterResponse[]>(cacheKey, source$);
  }

  getDrepEpochSummary(epochNo: string): Observable<any> {
    const cacheKey = `drepEpochSummary-${epochNo}`;
    const source$ = this.masterApiService.get<{ data: any }>(
        `drep_epoch_summary/${epochNo}`
      )
      .pipe(
        map((response) => response.data)
      );
    return this.getCachedData<any>(cacheKey, source$);
  }
}
