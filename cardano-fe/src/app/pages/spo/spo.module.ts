import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Routes } from '@angular/router';
import { SpoComponent } from './spo.component';
import { SpoDetailsComponent } from './spo-details/spo-details.component';

const routes: Routes = [
  {
    path: '',
    component: SpoComponent
  },
  {
    path: ':id',
    component: SpoDetailsComponent
  }
];

@NgModule({
  imports: [
    CommonModule,
    RouterModule.forChild(routes),
    SpoComponent,
    SpoDetailsComponent
  ]
})
export class SpoModule { }
