/// <reference path="../../typings/index.d.ts" />
"use strict";
import Vue = require("vue");
import * as request from "superagent";
import * as querystring from "querystring";
import {baseUrl, enabled, disabled} from "./util";

interface Assembly {
  name: string;
  checked: boolean
}

interface SearchInformation {
  query: string;
  strict: string;
  similarity: string;
  ignore_arg_style: string;
}

function boolToStatus(value: boolean): string {
  return value ? enabled : disabled;
}

function validate(query: string): boolean {
  return Boolean(query);
}

function buildQuery(query: string, assemblies: Assembly[]): string {
  const space = "+";
  const asms = assemblies.filter((a: Assembly) => !a.checked).map((a: Assembly) => "-" + a.name).join(space);
  return asms ? query + space + asms : query;
}

function searchApis(info: SearchInformation) {
  return request
    .get(baseUrl + "/api/search")
    .query(info);
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
    search
  }
});

function search(input?: string) {
  const query = input ? input : app.$get("query");
  if (! app.$get("hide_progress")) {
    return;
  } else if (validate(query)) {
    app.$set("hide_progress", false);
    searchApis({
      query: buildQuery(query, app.$get("all_assemblies")),
      strict: boolToStatus(app.$get("strict")),
      similarity: boolToStatus(app.$get("similarity")),
      ignore_arg_style: boolToStatus(app.$get("ignore_arg_style"))
    })
      .end((err, res) => {
        if (err || !res.ok) {
          app.$set("error_message", res.text);
          app.$set("search_results", []);
        } else {
          app.$set("error_message", undefined);
          app.$set("search_results", JSON.parse(res.text).values);
        }
        app.$set("hide_progress", true);
      });
  }
}

function setStatus(name: string, status: string) {
  if (status === enabled) {
    app.$set(name, true);
  } else if (status === disabled) {
    app.$set(name, false);
  }
}

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
      if (window.location.search) {
        const queries = querystring.parse(window.location.search.substring(1));
        if (queries.strict) {
          setStatus("strict", queries.strict);
        }
        if (queries.similarity) {
          setStatus("similarity", queries.similarity);
        }
        if (queries.ignore_arg_style) {
          setStatus("ignore_arg_style", queries.ignore_arg_style);
        }
        search(queries.query);
      }
    }
  });
