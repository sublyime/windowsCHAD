// Electron preload types
declare module 'electron' {
  interface IpcRenderer {
    invoke(channel: string, ...args: any[]): Promise<any>;
    send(channel: string, ...args: any[]): void;
    on(channel: string, func: (...args: any[]) => void): void;
    once(channel: string, func: (...args: any[]) => void): void;
    removeListener(channel: string, func: (...args: any[]) => void): void;
    removeAllListeners(channel: string): void;
  }

  interface ContextBridge {
    exposeInMainWorld(apiKey: string, api: any): void;
  }
}