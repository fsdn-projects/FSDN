"use strict";

export const baseUrl =
  function() {
    let u = window.location.href.split("/");
    u.pop();
    return u.join("/");
  }();
