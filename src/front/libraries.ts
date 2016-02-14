/// <reference path="../../typings/main.d.ts" />
"use strict";
import Vue = require("vue");
import * as request from "superagent";

let app = new Vue({
  el: "#app",
  data: {
    libraries: []
  }
});

const baseUrl =
  function() {
    let u = window.location.href.split("/");
    u.pop();
    return u.join("/");
  }();

request
  .get(baseUrl + "/api/libraries")
  .end((err, res) => {
    if (err || !res.ok) {
    } else {
      app.$set("libraries", JSON.parse(res.text).libraries);
    }
  });
