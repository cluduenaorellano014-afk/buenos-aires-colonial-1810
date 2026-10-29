// Browser device detection for DeviceInfo.cs (WebGL builds only).
mergeInto(LibraryManager.library, {
  // Phones/tablets, including iPadOS Safari, which reports itself as a Mac.
  SalaIsMobileBrowser: function () {
    var ua = navigator.userAgent || "";
    var iPadAsMac = /Macintosh/.test(ua) && navigator.maxTouchPoints > 1;
    return (/Android|iPhone|iPad|iPod|Mobile|Silk|Kindle/i.test(ua) || iPadAsMac) ? 1 : 0;
  },
  SalaIsIOS: function () {
    var ua = navigator.userAgent || "";
    return (/iPhone|iPad|iPod/.test(ua) || (/Macintosh/.test(ua) && navigator.maxTouchPoints > 1)) ? 1 : 0;
  }
});
