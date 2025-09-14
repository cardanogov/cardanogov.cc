import { Component, Input } from '@angular/core';
import { NbCardModule, NbIconModule, NbButtonModule, NbTooltipModule, NbBadgeModule, NbToastrModule, NbToastrService, NbDialogService } from '@nebular/theme';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { CommitteeInfoResponse, CommitteeService } from '../../../core/services/committee.service';
import { VotingHistoryModalComponent } from './voting-history-modal/voting-history-modal.component';
import { MatDialog } from '@angular/material/dialog';
import { truncateMiddle } from '../../../core/helper/format.helper';

@Component({
  selector: 'app-committee-card',
  standalone: true,
  imports: [
    NbCardModule,
    NbIconModule,
    NbButtonModule,
    NbTooltipModule,
    NbBadgeModule,
    CommonModule,
    NbToastrModule,
  ],
  templateUrl: './committee-card.component.html',
  styleUrls: ['./committee-card.component.scss']
})
export class CommitteeCardComponent {
  @Input() logo: string = '';
  @Input() title: string = '';
  @Input() address: string = '';
  @Input() organization: number = 0;
  @Input() company: string = '';
  @Input() endDate: string = '';
  @Input() membersLink: string = '';
  @Input() committeeInfo: CommitteeInfoResponse[] = [];
  @Input() votingPower: string = '';
  @Input() members: number = 0;
  @Input() endEpoch: number = 0;
  @Input() currentEpoch: number = 0;
  @Input() ccHot: string | null = null;
  @Input() ccCold: string | null = null;

  votes: any[] = [];
  timesVoted: number = 0;
  status: string = 'Active';

  constructor(
    private committeeService: CommitteeService,
    private toastr: NbToastrService,
    private dialog: MatDialog
  ) {}

  ngOnInit() {
    this.getCommitteeVotes();
    this.mapStatus();
  }

  navigateToMembers() {
    if (this.membersLink) {
      window.open(this.membersLink, '_blank');
    }
  }

  getCommitteeVotes() {
    // Use cc_hot address for API calls, if not available return 0
    if (!this.ccHot) {
      this.timesVoted = 0;
      this.votes = [];
      return;
    }

    this.committeeService.getCommitteeVotes(this.ccHot).subscribe((votes) => {
      this.votes = votes;
      let voteCount = 0;
      votes.forEach((vote) => {
        if (vote.vote === 'Yes' || vote.vote === 'No') {
          voteCount++;
        }
      });
      this.timesVoted = voteCount;
    });
  }

  mapStatus() {
    this.committeeInfo.forEach((data) => {
      data.members?.forEach((member) => {
        if (member.cc_hot_id === this.address) {
          if (member.status === 'authorized') {
            this.status = "Authorized";
          } else if (member.status === 'not_authorized') {
            this.status = "Not Authorized";
          } else if (member.status === 'resigned') {
            this.status = "Resigned";
          }
        }
      });
    });
  }

  copyToClipboard(value: string) {
    if (!value) return;
    navigator.clipboard.writeText(value).then(
      () => {
        this.toastr.success('Copied to clipboard!', 'Success');
      },
      (err) => {
        this.toastr.danger('Failed to copy!', 'Error');
        console.error('Could not copy text: ', err);
      }
    );
  }

  truncateMiddle(value: string): string {
    return truncateMiddle(value);
  }

  openVotingHistory() {
    this.dialog.open(VotingHistoryModalComponent, {
      width: '90vw',
      height: '90vh',
      maxWidth: 'none',
      panelClass: 'fullscreen-modal',
      data: {
        title: 'Voting History',
        votes: this.votes,
      },
    });
  }

  calculateTenure(): string {
    if (!this.endEpoch || !this.currentEpoch) return 'N/A';

    const epochsRemaining = this.endEpoch - this.currentEpoch;
    const daysRemaining = epochsRemaining * 5;

    if (daysRemaining <= 0) return 'Expired';

    const months = Math.floor(daysRemaining / 30);
    const days = daysRemaining % 30;

    if (months > 0) {
      return `${months} month${months > 1 ? 's' : ''} ${days} day${days > 1 ? 's' : ''}`;
    } else {
      return `${days} day${days > 1 ? 's' : ''}`;
    }
  }
}
