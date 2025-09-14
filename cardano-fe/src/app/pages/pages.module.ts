import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { PagesComponent } from './pages.component';
import { PagesRoutingModule } from './pages-routing.module';
import { LayoutComponent } from "../layout/layout.component";
import { NgxSpinnerModule } from 'ngx-spinner';
import { CookieService } from 'ngx-cookie-service';

@NgModule({
  imports: [
    CommonModule,
    PagesRoutingModule,
    LayoutComponent,
    NgxSpinnerModule
  ],
  declarations: [
    PagesComponent
  ],
  providers: [CookieService]
})
export class PagesModule {}
