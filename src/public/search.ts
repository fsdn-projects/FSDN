import * as Vue from "vue"
import Search from "./components/search"

new Vue({
  el: "#app",
  components: {
    "app": Search
  },
  render: h => h("app")
})