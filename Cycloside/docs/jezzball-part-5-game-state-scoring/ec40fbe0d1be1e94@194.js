// https://observablehq.com/@sdl60660/elastic-collisions@194
import define1 from "./3d9d1394d858ca97@553.js";

function _1(md){return(
md`# Elastic Collisions

This is a set of classes/functions for handling [elastic collisions in a 2D space](https://en.wikipedia.org/wiki/Elastic_collision#Two-dimensional_collision_with_two_moving_objects). It's largely derived from [this code](https://gist.github.com/winduptoy/a1aa09c3499e09edbd33) and [this document](http://cobweb.cs.uga.edu/~maria/classes/4070-Spring-2017/Adam%20Brookes%20Elastic%20collision%20Code.pdf) by Adam Brookes.
`
)}

function _2(md){return(
md`
The 2D collision behavior that the elastic collision function will allow you to mimic is modeled below ([source](https://en.wikipedia.org/wiki/Elastic_collision#/media/File:Elastischer_sto%C3%9F_2D.gif)):
`
)}

function _3(html){return(
html`<img src="https://upload.wikimedia.org/wikipedia/commons/2/2c/Elastischer_sto%C3%9F_2D.gif" alt="animation demonstrati 2D elastic collision behavior">`
)}

function _4(md){return(
md`
~~~js
import {elasticCollision, Ball} from "@sdl60660/elastic-collisions"
~~~
`
)}

