const { contextBridge, ipcRenderer } = require('electron');

contextBridge.exposeInMainWorld('petHost', {
  bridgePort: Number(process.env.HACHIMI_PET_PORT || process.env.AGY_WHALE_PORT || 17890),
  setMouseCapture(capture) {
    ipcRenderer.send('petHost:mouseCapture', !!capture);
  },
  focusWindow() {
    ipcRenderer.send('petHost:focusWindow');
  },
  blurWindow() {
    ipcRenderer.send('petHost:blurWindow');
  },
  openExternal(url) {
    ipcRenderer.send('petHost:openExternal', url);
  },
  quitApp() {
    ipcRenderer.send('petHost:quit');
  },
  pickDirectory() {
    return ipcRenderer.invoke('petHost:pickDirectory');
  },
  onCursor(callback) {
    const listener = (_event, pt) => callback(pt);
    ipcRenderer.on('petHost:cursor', listener);
    return () => ipcRenderer.removeListener('petHost:cursor', listener);
  },
  onCommand(callback) {
    const listener = (_event, cmd) => callback(cmd);
    ipcRenderer.on('petHost:command', listener);
    return () => ipcRenderer.removeListener('petHost:command', listener);
  },
});
