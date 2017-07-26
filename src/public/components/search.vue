<template>
  <div class="row">
    <div id="search" class="col s12 m8 l9">
      <form v-on:submit.prevent="search()">
        <div class="input-field col s12">
          <input placeholder="'a -> 'a" id="search_query" v-model="query" type="text" class="required" minlength="1">
          <label for="search_query">Query</label>
        </div>
        <button class="btn tooltipped" v-cloak v-bind:class="{ 'disabled': ! valid, 'waves-effect': valid, 'waves-light': valid }" data-position="bottom" data-delay="50" data-tooltip="search query requires non empty.">
          <i class="fa fa-search fa-inversex left"></i>
          Search
        </button>
        <div class="preloader-wrapper small active">
          <div class="spinner-layer spinner-green-only" v-cloak v-show="progress">
            <div class="circle-clipper left">
              <div class="circle"></div>
            </div>
            <div class="gap-patch">
              <div class="circle"></div>
            </div>
            <div class="circle-clipper right">
              <div class="circle"></div>
            </div>
          </div>
        </div>
      </form>
      <div class="row" v-cloak v-show="!(searched || raised_error)">
        <div class="col s12 m6">
          <div class="card light-blue darken-2">
            <div class="card-content white-text">
              <span class="card-title">Examples</span>
              <div class="collection">
                <a class="collection-item" v-on:click="search(&quot;'a -&gt; 'a&quot;)">'a -&gt; 'a</a>
                <a class="collection-item" v-on:click="search(&quot;?a -&gt; ?a&quot;)">?a -&gt; ?a</a>
                <a class="collection-item" v-on:click="search(&quot;id : 'a -&gt; 'a&quot;)">id : 'a -&gt; 'a</a>
                <a class="collection-item" v-on:click="search(&quot;map : _&quot;)">map : _</a>
                <a class="collection-item" v-on:click="search(&quot;*map* : _&quot;)">*map* : _</a>
                <a class="collection-item" v-on:click="search(&quot;(+) : _&quot;)">(+) : _</a>
                <a class="collection-item" v-on:click="search(&quot;? -&gt; list&lt;?&gt; -&gt; ?&quot;)">? -&gt; list&lt;?&gt; -&gt; ?</a>
                <a class="collection-item" v-on:click="search(&quot;DateTime -&gt; DayOfWeek&quot;)">DateTime -&gt; DayOfWeek</a>
                <a class="collection-item" v-on:click="search(&quot;(|_|) : Expr -&gt; ?&quot;)">(|_|) : Expr -&gt; ?</a>
                <a class="collection-item" v-on:click="search(&quot;List.* : _&quot;)">List.* : _</a>
                <a class="collection-item" v-on:click="search(&quot;new : string -&gt; Uri&quot;)">new : string -&gt; Uri</a>
                <a class="collection-item" v-on:click="search(&quot;'a -&gt; Option&lt;'a&gt;&quot;)">'a -&gt; Option&lt;'a&gt;</a>
                <a class="collection-item" v-on:click="search(&quot;Seq : _&quot;)">Seq : _</a>
                <a class="collection-item" v-on:click="search(&quot;{ let! } : Async&lt;'T&gt;&quot;)">{ let! } : Async&lt;'T&gt;</a>
              </div>
            </div>
          </div>
        </div>
      </div>
      <div class="row" v-cloak v-show="raised_error">
        <div class="col s10 m8">
          <div class="card red darken-2">
            <div class="card-content white-text">
              <span class="card-title">Error!</span>
              <pre>{{error_message}}</pre>
              <div class="card-action">
                <a href="https://github.com/pocketberserker/FSDN/issues">
                  <span class="octicon octicon-issue-opened"></span>
                  Report Issue
                </a>
              </div>
            </div>
          </div>
        </div>
      </div>
      <p v-if="searched && (! raised_error)">{{ api_count }} results.</p>
      <ul class="collapsible popout" data-collapsible="expandable" v-show="searched && (! raised_error)">
        <li v-for="result in search_results">
          <div class="collapsible-header">
            <span><font color="#BDBDBD" v-if="result.api.name.class_name">{{ result.api.name.class_name }}.</font>{{ result.api.name.id }} : {{ result.api.signature }}</span>
          </div>
          <div class="collapsible-body">
            <ul class="collection" id="assembly_info">
              <li class="collection-item" id="type_constraints" v-if="result.api.type_constraints">{{ result.api.type_constraints }}</span>
              <li class="collection-item" id="namespace" v-if="result.api.name.namespace">namespace: {{ result.api.name.namespace }}</li>
              <li class="collection-item" id="distance">distance: {{ result.distance }}</li>
              <li class="collection-item" id="kind">kind: {{ result.api.kind }}</li>
              <li class="collection-item" id="assembly">assembly: {{ result.api.assembly }}</li>
              <li class="collection-item" id="xml_doc" v-if="result.api.xml_doc">{{ result.api.xml_doc }}</li>
              <li class="collection-item" id="link" v-if="result.api.link"><a v-bind:href="result.api.link" target="_blank" rel="noopener">{{ result.api.link }}</a></li>
            </ul>
          </div>
        </li>
      </ul>
    </div>
    <div id="search_options" class="col s12 m4 l3">
      <p>
        <input type="checkbox" class="filled-in" v-model="respect_name_difference" id="respect_name_difference" />
        <label for="respect_name_difference">respect-name-difference</label>
      </p>
      <p>
        <input type="checkbox" class="filled-in" v-model="greedy_matching" id="greedy_matching" />
        <label for="greedy_matching">greedy-matching</label>
      </p>
      <p>
        <input type="checkbox" class="filled-in" v-model="ignore_parameter_style" id="ignore_parameter_style" />
        <label for="ignore_parameter_style">ignore-parameter-style</label>
      </p>
      <p>
        <input type="checkbox" class="filled-in" v-model="ignore_case" id="ignore_case" />
        <label for="ignore_case">ignore-case</label>
      </p>
      <p>
        <input type="checkbox" class="filled-in" v-model="swap_order" id="swap_order" />
        <label for="swap_order">swap-order</label>
      </p>
      <p>
        <input type="checkbox" class="filled-in" v-model="complement" id="complement" />
        <label for="complement">complement</label>
      </p>
      <p>
        <input type="checkbox" class="filled-in" v-model="single_letter_as_variable" id="single_letter_as_variable" />
        <label for="single_letter_as_variable">single-letter-as-variable</label>
      </p>
      <div class="card light-blue darken-2">
        <div class="card-content white-text">
          <span class="card-title">Target Assemblies</span>
          <div class="collection">
            <p class="collection-item" v-for="assembly in all_assemblies">
              <input type="checkbox" class="filled-in" v-bind:id="assembly.name" v-bind:value="assembly.name" v-model="assembly.checked" />
              <label v-bind:for="assembly.name">{{assembly.name}}</label>
            </p>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script lang="ts">
