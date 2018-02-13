import Vue, {ComponentOptions} from "vue"
import Search from "./components/search"

new Vue({
  el: "#app",
  components: {
    "app": Search
  },
  render: h => h("app")
} as ComponentOptions<Vue>)