var gulp = require("gulp");
var browserify = require("browserify");
var source = require("vinyl-source-stream");
var buffer = require("vinyl-buffer");
var uglify = require("gulp-uglify");
var sourcemaps = require("gulp-sourcemaps");
var glob = require('glob');
var exec = require('child_process').exec;
var path = require("path");

gulp.task("compile", function(cb) {
  exec("tsc -p ./src/front", function(err, stdout, stderr) {
    console.log(stdout);
    console.log(stderr);
    cb(err);
  });
});

var targets = glob.sync("./src/front/!(util).ts")
  .map(function(target) {
    return path.basename(target, ".ts");
  });
targets.forEach(function(target) {
  gulp.task("pack-" + target, ["compile"], function() {
    return browserify({
      entries: "./bin/scripts/" + target + ".js"
    })
      .plugin("licensify")
      .bundle()
      .pipe(source(target + ".js"))
      .pipe(buffer())
      .pipe(sourcemaps.init({loadMaps: true}))
      .pipe(uglify({
          preserveComments: "license"
      }))
      .pipe(sourcemaps.write("./"))
      .pipe(gulp.dest("./bin/FSDN/"));
  });
});

gulp.task("pack", targets.map(function(target) { return "pack-" + target; }));

gulp.task("default", ["compile"]);
