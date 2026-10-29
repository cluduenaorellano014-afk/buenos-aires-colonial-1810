// Pointer lock driven by the page instead of Unity's Cursor.lockState: Safari only accepts
// requestPointerLock() inside a real click handler and throws otherwise, which stopped the game.
mergeInto(LibraryManager.library, {
  WebPointerLockInit: function () {
    if (window.__plInit) return;
    window.__plInit = true;
    window.__plWant = false;
    var canvas = Module.canvas || document.querySelector("canvas");
    canvas.addEventListener("pointerdown", function (e) {
      if (!window.__plWant || e.pointerType === "touch" || document.pointerLockElement === canvas) return;
      try {
        var p = canvas.requestPointerLock();
        if (p && p.catch) p.catch(function () {});
      } catch (err) {}
    });
  },
  WebPointerLockWant: function (want) { window.__plWant = !!want; },
  WebPointerLockIsLocked: function () {
    var canvas = Module.canvas || document.querySelector("canvas");
    return document.pointerLockElement === canvas ? 1 : 0;
  },
  WebPointerLockRelease: function () {
    try { if (document.pointerLockElement) document.exitPointerLock(); } catch (err) {}
  }
});
