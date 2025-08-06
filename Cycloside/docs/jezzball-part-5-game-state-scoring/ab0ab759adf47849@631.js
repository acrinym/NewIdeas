// https://observablehq.com/@sdl60660/observable-inputs@631
function _1(md){return(
md`# Observable Inputs

This is a copy of the [Observable Inputs](https://observablehq.com/@observablehq/inputs) notebook that excludes the file import for examples while retaining the functionality of the input functions
`
)}

function _usage(md){return(
md`---
## Usage

Declare your inputs with [viewof](/@observablehq/introduction-to-views), like so:`
)}

function _gain(Inputs){return(
Inputs.range([0, 11], {value: 5, step: 0.1, label: "Gain"})
)}

function _4(md){return(
md`Now you can reference the input’s value (here *gain*) in any cell, and the cell will run whenever the input changes. No event listeners required!`
)}

function _5(gain){return(
gain
)}

function _6(html,gain){return(
html`The current gain is ${gain.toFixed(1)}!`
)}

function _7(md){return(
md`Observable Inputs are released under the [ISC license](https://github.com/observablehq/inputs/blob/main/LICENSE) and depend only on [Hypertext Literal](/@observablehq/htl), our tagged template literal for safely generating dynamic HTML. If you are interested in contributing or wish to report an issue, please see [our repository](https://github.com/observablehq/inputs). For recent changes, please see [our release notes](https://github.com/observablehq/inputs/releases).`
)}

function _basics(md){return(
md`---
## Basics

These basic inputs will get you started.

* [Button](/@observablehq/input-button) - do something when a button is clicked
* [Toggle](/@observablehq/input-toggle) - toggle between two values (on or off)
* [Checkbox](/@observablehq/input-checkbox) - choose any from a set
* [Radio](/@observablehq/input-radio) - choose one from a set
* [Range](/@observablehq/input-range) - choose a number in a range (slider)
* [Select](/@observablehq/input-select) - choose one or any from a set (drop-down menu)
* [Text](/@observablehq/input-text) - enter freeform single-line text
* [Textarea](/@observablehq/input-textarea) - enter freeform multi-line text`
)}

function _9(md){return(
md`---
### [Button](/@observablehq/input-button) 

Do something when a button is clicked. [Examples ›](/@observablehq/input-button) [API Reference ›](https://github.com/observablehq/inputs/blob/main/README.md#button)`
)}

function _clicks(Inputs){return(
Inputs.button("Click me")
)}

function _11(clicks){return(
clicks
)}

function _12(md){return(
md`---
### [Toggle](/@observablehq/input-toggle) 

Toggle between two values (on or off). [Examples ›](/@observablehq/input-toggle) [API Reference ›](https://github.com/observablehq/inputs/blob/main/README.md#toggle)`
)}

function _mute(Inputs){return(
Inputs.toggle({label: "Mute"})
)}

function _14(mute){return(
mute
)}

function _15(md){return(
md`---
### [Checkbox](/@observablehq/input-checkbox) 

Choose any from a set. [Examples ›](/@observablehq/input-checkbox) [API Reference ›](https://github.com/observablehq/inputs/blob/main/README.md#checkbox)`
)}

function _colors(Inputs){return(
Inputs.checkbox(["red", "green", "blue"], {label: "Colors"})
)}

function _17(colors){return(
colors
)}

function _18(md){return(
md`---
### [Radio](/@observablehq/input-radio)

Choose one from a set. [Examples ›](/@observablehq/input-radio) [API Reference ›](https://github.com/observablehq/inputs/blob/main/README.md#radio)`
)}

function _color(Inputs){return(
Inputs.radio(["red", "green", "blue"], {label: "Color"})
)}

function _20(color){return(
color
)}

function _21(md){return(
md`---
### [Range](/@observablehq/input-range)

Pick a number. [Examples ›](/@observablehq/input-range) [API Reference ›](https://github.com/observablehq/inputs/blob/main/README.md#range)`
)}

function _n(Inputs){return(
Inputs.range([0, 255], {step: 1, label: "Favorite number"})
)}

function _23(n){return(
n
)}

function _24(md){return(
md`---
### [Select](/@observablehq/input-select)

Choose one, or any, from a menu. [Examples ›](/@observablehq/input-select) [API Reference ›](https://github.com/observablehq/inputs/blob/main/README.md#select)`
)}

function _25(md){return(
md`---
### [Text](/@observablehq/input-text)

Enter freeform single-line text. [Examples ›](/@observablehq/input-text) [API Reference ›](https://github.com/observablehq/inputs/blob/main/README.md#text)`
)}

function _name(Inputs){return(
Inputs.text({label: "Name", placeholder: "What’s your name?"})
)}

function _27(name){return(
name
)}

function _28(md){return(
md`---
### [Textarea](/@observablehq/input-textarea)

Enter freeform multi-line text. [Examples ›](/@observablehq/input-textarea) [API Reference ›](https://github.com/observablehq/inputs/blob/main/README.md#textarea)`
)}

