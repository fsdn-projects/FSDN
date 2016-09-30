/// <reference path="../../typings/index.d.ts" />
"use strict";
import Vue = require("vue");
import * as request from "superagent";
import {baseUrl} from "./util";
import {tweet, sideTweet} from "./tweet";

let tw = tweet;
let side = sideTweet;

let app = new Vue({
  el: "#app",
  data: {
    assemblies: [],
    error_message: undefined
  },
  computed: {
    raised_error: function(): boolean {
      return this.error_message !== undefined;
    }
  }
});

const errorMessage = "error_message";

request
  .get(baseUrl + "/api/assemblies")
  .end((err, res) => {
    if (err || !res.ok) {
      app.$set(errorMessage, res.text);
    } else {
      app.$set(errorMessage, undefined);
      app.$set("assemblies", JSON.parse(res.text).values);
    }
  });
