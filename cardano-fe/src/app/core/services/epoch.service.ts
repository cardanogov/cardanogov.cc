import { Injectable } from '@angular/core';
import { Observable, map } from 'rxjs';
import { ApiService } from './api.service';
import { CacheService } from './cache.service';

export interface CurrentEpochResponse {
  epoch_no?: number;
  amount?: string;
  dreps?: number;
}

export interface EpochInfoResponse {
  epoch_no?: number;
  out_sum?: string;
  fees?: string;
  tx_count?: number;
  blk_count?: number;
  start_time?: number;
  end_time?: number;
  first_block_time?: number;
  last_block_time?: number;
  active_stake?: string;
  total_rewards?: string;
  avg_blk_reward?: string;
}

export interface TotalStakeResponse {
  totalADA?: number;
  totalSupply?: number;
  chartStats?: number[];
}

export interface EpochProtocolParametersResponse {
  epoch_no?: number;
  min_fee_a?: number;
  min_fee_b?: number;
  max_block_size?: number;
  max_tx_size?: number;
  max_block_header_size?: number;
  key_deposit?: string;
  pool_deposit?: string;
  max_epoch?: number;
  optimal_pool_count?: number;
  influence?: number;
  monetary_expand_rate?: number;
  treasury_growth_rate?: number;
  decentralisation?: number;
  entropy?: string;
  protocol_major?: number;
  protocol_minor?: number;
  min_utxo?: string;
  min_pool_cost?: string;
  nonce?: string;
  price_mem?: number;
  price_step?: number;
  max_tx_ex_mem?: string;
  max_tx_ex_steps?: string;
  max_block_ex_mem?: string;
  max_block_ex_steps?: string;
  max_val_size?: number;
  collateral_percent?: number;
  max_collateral_inputs?: number;
  coins_per_utxo_size?: string;
}

@Injectable({
  providedIn: 'root',
})
export class EpochService extends CacheService {
  constructor(private apiService: ApiService) {
    super();
  }

  getCurrentEpoch(): Observable<CurrentEpochResponse[]> {
    const cacheKey = 'currentEpoch';
    const source$ = this.apiService
      .get<{ data: CurrentEpochResponse[] }>('current_epoch')
      .pipe(map((response) => response.data));
    return this.getCachedData<CurrentEpochResponse[]>(cacheKey, source$);
  }

  getEpochInfo(epochNo: number): Observable<EpochInfoResponse> {
    const cacheKey = `epochInfo-${epochNo}`;
    const source$ = this.apiService
      .get<{ data: EpochInfoResponse }>(`epoch_info/${epochNo}`)
      .pipe(map((response) => response.data));
    return this.getCachedData<EpochInfoResponse>(cacheKey, source$);
  }

  getCurrentEpochInfo(): Observable<EpochInfoResponse> {
    const cacheKey = 'currentEpochInfo';
    const source$ = this.apiService
      .get<{ data: EpochInfoResponse }>('current_epoch_info')
      .pipe(map((response) => response.data));
    return this.getCachedData<EpochInfoResponse>(cacheKey, source$);
  }

  getEpochInfoSpo(epochNo: number): Observable<number> {
    const cacheKey = `epochInfoSpo-${epochNo}`;
    const source$ = this.apiService
      .get<{ data: number }>(`epoch_info_spo/${epochNo}`)
      .pipe(map((response) => response.data));
    return this.getCachedData<number>(cacheKey, source$);
  }

  getTotalStake(): Observable<TotalStakeResponse> {
    const cacheKey = 'totalStake';
    const source$ = this.apiService
      .get<{ data: TotalStakeResponse }>('total_stake')
      .pipe(map((response) => response.data));
    return this.getCachedData<TotalStakeResponse>(cacheKey, source$);
  }

  getEpochProtocolParameters(
    epochNo: number
  ): Observable<EpochProtocolParametersResponse> {
    const cacheKey = `epochProtocolParameters-${epochNo}`;
    const source$ = this.apiService
      .get<{ data: EpochProtocolParametersResponse }>(`epoch_protocol_parameters/${epochNo}`)
      .pipe(map((response) => response.data));
    return this.getCachedData<EpochProtocolParametersResponse>(cacheKey, source$);
  }
}
