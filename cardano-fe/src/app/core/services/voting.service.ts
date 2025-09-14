import { Injectable } from '@angular/core';
import { ApiService } from './api.service';
import { BehaviorSubject, Observable, map } from 'rxjs';
import { CacheService } from './cache.service';
import { HttpParams } from '@angular/common/http';

export interface VotingCardInfo {
  currentRegister?: number;
  registerChange?: string;
  registerRate?: string;
  abstainAmount?: number;
  abstainChange?: string;
  currentStake?: number;
  stakeChange?: string;
  currentSuplly?: number;
  supplyChange?: string;
  supplyRate?: string;
}

export interface VotingHistoryResponse {
  totalVote?: number;
  filteredVoteInfo?: VotingHistory[];
}

export interface VotingHistory {
  amount?: number;
  block_time?: string;
  epoch_no?: number;
  name?: string;
  proposal_type?: string;
  vote?: string;
  voter_id?: string;
  voter_role?: string;
}

export interface VoteListResponse {
  vote_tx_hash?: string;
  voter_role?: string;
  voter_id?: string;
  proposal_id?: string;
  proposal_tx_hash?: string;
  proposal_index?: number;
  proposal_type?: string;
  epoch_no?: number;
  block_height?: number;
  block_time?: number;
  vote?: string;
  meta_url?: string;
  meta_hash?: string;
  meta_json?: string;
}

export interface VoteStatisticResponse {
  epoch_no?: number;
  sum_yes_voting_power?: VoteStatistic[];
  sum_no_voting_power?: VoteStatistic[];
}

export interface VoteStatistic {
  id?: string;
  power?: number;
  block_time?: number;
  name?: string;
  epoch_no?: number;
}

@Injectable({ providedIn: 'root' })
export class VotingService extends CacheService {
  private votesSubject = new BehaviorSubject<any[]>([]);
  votes$ = this.votesSubject.asObservable();

  constructor(private api: ApiService) {
    super();
  }

  getVotingCardInfo(): Observable<VotingCardInfo> {
    const cacheKey = 'votingCardInfo';
    const source$ = this.api
      .get<{ data: VotingCardInfo }>(`voting_cards_data`)
      .pipe(map((response) => response.data));
    return this.getCachedData<VotingCardInfo>(cacheKey, source$);
  }

  getVotingHistory(
    page: number,
    filter?: string,
    search?: string
  ): Observable<VotingHistoryResponse> {
    let params = new HttpParams().set('page', page);

    if (filter !== undefined && filter !== null) {
      params = params.set('filter', filter);
    }

    if (search) {
      params = params.set('search', search);
    }

    const cacheKey = `votingHistory-p${page}-f${filter || ''}-s${search || ''}`;
    const source$ = this.api
      .get<{ data: VotingHistoryResponse }>('voting_history', params)
      .pipe(map((response) => response.data));
    return this.getCachedData<VotingHistoryResponse>(cacheKey, source$);
  }

  getVoteList(): Observable<VoteListResponse[]> {
    const cacheKey = 'voteList';
    const source$ = this.api
      .get<{ data: VoteListResponse[] }>(`vote_list`)
      .pipe(map((response) => response.data));
    return this.getCachedData<VoteListResponse[]>(cacheKey, source$);
  }

  getVoteStatisticDrepSpo(): Observable<VoteStatisticResponse[]> {
    const cacheKey = 'voteStatisticDrepSpo';
    const source$ = this.api
      .get<{ data: VoteStatisticResponse[] }>(`vote_statistic_drep_spo`)
      .pipe(map((response) => response.data));
    return this.getCachedData<VoteStatisticResponse[]>(cacheKey, source$);
  }

  getVoteParticipationIndex(): Observable<number> {
    const cacheKey = 'voteParticipationIndex';
    const source$ = this.api
      .get<{ data: number }>(`vote_participation_index`)
      .pipe(map((response) => response.data));
    return this.getCachedData<number>(cacheKey, source$);
  }
}
