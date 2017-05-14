var webpack = require("webpack");
var path = require("path");
var HtmlWebpackPlugin = require("html-webpack-plugin");

var srcDir = path.join(__dirname, "src/public");
var docDir = path.join(srcDir, "docs");
var outDir = path.join(__dirname, "bin/FSDN");
var isProd = process.env.NODE_ENV === 'production';

var config = {
  entry: {
    "app": path.join(srcDir, "app.ts")
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
      chunks: ['app'],
      filename: path.join(outDir, 'index.html'),
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