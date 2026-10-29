import { InjectionToken } from '@angular/core';

// Neutral stub for the red phase. The real token will read environment.features.rematchButton.
export const REMATCH_BUTTON_ENABLED = new InjectionToken<boolean>('REMATCH_BUTTON_ENABLED', {
  providedIn: 'root',
  factory: () => false
});
