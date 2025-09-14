export interface Proposal {
  id: string;
  category: string;
  title: string;
  status: string;
  timestamp: string;
  result: string;
  yes_votes: number;
  no_votes: number;
  total_votes: number;
}

export interface ProposalStats {
  total: number;
  approved: number;
  percentage: number;
  timePeriod: string;
}
