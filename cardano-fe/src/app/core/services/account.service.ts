import { Injectable } from '@angular/core';
import { Observable, map } from 'rxjs';
import { ApiService } from './api.service';
import { CacheService } from './cache.service';

export interface AccountResponse {
  stake_address: string;
  stake_address_hex: string;
  script_hash: string;
}

@Injectable({
  providedIn: 'root',
})
export class AccountService extends CacheService {
  constructor(private masterApiService: ApiService) {
    super();
  }

  /**
   * Get all account list
   * @returns Observable of Account
   */
  getTotalStakeAddresses(): Observable<number> {
    const cacheKey = 'totalStakeAddresses';
    const source$ = this.masterApiService
      .get<{ data: number }>(`total_stake_addresses`)
      .pipe(map((response) => response.data));
    return this.getCachedData<number>(cacheKey, source$);
  }
}