function _5(signature,elasticCollision,md){return(
signature(elasticCollision, {
  description: md`
This function will calculate and assign new angles/speeds to a pair of colliding <code>Ball</code> objects. It's not a requirement that the Ball objects use the <code>Ball</code> class included in this notebook, but it is a requirement that whatever objects are used as inputs have the following attributes:
* **Ball.x**: designating the ball's x coordinate position
* **Ball.y**: designating the ball's y coordinate position
* **Ball.angle**: designating the ball's current direction of travel in degrees
* **Ball.speed**: designating the ball's current speed of travel
* **Ball.mass**: designating the ball's mass (though this will not matter if the <code>considerMass</code> flag is set to false)

The function takes the following parameters:

${Object.entries({
      ball1: 'The first of the two colliding ball objects. Does not need to be an object of the <code>Ball</code> class in this notebook, but must have the attributes listed above.',
      ball2: 'The second of the two colliding ball objects. Does not need to be an object of the <code>Ball</code> class in this notebook, but must have the attributes listed above.',
      angleOnly: 'If set to true, will only recalculate angles of objects after the collision, will not adjust speeds. Useful in cases such as [this game](https://observablehq.com/@sdl60660/jezzball-part-5-game-state-scoring), where collisions should be realistic, but speeds should be constant. Optional, defaults to <code>false</code>.',
      considerMass: 'If set to true, the masses of the colliding objects will be considered in calculating their new movement vectors. If set to false, it will treat the objects as equal mass. Optional, defaults to <code>true</code>',
    }).map(([k,v]) => `- \`${k}:\` ${v}\n`)}`

})
)}

function _elasticCollision(Vector,geometric){return(
(ball1, ball2, angleOnly = false, considerMass = true) => {
  ball1.vector = new Vector({
    pair: geometric.pointTranslate([0, 0], ball1.angle, ball1.speed)
  });
  ball2.vector = new Vector({
    pair: geometric.pointTranslate([0, 0], ball2.angle, ball2.speed)
  });

  const x = ball2.x - ball1.x;
  const y = ball2.y - ball1.y;

  const normalVector = new Vector({ x, y }).normalize();
  const tangentVector = new Vector({
    x: normalVector.y * -1,
    y: normalVector.x
  });

  const ball1scalarNormal = Vector.dot(normalVector, ball1.vector);
  const ball2scalarNormal = Vector.dot(normalVector, ball2.vector);

  const ball1scalarTangential = Vector.dot(tangentVector, ball1.vector);
  const ball2scalarTangential = Vector.dot(tangentVector, ball2.vector);

  const ball1Mass = considerMass ? ball1.mass : 1;
  const ball2Mass = considerMass ? ball2.mass : 1;
  
  const ball1ScalarNormalAfter =
    (ball1scalarNormal * (ball1Mass - ball2Mass) +
      2 * ball2Mass* ball2scalarNormal) /
    (ball1Mass + ball2Mass);

  const ball2ScalarNormalAfter =
    (ball2scalarNormal * (ball2Mass - ball1Mass) +
      2 * ball1Mass * ball1scalarNormal) /
    (ball2Mass + ball1Mass);

  const ball1scalarNormalAfter_vector = Vector.multiply(
    normalVector,
    ball1ScalarNormalAfter
  );
  const ball2scalarNormalAfter_vector = Vector.multiply(
    normalVector,
    ball2ScalarNormalAfter
  );

  const ball1ScalarNormalVector = Vector.multiply(
    tangentVector,
    ball1scalarTangential
  );
  const ball2ScalarNormalVector = Vector.multiply(
    tangentVector,
    ball2scalarTangential
  );

  const ball1final = Vector.add(
    ball1ScalarNormalVector,
    ball1scalarNormalAfter_vector
  );
  const ball2final = Vector.add(
    ball2ScalarNormalVector,
    ball2scalarNormalAfter_vector
  );

  ball1.angle = geometric.lineAngle([
    [0, 0],
    [ball1final.x, ball1final.y]
  ]);
  ball2.angle = geometric.lineAngle([
    [0, 0],
    [ball2final.x, ball2final.y]
  ]);

  if (angleOnly === false) {
    ball1.speed = geometric.lineLength([
      [0, 0],
      [ball1final.x, ball1final.y]
    ]);
    ball2.speed = geometric.lineLength([
      [0, 0],
      [ball2final.x, ball2final.y]
    ]);
  }
}
)}

function _Vector(){return(
class Vector {
  constructor({ x, y, pair }) {
    if (pair) {
      this.x = pair[0];
      this.y = pair[1]
    }
    else {
      this.x = x || 0;
    	this.y = y || 0;
    }
  }

  negative() {
		this.x = -this.x;
		this.y = -this.y;
		return this;
	}

  add(v) {
		if (v instanceof Vector) {
			this.x += v.x;
			this.y += v.y;
		} else {
			this.x += v;
			this.y += v;
		}
		return this;
	}
  
	multiply(v) {
		if (v instanceof Vector) {
			this.x *= v.x;
			this.y *= v.y;
		} else {
			this.x *= v;
			this.y *= v;
		}
		return this;
	}
  
	static add(a, b) {
		if (b instanceof Vector) {
      return new Vector({ x: a.x + b.x, y: a.y + b.y });
    }
	  else {
      return new Vector({ x: a.x + b, y: a.y + b });
    }
	}
  
	subtract(v) {
		if (v instanceof Vector) {
			this.x -= v.x;
			this.y -= v.y;
		} else {
			this.x -= v;
			this.y -= v;
		}
		return this;
	}
  
	static multiply(a, b) {
		if (b instanceof Vector) {
      return new Vector({ x: a.x * b.x, y: a.y * b.y });
    }
	  else {
      return new Vector({ x: a.x * b, y: a.y * b });
    }
	}
  
	divide(v) {
		if (v instanceof Vector) {
			if(v.x != 0) this.x /= v.x;
			if(v.y != 0) this.y /= v.y;
		} else {
			if(v != 0) {
				this.x /= v;
				this.y /= v;
			}
		}
		return this;
	}
  
	equals(v) {
		return this.x == v.x && this.y == v.y;
	}
  
	static dot(a, b) {
		return a.x * b.x + a.y * b.y;
	}
  
	cross(v) {
		return this.x * v.y - this.y * v.x
	}

  dot(v) {
		return this.x * v.x + this.y * v.y;
	}
  
	length() {
		return Math.sqrt(this.dot(this));
	}
  
	normalize() {
		return this.divide(this.length());
	}
  
	min() {
		return Math.min(this.x, this.y);
	}
  
	max() {
		return Math.max(this.x, this.y);
	}
  
	toAngles() {
		return -Math.atan2(-this.y, this.x);
	}
  
	angleTo(a) {
		return Math.acos(this.dot(a) / (this.length() * a.length()));
	}
  
	toArray(n) {
		return [this.x, this.y].slice(0, n || 2);
	}
  
	clone() {
		return new Vector(this.x, this.y);
	}
  
	set(x, y) {
		this.x = x; this.y = y;
		return this;
	} 
}
)}

function _Ball(geometric){return(
class Ball {
  constructor({ id, x, y, radius, angle, speed, color, ctx }) {
    this.id = id;
    this.x = x || 20;
    this.y = y || 20;
    this.pos = [x, y];
    this.radius = radius || 5;
    this.mass = radius * radius;
    this.angle = angle || 45;
    this.speed = speed || 5;
    this.color = color || "red";
    this.ctx = ctx || null;
  }

  drawCircle() {
    this.ctx.beginPath();
    this.ctx.arc(this.x, this.y, this.radius, 0, 2 * Math.PI);
    this.ctx.fillStyle = this.color;
    this.ctx.stroke();
    this.ctx.fill();
  }

  tick() {
    [this.x, this.y] = this.pos = geometric.pointTranslate(
      this.pos,
      this.angle,
      this.speed
    );
  }
}
)}

function _geometric(require){return(
require("geometric")
)}

export default function define(runtime, observer) {
  const main = runtime.module();
  main.variable(observer()).define(["md"], _1);
  main.variable(observer()).define(["md"], _2);
  main.variable(observer()).define(["html"], _3);
  main.variable(observer()).define(["md"], _4);
  main.variable(observer()).define(["signature","elasticCollision","md"], _5);
  main.variable(observer("elasticCollision")).define("elasticCollision", ["Vector","geometric"], _elasticCollision);
  main.variable(observer("Vector")).define("Vector", _Vector);
  main.variable(observer("Ball")).define("Ball", ["geometric"], _Ball);
  main.variable(observer("geometric")).define("geometric", ["require"], _geometric);
  const child1 = runtime.module(define1);
  main.import("signature", child1);
  return main;
}
