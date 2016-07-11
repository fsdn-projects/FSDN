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
    progress: false,
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

const queryLiteral = "query";
const progress = "progress";
const errorMessage = "error_message";
const searchResults = "search_results";
const allAssemblies = "all_assemblies";
const strictLiteral = "strict";
const similarity = "similarity";
const ignoreArgStyle = "ignore_arg_style";

function search(input?: string) {
  const query = input ? input : app.$get(queryLiteral);
  if (app.$get(progress)) {
    return;
  } else if (validate(query)) {
    app.$set(progress, true);
    searchApis({
      query: buildQuery(query, app.$get(allAssemblies)),
      strict: boolToStatus(app.$get(strictLiteral)),
      similarity: boolToStatus(app.$get(similarity)),
      ignore_arg_style: boolToStatus(app.$get(ignoreArgStyle))
    })
      .end((err, res) => {
        if (err || !res.ok) {
          app.$set(errorMessage, res.text);
          app.$set(searchResults, []);
        } else {
          app.$set(errorMessage, undefined);
          app.$set(searchResults, JSON.parse(res.text).values);
        }
        app.$set(queryLiteral, query);
        app.$set(progress, false);
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
      app.$set(errorMessage, res.text);
    } else {
      app.$set(errorMessage, undefined);
      app.$set(
        allAssemblies,
        JSON.parse(res.text).values.map((a: any) => ({
          name: a.name,
          checked: a.checked
        }))
      );
      if (window.location.search) {
        const queries = querystring.parse(window.location.search.substring(1));
        if (queries.strict) {
          setStatus(strictLiteral, queries.strict);
        }
        if (queries.similarity) {
          setStatus(similarity, queries.similarity);
        }
        if (queries.ignore_arg_style) {
          setStatus(ignoreArgStyle, queries.ignore_arg_style);
        }
        search(queries.query);
      }
    }
  });
