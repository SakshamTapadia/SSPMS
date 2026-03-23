import { Injectable } from '@angular/core';

export const MAX_TAB_SWITCHES = 3;

@Injectable({ providedIn: 'root' })
export class ProctorService {
  private mediaStream: MediaStream | null = null;
  violationCount = 0;   // fullscreen exits
  tabSwitchCount = 0;   // visibility-change switches

  async requestPermissions(): Promise<{ granted: boolean; error?: string }> {
    try {
      this.mediaStream = await navigator.mediaDevices.getUserMedia({ video: true, audio: true });
      return { granted: true };
    } catch (err: any) {
      return { granted: false, error: err?.message ?? 'Camera and microphone access denied.' };
    }
  }

  getMediaStream(): MediaStream | null {
    return this.mediaStream;
  }

  async enterFullscreen(): Promise<void> {
    if (!document.fullscreenElement) {
      await document.documentElement.requestFullscreen();
    }
  }

  isFullscreen(): boolean {
    return !!document.fullscreenElement;
  }

  /** Registers a fullscreenchange listener and returns an unsubscribe fn. */
  onFullscreenChange(handler: () => void): () => void {
    document.addEventListener('fullscreenchange', handler);
    return () => document.removeEventListener('fullscreenchange', handler);
  }

  /**
   * Registers a visibilitychange listener (tab/app switch detection).
   * Handler is called only when the tab becomes hidden (user switched away).
   * Returns an unsubscribe fn.
   */
  onVisibilityChange(handler: () => void): () => void {
    const listener = () => { if (document.hidden) handler(); };
    document.addEventListener('visibilitychange', listener);
    return () => document.removeEventListener('visibilitychange', listener);
  }

  recordTabSwitch(): number {
    return ++this.tabSwitchCount;
  }

  stopMedia(): void {
    this.mediaStream?.getTracks().forEach(t => t.stop());
    this.mediaStream = null;
  }

  cleanup(): void {
    this.stopMedia();
    this.violationCount = 0;
    this.tabSwitchCount = 0;
    if (document.fullscreenElement) {
      document.exitFullscreen().catch(() => {});
    }
  }
}
