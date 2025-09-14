import { CommonModule } from '@angular/common';
import {
  ChangeDetectorRef,
  Component,
  ElementRef,
  HostListener,
  OnInit,
  ViewChild,
} from '@angular/core';
import { RouterModule, Router, NavigationEnd } from '@angular/router';
import { HeaderComponent } from './header/header.component';
import { SidebarComponent } from './sidebar/sidebar.component';
import { FooterComponent } from './footer/footer.component';
import {
  NbLayoutModule,
  NbSidebarModule,
  NbSidebarService,
  NbSidebarState,
} from '@nebular/theme';
import { SharedModule } from '../shared/shared.module';
import { fade } from '../animations';
import { BreakpointObserver, Breakpoints } from '@angular/cdk/layout';
import { filter } from 'rxjs/operators';

interface BreadcrumbItem {
  label: string;
  path?: string;
}

@Component({
    selector: 'app-layout',
    templateUrl: './layout.component.html',
    styleUrls: ['./layout.component.scss'],
    animations: [fade],
    imports: [
        CommonModule,
        RouterModule,
        NbLayoutModule,
        NbSidebarModule,
        HeaderComponent,
        SidebarComponent,
        FooterComponent,
        NbLayoutModule,
        SharedModule
    ]
})
export class LayoutComponent implements OnInit {
  sidebarCompacted = true;
  sidebarState: NbSidebarState = 'expanded';
  breadcrumbItems: BreadcrumbItem[] = [];

  constructor(
    private sidebarService: NbSidebarService,
    private breakpointObserver: BreakpointObserver,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.breakpointObserver
      .observe([Breakpoints.XSmall, Breakpoints.Small])
      .subscribe((result: { matches: boolean }) => {
        if (result.matches) {
          this.sidebarState = 'compacted';
          this.sidebarCompacted = true;
        } else {
          this.sidebarState = 'expanded';
          this.sidebarCompacted = false;
        }
      });

    // Listen for expand event
    this.sidebarService.onExpand().subscribe(() => {
      this.sidebarCompacted = false;
      this.sidebarState = 'expanded';
    });

    // Listen for toggle event to handle compacted/expanded transitions
    this.sidebarService.onToggle().subscribe(() => {
      this.sidebarCompacted = !this.sidebarCompacted;
      this.sidebarState = 'compacted';
    });

    // Update breadcrumb on route changes
    this.router.events
      .pipe(filter((event) => event instanceof NavigationEnd))
      .subscribe(() => {
        this.updateBreadcrumb();
      });

    // Initial breadcrumb update
    this.updateBreadcrumb();
  }

  private updateBreadcrumb() {
    const url = this.router.url;
    const segments = url.split('/').filter((segment) => segment);

    this.breadcrumbItems = [{ label: 'Home', path: '/' }];

    let currentPath = '';
    segments.forEach((segment, index) => {
      currentPath += `/${segment}`;
      const label = this.formatSegment(segment);
      this.breadcrumbItems.push({
        label,
        path: index === segments.length - 1 ? undefined : currentPath,
      });
    });
  }

  private formatSegment(segment: string): string {
    return segment
      .split('-')
      .map((word) => word.charAt(0).toUpperCase() + word.slice(1))
      .join(' ');
  }
}
