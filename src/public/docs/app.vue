<template>
  <div class="row">
    <div class="markdown-body">
      <markdown></markdown>
    </div>
  </div>
</template>

<script>
import 'highlight.js/styles/github.css'
import 'github-markdown-css'
import markdown from './README.md'

export default {
  components: {
    markdown
  }
}
</script>
<style>
  .markdown-body {
    box-sizing: border-box;
    min-width: 200px;
    max-width: 980px;
    margin: 10px;
  }
</style>
