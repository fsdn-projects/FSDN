var gulp = require("gulp");
var browserify = require("browserify");
var source = require("vinyl-source-stream");
var buffer = require("vinyl-buffer");
var uglify = require("gulp-uglify");
var sourcemaps = require("gulp-sourcemaps");
var glob = require('glob');
var exec = require('child_process').exec;

gulp.task("compile", function(cb) {
  exec("tsc -p ./src/front", function(err, stdout, stderr) {
    console.log(stdout);
    console.log(stderr);
    cb(err);
  });
});

gulp.task("pack", ["compile"], function() {
  var files = glob.sync("./bin/scripts/**/*.js");
  browserify({
    entries: files
  })
    .plugin("licensify")
    .bundle()
    .pipe(source("app.js"))
    .pipe(buffer())
    .pipe(sourcemaps.init({loadMaps: true}))
    .pipe(uglify({
      preserveComments: "license"
    }))
    .pipe(sourcemaps.write("./"))
    .pipe(gulp.dest("./bin/FSDN/"));
});

gulp.task("default", ["compile"]);
