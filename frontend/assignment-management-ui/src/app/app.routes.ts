import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { roleGuard } from './core/guards/role.guard';
import { ShellComponent } from './layout/shell.component';
import { LoginComponent } from './features/auth/login/login.component';
import { DashboardComponent } from './features/dashboard/dashboard.component';
import { UsersComponent } from './features/admin/users/users.component';
import { ClassesComponent } from './features/admin/classes/classes.component';
import { SubjectsComponent } from './features/admin/subjects/subjects.component';
import { AllocationsComponent } from './features/admin/allocations/allocations.component';
import { AdminAssignmentsComponent } from './features/admin/assignments/admin-assignments.component';
import { TeacherAssignmentsComponent } from './features/teacher/assignments/teacher-assignments.component';
import { AssignmentFormComponent } from './features/teacher/assignment-form/assignment-form.component';
import { SubmissionsComponent } from './features/teacher/submissions/submissions.component';
import { StudentAssignmentsComponent } from './features/student/assignments/student-assignments.component';
import { StudentAssignmentDetailComponent } from './features/student/assignment-detail/student-assignment-detail.component';
import { MySubmissionsComponent } from './features/student/my-submissions/my-submissions.component';

/**
 * Application routes. Authenticated pages live under the shell; each feature route is
 * additionally guarded by role for navigation UX only — the backend authorizes every
 * request and remains the security boundary.
 */
export const routes: Routes = [
  { path: 'login', component: LoginComponent },
  {
    path: '',
    component: ShellComponent,
    canActivate: [authGuard],
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
      { path: 'dashboard', component: DashboardComponent },

      // Admin
      { path: 'admin/users', component: UsersComponent, canActivate: [roleGuard('Admin')] },
      { path: 'admin/classes', component: ClassesComponent, canActivate: [roleGuard('Admin')] },
      { path: 'admin/subjects', component: SubjectsComponent, canActivate: [roleGuard('Admin')] },
      { path: 'admin/allocations', component: AllocationsComponent, canActivate: [roleGuard('Admin')] },
      { path: 'admin/assignments', component: AdminAssignmentsComponent, canActivate: [roleGuard('Admin')] },
      { path: 'admin/assignments/:id/submissions', component: SubmissionsComponent, canActivate: [roleGuard('Admin')] },

      // Teacher
      { path: 'teacher/assignments', component: TeacherAssignmentsComponent, canActivate: [roleGuard('Teacher')] },
      { path: 'teacher/assignments/new', component: AssignmentFormComponent, canActivate: [roleGuard('Teacher')] },
      { path: 'teacher/assignments/:id/edit', component: AssignmentFormComponent, canActivate: [roleGuard('Teacher')] },
      { path: 'teacher/assignments/:id/submissions', component: SubmissionsComponent, canActivate: [roleGuard('Teacher')] },

      // Student
      { path: 'student/assignments', component: StudentAssignmentsComponent, canActivate: [roleGuard('Student')] },
      { path: 'student/assignments/:id', component: StudentAssignmentDetailComponent, canActivate: [roleGuard('Student')] },
      { path: 'student/submissions', component: MySubmissionsComponent, canActivate: [roleGuard('Student')] },
    ],
  },
  { path: '**', redirectTo: 'dashboard' },
];
