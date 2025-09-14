import { Component, Inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NbCardModule, NbIconModule } from '@nebular/theme';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { References } from '../../../../core/services/drep.service';


@Component({
	selector: 'app-drep-detail-modal',
	standalone: true,
	imports: [CommonModule, NbIconModule, NbCardModule],
	template: `
    <div class="container-fluid p-4">
      <div class="d-flex justify-content-end">
        <button class="close-btn" (click)="close()">
          <nb-icon icon="close"></nb-icon>
        </button>
      </div>
    </div>
		<div class="p-4"> 
			<nb-card>
				<nb-card-header>
					<div class="header-container d-flex justify-content-between align-items-center">
						<div class="header-main d-flex justify-content-between align-items-center">
							<div class="d-flex align-items-center">
								<img src="assets/icons/logo.png" alt="Cardano Logo" class="logo">
								<span>{{ data.title }}</span>
							</div>
						</div>
							<div class="icon-container">
                <div *ngIf="data.cardData.references.length" class="d-flex align-items-center">
                  <div *ngFor="let ref of data.cardData.references">
                    <ng-container [ngSwitch]="getReferenceType(ref)">
                      <a *ngSwitchCase="'Twitter'" [href]="ref.uri" target="_blank" class="reference-link">
                        <nb-icon icon="twitter-outline" pack="eva"></nb-icon>
                      </a>
                      <a *ngSwitchCase="'Github'" [href]="ref.uri" target="_blank" class="reference-link">
                        <nb-icon icon="github-outline" pack="eva"></nb-icon>
                      </a>
                      <a *ngSwitchCase="'Telegram'" [href]="ref.uri" target="_blank" class="reference-link">
                        <img src="../../../assets/icons/telegram-icon.svg" alt="Telegram Icon" />
                      </a>
                      <a *ngSwitchCase="'Discord'" [href]="ref.uri" target="_blank" class="reference-link">
                        <img src="../../../assets/icons/discord-icon.svg" alt="Discord Icon" />
                      </a>
                      <a *ngSwitchCase="'Youtube'" [href]="ref.uri" target="_blank" class="reference-link">
                        <img src="../../../assets/icons/youtube-icon.svg" alt="Youtube Icon" />
                      </a>
                      <a *ngSwitchCase="'Linkedin'" [href]="ref.uri" target="_blank" class="reference-link">
                        <img src="../../../assets/icons/linkedin-icon.svg" alt="Linkedin Icon" />
                      </a>
                      <a *ngSwitchCase="'Linktree'" [href]="ref.uri" target="_blank" class="reference-link">
                        <img src="../../../assets/icons/linktree-icon.svg" alt="Linktree Icon" />
                      </a>
                      <a *ngSwitchDefault [href]="ref.uri" target="_blank" class="reference-link">
                        <nb-icon icon="globe-outline" pack="eva"></nb-icon>
                      </a>
                    </ng-container>
                  </div>
                </div>
							</div>
					</div>
				</nb-card-header>

				<nb-card-body>
					<div class="table-responsive">
						<h6>Objectives</h6>
						<p>{{ data.cardData.objectives }}</p>

						<h6>Motivations</h6>
						<p>{{ data.cardData.motivations }}</p>

						<h6>Qualifications</h6>
						<p>{{ data.cardData.qualifications }}</p>
					</div>
				</nb-card-body>
			</nb-card>
		</div>
  `,
	styles: [`
    .close-btn {
      background: none;
      border: none;
      cursor: pointer;
      padding: 8px;
    }

		nb-card {
      border-radius: 8px;
      box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
      overflow: hidden;
    }

		nb-card-header {
      background-color: #f7f9fc;
      padding: 16px;
    }

		.header-main {
      display: flex;
    }

    .header-main .logo {
      width: 30px;
      height: 30px;
      margin-right: 12px;
      border-radius: 5px;
    }

    .header-main span {
      font-size: 16px;
      font-weight: 600;
      color: #808080;
      line-height: 1.2;
      align-items: center;
      margin: 0;
    }

    .icon-container img {
      cursor: pointer;
    }

		nb-card-body {
      padding: 16px;
    }

    .table-responsive {
      overflow-x: auto;
      width: 100%;
    }

    .table-responsive h6 {
      font-size: 16px;
      font-weight: 600;
    }

    .table-responsive p {
      font-size: 14px;
    }

        .icon-container {
			display: flex;
			align-items: center;
			gap: 8px;
			
      img {
        cursor: pointer;
      }

      .reference-link {
        display: flex;
        align-items: center;
        justify-content: center;
        width: 32px;
        height: 32px;
        color: #666;
        text-decoration: none;
        border-radius: 4px;
        transition: all 0.2s ease;
        margin-right: 4px;

        nb-icon {
          font-size: 18px;
		  
		  &:hover {
			color: #666 !important;
		  }
        }

        img {
          width: 18px;
          height: 18px;
        }
      }
    }
  `]
})
export class DrepDetailModalComponent {

	constructor(
		public dialogRef: MatDialogRef<DrepDetailModalComponent>,
		@Inject(MAT_DIALOG_DATA) public data: {
			title: string;
			cardData: {
				objectives: string;
				motivations: string;
				qualifications: string;
        references: References[];
			}
		}
	) { }

	close() {
		this.dialogRef.close();
	}

  getReferenceType(ref: any): string {
    if (!ref?.uri || typeof ref.uri !== 'string') return 'default';
    const uri = ref.uri.toLowerCase();
    if (uri.includes('twitter.com') || uri.includes('x.com')) return 'Twitter';
    if (uri.includes('github.com')) return 'Github';
    if (uri.includes('t.me')) return 'Telegram';
    if (uri.includes('discord.gg') || uri.includes('discord.com')) return 'Discord';
    if (uri.includes('youtube.com') || uri.includes('youtu.be')) return 'Youtube';
    if (uri.includes('linkedin.com')) return 'Linkedin';
    if (uri.includes('linktr.ee')) return 'Linktree';
    return 'default';
  }
} 