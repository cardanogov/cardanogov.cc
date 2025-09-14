import { Component, OnInit, OnDestroy } from '@angular/core';
import { Subscription } from 'rxjs';
import { LoadingService } from '../core/services/loading.service';
import { MENU_ITEMS } from './pages-menu';

@Component({
  selector: 'cardano-pages',
  styleUrls: ['pages.component.scss'],
  template: `
    <app-layout>
      <router-outlet></router-outlet>
    </app-layout>
    <ngx-spinner type="ball-clip-rotate" [showSpinner]="isLoading">
      <p style="color: white">{{ spinnerText }}</p>
    </ngx-spinner>
  `,
  standalone: false
})
export class PagesComponent implements OnInit, OnDestroy {
  menu = MENU_ITEMS;
  isLoading: boolean = false;
  spinnerText: string = 'Loading...';
  private subscriptions: Subscription = new Subscription();

  constructor(private loadingService: LoadingService) {}

  ngOnInit(): void {
    // Subscribe to loading state
    this.subscriptions.add(
      this.loadingService.loading$.subscribe((loading) => {
        this.isLoading = loading;
      })
    );

    // Subscribe to spinner text
    this.subscriptions.add(
      this.loadingService.spinnerText$.subscribe((text) => {
        this.spinnerText = text;
      })
    );
  }

  ngOnDestroy(): void {
    // Unsubscribe to prevent memory leaks
    this.subscriptions.unsubscribe();
  }
}
