import { Injectable } from '@angular/core';
import { Injectable } from '@angular/core';
import { Observable, map } from 'rxjs';
import { ApiService } from './api.service';
import { CacheService } from './cache.service';

export interface SpoStats {
  totalStaked: number;
  activePools: number;
  totalStakeAddresses: number;
  votingPower: {
    active: number;
    inactive: number;
  };
}

@Injectable({
  providedIn: 'root',
})
export class SpoService extends CacheService {
  constructor(private masterApiService: ApiService) {
    super();
  }

  getSpoStats(): Observable<SpoStats> {
    const cacheKey = 'spoStats';
    const source$ = this.masterApiService
      .get<{ data: SpoStats }>(`spo_stats`)
      .pipe(map((response) => response.data));
    return this.getCachedData<SpoStats>(cacheKey, source$);
  }
}
