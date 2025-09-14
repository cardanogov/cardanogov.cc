import { Injectable } from '@angular/core';
import { Observable, map } from 'rxjs';
import { ApiService } from './api.service';
import { CacheService } from './cache.service';

export interface CommitteeVotesResponse {
  proposal_id?: string;
  proposal_tx_hash?: number;
  proposal_index?: number;
  vote_tx_hash?: string;
  block_time?: number;
  vote?: string;
  meta_url?: string;
  meta_hash?: string;
  cc_hot_id?: string;
}

export interface MemberDto {
  status?: string;
  cc_hot_id?: string;
  cc_cold_id?: string;
  cc_hot_hex?: string;
  cc_cold_hex?: string;
  expiration_epoch?: number;
  cc_hot_has_script?: boolean;
  cc_cold_has_script?: boolean;
}

export interface CommitteeInfoResponse {
  proposal_id?: string;
  proposal_tx_hash?: string;
  proposal_index?: number;
  quorum_numerator?: number;
  quorum_denominator?: number;
  members?: MemberDto[];
}

@Injectable({
  providedIn: 'root',
})
export class CommitteeService extends CacheService {
  constructor(private masterApiService: ApiService) {
    super();
  }

  /**
   * Get all account list
   * @returns Observable of Account
   */
  getTotalCommittee(): Observable<number> {
    const cacheKey = 'totalCommittee';
    const source$ = this.masterApiService
      .get<{ data: number }>(`total_committee`)
      .pipe(map((response) => response.data));
    return this.getCachedData<number>(cacheKey, source$);
  }

  getCommitteeVotes(ccId: string): Observable<CommitteeVotesResponse[]> {
    const cacheKey = `committeeVotes-${ccId}`;
    const source$ = this.masterApiService
      .get<{ data: CommitteeVotesResponse[] }>(`committee_votes/${ccId}`)
      .pipe(map((response) => response.data));
    return this.getCachedData<CommitteeVotesResponse[]>(cacheKey, source$);
  }

  getCommitteeInfo(): Observable<CommitteeInfoResponse[]> {
    const cacheKey = 'committeeInfo';
    const source$ = this.masterApiService
      .get<{ data: CommitteeInfoResponse[] }>('committee_info')
      .pipe(map((response) => response.data));
    return this.getCachedData<CommitteeInfoResponse[]>(cacheKey, source$);
  }
}
