import { Component, Input } from '@angular/core';
import { Router } from '@angular/router';

interface BreadcrumbItem {
  label: string;
  path?: string;
}

@Component({
    selector: 'app-breadcrumb',
    templateUrl: './breadcrumb.component.html',
    styleUrls: ['./breadcrumb.component.scss'],
    standalone: false
})
export class BreadcrumbComponent {
  @Input() items: BreadcrumbItem[] = [];

  constructor(private router: Router) {}

  navigateToPath(path: string): void {
    if (path) {
      this.router.navigate([path]);
    }
  }
}
