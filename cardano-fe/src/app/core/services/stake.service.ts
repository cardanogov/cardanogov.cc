import { Injectable } from '@angular/core';
import { Observable, map } from 'rxjs';
import { ApiService } from './api.service';
import { CacheService } from './cache.service';

export interface TotalStakeResponse {
  totalADA: number;
  totalSupply: number;
  chartStats: number[];
}

@Injectable({
  providedIn: 'root',
})
export class StakeService extends CacheService {
  constructor(private masterApiService: ApiService) {
    super();
  }

  getTotalStake(): Observable<TotalStakeResponse> {
    const cacheKey = 'totalStake';
    const source$ = this.masterApiService
      .get<{ data: TotalStakeResponse }>(`total_stake`)
      .pipe(map((response) => response.data));
    return this.getCachedData<TotalStakeResponse>(cacheKey, source$);
  }
}
