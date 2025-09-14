import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Routes } from '@angular/router';
import { CcComponent } from './cc.component';

const routes: Routes = [
  {
    path: '',
    component: CcComponent
  }
];

@NgModule({
  imports: [
    CommonModule,
    RouterModule.forChild(routes),
    CcComponent
  ]
})
export class CcModule { }