import {Vue, Component, Lifecycle} from "av-ts"
import axios from "axios";
import * as querystring from "querystring";
import {baseUrl, enabled, disabled} from "../util";

interface Assembly {
  name: string;
  checked: boolean
}

interface SearchInformation {
  query: string;
  exclusion: string;
  respect_name_difference: string;
  greedy_matching: string;
  ignore_parameter_style: string;
  ignore_case: string;
  swap_order: string;
  complement: string;
  language: string;
  single_letter_as_variable: string;
  limit: number;
}

function boolToStatus(value: boolean): string {
  return value ? enabled : disabled;
}

function statusToBool(status: string) {
  return status === enabled;
}

function validate(query: string): boolean {
  return Boolean(query);
}

const space = "+";

function buildExclusion(assemblies: Assembly[]): string {
  return assemblies.filter((a: Assembly) => !a.checked).map((a: Assembly) => a.name).join(space);
}

function searchApis(info: SearchInformation) {
  return axios
    .get(baseUrl + "/api/search", {
      params: info
    });
}

@Component
export default class Search extends Vue {
  query: string = undefined
  respect_name_difference = true
  greedy_matching = false
  ignore_parameter_style = true
  ignore_case = true
  swap_order = true
  complement = true
  single_letter_as_variable = true
  all_assemblies = <Assembly[]>[]
  progress = false
  search_results: any[] = undefined
  error_message: string = undefined

  get valid(): boolean {
    return validate(this.query);
  }

  get searched(): boolean {
    return this.search_results !== undefined;
  }

  get raised_error(): boolean {
    return this.error_message !== undefined;
  }

  get api_count(): number {
    return this.search_results.length;
  }

  search(input?: string) {
    const query = input ? input : this.query;
    if (this.progress) {
      return;
    } else if (validate(query)) {
      this.progress = true;
      searchApis({
        query,
        exclusion: buildExclusion(this.all_assemblies),
        respect_name_difference: boolToStatus(this.respect_name_difference),
        greedy_matching: boolToStatus(this.greedy_matching),
        ignore_parameter_style: boolToStatus(this.ignore_parameter_style),
        ignore_case: boolToStatus(this.ignore_case),
        swap_order: boolToStatus(this.swap_order),
        complement: boolToStatus(this.complement),
        single_letter_as_variable: boolToStatus(this.single_letter_as_variable),
        language: "",
        limit: 500
      })
        .then(res => {
          if (res.status !== 200) {
            this.error_message = res.data;
            this.search_results = [];
          } else {
            this.error_message = undefined;
            this.search_results = res.data.values;
          }
          this.query = query;
          this.progress = false;
        })
        .catch(err => {
          if (err.response){
            this.error_message = err.response.data;
          } else {
            this.error_message = err;
          }
          this.search_results = [];
          this.query = query;
          this.progress = false;
        });
    }
  }

  @Lifecycle beforeMount() {
    axios
      .get(baseUrl + "/api/assemblies")
      .then(res => {
        if (res.status !== 200) {
          this.error_message = res.data;
        } else {
          this.error_message = undefined;
          this.all_assemblies =
            res.data.values.map((a: any) => ({
              name: a.name,
              checked: a.checked
            }));
          if (window.location.search) {
            const queries = querystring.parse(window.location.search.substring(1));
            if (queries.exclusion) {
              const exclusion = queries.exclusion.split("+");
              this.all_assemblies.forEach((asm: any) => {
                if (exclusion.indexOf(asm.name) == -1) {
                  asm.checked = true;
                }
              });
            } else {
              this.all_assemblies.forEach((asm: any) => {
                asm.checked = true;
              });
            }
            if (queries.respect_name_difference) {
              this.respect_name_difference = statusToBool(queries.respect_name_difference);
            }
            if (queries.greedy_matching) {
              this.greedy_matching = statusToBool(queries.greedy_matching);
            }
            if (queries.ignore_parameter_style) {
              this.ignore_parameter_style = statusToBool(queries.ignore_parameter_style);
            }
            if (queries.ignore_case) {
              this.ignore_case = statusToBool(queries.ignore_case);
            }
            if (queries.swap_order) {
              this.swap_order = statusToBool(queries.swap_order);
            }
            if (queries.complement) {
              this.complement = statusToBool(queries.complement);
            }
            if (queries.single_letter_as_variable) {
              this.single_letter_as_variable = statusToBool(queries.single_letter_as_variable);
            }
            this.search(queries.query);
          }
        }
      })
      .catch(err => {
        this.error_message = err;
      });
  }
}
</script>
