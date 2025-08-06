// https://observablehq.com/@observablehq/synchronized-inputs@211
function _1(md){return(
md`<div style="color: grey; font: 13px/25.5px var(--sans-serif); text-transform: uppercase;"><a href="/@observablehq/inputs">Observable Inputs</a> › BIND · <a href="https://github.com/observablehq/inputs/blob/main/README.md#inputsbindtarget-source-invalidation">API</a></div>

# Synchronized Inputs

[Observable Inputs](/@observablehq/inputs) behave similarly to native HTML inputs: they expose a value and emit an *input* event when the user changes the value. This makes them compatible with [Observable’s viewof operator](/@observablehq/introduction-to-views). It also means that you can programmatically control the value of an input.`
)}

function _2(md){return(
md`Setting the value of an input with viewof requires two steps:

1. Assign the input’s value (<code>viewof x.value</code>)
2. Dispatch an *input* event (<code>viewof x.dispatchEvent</code>)

As a function:`
)}

function _set(Event){return(
function set(input, value) {
  input.value = value;
  input.dispatchEvent(new Event("input", {bubbles: true}));
}
)}

function _4(md){return(
md`Now let’s try it!`
)}

function _x(Inputs){return(
Inputs.range([0, 100], {step: 1})
)}

function _6(Inputs,set,$0){return(
Inputs.button([
  ["Set to 0", () => set($0, 0)],
  ["Set to 100", () => set($0, 100)]
])
)}

function _7(x){return(
x
)}

function _8(md){return(
md`Here \`viewof x\` refers to the view (the HTMLFormElement returned by [Inputs.range](/@observablehq/input-range)), whereas \`x\` refers to the view’s live value. A cell referencing \`x\` will run whenever the value of \`x\` changes, but a cell that only references \`viewof x\` only runs if the view itself is redefined (which typically only occurs on load and when the code is edited).`
)}

function _9($0){return(
$0
)}

function _10(md){return(
md`Programmatic control allows multiple inputs value to control a single value — to be synchronized. For example, in a tall notebook, you might offer redundant inputs under each chart so that the reader doesn’t have to scroll to change an input.`
)}

function _11(md){return(
md`Inputs.**bind** allows you to bind two inputs together so that interacting with one input updates the second and vice versa. It takes two inputs as arguments and returns the first input. This makes it convenient to bind a secondary input to an existing input while displaying it in a cell or elsewhere (even inline!).`
)}

function _12(Inputs,$0){return(
Inputs.bind(Inputs.range([0, 100]), $0)
)}

function _13(Inputs,htl,$0,md){return(
md`This is a range input ${Inputs.bind(htl.html`<input type=range style="width: 80px;">`, $0)}.

This is a number input ${Inputs.bind(htl.html`<input type=number style="width: 80px;">`, $0)}.`
)}

function _14(md){return(
md`Sometimes there isn’t an obvious “primary” input to bind to. In this case, you can use Inputs.**input** to create a lightweight mutable value store. The Input function takes an optional initial value and can be passed to bind as the second argument. Try dragging either slider below!`
)}

function _i(Inputs){return(
Inputs.input(42)
)}

function _16(Inputs,$0){return(
Inputs.bind(Inputs.range([0, 100]), $0)
)}

function _17(Inputs,$0){return(
Inputs.bind(Inputs.range([0, 100]), $0)
)}

function _18(i){return(
i
)}

export default function define(runtime, observer) {
  const main = runtime.module();
  main.variable(observer()).define(["md"], _1);
  main.variable(observer()).define(["md"], _2);
  main.variable(observer("set")).define("set", ["Event"], _set);
  main.variable(observer()).define(["md"], _4);
  main.variable(observer("viewof x")).define("viewof x", ["Inputs"], _x);
  main.variable(observer("x")).define("x", ["Generators", "viewof x"], (G, _) => G.input(_));
  main.variable(observer()).define(["Inputs","set","viewof x"], _6);
  main.variable(observer()).define(["x"], _7);
  main.variable(observer()).define(["md"], _8);
  main.variable(observer()).define(["viewof x"], _9);
  main.variable(observer()).define(["md"], _10);
  main.variable(observer()).define(["md"], _11);
  main.variable(observer()).define(["Inputs","viewof x"], _12);
  main.variable(observer()).define(["Inputs","htl","viewof x","md"], _13);
  main.variable(observer()).define(["md"], _14);
  main.variable(observer("viewof i")).define("viewof i", ["Inputs"], _i);
  main.variable(observer("i")).define("i", ["Generators", "viewof i"], (G, _) => G.input(_));
  main.variable(observer()).define(["Inputs","viewof i"], _16);
  main.variable(observer()).define(["Inputs","viewof i"], _17);
  main.variable(observer()).define(["i"], _18);
  return main;
}
