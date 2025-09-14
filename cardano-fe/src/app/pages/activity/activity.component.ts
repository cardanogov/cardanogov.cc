import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { CardComponent } from '../../shared/components/card/card.component';
import { NbIconModule } from '@nebular/theme';
import {
  GovernanceActionResponse,
  ProposalInfoResponse,
  ProposalService,
} from '../../core/services/proposal.service';
import { CarouselTagComponent } from '../../shared/components/carousel-tag/carousel-tag.component';
import { SearchService } from '../../core/services/search.service';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { CardSkeletonComponent } from '../../shared/components/skeleton/card-skeleton.component';
import { FormsModule } from '@angular/forms';

interface ProposalType {
  key: string;
  value: string;
}

@Component({
  selector: 'app-activity',
  standalone: true,
  imports: [
    CardComponent,
    NbIconModule,
    CarouselTagComponent,
    CommonModule,
    CardSkeletonComponent,
    FormsModule,
  ],
  templateUrl: './activity.component.html',
  styleUrl: './activity.component.scss',
})
export class ActivityComponent implements OnInit {
  governanceAction: GovernanceActionResponse = {
    total_proposals: 0,
    approved_proposals: 0,
    percentage_change: 0,
    proposal_info: [],
  };
  filteredProposals: GovernanceActionResponse = {
    total_proposals: 0,
    approved_proposals: 0,
    percentage_change: 0,
    proposal_info: [],
  };
  displayedProposals: ProposalInfoResponse[] = [];
  itemsPerPage = 9;
  currentPage = 1;
  proposalLiveInfo: ProposalInfoResponse[] = [];
  lstProposalType: ProposalType[] = [
    { key: 'InfoAction', value: '0' },
    { key: 'ParameterChange', value: '0' },
    { key: 'NewConstitution', value: '0' },
    { key: 'HardForkInitiation', value: '0' },
    { key: 'TreasuryWithdrawals', value: '0' },
    { key: 'NoConfidence', value: '0' },
    { key: 'NewCommittee', value: '0' },
  ];
  searchText = '';
  selectedProposalType: string | null = null;
  differenceEpoch: number = 0;

  public isLoading = false;
  public isLoadingCard = false;
  public isLoadingProposalLive = false;
  public isLoadingExpiredProposal = false;

  currentSlide = 0;

  constructor(
    private proposalService: ProposalService,
    private searchService: SearchService,
    private cdr: ChangeDetectorRef,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.isLoading = true;
    this.isLoadingCard = true;
    this.isLoadingProposalLive = true;
    this.isLoadingExpiredProposal = true;

    this.proposalService.getProposals().subscribe({
      next: (response) => {
        this.proposalLiveInfo =
          response.proposal_info?.filter((s) => s.status == 'Active') ?? [];
        this.isLoadingProposalLive = false;

        this.governanceAction = response;
        this.filteredProposals = response;
        this.updateDisplayedProposals();
        const proposalTypeCount: Record<string, number> = {};

        // Initialize all proposal types with 0
        this.lstProposalType.forEach((type) => {
          proposalTypeCount[type.key] = 0;
        });

        for (const proposal of response.proposal_info || []) {
          const type = proposal.proposalType;
          if (type) {
            proposalTypeCount[type] = (proposalTypeCount[type] || 0) + 1;
          }
        }

        // Keep the original counts for reference, but display will be dynamic
        this.lstProposalType = this.lstProposalType.map((type) => ({
          key: type.key,
          value: String(proposalTypeCount[type.key] || 0),
        }));

        this.isLoadingCard = false;
        this.isLoadingExpiredProposal = false;
        this.isLoading = false;
      },
      error: (err) => {
        console.error('Error fetching governance actions:', err);
        this.isLoadingCard = false;
        this.isLoadingExpiredProposal = false;
        this.isLoading = false;
      },
    });

    this.proposalService.getProposalStats().subscribe({
      next: (response) => {
        this.differenceEpoch = response.difference ? response.difference : 0;
        this.isLoadingProposalLive = false;
      },
      error: (err) => {
        console.error('Error fetching governance actions:', err);
        this.isLoadingProposalLive = false;
      },
    });
  }

  onViewDetails(proposalId: string, isLiveProposal: boolean) {
    this.router.navigate(['/activity', proposalId], {
      queryParams: { isLive: isLiveProposal },
    });
  }

  onSearch() {
    this.searchService.updateSearch(this.searchText);
    this.currentPage = 1; // Reset to first page when searching
    this.applyFilters();
  }

  onSearchInput() {
    // Apply filters in real-time as user types
    this.currentPage = 1;
    this.applyFilters();
  }

  nextSlide() {
    if (this.proposalLiveInfo.length > 0) {
      this.currentSlide =
        (this.currentSlide + 1) % this.proposalLiveInfo.length;
    }
  }

  prevSlide() {
    if (this.proposalLiveInfo.length > 0) {
      this.currentSlide =
        (this.currentSlide - 1 + this.proposalLiveInfo.length) %
        this.proposalLiveInfo.length;
    }
  }

  goToSlide(index: number) {
    if (index >= 0 && index < this.proposalLiveInfo.length) {
      this.currentSlide = index;
    }
  }

  getPrevIndex(): number {
    return (
      (this.currentSlide - 1 + this.proposalLiveInfo.length) %
      this.proposalLiveInfo.length
    );
  }

  getNextIndex(): number {
    return (this.currentSlide + 1) % this.proposalLiveInfo.length;
  }

  onVotingClick(): void {
    this.router.navigate(['/voting']);
  }

  formatKey(value: string): string {
    if (!value) return '';
    return value.replace(/([A-Z])/g, ' $1').trim();
  }

  filterProposal(key: string) {
    // Toggle filter: if clicking the same filter, deselect it
    if (this.selectedProposalType === key) {
      this.selectedProposalType = null;
    } else {
      this.selectedProposalType = key;
    }
    this.currentPage = 1; // Reset to first page when filtering
    this.applyFilters();
  }

  loadMore() {
    this.currentPage++;
    this.updateDisplayedProposals();
  }

  private updateDisplayedProposals() {
    const startIndex = 0;
    const endIndex = this.currentPage * this.itemsPerPage;
    this.displayedProposals =
      this.filteredProposals.proposal_info?.slice(startIndex, endIndex) || [];
  }

  private applyFilters() {
    let filtered = this.governanceAction.proposal_info || [];

    // Apply search filter
    if (this.searchText && this.searchText.trim()) {
      filtered = filtered.filter((proposal) =>
        proposal.title?.toLowerCase().includes(this.searchText.toLowerCase())
      );
    }

    // Apply proposal type filter
    if (this.selectedProposalType) {
      filtered = filtered.filter(
        (proposal) => proposal.proposalType === this.selectedProposalType
      );
    }

    this.filteredProposals = {
      ...this.governanceAction,
      proposal_info: filtered,
    };

    this.updateDisplayedProposals();
  }

  getFilteredCount(proposalType: string): string {
    if (!this.governanceAction.proposal_info) return '0';

    let filtered = this.governanceAction.proposal_info;

    // Apply search filter first if there's a search term
    if (this.searchText && this.searchText.trim()) {
      filtered = filtered.filter((proposal) =>
        proposal.title?.toLowerCase().includes(this.searchText.toLowerCase())
      );
    }

    // Then count by proposal type
    const count = filtered.filter(
      (proposal) => proposal.proposalType === proposalType
    ).length;
    return String(count);
  }
}
