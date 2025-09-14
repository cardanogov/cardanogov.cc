import { CommonModule } from '@angular/common';
import {
  Component,
  EventEmitter,
  HostListener,
  OnInit,
  Output,
} from '@angular/core';
import { RouterModule } from '@angular/router';
import {
  NbActionsModule,
  NbContextMenuModule,
  NbIconModule,
  NbMenuItem,
  NbMenuModule,
  NbPopoverModule,
  NbSidebarService,
  NbUserModule,
} from '@nebular/theme';
import { SharedModule } from '../../shared/shared.module';
import { NgbDropdownModule } from '@ng-bootstrap/ng-bootstrap';
import { BreakpointObserver, Breakpoints } from '@angular/cdk/layout';
import { FullScreenSearchComponent } from '../../shared/components/fullscreen-search/fullscreen-search.component';
import {
  ProposalInfoResponse,
  ProposalService,
} from '../../core/services/proposal.service';
import { CookieService } from 'ngx-cookie-service';

export interface MenuItem {
  title: string;
  link?: string;
  description?: string;
  icon?: string;
  iconBgColor?: string;
  children?: MenuItem[];
}

@Component({
  selector: 'app-header',
  templateUrl: './header.component.html',
  styleUrl: './header.component.scss',
  imports: [
    CommonModule,
    RouterModule,
    NbActionsModule,
    NbUserModule,
    SharedModule,
    NbMenuModule,
    NbContextMenuModule,
    NbIconModule,
    NbPopoverModule,
    NgbDropdownModule,
    FullScreenSearchComponent,
  ],
})
export class HeaderComponent implements OnInit {
  menuToggleIcon = '';
  toggleState = false;
  logo = '../../../assets/images/Logo-trans.png';
  activityItems: NbMenuItem[] = [
    { title: '', icon: 'twitter' },
    {
      title: '',
      icon: 'grid-outline',
    },
    { title: '', icon: 'bell-outline' },
  ];

  menuItems = [
    {
      title: 'Cardano GovTool',
      description: `The Cardano Govtool is a community tool that supports the key steps of Cardano's Governance process as described in CIP-1694. Govtool is part of the core governance tools.`,
      image: 'assets/images/gov-tool.png',
      link: 'https://gov.tools/',
    },
    {
      title: '1694.io',
      description:
        'Introducing CIP 1694 - An on-chain decentralized Governance mechanism for Voltaire',
      image: 'assets/images/gov-tool.png',
      link: 'https://www.1694.io/en',
    },
    {
      title: 'Intersect',
      description: `Intersect is a member-based organization tasked with ensuring the Cardano ecosystem's continuity and future development`,
      image: 'assets/icons/cc/intersect.png',
      link: 'https://intersectmbo.org/',
    },
    {
      title: 'Cardano',
      description:
        'Discover more about Cardano: the next-generation blockchain, secure, sustainable, supporting decentralized applications.',
      image: 'assets/images/cardano.png',
      link: 'https://cardano.org/',
    },
  ];

  notifyItems: any[] = [];
  unreadCount = 0;

  @Output() showSearchMobile = new EventEmitter<void>();

  constructor(
    private sidebarService: NbSidebarService,
    private breakpointObserver: BreakpointObserver,
    private proposalService: ProposalService,
    private cookieService: CookieService
  ) {}

  toggle() {
    this.sidebarService.toggle();
    this.toggleState = !this.toggleState;
    this.changeToggleIcon(this.toggleState);
  }

  changeToggleIcon(compacted: boolean) {
    this.menuToggleIcon = compacted
      ? 'arrowhead-left-outline'
      : 'arrowhead-right-outline';
  }

  ngOnInit(): void {
    this.checkScreenSize();
    this.getNotifies();

    this.breakpointObserver
      .observe([Breakpoints.XSmall, Breakpoints.Small])
      .subscribe((result) => {
        if (result.matches) {
          this.menuToggleIcon = 'arrowhead-right-outline';
          this.toggleState = false;
        } else {
          this.menuToggleIcon = 'arrowhead-left-outline';
          this.toggleState = true;
        }
      });
  }

  @HostListener('window:resize', ['$event'])
  checkScreenSize() {
    if (window.innerWidth <= 768) {
      this.sidebarService.compact();
      this.menuToggleIcon = 'arrowhead-left-outline';
    }
  }

  getNotifies() {
    this.proposalService.getProposalLive().subscribe({
      next: (data: ProposalInfoResponse[]) => {
        this.notifyItems = data
          .filter((d) => d.status === 'Active')
          .map((proposal) => {
            return {
              id: proposal.proposalId,
              link: '/activity/' + proposal.proposalId,
              title: proposal.title,
              imageUrl: proposal.imageUrl,
              timeline: proposal.timeLine,
            };
          });
        this.updateUnreadCount();
      },
      error: (err: any) => {},
    });
  }

  isRead(notificationId: string): boolean {
    const readNotifications = this.getReadNotifications();
    return readNotifications.includes(notificationId);
  }

  markAsRead(notificationId: string) {
    // Lấy danh sách ID thông báo đã xem từ cookie
    let readNotifications: string[] = this.getReadNotifications();

    // Thêm ID thông báo vừa được click vào danh sách nếu chưa có
    if (!readNotifications.includes(notificationId)) {
      readNotifications.push(notificationId);
      // Lưu danh sách đã xem vào cookie
      this.cookieService.set(
        'read_notifications',
        JSON.stringify(readNotifications),
        { expires: 30 } // Cookie hết hạn sau 30 ngày
      );
      // Cập nhật số lượng thông báo chưa xem
      this.updateUnreadCount();
    }
  }

  getReadNotifications(): string[] {
    // Lấy danh sách ID thông báo đã xem từ cookie
    const readNotifications = this.cookieService.get('read_notifications');
    return readNotifications ? JSON.parse(readNotifications) : [];
  }

  updateUnreadCount() {
    // Lấy danh sách ID thông báo đã xem
    const readNotifications = this.getReadNotifications();
    // Tính số lượng thông báo chưa xem
    this.unreadCount = this.notifyItems.filter(
      (item) => !readNotifications.includes(item.id)
    ).length;
  }

}
