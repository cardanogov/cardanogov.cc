import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Routes } from '@angular/router';
import { ActivityComponent } from './activity.component';
import { ActivityDetailsComponent } from './activity-details/activity-details.component';

const routes: Routes = [
  {
    path: '',
    component: ActivityComponent,
  },
  {
    path: ':id',
    component: ActivityDetailsComponent,
  },
];

@NgModule({
  imports: [
    CommonModule,
    RouterModule.forChild(routes),
    ActivityComponent,
    ActivityDetailsComponent
  ],
})
export class ActivityModule {}
