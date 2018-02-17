import Vue, {ComponentOptions} from "vue"
import Assemblies from "./components/assemblies"

new Vue({
  el: "#app",
  components: {
    "app": Assemblies
  },
  render: h => h("app")
} as ComponentOptions<Vue>)