import { Injectable } from '@angular/core';
import { Observable, map } from 'rxjs';
import { ApiService } from './api.service';
import { CacheService } from './cache.service';

export interface MembershipDataResponse {
  total_stake_addresses?: number;
  total_pool?: number;
  total_drep?: number;
  total_committee?: number;
}

export interface PoolData {
  epoch_no?: number;
  total?: number;
}

export interface DrepData {
  epoch_no?: number;
  dreps?: number;
}

export interface ParticipateInVotingResponse {
  pool?: PoolData[];
  drep?: DrepData[];
  committee?: number[];
}

export interface GovernanceParametersResponse {
  epoch_no?: number;
  max_block_size?: number;
  max_tx_size?: number;
  max_bh_size?: number;
  max_val_size?: number;
  max_tx_ex_mem?: number;
  max_tx_ex_steps?: number;
  max_block_ex_mem?: number;
  max_block_ex_steps?: number;
  max_collateral_inputs?: number;
  min_fee_a?: number;
  min_fee_b?: number;
  key_deposit?: number;
  pool_deposit?: number;
  monetary_expand_rate?: number;
  treasury_growth_rate?: number;
  min_pool_cost?: number;
  coins_per_utxo_size?: number;
  price_mem?: number;
  price_step?: number;
  influence?: number;
  max_epoch?: number;
  optimal_pool_count?: number;
  cost_models?: { [key: string]: number[] };
  collateral_percent?: number;
  gov_action_lifetime?: number;
  gov_action_deposit?: number;
  drep_deposit?: number;
  drep_activity?: number;
  committee_min_size?: number;
  committee_max_term_length?: number;
}

export interface AllocationResponse {
  totalActive?: number;
  circulatingSupply?: number;
  delegation?: number;
  adaStaking?: number;
  total?: number;
}

export interface ChartDto {
  title?: string;
  url?: string;
}

export interface ProposalSearchDto {
  id?: string;
  title?: string;
  url?: string;
  type?: string;
  yes?: number;
  no?: number;
  abstain?: number;
}

export interface DrepSearchDto {
  id?: string;
  title?: string;
  url?: string;
  delegator?: number;
  live_stake?: number;
}

export interface PoolSearchDto {
  id?: string;
  title?: string;
  url?: string;
  live_stake?: number;
  margin?: number;
  fixed_cost?: number;
  pledge?: number;
}

export interface CcSearchDto {
  id?: string;
  title?: string;
  url?: string;
}

export interface SearchApiResponse {
  charts?: ChartDto[];
  proposals?: ProposalSearchDto[];
  dreps?: DrepSearchDto[];
  pools?: PoolSearchDto[];
  ccs?: CcSearchDto[];
}

@Injectable({
  providedIn: 'root',
})
export class CombineService extends CacheService {
  constructor(private masterApiService: ApiService) {
    super();
  }

  getMembershipData(): Observable<MembershipDataResponse> {
    const cacheKey = 'membershipData';
    const source$ = this.masterApiService
      .get<{ data: MembershipDataResponse }>('totals_membership')
      .pipe(map(response => response.data));
    return this.getCachedData<MembershipDataResponse>(cacheKey, source$);
  }

  getCurrentAdaPrice(): Observable<number> {
    const cacheKey = 'currentAdaPrice';
    const source$ = this.masterApiService
      .get<{ data: number }>(`usd_price`)
      .pipe(map(response => response.data));
    return this.getCachedData<number>(cacheKey, source$);
  }

  getParticipateInVoting(): Observable<ParticipateInVotingResponse> {
    const cacheKey = 'participateInVoting';
    const source$ = this.masterApiService
      .get<{ data: ParticipateInVotingResponse }>(`participate_in_voting`)
      .pipe(map(response => response.data));
    return this.getCachedData<ParticipateInVotingResponse>(cacheKey, source$);
  }

  getGovernanceParameters(): Observable<GovernanceParametersResponse[]> {
    const cacheKey = 'governanceParameters';
    const source$ = this.masterApiService
      .get<{ data: GovernanceParametersResponse[] }>(`governance_parameters`)
      .pipe(map(response => response.data));
    return this.getCachedData<GovernanceParametersResponse[]>(cacheKey, source$);
  }

  getAllocation(): Observable<AllocationResponse> {
    const cacheKey = 'allocation';
    const source$ = this.masterApiService
      .get<{ data: AllocationResponse }>(`allocation`)
      .pipe(map(response => response.data));
    return this.getCachedData<AllocationResponse>(cacheKey, source$);
  }

  search(term: string): Observable<SearchApiResponse> {
    const cacheKey = `search-${term}`;
    const source$ = this.masterApiService
      .get<{ data: SearchApiResponse }>(`search?searchTerm=${term}`)
      .pipe(map(response => response.data));
    return this.getCachedData<SearchApiResponse>(cacheKey, source$);
  }
}
