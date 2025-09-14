import { Component, Input, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MenuItem, MenuConfig } from '../../interfaces/menu.interface';
import { SharedModule } from '../../shared.module';
import { IconComponent } from '../icon/icon.component';

@Component({
  selector: 'app-menu',
  standalone: true,
  imports: [CommonModule, RouterModule, SharedModule, IconComponent],
  templateUrl: './menu.component.html',
  styleUrls: ['./menu.component.scss'],
})
export class MenuComponent implements OnInit {
  @Input() items: MenuItem[] = [];
  @Input() config: MenuConfig = {
    orientation: 'vertical',
    compact: false,
    theme: 'light',
  };

  constructor() {}

  ngOnInit(): void {}

  getMenuClasses(): string {
    const classes = ['menu'];
    classes.push(`menu-${this.config.orientation || 'vertical'}`);
    if (this.config.theme === 'dark') classes.push('menu-dark');
    if (this.config.compact) classes.push('menu-compact');
    return classes.join(' ');
  }

  isActive(item: MenuItem): boolean {
    return window.location.pathname.includes(item.link!);
  }

  onItemClick(item: MenuItem): void {
    // Handle item click if needed
  }
}
