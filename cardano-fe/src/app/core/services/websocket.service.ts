import { Injectable } from '@angular/core';
import { WebSocketSubject } from 'rxjs/webSocket';

@Injectable({ providedIn: 'root' })
export class WebSocketService {
  private socket$: WebSocketSubject<any>;

  constructor() {
    this.socket$ = new WebSocketSubject('wss://your-websocket-api.com');
  }

  sendMessage(msg: any) {
    this.socket$.next(msg);
  }

  getMessages() {
    return this.socket$;
  }
}
