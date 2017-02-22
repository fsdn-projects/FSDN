declare var require: any

import * as Vue from "vue"
var NotFound = require("./components/notfound").default

new Vue({
  el: "#app",
  components: {
    "app": NotFound
  },
  render: h => h("app")
})