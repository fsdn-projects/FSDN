window.onload = function(){

  var css = document.createElement("link");
  css.setAttribute("rel", "stylesheet");
  css.setAttribute("href", "https://cdnjs.cloudflare.com/ajax/libs/highlight.js/9.1.0/styles/default.min.css");
  document.getElementsByTagName("head")[0].appendChild(css);

  hljs.initHighlighting();
}

var js = document.createElement("script");
js.setAttribute("src", "https://cdnjs.cloudflare.com/ajax/libs/highlight.js/9.1.0/highlight.min.js");
document.body.appendChild(js);
