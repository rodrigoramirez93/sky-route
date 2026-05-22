import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    redirectTo: 'search',
  },
  {
    path: 'search',
    loadChildren: () => import('./features/search').then((m) => m.searchFeatureRoutes),
  },
  {
    path: 'book',
    loadChildren: () => import('./features/book').then((m) => m.bookFeatureRoutes),
  },
];
