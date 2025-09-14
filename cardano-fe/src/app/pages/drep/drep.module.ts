import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Routes } from '@angular/router';
import { DrepComponent } from './drep.component';
import { DrepDetailComponent } from './drep-detail/drep-detail.component';

const routes: Routes = [
  {
    path: '',
    component: DrepComponent
  },
  {
    path: ':id',
    component: DrepDetailComponent
  }
];

@NgModule({
  declarations: [],
  imports: [
    CommonModule,
    RouterModule.forChild(routes),
    DrepDetailComponent,
    DrepComponent
  ]
})
export class DrepModule { }