function _bio(Inputs){return(
Inputs.textarea({label: "Biography", placeholder: "What’s your story?"})
)}

function _30(bio){return(
bio
)}

function _tables(md){return(
md`---
## Tabular data

These fancy inputs are designed to work with tabular data such as CSV or TSV [file attachments](/@observablehq/file-attachments) and [database clients](/@observablehq/databases).

* [Search](/@observablehq/input-search) - query a tabular dataset
* [Table](/@observablehq/input-table) - browse a tabular dataset`
)}

function _32(md){return(
md`---
### [Search](/@observablehq/input-search)

Query a tabular dataset. [Examples ›](/@observablehq/input-search) [API Reference ›](https://github.com/observablehq/inputs/blob/main/README.md#search)`
)}

function _33(md){return(
md`---
### [Table](/@observablehq/input-table)

Browse a tabular dataset. [Examples ›](/@observablehq/input-table) [API Reference ›](https://github.com/observablehq/inputs/blob/main/README.md#table)`
)}

function _techniques(md){return(
md`---
## And more!

Got the basics? Here are a few more advanced techniques:

* [Synchronized inputs](/@observablehq/synchronized-inputs) - bind two or more inputs
* [Introduction to Views](/@observablehq/introduction-to-views) - more on Observable’s viewof
* More guides coming soon!
`
)}

function _35(md){return(
md`We are grateful to Jeremy Ashkenas for blazing the trail with [“The Grand Native Inputs Bazaar”](/@jashkenas/inputs). To migrate from Jeremy’s inputs to our new official inputs, consider our [legacy inputs](/@observablehq/legacy-inputs).`
)}

function _36(md,Inputs,html){return(
md`For even more, consider these “friends & family” inputs and techniques shared by the Observable community:

${Inputs.table([
  [["2D Slider", "/d/98bbb19bf9e859ee"], "Fabian Iwand", "a two-dimensional range"],
  [["Binary Input", "/@rreusser/binary-input"], "Ricky Reusser", "bitwise IEEE floating point"],
  [["DIY inputs", "/@bartok32/diy-inputs"], "Bartosz Prusinowski", "inputs with fun, custom styles"],
  [["FineRange", "/@rreusser/fine-range"], "Ricky Reusser", "high-precision numeric control"],
  [["Form Input", "/@mbostock/form-input"], "Mike Bostock", "multiple inputs in single cell"],
  [["Inputs", "/@jashkenas/inputs"], "Jeremy Ashkenas", "the original"],
  [["Player", "/@oscar6echo/player"], "oscar6echo", "detailed timing control for animation"],
  [["Scrubber", "/@mbostock/scrubber"], "Mike Bostock", "play/pause/scrub control for animation"],
  [["Range Slider", "/@mootari/range-slider"], "Fabian Iwand", "a two-ended range"],
  [["Ternary Slider", "/@yurivish/ternary-slider"], "Yuri Vishnevsky", "a proportion of three values"],
  [["Data driven range sliders", "/@bumbeishvili/data-driven-range-sliders"], "David B.", "a range input with a histogram"],
  [["Snapping Histogram Slider", "/@trebor/snapping-histogram-slider"], "Robert Harris", "a range input with a histogram"],
  [["Inputs in grid", "/@bumbeishvili/input-groups"], "David B.", "combine multiple inputs into a compact grid"],
  [["List Input", "/@harrislapiroff/list-input"], "Harris L.", "enter more than one of something"],
  [["Copier", "/@mbostock/copier"], "Mike Bostock", "a button to copy to the clipboard"],
  [["Tangle", "/@mbostock/tangle"], "Mike Bostock", "Bret Victor-inspired inline scrubbable numbers"],
  [["Editor", "/@cmudig/editor"], "CMU Data Interaction Group", "code editor with syntax highlighting"],
  [["Color Scheme Picker", "/@zechasault/color-schemes-and-interpolators-picker"], "Zack Ciminera", "pick a D3 color scheme"],
  [["Easing Curve Editor", "/@nhogs/easing-graphs-editor"], "Nhogs", "create a custom easing curve"]
].map(([Name, Author, Description]) => ({Name, Author, Description})), {
  sort: "Name",
  rows: Infinity,
  layout: "auto",
  width: {
    "Description": "60%"
  },
  format: {
    Name: ([title, link]) => html`<a href=${link} target=_blank>${title}`
  }
})}

To share your reusable input or technique, please leave a comment.`
)}

function _37(md){return(
md`---

## Appendix`
)}

function _38(md){return(
md`The cells below provide deprecated aliases for older versions of Inputs. Please use Inputs.button *etc.* instead of importing from this notebook.`
)}

function _Button(Inputs){return(
Inputs.button
)}

function _Toggle(Inputs){return(
Inputs.toggle
)}

function _Radio(Inputs){return(
Inputs.radio
)}

function _Checkbox(Inputs){return(
Inputs.checkbox
)}

function _Range(Inputs){return(
Inputs.range
)}

function _Select(Inputs){return(
Inputs.select
)}

function _Text(Inputs){return(
Inputs.text
)}

