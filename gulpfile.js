var gulp = require("gulp");
var browserify = require("browserify");
var source = require("vinyl-source-stream");
var buffer = require("vinyl-buffer");
var uglify = require("gulp-uglify");
var sourcemaps = require("gulp-sourcemaps");
var glob = require('glob');
var exec = require('child_process').exec;
var path = require("path");
var rev = require('gulp-rev');
var extend = require('gulp-extend');
var revReplace = require("gulp-rev-replace");

var outDir = "./bin/FSDN/";
var scriptDir = "./bin/scripts/";

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
      entries: scriptDir + target + ".js"
    })
      .plugin("licensify")
      .bundle()
      .pipe(source(target + ".js"))
      .pipe(buffer())
      .pipe(sourcemaps.init({loadMaps: true}))
      .pipe(uglify({
          preserveComments: "license"
      }))
      .pipe(rev())
      .pipe(sourcemaps.write("./"))
      .pipe(gulp.dest(outDir))
      .pipe(rev.manifest("rev-" + target + "-manifest.json"))
      .pipe(gulp.dest(scriptDir));
  });
});

gulp.task("pack", targets.map(function(target) { return "pack-" + target; }));

gulp.task("replace", function () {
  var manifest =
    gulp.src(scriptDir + "rev-*-manifest.json")
      .pipe(extend("manifest.json"))
      .pipe(gulp.dest(scriptDir));
  return gulp.src(outDir + "/**/*.html")
    .pipe(revReplace({ manifest: manifest }))
    .pipe(gulp.dest(outDir));
});

gulp.task("default", ["compile"]);
