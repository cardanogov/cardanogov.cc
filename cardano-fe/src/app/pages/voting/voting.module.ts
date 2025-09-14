import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Routes } from '@angular/router';
import { VotingComponent } from './voting.component';

const routes: Routes = [
  {
    path: '',
    component: VotingComponent
  }
];

@NgModule({
  imports: [
    CommonModule,
    RouterModule.forChild(routes),
    VotingComponent
  ]
})
export class VotingModule { }
