# DTOs Generated from Angular Services

This directory contains C# DTOs (Data Transfer Objects) generated from the Angular TypeScript interfaces found in the `cardano-fe/src/app/core/services` directory. All properties use snake_case naming convention as requested.

## Files Created

### 1. DrepDtos.cs
Contains DTOs for Drep service interfaces:
- `TotalDrepResponseDto`
- `DrepInfoResponseDto`
- `DrepVotingPowerHistoryResponseDto`
- `DrepDelegatorsResponseDto`
- `DrepPoolVotingThresholdResponseDto`
- `DrepCardDataResponseDto`
- `DrepCardDataByIdResponseDto`
- `ReferencesDto`
- `DrepVoteInfoResponseDto`
- `DrepDelegationResponseDto`
- `DrepDelegationTableResponseDto`
- `DrepRegistrationTableResponseDto`
- `DrepDetailsVotingPowerResponseDto`
- `DrepListResponseDto`
- `DrepListDto`
- `DrepsVotingPowerResponseDto`
- `VotingPowerDataDto`
- `DrepNewRegisterResponseDto`

### 2. ProposalDtos.cs
Contains DTOs for Proposal service interfaces:
- `ProposalDto`
- `ProposalResponseDto`
- `ProposalStatsResponseDto`
- `ProposalInfoResponseDto`
- `GovernanceActionResponseDto`
- `ProposalVotingSummaryResponseDto`
- `GovernanceActionsStatisticsResponseDto`
- `GovernanceActionsStatisticsByEpochResponseDto`
- `ProposalVotingResponseDto`
- `ProposalVotesResponseDto`
- `ProposalVotesDto`
- `ProposalActionTypeResponseDto`

### 3. PoolDtos.cs
Contains DTOs for Pool service interfaces:
- `TotalInfoResponseDto`
- `SpoVotingPowerHistoryResponseDto`
- `AdaStatisticsResponseDto`
- `PoolResultDto`
- `DrepResultDto`
- `SupplyResultDto`
- `AdaStatisticsPercentageResponseDto`
- `PoolDto`
- `PoolInfoDto`
- `VotingPowerDto`
- `PoolStatusDto`
- `PoolInformationDto`
- `VoteInfoDto`
- `DelegationDto`
- `RegistrationDto`
- `PoolResponseDto`

### 4. VotingDtos.cs
Contains DTOs for Voting service interfaces:
- `VotingCardInfoDto`
- `VotingHistoryResponseDto`
- `VotingHistoryDto`
- `VoteListResponseDto`
- `VoteStatisticResponseDto`
- `VoteStatisticDto`

### 5. CombineDtos.cs
Contains DTOs for Combine service interfaces:
- `MembershipDataResponseDto`
- `ParticipateInVotingResponseDto`
- `PoolDataDto`
- `DrepDataDto`
- `GovernanceParametersResponseDto`
- `AllocationResponseDto`
- `SearchApiResponseDto`
- `ChartDto`
- `ProposalSearchDto`
- `DrepSearchDto`
- `PoolSearchDto`
- `CcSearchDto`

### 6. EpochDtos.cs
Contains DTOs for Epoch service interfaces:
- `EpochDto`
- `EpochInfoDto`

### 7. CommitteeDtos.cs
Contains DTOs for Committee service interfaces:
- `CommitteeVotesResponseDto`
- `CommitteeInfoResponseDto`
- `MemberDto`

### 8. TreasuryDtos.cs
Contains DTOs for Treasury service interfaces:
- `TreasuryDataResponseDto`
- `TreasuryVolatilityResponseDto`
- `TreasuryWithdrawalsResponseDto`

### 9. SpoDtos.cs
Contains DTOs for SPO service interfaces:
- `SpoStatsDto`
- `VotingPowerDto`

### 10. StakeDtos.cs
Contains DTOs for Stake service interfaces:
- `TotalStakeResponseDto`

### 11. AccountDtos.cs
Contains DTOs for Account service interfaces:
- `AccountResponseDto`

### 12. AuthDtos.cs
Contains DTOs for Auth service interfaces:
- `LoginCredentialsDto`
- `LoginResponseDto`

## Notes

- All properties use snake_case naming convention
- Shared DTOs like `ReferencesDto` and `VotingPowerDto` are defined in their respective service files
- Object types are preserved as `object` where the original TypeScript interface used `any`
- Nullable types are used where appropriate (e.g., `double?` for optional numeric values)
- Collections are properly typed as `List<T>` or `Dictionary<K,V>` as appropriate
- All DTOs are in the `SharedLibrary.Models.DTOs` namespace

## Usage

These DTOs can be used in your C# backend services to:
1. Define API response models
2. Map data from external APIs
3. Define request/response contracts
4. Ensure type safety when working with the frontend data structures 