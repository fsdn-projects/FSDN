/// <reference path="../../typings/index.d.ts" />
"use strict";
import Vue = require("vue");
import * as request from "superagent";
import {baseUrl} from "./util";

interface SearchOptions {
  strict: string;
  similarity: string;
  ignore_arg_style: string;
}

interface Assembly {
  name: string;
  checked: boolean
}

interface SearchInformation {
  query: string;
  search_options: SearchOptions;
  target_assemblies: string[];
}

function boolToStatus(value: boolean): string {
  return value ? "enabled" : "disabled";
}

function validate(query: string): boolean {
  return Boolean(query);
}

function search(info: SearchInformation) {
  return request
    .post(baseUrl + "/api/search")
    .send(info);
}

let app = new Vue({
  el: "#app",
  data: {
    query: undefined,
    strict: true,
    similarity: false,
    ignore_arg_style: true,
    all_assemblies: <Assembly[]>[],
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
        search({
          query,
          search_options: {
            strict: boolToStatus(this.strict),
            similarity: boolToStatus(this.similarity),
            ignore_arg_style: boolToStatus(this.ignore_arg_style)
          },
          target_assemblies:
            this.all_assemblies.filter((a: Assembly) => a.checked)
              .map((a: Assembly) => a.name)
        })
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

request
  .get(baseUrl + "/api/assemblies")
  .end((err, res) => {
    if (err || !res.ok) {
      app.$set("error_message", res.text);
    } else {
      app.$set("error_message", undefined);
      app.$set(
        "all_assemblies",
        JSON.parse(res.text).values.map((a: any) => ({
          name: a.name,
          checked: a.checked
        }))
      );
    }
  });
