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
  respect_name_difference: string;
  greedy_matching: string;
  ignore_parameter_style: string;
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
    respect_name_difference: true,
    greedy_matching: false,
    ignore_parameter_style: true,
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
const respectNameDifference = "respect_name_difference";
const greedyMatching = "greedy_matching";
const ignoreParameterStyle = "ignore_parameter_style";

function search(input?: string) {
  const query = input ? input : app.$get(queryLiteral);
  if (app.$get(progress)) {
    return;
  } else if (validate(query)) {
    app.$set(progress, true);
    searchApis({
      query: buildQuery(query, app.$get(allAssemblies)),
      respect_name_difference: boolToStatus(app.$get(respectNameDifference)),
      greedy_matching: boolToStatus(app.$get(greedyMatching)),
      ignore_parameter_style: boolToStatus(app.$get(ignoreParameterStyle))
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
        if (queries.respect_name_difference) {
          setStatus(respectNameDifference, queries.respect_name_difference);
        }
        if (queries.greedy_matching) {
          setStatus(greedyMatching, queries.greedy_matching);
        }
        if (queries.ignore_parameter_style) {
          setStatus(ignoreParameterStyle, queries.ignore_parameter_style);
        }
        search(queries.query);
      }
    }
  });
