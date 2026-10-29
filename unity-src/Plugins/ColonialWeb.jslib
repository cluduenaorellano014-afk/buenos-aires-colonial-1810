// Browser bridge for Colonial.ColonialGame (WebGL builds only).
mergeInto(LibraryManager.library, {
  // 1 when the game runs inside the colonial map page (iframe).
  ColonialIsEmbedded: function () {
    try { return window.self !== window.top ? 1 : 0; } catch (e) { return 1; }
  },
  // Close the 3D view: the map page listens for this message; standalone, go to the map page.
  ColonialBackToMap: function () {
    try {
      if (window.self !== window.top) { window.parent.postMessage({ tipo: "volverMapa" }, "*"); return; }
    } catch (e) {}
    window.location.href = "../buenos_aires_colonial.html";
  }
});
