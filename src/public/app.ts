import * as Vue from "vue"
import VueRouter from "vue-router"
import Search from "./components/search"
import Assemblies from "./components/assemblies"
import NotFound from "./components/notfound"

Vue.use(VueRouter)

const router = new VueRouter({
  mode: "history",
  routes: [
    {
      path: "/fsharp",
      name: "fsharp",
      component: Search,
      props: {
        language: "f#",
        examples: [
          { query: "'a -> 'a" },
          { query: "?a -> ?a" },
          { query: "id : 'a -> 'a" },
          { query: "map : _" },
          { query: "*map* : _" },
          { query: "(+) : _" },
          { query: "? -> list<?> -> ?" },
          { query: "DateTime -> DayOfWeek" },
          { query: "(|_|) : Expr -> ?" },
          { query: "List.* : _" },
          { query: "new : string -> Uri" },
          { query: "'a -> Option<'a>" },
          { query: "Seq : _" },
          { query: "{ let! } : Async<'T>" }
        ]
      },
      alias: "/"
    },
    {
      path: "/csharp",
      name: "csharp",
      component: Search,
      props: {
        language: "c#",
        examples: [
        ]
      }
    },
    {
      path: "/assemblies",
      name: "assemblies",
      component: Assemblies
    },
    {
      path: "*",
      component: NotFound
    }
  ]
})

const app = new Vue({
  router
}).$mount("#app")

app.$router.push("/");
app.$route.params;