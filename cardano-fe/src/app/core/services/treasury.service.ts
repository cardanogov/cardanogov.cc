import { Injectable } from '@angular/core';
import { Observable, map } from 'rxjs';
import { ApiService } from './api.service';
import { CacheService } from './cache.service';

export interface TreasuryDataResponse {
  treasury?: any;
  total_withdrawals?: any;
  chart_stats?: number[];
}

export interface TreasuryResponse {
  volatilities: TreasuryVolatilityResponse[];
  withdrawals: TreasuryWithdrawalsResponse[];
}

export interface TreasuryVolatilityResponse {
  epoch_no?: number;
  treasury?: number;
  treasury_usd?: number;
}

export interface TreasuryWithdrawalsResponse {
  epoch_no?: number;
  amount?: number;
}

@Injectable({
  providedIn: 'root',
})
export class TreasuryService extends CacheService {
  constructor(private masterApiService: ApiService) {
    super();
  }

  getTreasuryData(): Observable<TreasuryDataResponse> {
    const cacheKey = 'treasuryData';
    const source$ = this.masterApiService
      .get<{ data: TreasuryDataResponse }>(`total_treasury`)
      .pipe(map((response) => response.data));
    return this.getCachedData<TreasuryDataResponse>(cacheKey, source$);
  }

  getTreasuryVolatility(): Observable<TreasuryResponse> {
    const cacheKey = 'treasuryVolatility';
    const source$ = this.masterApiService
      .get<{ data: TreasuryResponse }>(`treasury_volatility`)
      .pipe(map((response) => response.data));
    return this.getCachedData<TreasuryResponse>(cacheKey, source$);
  }

  getTreasuryWithdrawals(): Observable<TreasuryWithdrawalsResponse[]> {
    const cacheKey = 'treasuryWithdrawals';
    const source$ = this.masterApiService
      .get<{ data: TreasuryWithdrawalsResponse[] }>(`treasury_withdrawals`)
      .pipe(map((response) => response.data));
    return this.getCachedData<TreasuryWithdrawalsResponse[]>(cacheKey, source$);
  }
}
