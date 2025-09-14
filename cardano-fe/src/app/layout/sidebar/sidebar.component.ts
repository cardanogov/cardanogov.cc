import { Component, Input, OnInit, OnChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MenuComponent } from '../../shared/components/menu/menu.component';
import { MENU_ITEMS } from '../../shared/config/menu.config';
import { MenuConfig } from '../../shared/interfaces/menu.interface';

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [CommonModule, MenuComponent],
  templateUrl: './sidebar.component.html',
  styleUrls: ['./sidebar.component.scss']
})
export class SidebarComponent implements OnInit, OnChanges {
  @Input() isCompact: boolean = false;

  menuItems = MENU_ITEMS;

  menuConfig: MenuConfig = {
    orientation: 'vertical',
    theme: 'dark',
    compact: false
  };

  ngOnInit() {
    this.menuConfig = {
      ...this.menuConfig,
      compact: this.isCompact
    };
  }

  ngOnChanges() {
    this.menuConfig = {
      ...this.menuConfig,
      compact: this.isCompact
    };
  }
}
