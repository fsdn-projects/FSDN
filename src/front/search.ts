/// <reference path="../../typings/main.d.ts" />
"use strict";
import Vue = require("vue");
import * as request from "superagent";
import {baseUrl} from "./util";

function boolToStatus(value: boolean): string {
  return value ? "enabled" : "disabled";
}

function validate(query: string): boolean {
  return Boolean(query);
}

function search(query: string, strict: boolean, similarity: boolean) {
  return request
    .get(baseUrl + "/api/search")
    .query({
      query,
      strict: boolToStatus(strict),
      similarity: boolToStatus(similarity)
    });
}

let app = new Vue({
  el: "#app",
  data: {
    query: undefined,
    strict: true,
    similarity: false,
    hide_progress: true,
    search_results: undefined,
    error_message: undefined
  },
  computed: {
    valid: function(): boolean {
      return validate(this.query);
    },
    searched: function(): boolean {
      return this.search_results !== undefined;
    },
    raised_error: function(): boolean {
      return this.error_message !== undefined;
    }
  },
  methods: {
    search: function(input?: string) {
      const query = input ? input : this.query;
      if (! this.hide_progress) {
        return;
      } else if (validate(query)) {
        this.hide_progress = false;
        search(query, this.strict, this.similarity)
          .end((err, res) => {
            if (err || !res.ok) {
              this.error_message = res.text;
              this.search_results = [];
            } else {
              this.error_message = undefined;
              this.search_results = JSON.parse(res.text).values;
            }
            this.hide_progress = true;
          });
      }
    }
  }
});
