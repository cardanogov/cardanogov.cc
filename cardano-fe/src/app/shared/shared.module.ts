import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  NbButtonModule,
  NbCardModule,
  NbFormFieldModule,
  NbIconModule,
  NbInputModule,
  NbSearchModule,
} from '@nebular/theme';
import { SearchComponent } from './components/search/search.component';
import { FormsModule } from '@angular/forms';
import { BreadcrumbComponent } from './components/breadcrumb/breadcrumb.component';

@NgModule({
  declarations: [SearchComponent, BreadcrumbComponent],
  imports: [
    CommonModule,
    FormsModule,
    NbCardModule,
    NbInputModule,
    NbButtonModule,
    NbIconModule,
    NbFormFieldModule,
    NbSearchModule,
  ],
  exports: [SearchComponent, BreadcrumbComponent],
  bootstrap: [],
})
export class SharedModule {}
// import { NgModule } from '@angular/core';
// import { CommonModule } from '@angular/common';
// import { NbCardModule, NbButtonModule } from '@nebular/theme';
// import { CardComponent } from './components/card/card.component';

// @NgModule({
//   declarations: [SearchComponent],
//   imports: [
//     CommonModule,
//     FormsModule,
//     NbCardModule,
//     NbInputModule,
//     NbButtonModule,
//     NbIconModule,
//     NbFormFieldModule,
//     NbSearchModule,
//   ],
//   exports: [SearchComponent],
// })
// export class SharedModule {}