function _Textarea(Inputs){return(
Inputs.textarea
)}

function _Search(Inputs){return(
Inputs.search
)}

function _Table(Inputs){return(
Inputs.table
)}

function _Input(Inputs){return(
Inputs.input
)}

function _bind(Inputs){return(
Inputs.bind
)}

function _disposal(Inputs){return(
Inputs.disposal
)}

function _svg(htl){return(
htl.svg
)}

function _html(htl){return(
htl.html
)}

export default function define(runtime, observer) {
  const main = runtime.module();
  main.variable(observer()).define(["md"], _1);
  main.variable(observer("usage")).define("usage", ["md"], _usage);
  main.variable(observer("viewof gain")).define("viewof gain", ["Inputs"], _gain);
  main.variable(observer("gain")).define("gain", ["Generators", "viewof gain"], (G, _) => G.input(_));
  main.variable(observer()).define(["md"], _4);
  main.variable(observer()).define(["gain"], _5);
  main.variable(observer()).define(["html","gain"], _6);
  main.variable(observer()).define(["md"], _7);
  main.variable(observer("basics")).define("basics", ["md"], _basics);
  main.variable(observer()).define(["md"], _9);
  main.variable(observer("viewof clicks")).define("viewof clicks", ["Inputs"], _clicks);
  main.variable(observer("clicks")).define("clicks", ["Generators", "viewof clicks"], (G, _) => G.input(_));
  main.variable(observer()).define(["clicks"], _11);
  main.variable(observer()).define(["md"], _12);
  main.variable(observer("viewof mute")).define("viewof mute", ["Inputs"], _mute);
  main.variable(observer("mute")).define("mute", ["Generators", "viewof mute"], (G, _) => G.input(_));
  main.variable(observer()).define(["mute"], _14);
  main.variable(observer()).define(["md"], _15);
  main.variable(observer("viewof colors")).define("viewof colors", ["Inputs"], _colors);
  main.variable(observer("colors")).define("colors", ["Generators", "viewof colors"], (G, _) => G.input(_));
  main.variable(observer()).define(["colors"], _17);
  main.variable(observer()).define(["md"], _18);
  main.variable(observer("viewof color")).define("viewof color", ["Inputs"], _color);
  main.variable(observer("color")).define("color", ["Generators", "viewof color"], (G, _) => G.input(_));
  main.variable(observer()).define(["color"], _20);
  main.variable(observer()).define(["md"], _21);
  main.variable(observer("viewof n")).define("viewof n", ["Inputs"], _n);
  main.variable(observer("n")).define("n", ["Generators", "viewof n"], (G, _) => G.input(_));
  main.variable(observer()).define(["n"], _23);
  main.variable(observer()).define(["md"], _24);
  main.variable(observer()).define(["md"], _25);
  main.variable(observer("viewof name")).define("viewof name", ["Inputs"], _name);
  main.variable(observer("name")).define("name", ["Generators", "viewof name"], (G, _) => G.input(_));
  main.variable(observer()).define(["name"], _27);
  main.variable(observer()).define(["md"], _28);
  main.variable(observer("viewof bio")).define("viewof bio", ["Inputs"], _bio);
  main.variable(observer("bio")).define("bio", ["Generators", "viewof bio"], (G, _) => G.input(_));
  main.variable(observer()).define(["bio"], _30);
  main.variable(observer("tables")).define("tables", ["md"], _tables);
  main.variable(observer()).define(["md"], _32);
  main.variable(observer()).define(["md"], _33);
  main.variable(observer("techniques")).define("techniques", ["md"], _techniques);
  main.variable(observer()).define(["md"], _35);
  main.variable(observer()).define(["md","Inputs","html"], _36);
  main.variable(observer()).define(["md"], _37);
  main.variable(observer()).define(["md"], _38);
  main.variable(observer("Button")).define("Button", ["Inputs"], _Button);
  main.variable(observer("Toggle")).define("Toggle", ["Inputs"], _Toggle);
  main.variable(observer("Radio")).define("Radio", ["Inputs"], _Radio);
  main.variable(observer("Checkbox")).define("Checkbox", ["Inputs"], _Checkbox);
  main.variable(observer("Range")).define("Range", ["Inputs"], _Range);
  main.variable(observer("Select")).define("Select", ["Inputs"], _Select);
  main.variable(observer("Text")).define("Text", ["Inputs"], _Text);
  main.variable(observer("Textarea")).define("Textarea", ["Inputs"], _Textarea);
  main.variable(observer("Search")).define("Search", ["Inputs"], _Search);
  main.variable(observer("Table")).define("Table", ["Inputs"], _Table);
  main.variable(observer("Input")).define("Input", ["Inputs"], _Input);
  main.variable(observer("bind")).define("bind", ["Inputs"], _bind);
  main.variable(observer("disposal")).define("disposal", ["Inputs"], _disposal);
  main.variable(observer("svg")).define("svg", ["htl"], _svg);
  main.variable(observer("html")).define("html", ["htl"], _html);
  return main;
}
