import { Injectable } from '@angular/core';
import { Observable, map } from 'rxjs';
import { ApiService } from './api.service';
import { CacheService } from './cache.service';
import { HttpParams } from '@angular/common/http';

export interface TotalInfoResponse {
  epoch_no?: number;
  circulation?: string;
  treasury?: string;
  reward?: string;
  supply?: string;
  reserves?: string;
  fees?: string;
  deposits_stake?: string;
  deposits_drep?: string;
  deposits_proposal?: string;
}

export interface SpoVotingPowerHistoryResponse {
  ticker?: string;
  active_stake?: number;
  pool_status?: string;
  percentage?: number;
  group?: string;
}

export interface PoolResult {
  epoch_no?: number;
  total_active_stake?: string;
}

export interface DrepResult {
  epoch_no?: number;
  amount?: string;
}

export interface SupplyResult {
  epoch_no?: number;
  supply?: string;
}

export interface AdaStatisticsResponse {
  pool_result?: PoolResult[];
  drep_result?: DrepResult[];
  supply_result?: SupplyResult[];
}

export interface AdaStatisticsPercentageResponse {
  ada_staking?: string;
  ada_staking_percentage?: number;
  ada_register_to_vote?: string;
  ada_register_to_vote_percentage?: number;
  circulating_supply?: string;
  circulating_supply_percentage?: number;
  ada_abstain?: string;
  ada_abstain_percentage?: number;
}

export interface Pool {
  ticker?: string;
  pool_status?: string;
  active_stake?: number;
  pool_id_bech32?: string;
  active_epoch_no?: number;
  meta_url?: string;
  delegator?: number;
  voting_amount?: number;
  block_time?: number;
  vote?: number;
  homepage?: string;
  status?: string;
  references?: string;
}

export interface VotingPower {
  epoch_no?: number;
  amount?: number;
}

export interface PoolStatus {
  registration?: number;
  last_activity?: number;
  status?: string;
}

export interface PoolInformation {
  description?: string;
  name?: string;
  ticker?: string;
  live_stake?: number;
  deposit?: number;
  margin?: number;
  fixed_cost?: number;
  active_epoch_no?: number;
  block_count?: number;
  created?: number;
  delegators?: number;
}

export interface VoteInfo {
  proposal_id?: string;
  title?: string;
  proposal_type?: string;
  block_time?: number;
  vote?: string;
  meta_url?: string;
}

export interface Delegation {
  stake_address?: string;
  amount?: number;
  latest_delegation_tx_hash?: string;
  block_time?: number;
}

export interface Registration {
  block_time?: number;
  ticker?: string;
  meta_url?: string;
}

export interface PoolInfo {
  ticker?: string;
  pool_id_bech32?: string;
  voting_power?: VotingPower[];
  status?: PoolStatus;
  information?: PoolInformation;
  vote_info?: VoteInfo[];
  delegation?: Delegation[];
  registration?: Registration[];
}

export interface PoolResponse {
  items?: Pool[];
  total?: number;
  pageNumber?: number;
  pageSize?: number;
}

export interface DelegationResponse {
  items?: Delegation[];
  total?: number;
  pageNumber?: number;
  pageSize?: number;
}

@Injectable({
  providedIn: 'root',
})
export class PoolService extends CacheService {
  constructor(private masterApiService: ApiService) {
    super();
  }

  /**
   * Get all account list
   * @returns Observable of Account
   */
  getTotalPool(): Observable<number> {
    const cacheKey = 'totalPool';
    const source$ = this.masterApiService
      .get<{ data: number }>(`total_pool`)
      .pipe(map(response => response.data));
    return this.getCachedData<number>(cacheKey, source$);
  }

  getTotals(epochNo: number): Observable<TotalInfoResponse[]> {
    const cacheKey = `totals-${epochNo}`;
    const source$ = this.masterApiService
      .get<{ data: TotalInfoResponse[] }>(`totals/${epochNo}`)
      .pipe(map(response => response.data));
    return this.getCachedData<TotalInfoResponse[]>(cacheKey, source$);
  }

  getSpoVotingPowerHistory(): Observable<SpoVotingPowerHistoryResponse[]> {
    const cacheKey = 'spoVotingPowerHistory';
    const source$ = this.masterApiService
      .get<{ data: SpoVotingPowerHistoryResponse[] }>(`spo_voting_power_history`)
      .pipe(map(response => response.data));
    return this.getCachedData<SpoVotingPowerHistoryResponse[]>(cacheKey, source$);
  }

  getAdaStatistics(): Observable<AdaStatisticsResponse> {
    const cacheKey = 'adaStatistics';
    const source$ = this.masterApiService
      .get<{ data: AdaStatisticsResponse }>(`ada_statistics`)
      .pipe(map(response => response.data));
    return this.getCachedData<AdaStatisticsResponse>(cacheKey, source$);
  }

  getAdaStatisticsPercentage(): Observable<AdaStatisticsPercentageResponse> {
    const cacheKey = 'adaStatisticsPercentage';
    const source$ = this.masterApiService
      .get<{ data: AdaStatisticsPercentageResponse }>(`ada_statistics_percentage`)
      .pipe(map(response => response.data));
    return this.getCachedData<AdaStatisticsPercentageResponse>(cacheKey, source$);
  }

  getPools(
    page: number = 1,
    search?: string,
    status?: 'active' | 'inactive' | null
  ): Observable<PoolResponse> {
    let params = new HttpParams().set('page', page);

    if (status !== undefined && status !== null) {
      params = params.set('status', status);
    }

    if (search) {
      params = params.set('search', search);
    }

    const cacheKey = `pools-p${page}-s${search || ''}-st${status || ''}`;
    const source$ = this.masterApiService
      .get<{ data: PoolResponse }>(`pool_list`, params)
      .pipe(map(response => response.data));
    return this.getCachedData<PoolResponse>(cacheKey, source$);
  }

  getPoolInfo(poolId: string): Observable<PoolInfo> {
    const cacheKey = `poolInfo-${poolId}`;
    const source$ = this.masterApiService
      .get<{ data: PoolInfo }>(`pool_info/${poolId}`)
      .pipe(map(response => response.data));
    return this.getCachedData<PoolInfo>(cacheKey, source$);
  }

  getPoolDelegation(
    poolId: string,
    page: number = 1,
    pageSize: number = 20,
    sortBy?: string,
    sortOrder?: string
  ): Observable<DelegationResponse> {
    let params = new HttpParams()
      .set('page', page)
      .set('pageSize', pageSize);

    if (sortBy) {
      params = params.set('sortBy', sortBy);
    }

    if (sortOrder) {
      params = params.set('sortOrder', sortOrder);
    }

    const cacheKey = `poolDelegation-${poolId}-p${page}-ps${pageSize}-sb${sortBy || ''}-so${sortOrder || ''}`;
    const source$ = this.masterApiService
      .get<{ data: DelegationResponse }>(`pool_delegation/${poolId}`, params)
      .pipe(map(response => response.data));
    return this.getCachedData<DelegationResponse>(cacheKey, source$);
  }
}
