import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { AppComponent } from './app.component';
import { NbThemeModule, NbLayoutModule, NbMenuModule, NbSidebarModule, NbIconModule, NbToastrModule } from '@nebular/theme';
import { NgxSpinnerModule } from 'ngx-spinner';
import { PagesModule } from './pages/pages.module';
import { NbEvaIconsModule } from '@nebular/eva-icons';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { SharedModule } from './shared/shared.module';
import { FormsModule } from '@angular/forms';
import { NgbModule } from '@ng-bootstrap/ng-bootstrap';
import { AppRoutingModule } from './app.routes';
import { provideHttpClient } from '@angular/common/http';
import { MarkdownModule } from 'ngx-markdown';
@NgModule({
  declarations: [AppComponent],
  imports: [
    BrowserModule,
    BrowserAnimationsModule,
    NbThemeModule.forRoot({ name: 'corporate' }),
    NbMenuModule.forRoot(),
    NbSidebarModule.forRoot(),
    NbIconModule,
    NbEvaIconsModule,
    NbLayoutModule,
    NgxSpinnerModule,
    PagesModule,
    FormsModule,
    SharedModule,
    NgbModule,
    AppRoutingModule,
    NbToastrModule.forRoot(),
    MarkdownModule.forRoot()
  ],
  providers: [provideHttpClient()],
  bootstrap: [AppComponent]
})
export class AppModule {}
