var webpack = require("webpack");
var path = require("path");
var HtmlWebpackPlugin = require("html-webpack-plugin");

var srcDir = path.join(__dirname, "src/public");
var docDir = path.join(srcDir, "docs");
var outDir = path.join(__dirname, "bin/FSDN");
var isProd = process.env.NODE_ENV === 'production';

var config = {
  entry: {
    "assemblies": path.join(srcDir, "assemblies.ts"),
    "search": path.join(srcDir, "search.ts"),
    "en/query_spec": path.join(docDir, "en/query_spec.js"),
    "ja/query_spec": path.join(docDir, "ja/query_spec.js"),
    "notfound": path.join(srcDir, "notfound.ts")
  },
  output : {
    path: outDir,
    filename: '[name]-[chunkHash].js'
  },
  module : {
    rules: [
      {
        test: /\.vue$/,
        loader: 'vue-loader',
        options: {
          esModule: true
        }
      },
      {
        test: /\.ts$/,
        loader: 'ts-loader',
        include: srcDir,
        exclude: /node_modules/,
        options: {
          appendTsSuffixTo: [/\.vue$/]
        }
      },
      {
        test: /\.js$/,
        loader: 'babel-loader',
        exclude: /node_modules/,
        options: {
          presets: [
            "es2015"
          ]
        }
      },
      {
        test: /\.css$/,
        use: [
          "style-loader",
          "css-loader"
        ]
      },
      {
        test: /\.md$/,
        loader: 'vue-markdown-loader'
      }
    ],
  },
  devtool: "source-map",
  resolve: {
    extensions: ["*", ".js", ".ts", ".vue", ".md"],
    alias: {
      'vue$': 'vue/dist/vue.common.js'
    }
  },
  plugins: [
    new HtmlWebpackPlugin({
      chunks: ['assemblies'],
      filename: path.join(outDir, 'assemblies.html'),
      template: path.join(srcDir, 'views/template.html')
    }),
    new HtmlWebpackPlugin({
      chunks: ['search'],
      filename: path.join(outDir, 'index.html'),
      template: path.join(srcDir, 'views/template.html')
    }),
    new HtmlWebpackPlugin({
      chunks: ['en/query_spec'],
      filename: path.join(outDir, 'en/query_spec.html'),
      template: path.join(srcDir, 'views/template.html')
    }),
    new HtmlWebpackPlugin({
      chunks: ['ja/query_spec'],
      filename: path.join(outDir, 'ja/query_spec.html'),
      template: path.join(srcDir, 'views/template.html')
    }),
    new HtmlWebpackPlugin({
      chunks: ['notfound'],
      filename: path.join(outDir, '404.html'),
      template: path.join(srcDir, 'views/template.html')
    })
  ]
};

if (isProd) {
  config.plugins = [
    new webpack.DefinePlugin({
      "process.env.NODE_ENV": JSON.stringify("production")
    })
  ]
    .concat(config.plugins)
    .concat([
      new webpack.optimize.UglifyJsPlugin({
        compress: {
          warnings: false
        },
        output  : {
          comments: require("uglify-save-license")
        }
      })
    ]);
}
module.exports = config;