import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () => import('./features/auth/login/login.component').then(m => m.LoginComponent)
  },
  {
    path: 'dashboard',
    canActivate: [authGuard],
    loadComponent: () => import('./features/dashboard/dashboard.component').then(m => m.DashboardComponent)
  },
  {
    path: 'exercise/generate',
    canActivate: [authGuard],
    loadComponent: () => import('./features/exercises/generator/generator.component').then(m => m.GeneratorComponent)
  },
  {
    path: 'exercise/:id',
    canActivate: [authGuard],
    loadComponent: () => import('./features/exercises/players/exercise-player.component').then(m => m.ExercisePlayerComponent)
  },
  {
    path: 'exercise/:id/result',
    canActivate: [authGuard],
    loadComponent: () => import('./features/exercises/result/result.component').then(m => m.ResultComponent)
  },
  {
    path: 'progress',
    canActivate: [authGuard],
    loadComponent: () => import('./features/progress/progress.component').then(m => m.ProgressComponent)
  },
  { path: '', redirectTo: '/dashboard', pathMatch: 'full' },
  { path: '**', redirectTo: '/dashboard' }
];
