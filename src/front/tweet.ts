/// <reference path="../../typings/index.d.ts" />
"use strict";
import Vue = require("vue");
import * as url from "url";
import {baseUrl} from "./util";

export const defaultTweet = "https://twitter.com/intent/tweet?text=FSDN%20is%20awesome%21%21&url=" + encodeURIComponent(baseUrl) + "&hashtags=fsdn%2Cfsharp";

export let tweet = new Vue({
  el: "#tweet",
  data: {
    link: defaultTweet
  }
});

export let sideTweet = new Vue({
  el: "#side_tweet",
  data: {
    link: defaultTweet
  }
});

export function link(path: string): void {
  const uri = baseUrl + "/search/" + path;
  const link = "https://twitter.com/intent/tweet?text=search%20result&url=" + encodeURIComponent(uri) + "&hashtags=fsdn%2Cfsharp";
  tweet.$set("link", link);
  sideTweet.$set("link", link);
};
