import { Component, Inject, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NbIconModule } from '@nebular/theme';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { TableColumn, TableComponent } from '../../../../shared/components/table/table.component';
import { ProposalActionTypeResponse, ProposalService } from '../../../../core/services/proposal.service';
import { Router, NavigationStart } from '@angular/router';
import { filter, takeUntil } from 'rxjs/operators';
import { Subject } from 'rxjs';

interface VotingHistory {
  date: string;
  governanceAction: string;
  type: string;
  vote: string;
  power: string;
  id: string;
}

@Component({
  selector: 'app-voting-history-modal',
  standalone: true,
  imports: [CommonModule, NbIconModule, TableComponent],
  templateUrl: './voting-history-modal.component.html',
  styleUrls: ['./voting-history-modal.component.scss']
})
export class VotingHistoryModalComponent implements OnDestroy {
  private destroy$ = new Subject<void>();
  votes: VotingHistory[] = [];
  isLoading = true;
  votingHistoryColumns: TableColumn[] = [
    { key: 'date', title: 'Date' },
    { key: 'governanceAction', title: 'Governance Action' },
    { key: 'type', title: 'Type' },
    { key: 'vote', title: 'Vote ' },
    { key: 'power', title: 'Power' },
  ];

  private formatDateTime(blockTime: string): string {
    if (!blockTime) return '';

    // Convert epoch time to milliseconds
    const epochTime = parseInt(blockTime) * 1000;
    const date = new Date(epochTime);

    return date.toLocaleString('en-US', {
      timeZone: 'UTC',
      year: 'numeric',
      month: 'short',
      day: 'numeric'
    });
  }

  private proposalTitles: { [key: string]: string } = {};
  private proposalTypes: { [key: string]: string } = {};

  constructor(
    public dialogRef: MatDialogRef<VotingHistoryModalComponent>,
    @Inject(MAT_DIALOG_DATA) public data: any,
    private proposalService: ProposalService,
    private router: Router
  ) {
    // Subscribe to router events to close modal on navigation
    this.router.events.pipe(
      filter(event => event instanceof NavigationStart),
      takeUntil(this.destroy$)
    ).subscribe(() => {
      this.close();
    });

    this.isLoading = true;
    this.proposalService.getProposalActionType().subscribe({
      next: (proposals: ProposalActionTypeResponse[]) => {
        // Create maps of proposal_id to title and type
        this.proposalTitles = proposals.reduce((acc: { [key: string]: string }, proposal: ProposalActionTypeResponse) => {
          acc[proposal.proposal_id || ''] = proposal.meta_json?.body?.title || 'No Title';
          return acc;
        }, {});

        this.proposalTypes = proposals.reduce((acc: { [key: string]: string }, proposal: ProposalActionTypeResponse) => {
          acc[proposal.proposal_id || ''] = proposal.proposal_type || '';
          return acc;
        }, {});

        if (this.data?.votes) {
          this.votes = this.data.votes.map((vote: any) => ({
            date: this.formatDateTime(vote.block_time),
            governanceAction: this.proposalTitles[vote.proposal_id] || vote.proposal_id || '',
            type: this.proposalTypes[vote.proposal_id] || vote.proposal_type || '',
            vote: vote.vote || '',
            power: '14.3%',
            id: vote.proposal_id
          }));
        }
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Error fetching proposal data:', error);
        this.isLoading = false;
      }
    });
  }

  ngOnDestroy() {
    this.destroy$.next();
    this.destroy$.complete();
  }

  close() {
    this.dialogRef.close();
  }
}
