declare var require: any

import Vue, {ComponentOptions} from "vue"
var NotFound = require("./components/notfound").default

new Vue({
  el: "#app",
  components: {
    "app": NotFound
  },
  render: h => h("app")
} as ComponentOptions<Vue>)