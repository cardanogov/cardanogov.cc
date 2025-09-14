import { RouterModule, Routes } from "@angular/router";
import { NgModule } from "@angular/core";
import { PagesComponent } from "./pages.component";

const routes: Routes = [
  {
    path: "",
    component: PagesComponent,
    children: [
      {
        path: "",
        loadChildren: () => import('./dashboard/dashboard.module').then(m => m.DashboardModule),
      },
      {
        path: "dashboard",
        loadChildren: () => import('./dashboard/dashboard.module').then(m => m.DashboardModule),
      },
      {
        path: "dreps",
        loadChildren: () => import('./drep/drep.module').then(m => m.DrepModule),
      },
      {
        path: "voting",
        loadChildren: () => import('./voting/voting.module').then(m => m.VotingModule),
      },
      {
        path: "activity",
        loadChildren: () => import('./activity/activity.module').then(m => m.ActivityModule),
      },
      {
        path: "spo",
        loadChildren: () => import('./spo/spo.module').then(m => m.SpoModule),
      },
      {
        path: "cc",
        loadChildren: () => import('./cc/cc.module').then(m => m.CcModule),
      },
      {
        path: "more",
        loadChildren: () => import('./more/more.module').then(m => m.MoreModule),
      }
    ],
  },
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule],
})
export class PagesRoutingModule { }
