import { InjectionToken } from '@angular/core';
import { environment } from '../environments/environment';

export const REMATCH_BUTTON_ENABLED = new InjectionToken<boolean>('REMATCH_BUTTON_ENABLED', {
  providedIn: 'root',
  factory: () => environment.features.rematchButton
});
