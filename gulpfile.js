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

var pack = function(path, name) {
  var files = glob.sync(path);
  return browserify({
    entries: files
  })
    .plugin("licensify")
    .bundle()
    .pipe(source(name + ".js"))
    .pipe(buffer())
    .pipe(sourcemaps.init({loadMaps: true}))
    .pipe(uglify({
      preserveComments: "license"
    }))
    .pipe(sourcemaps.write("./"))
    .pipe(gulp.dest("./bin/FSDN/"));
};

gulp.task("pack-search", ["compile"], function() {
  pack("./bin/scripts/search.js", "search");
});

gulp.task("pack-libraries", ["compile"], function() {
  pack("./bin/scripts/libraries.js", "libraries");
});

gulp.task("pack", ["pack-search", "pack-libraries"]);

gulp.task("default", ["compile"]);
