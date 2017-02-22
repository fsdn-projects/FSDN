<template>
  <div class="row">
    <div id="assemblies" class="col s8">
      <h2>Target Assemblies</h2>
      <div class="row" v-cloak v-show="raised_error">
        <div class="col s8 m6">
          <div class="card red darken-2">
            <div class="card-content white-text">
              <span class="card-title">Error!</span>
              <p>{{ error_message }}</p>
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
      <ul class="collection" v-cloak v-show="! raised_error">
        <li class="collection-item" v-for="assembly in assemblies">
          <img v-bind:src="assembly.icon_url" alt="" width="32px" height="32px" v-if="assembly.icon_url"></img>
          {{ assembly.name }} {{ assembly.version }}
          <a v-bind:href="'https://www.nuget.org/packages/' + assembly.name + '/' + assembly.version" class="secondary-content" v-if="! assembly.checked"><i class="fa fa-arrow-circle-right"></i></a>
        </li>
      </ul>
    </div>
  </div>
</template>

<script lang="ts">
import {Vue, Component, Lifecycle} from "av-ts"
import axios from "axios";
import {baseUrl} from "../util";

interface Assembly {
  version: string;
  name: string;
  checked: boolean;
  icon_url: string;
  assemblies: string[];
}

@Component
export default class Assemblies extends Vue {
  assemblies: Assembly[] = []
  error_message: string = undefined

  get raised_error(): boolean {
    return this.error_message !== undefined;
  }

  @Lifecycle beforeMount() {
    axios
      .get(baseUrl + "/api/assemblies")
      .then(res => {
        if (res.status !== 200) {
          this.error_message = res.data;
        } else {
          this.error_message = undefined;
          this.assemblies = res.data.values;
        }
      })
      .catch(err => {
        this.error_message = err;
      });
  }
}
</script>