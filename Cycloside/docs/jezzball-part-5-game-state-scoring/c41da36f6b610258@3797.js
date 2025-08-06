import define1 from "./ab0ab759adf47849@631.js";
import define2 from "./3c37c9bffc213235@211.js";
import define3 from "./ec40fbe0d1be1e94@194.js";

function _1(md){return(
md`# Jezzball Part 5/Final (Game State and Scoring)

This is the fifth and final step in recreating a classic Windows game, [Jezzball](https://en.wikipedia.org/wiki/JezzBall), on canvas.

Here, I've added game state, where the simulation has an understanding of score/lives/area cleared/rounds and is capable of resetting the game on a player loss or advancing to the next round on a round victory. I've also added a few game features, such as limiting the players' ability to spawn new bars, while another is still active.

Scoring rules were based on [this FAQ](https://www.ign.com/faqs/2001/jezzball-walkthroughfaq-223013).

See this full collection [here](https://observablehq.com/collection/@sdl60660/jezzball).

**Credits**: The original ball collision code here is taken and adapted from [Harry Stevens](/@harrystevens)' ["Bouncing Balls" block](https://bl.ocks.org/HarryStevens/dbceb1a3590a6435c131dab724c0ead1), with a little refactoring and some interactivity added. The elastic collision logic is derived from [this document](http://cobweb.cs.uga.edu/~maria/classes/4070-Spring-2017/Adam%20Brookes%20Elastic%20collision%20Code.pdf) and [this code](https://gist.github.com/winduptoy/a1aa09c3499e09edbd33) and now lives in its own notebook [here](https://observablehq.com/@sdl60660/elastic-collisions)
`
)}

function _2(md){return(
md`
## Directions

* The goal of each round is to build walls that close off at least 75% of the space from the bouncing balls, using wall dividers. 
* Click anywhere on the canvas to create a wall divider.
* Use the "Bar Direction" radio buttons, or right-click, to toggle between vertical/horizontal walls.
* If a ball hits a wall before it finishes growing, you'll lose a life.
* Any enclosed space without balls will count towards "area cleared". When you've cleared at least 75% of the area in a round, you can advance to the next round.
* Each subsequent round will get harder by adding an additional ball.
* If you run out of lives on any round, the game is over.
`
)}

function _3(html,areaCleared,d3,simulation,currentLives){return(
html`
<div id="scoreboard">
  <div class="title"><h1>JezzBall</h1></div>

  <div>Area Cleared: ${areaCleared}%</div>
  <div>Score: ${d3.format(",")(simulation.totalScore + simulation.roundScore)}</div>

  <div>Round: ${simulation.level}</div>
  <div>Lives: ${currentLives}</div>
  <!-- <div>Time Left: </div> -->
</div>
`
)}

function _barDirection(Radio){return(
Radio(["Horizontal", "Vertical"], {label: "Bar Direction", value: "Horizontal"})
)}

function _canvas(DOM,width,height)
{
  const context = DOM.context2d(width, height);
  context.fillStyle = "#ffffff";
  context.clearRect(0, 0, width, height);

  context.canvas.id = "main-canvas";

  return context.canvas;
}


function _ctx(canvas){return(
canvas.getContext("2d")
)}

function _7(md){return(
md`
These mutables aren't used directly by the simulation object to track game state, but are used to trigger scoreboard updates, which the simulation properties won't.
`
)}

function _areaCleared(){return(
0
)}

function _currentLives(){return(
2
)}

function _numStartingBalls(){return(
2
)}

function _startSimulation(init)
{
//   ballSpeed;
//   barGrowthSpeed;
  
//   resetGame();
  
  init()
}


function _init(simulation){return(
() => {
  const tick = () => {
    let endState = false;
    if (
      simulation.lives > 0 &&
      (!simulation.areaCleared || simulation.areaCleared < 75)
    ) {
      requestAnimationFrame(tick);
    } else {
      simulation.tick();
      endState = true;
    }
    simulation.tick(endState);
  };

  tick();
}
)}

function _scoredCells(simulation){return(
simulation.grid.filter((a) => a.scored)
)}

function _clickPosition(){return(
null
)}

function _clickHandler(d3,$0,simulation,resetGame,nextRound,roundToGrid,gridSize,gridHeight,barDirection,addBar,set,$1)
{
  // Click handler
  d3.select("#main-canvas").on("click", (e) => {
    $0.value = d3.pointer(e);
    const [x, y] = d3.pointer(e);
    
    // If we're in a game over state, or between rounds, we're going to handle clicks differently
    if (simulation.roundState !== "running") {
      // Check if click is on button
      if (x >= simulation.button.left && x <= simulation.button.right &&
          y >= simulation.button.top && y <= simulation.button.bottom) {
        
        if (simulation.button.type === "game") {
          resetGame();
        }
        else {
          nextRound();
        }    
      }
      
      return;
    }
    
    // Check if this grid position is already scored (occupied by bar or scored space)
    let gridPositionFilled = false;
    const gridX = roundToGrid(x) / gridSize;
    const gridY = roundToGrid(y) / gridSize;
    
    const cellId = gridX*gridHeight + gridY;
    
    // If so, return without spawning new bars
    if (simulation.grid[cellId].scored) {
      return;
    }
    
    // No longer allowing new bars to spawn while there are still active bars (true to the original game)
    if (simulation.bars.find(bar => bar.active === true)) {
      return;
    };
    
    // Otherwise, spawn two new bar segments to the left/right or up/down, depending on selected orientation
    if (barDirection === "Horizontal") {
      addBar({ x, y, direction: "left" });
      addBar({ x, y, direction: "right" });
    }
    else {
      addBar({ x, y, direction: "up" });
      addBar({ x, y, direction: "down" });
    }
  })

  // Right-click handler
  d3.select("#main-canvas").on("contextmenu", (e) => {
    e.preventDefault();
    const direction = barDirection === "Horizontal" ? "Vertical" : "Horizontal";
    set($1, direction);
  });
}


function _addBall(Ball,simulation,ctx){return(
({ x, y }) => {
  const ball = new Ball({ id: simulation.balls.length, x, y, ctx });
  simulation.balls = [...simulation.balls, ball];
}
)}

function _addBar(Bar,ctx,simulation){return(
({ x, y, direction }) => {
  const id = `${x}-${y}-${direction}`;
  const bar = new Bar({ id, originX: x, originY: y, direction, ctx })
  simulation.bars = [...simulation.bars, bar];
}
)}

function _simulation(Simulation,width,height,ctx,ballSet,numStartingBalls,gridSet){return(
new Simulation({ width, height, ctx, balls: ballSet(numStartingBalls), grid: gridSet })
)}

function _gridSet(width,gridSize,height,gridHeight,gridWidth,GridCell,ctx)
{
  
  const gridCells = [];
  let id = 0;

  for (let x = 0; x < width; x += gridSize) {
    for (let y = 0; y < height; y += gridSize) {
      
      const neighbors = [];
      
      // up (if not at top)
      if (id % gridHeight !== 0) {
        neighbors.push(id-1);
      }
      // down (if not at bottom)
      if ((id + 1) % gridHeight !== 0) {
        neighbors.push(id+1);
      }
      // left (if not at left edge)
      if (id - gridHeight >= 0) {
        neighbors.push(id - gridHeight);
      }
      // right (if not at right edge)
      if (id + gridHeight < gridHeight*gridWidth) {
        neighbors.push(id + gridHeight);
      } 
      
      gridCells.push(
        new GridCell({
          id,
          x,
          y,
          width: (gridSize - 1),
          height: (gridSize - 1), ctx,
          neighbors 
        })
      );
      id += 1;
    }
  }

  return gridCells;
}


function _ballSet(Ball,d3,width,height,gridSize,ballSpeed,sizeModifier,ctx){return(
(numberBalls) => {
  const balls = [];
  for (let index = 0; index < numberBalls; ++index) {
    const ball = new Ball({
      id: index,
      x: d3.randomUniform(0, width)(),
      y: d3.randomUniform(0, height)(),
      radius: Math.round(0.8*gridSize / 2),
      color: d3.schemeCategory10[Math.round(d3.randomUniform(0, 9)())],
      speed: ballSpeed*sizeModifier,
      ctx
    });
    balls.push(ball);
  }

  return balls;
}
)}

function _Simulation(d3,fastDetectBallCollision,detectBallWallCollision,detectBarWallCollision,detectBallBarCollision,detectBarToBarCollision,evaluateGrid,endGame,endRound,width,height,$0){return(
class Simulation {
  constructor({ width, height, ctx, balls, bars, grid }) {
    this.width = width || 800;
    this.height = height || 800;
    this.center = [this.width / 2, this.height / 2];

    this.ctx = ctx || null;

    this.maxRadius = d3.max(balls, (a) => a.radius);
    this.quadtree = null;

    this.balls = balls || [];
    this.bars = bars || [];
    this.grid = grid;
    this.grid.forEach((cell) => {
      cell.scored = false;
    });

    this.fillQueue = [];
    this.button = null;

    // Game state
    this.roundState = "running";
    this.lives = 2;
    this.totalScore = 0;
    this.roundScore = 0;
    this.level = 1;
    this.areaCleared = 0;

    // Collision methods
    this.fastDetectBallCollision = fastDetectBallCollision;
    this.detectBallWallCollision = detectBallWallCollision;
    this.detectBarWallCollision = detectBarWallCollision;
    this.detectBallBarCollision = detectBallBarCollision;
    this.detectBarToBarCollision = detectBarToBarCollision;
    this.evaluateGrid = evaluateGrid;

    // End state methods
    this.endGame = endGame;
    this.endRound = endRound;

    // this.resetGame = resetGame;
    // this.nextRound = nextRound;
  }

  clearCanvas() {
    this.ctx.fillStyle = "#ffffff";
    this.ctx.clearRect(0, 0, width, height);
  }

  tick(endState = false) {
    if (endState) {
      $0.value = this.areaCleared;

      if (this.lives <= 0) {
        this.endGame();
      } else {
        this.endRound();
      }

      return;
    }

    this.clearCanvas();

    this.grid.forEach((cell) => {
      cell.drawCell();
    });

    this.quadtree = d3
      .quadtree()
      .x((d) => d.x)
      .y((d) => d.y)
      .extent([-1, -1], [this.width + 1, this.height + 1])
      .addAll(this.balls);

    this.balls.forEach((ball) => {
      this.detectBallBarCollision(ball);
      this.detectBallWallCollision(ball);
      this.fastDetectBallCollision(ball);

      ball.tick();
      ball.drawCircle();
    });

    this.bars = this.bars.filter((bar) => bar.remove === false);

    this.bars.forEach((bar) => {
      if (bar.active) {
        this.detectBarWallCollision(bar);
        this.detectBarToBarCollision(bar);
        bar.tick();
      }

      bar.drawRect();
    });

    if (this.bars.filter((bar) => bar.active === true).length === 0) {
      this.fillQueue.forEach((bar) => {
        this.evaluateGrid(bar);
      });

      this.fillQueue = [];
    }
  }
}
)}

function _nextRound(simulation,$0,$1,ballSet,init){return(
function() {
    simulation.totalScore += simulation.roundScore;
    simulation.roundScore = 0;
    simulation.level += 1;
    simulation.lives = simulation.level + 1;
    simulation.areaCleared = 0;
  
    $0.value = 0;
    $1.value = simulation.level + 1;
  
    simulation.balls = ballSet((simulation.level + 1));
    simulation.bars = [];
    simulation.grid.forEach(cell => { cell.scored = false });
  
    simulation.fillQueue = [];
    simulation.button = null;
    simulation.roundState = "running";
  
    init();
}
)}

function _resetGame(simulation,$0,$1,ballSet,init){return(
function() {
  simulation.roundScore = 0;
  simulation.totalScore = 0;
  simulation.lives = 2;
  simulation.level = 1;
  simulation.areaCleared = 0;

  $0.value = 0;
  $1.value = 2;

  simulation.balls = ballSet(2);
  simulation.bars = [];
  simulation.grid.forEach(cell => { cell.scored = false });

  simulation.fillQueue = [];
  simulation.button = null;
  simulation.roundState = "running";

  init();
}
)}

function _endGame(sizeModifier){return(
function() {
  this.roundState = "game over";
  
  this.ctx.font = `bold ${sizeModifier < 0.8 ? 35 : 50}px Courier`;
  this.ctx.strokeStyle = "white";
  this.ctx.fillStyle = "#787878";
  this.ctx.textAlign = "center";
  
  this.ctx.fillText("Game Over", this.center[0], this.center[1] - 60);
  this.ctx.strokeText("Game Over", this.center[0], this.center[1] - 60);

  this.ctx.fillStyle = "#787878";
  this.ctx.strokeStyle = "white";

  this.ctx.fillRect(this.center[0] - 70, this.center[1] - 40, 140, 35);
  this.ctx.strokeRect(this.center[0] - 70, this.center[1] - 40, 140, 35);

  this.ctx.font = "20px Courier";
  this.ctx.fillStyle = "white";
  this.ctx.fillText("New Game", this.center[0], this.center[1] - 17);

  this.button = ({ type: "game", left: this.center[0] - 70, right: this.center[0] + 70, top: this.center[1] - 40, bottom: this.center[1] - 5});
  
  return;
}
)}

function _endRound(sizeModifier){return(
function() {
  this.roundState = "between rounds";
  
  this.ctx.font = `bold ${sizeModifier < 0.8 ? 32 : 50}px Courier`;
  this.ctx.strokeStyle = "white";
  this.ctx.fillStyle = "#787878";
  this.ctx.textAlign = "center";
  
  this.ctx.fillText("Round Complete!", this.center[0], this.center[1] - 60);
  this.ctx.strokeText("Round Complete!", this.center[0], this.center[1] - 60);

  this.ctx.fillStyle = "#787878";
  this.ctx.strokeStyle = "white";

  this.ctx.fillRect(this.center[0] - 70, this.center[1] - 40, 140, 35);
  this.ctx.strokeRect(this.center[0] - 70, this.center[1] - 40, 140, 35);

  this.ctx.font = "20px Courier";
  this.ctx.fillStyle = "white";
  this.ctx.fillText("Next Round", this.center[0], this.center[1] - 17);

  this.button = ({ type: "round", left: this.center[0] - 70, right: this.center[0] + 70, top: this.center[1] - 40, bottom: this.center[1] - 5});
  
  return;
}
)}

function _fastDetectBallCollision(geometric,elasticCollision){return(
function (ball) {
  const r = ball.radius + this.maxRadius;
  const nx1 = ball.x - r;
  const nx2 = ball.x + r;
  const ny1 = ball.y - r;
  const ny2 = ball.y + r;

  this.quadtree.visit((visited, x1, y1, x2, y2) => {
    if (visited.data && visited.data.id !== ball.id) {
      // Collision
      if (
        geometric.lineLength([ball.pos, visited.data.pos]) <
        ball.radius + visited.data.radius
      ) {
        const keep = geometric.lineLength([
          geometric.pointTranslate(ball.pos, ball.angle, ball.speed),
          geometric.pointTranslate(
            visited.data.pos,
            visited.data.angle,
            visited.data.speed
          )
        ]);

        const swap = geometric.lineLength([
          geometric.pointTranslate(
            ball.pos,
            visited.data.angle,
            visited.data.speed
          ),
          geometric.pointTranslate(visited.data.pos, ball.angle, ball.speed)
        ]);

        if (swap > keep) {
          elasticCollision(ball, visited.data, true);
        }
      }
    }

    return x1 > nx2 || x2 < nx1 || y1 > ny2 || y2 < ny1;
  });
}
)}

function _detectBallWallCollision(geometric){return(
function(ball) {
  // Detect sides
  const wallVertical = ball.x <= ball.radius || ball.x >= this.width - ball.radius;
  const wallHorizontal = ball.y <= ball.radius || ball.y >= this.height - ball.radius;
  
  if (wallVertical || wallHorizontal) {
    
    const t0 = geometric.pointTranslate(ball.pos, ball.angle, ball.speed);
    const l0 = geometric.lineLength([this.center, t0]);
    
    const reflected = geometric.angleReflect(ball.angle, wallVertical ? 90 : 0);
    const t1 = geometric.pointTranslate(ball.pos, reflected, ball.speed);
    const l1 = geometric.lineLength([this.center, t1]);
    
    if (l1 < l0) {
      ball.angle = reflected;
    }
  }
}
)}

function _detectBarWallCollision(height,lightGray){return(
function (bar) {
  // Determine wall collision
  const wallDistance =
    bar.direction === "up"
      ? bar.originY + bar.height
      : bar.direction === "down"
      ? height - (bar.originY + bar.height)
      : bar.direction === "left"
      ? bar.originX + bar.width
      : this.width - (bar.originX + bar.width);

  if (wallDistance <= 0) {
    bar.active = false;
    bar.color = lightGray;
    bar.drawRect();
    bar.multiplier = 0;

    this.grid
      .filter(
        (cell) =>
          cell.y < bar.bottom &&
          cell.y >= bar.top &&
          cell.x >= bar.left &&
          cell.x < bar.right
      )
      .forEach((cell) => {
        cell.scored = true;
      });

    this.fillQueue.push(bar);
    // this.evaluateGrid(bar);
  }
}
)}

function _detectBallBarCollision($0,geometric){return(
function (ball) {
  const ballLeft = ball.x - ball.radius;
  const ballRight = ball.x + ball.radius;
  const ballTop = ball.y + ball.radius;
  const ballBottom = ball.y - ball.radius;
  
  this.bars.forEach(bar => {
    
    const verticalCollision = 
      ((ballRight >= bar.right && ballLeft <= bar.right) ||
       (ballLeft <= bar.left && ballRight >= bar.left)) &&
       (Math.round(ball.y) <= bar.bottom && Math.round(ball.y) >= bar.top);
    
    const horizontalCollision = 
      ((ballTop >= bar.top && ballBottom <= bar.top) ||
       (ballBottom <= bar.bottom && ballTop >= bar.bottom)) &&
       (Math.round(ball.x) <= bar.right && Math.round(ball.x) >= bar.left);
    
    if (verticalCollision || horizontalCollision) {
      
      if (bar.active) {
        bar.remove = true;
        this.lives -= 1;
        $0.value -= 1;
      }
      else {
        const reflected = geometric.angleReflect(ball.angle, verticalCollision ? 90 : 0);
        
        const t0 = geometric.pointTranslate(ball.pos, ball.angle, ball.speed);
        const t1 = geometric.pointTranslate(ball.pos, reflected, ball.speed);
        
        const barTargetCoordinates = verticalCollision ?
          [((bar.left + bar.right) / 2), ball.y] :
          [ball.x, ((bar.top + bar.bottom) / 2)];
        
        const l0 = geometric.lineLength([barTargetCoordinates, t0]);
        const l1 = geometric.lineLength([barTargetCoordinates, t1]);
        
        if (l1 > l0) {
          ball.angle = reflected;
        }
      }
    }
  })
}
)}

function _detectBarToBarCollision(lightGray){return(
function (bar) {
  const possibleBars = this.bars.filter((candidateBar) => {
    if (
      candidateBar.originX === bar.originX &&
      candidateBar.originY === bar.originY
    ) {
      return false;
    }

    if (bar.direction === "left") {
      return candidateBar.originX < bar.originX;
    } else if (bar.direction === "right") {
      return candidateBar.originX > bar.originX;
    } else if (bar.direction === "up") {
      return candidateBar.originY < bar.originY;
    } else {
      return candidateBar.originY > bar.originY;
    }
  });

  possibleBars.forEach((collisionBar) => {
    if (
      (bar.direction === "left" &&
        bar.left <= collisionBar.right &&
        bar.top <= collisionBar.bottom &&
        bar.bottom >= collisionBar.top) ||
      (bar.direction === "right" &&
        bar.right >= collisionBar.left - 1 &&
        bar.top <= collisionBar.bottom &&
        bar.bottom >= collisionBar.top) ||
      (bar.direction === "up" &&
        bar.top <= collisionBar.bottom &&
        bar.left <= collisionBar.right &&
        bar.right >= collisionBar.left) ||
      (bar.direction === "down" &&
        bar.bottom >= collisionBar.top - 1 &&
        bar.left <= collisionBar.right &&
        bar.right >= collisionBar.left)
    ) {
      bar.active = false;
      bar.color = lightGray;

      this.grid
        .filter(
          (cell) =>
            cell.y < bar.bottom &&
            cell.y >= bar.top &&
            cell.x >= bar.left &&
            cell.x < bar.right
        )
        .forEach((cell) => {
          cell.scored = true;
        });

      // this.evaluateGrid(bar);
      this.fillQueue.push(bar);
    }
  });
}
)}

function _evaluateGrid(gridSize,roundToGrid,gridHeight,floodFill,calculateScore){return(
function(bar) {
  const startingCells = 
    bar.orientation === "vertical" ?
    this.grid.filter(cell => {
      return (
        ((cell.x === bar.left - gridSize) || (cell.x === bar.right + 1)) &&
        cell.y >= bar.top &&
        cell.y < bar.bottom &&
        cell.visited === false
      )
    }) :
    this.grid.filter(cell => {
      return (
        ((cell.y === bar.top - gridSize) || (cell.y === bar.bottom + 1)) &&
        cell.x >= bar.left &&
        cell.x < bar.right &&
        cell.visited === false
      )
    })
      
  const ballCells = this.balls.map(ball => {
    const x = roundToGrid(ball.x) / gridSize;
    const y = roundToGrid(ball.y) / gridSize;
    
    const cellId = x*gridHeight + y;
    return cellId;
  })
  
  let visited = [];
  
  startingCells.forEach(startCell => {
    if (!visited.includes(startCell.id)) {
      const section = floodFill([startCell], this.grid);
      
      if (!section.find(id => ballCells.includes(id))) {
        section.forEach(id => { this.grid[id].scored = true });
      }
      
      visited = [...visited, ...section];
    }
  });

  calculateScore(this.grid, this);
}
)}

function _calculateScore($0){return(
(grid, simulation) => {
  $0.value = simulation.areaCleared = Math.round(100*grid.filter(cell => cell.scored === true).length / grid.length);
  simulation.roundScore = Math.round(47*(100*grid.filter(cell => cell.scored === true).length / grid.length));
}
)}

function _floodFill(){return(
(queue, grid) => {
  const section = [queue[0].id];
  
  while (queue.length) {
    const cell = queue.shift();

    cell.neighbors.forEach(neighbor => {
      const neighborCell = grid[neighbor];
      
      if (neighborCell.scored === false && neighborCell.visited === false) {
        neighborCell.visited = true;
        section.push(neighbor);
        queue.push(neighborCell);
      }
    });
  }

  return section;
}
)}

function _Ball(d3,width,height,gridSize,ballSpeed,geometric){return(
class Ball {
  constructor({ id, x, y, radius, angle, speed, color, ctx }) {
    this.id = id;
    this.x = x || d3.randomUniform(0, width)();
    this.y = y || d3.randomUniform(0, height)();
    this.pos = [x, y];
    this.radius = radius || Math.round((0.8 * gridSize) / 2);
    this.mass = radius * radius;
    this.angle = angle || d3.randomUniform(0, 360)();
    this.speed = speed || d3.randomUniform(1, ballSpeed)();
    this.color =
      color || d3.schemeCategory10[Math.round(d3.randomUniform(0, 9)())];
    this.ctx = ctx || null;
  }

  drawCircle() {
    this.ctx.beginPath();
    this.ctx.arc(this.x, this.y, this.radius, 0, 2 * Math.PI);
    this.ctx.stroke();

    // Maybe change this to a more 3D-looking gradient at some point?
    this.ctx.fillStyle = this.color;
    this.ctx.fill();
  }

  tick() {
    [this.x, this.y] = this.pos = geometric.pointTranslate(
      this.pos,
      this.angle,
      this.speed
    );
    // this.x += 0.1*this.speed;
    // this.y += 0.1*this.speed;
  }
}
)}

function _Bar(roundToGrid,gridSize,barGrowthSpeed,sizeModifier){return(
class Bar {
  constructor({ id, originX, originY, direction, ctx, height, width, active }) {
    this.id = id;

    this.direction = direction;
    this.color = direction === "up" || direction === "left" ? "red" : "blue";
    this.orientation = (direction === "up" || direction === "down") ? "vertical" : "horizontal";
    this.multiplier = (this.direction === "up" || this.direction === "left") ? -1 : 1;
    
    this.originX = direction === "left" ? roundToGrid(originX) - 1 : roundToGrid(originX);
    this.originY = direction === "up" ? roundToGrid(originY) - 1 : roundToGrid(originY);
      
    this.height = height || this.orientation == "vertical" ? 0 : (gridSize - 1);
    this.width = width || this.orientation === "horizontal" ? 0 : (gridSize - 1);
    
    this.ctx = ctx || null;
    
    this.growthSpeed = barGrowthSpeed;
    this.active = active === undefined ? true : active;
    this.remove = false;
    
    this.left = Math.min((this.originX + this.width), this.originX);
    this.right = Math.max((this.originX + this.width), this.originX);
    this.top = Math.min((this.originY + this.height), this.originY);
    this.bottom = Math.max((this.originY + this.height), this.originY);
  }

  drawRect() {    
    this.ctx.beginPath();
    this.ctx.rect(this.originX, this.originY, this.width, this.height);
    
    this.ctx.fillStyle = this.color;
    this.ctx.strokeStyle = this.color;
    
    this.ctx.fill();
    this.ctx.stroke();
  }

  tick() {
    if (this.orientation === "vertical") {
      this.height += sizeModifier*this.multiplier*this.growthSpeed;
    }
    else {
      this.width += sizeModifier*this.multiplier*this.growthSpeed;
    }
    
    this.left = Math.min((this.originX + this.width), this.originX);
    this.right = Math.max((this.originX + this.width), this.originX);
    this.top = Math.min((this.originY + this.height), this.originY);
    this.bottom = Math.max((this.originY + this.height), this.originY);
  }
}
)}

function _GridCell(gridSize,darkGray,backgroundColor,lightGray){return(
class GridCell {
  constructor({ id, x, y, width, height, ctx, neighbors }) {
    this.id = id;

    this.x = x;
    this.y = y;

    this.width = width || (gridSize - 1);
    this.height = height || (gridSize - 1);

    this.ctx = ctx;

    this.neighbors = neighbors || [];

    this.scored = false;
    this.visited = false;
  }

  drawCell() {
    this.visited = false;
    
    this.ctx.beginPath();
    this.ctx.rect(this.x, this.y, this.width, this.height);

    this.ctx.fillStyle = this.scored ? darkGray : backgroundColor;
    this.ctx.strokeStyle = this.scored ? darkGray : lightGray;

    this.ctx.fill();
    this.ctx.stroke();
  }
}
)}

function _roundToGrid(gridSize){return(
(value) => {
  return gridSize*Math.floor(value/gridSize);
}
)}

function _gridHeight(){return(
24
)}

function _gridWidth(){return(
36
)}

function _gridSize(baseGridSize){return(
Math.min(baseGridSize, Math.floor(window.innerWidth / 36))
)}

function _baseGridSize(){return(
25
)}

function _sizeModifier(gridSize,baseGridSize){return(
gridSize / baseGridSize
)}

function _width(gridWidth,gridSize){return(
gridWidth*gridSize
)}

function _height(gridHeight,gridSize){return(
gridHeight*gridSize
)}

function _backgroundColor(){return(
"white"
)}

function _darkGray(){return(
"#A9A9A9"
)}

function _lightGray(){return(
"#D3D3D3"
)}

function _fadedBlue(){return(
"#8187DE"
)}

function _fadedRed(){return(
"#B86566"
)}

function _ballSpeed(Range){return(
Range([1, 10], {value: 6, step: 1, label: "Ball Speed"})
)}

function _barGrowthSpeed(Range){return(
Range([1, 10], {value: 5, step: 1, label: "Bar Growth Speed"})
)}

function _d3(require){return(
require("d3")
)}

function _geometric(require){return(
require("geometric")
)}

function _57(html,width,sizeModifier){return(
html`<style>
  #main-canvas {
    border: 1px solid black;
  }

  #scoreboard {
    width: calc(${width}px - ${sizeModifier < 0.8 ? 2 : 4}rem);
    background-color: #787878;
    color: white;
    font-family: Courier;
    font-size: ${sizeModifier < 0.8 ? 13 : 18}px;
    display: grid;
    justify-items: left;
    grid-template-columns: 1fr 1fr;
    grid-gap: 3px;
    padding: 1rem ${sizeModifier < 0.8 ? 1 : 2}rem;
  }

  #scoreboard h1 {
    font-size: ${sizeModifier < 0.8 ? 32 : 40}px;
    font-family: Courier;
    color: white;
  }

  #scoreboard .title {
    grid-column: 1 / 3;
    justify-self: center;
  }

</style>`
)}

function _58(html,simulation,barDirection,areaCleared){return(
html`
<style>
  #main-canvas {
    cursor: ${
      (simulation && simulation.roundState !== "running") ? "pointer" :
      barDirection === "Horizontal" ? "ew-resize" : "ns-resize"
    };
  }
/* ${areaCleared} */
</style>

`
)}

export default function define(runtime, observer) {
  const main = runtime.module();
  main.variable(observer()).define(["md"], _1);
  main.variable(observer()).define(["md"], _2);
  main.variable(observer()).define(["html","areaCleared","d3","simulation","currentLives"], _3);
  main.variable(observer("viewof barDirection")).define("viewof barDirection", ["Radio"], _barDirection);
  main.variable(observer("barDirection")).define("barDirection", ["Generators", "viewof barDirection"], (G, _) => G.input(_));
  main.variable(observer("canvas")).define("canvas", ["DOM","width","height"], _canvas);
  main.variable(observer("ctx")).define("ctx", ["canvas"], _ctx);
  main.variable(observer()).define(["md"], _7);
  main.define("initial areaCleared", _areaCleared);
  main.variable(observer("mutable areaCleared")).define("mutable areaCleared", ["Mutable", "initial areaCleared"], (M, _) => new M(_));
  main.variable(observer("areaCleared")).define("areaCleared", ["mutable areaCleared"], _ => _.generator);
  main.define("initial currentLives", _currentLives);
  main.variable(observer("mutable currentLives")).define("mutable currentLives", ["Mutable", "initial currentLives"], (M, _) => new M(_));
  main.variable(observer("currentLives")).define("currentLives", ["mutable currentLives"], _ => _.generator);
  main.variable(observer("numStartingBalls")).define("numStartingBalls", _numStartingBalls);
  main.variable(observer("startSimulation")).define("startSimulation", ["init"], _startSimulation);
  main.variable(observer("init")).define("init", ["simulation"], _init);
  main.variable(observer("scoredCells")).define("scoredCells", ["simulation"], _scoredCells);
  main.define("initial clickPosition", _clickPosition);
  main.variable(observer("mutable clickPosition")).define("mutable clickPosition", ["Mutable", "initial clickPosition"], (M, _) => new M(_));
  main.variable(observer("clickPosition")).define("clickPosition", ["mutable clickPosition"], _ => _.generator);
  main.variable(observer("clickHandler")).define("clickHandler", ["d3","mutable clickPosition","simulation","resetGame","nextRound","roundToGrid","gridSize","gridHeight","barDirection","addBar","set","viewof barDirection"], _clickHandler);
  main.variable(observer("addBall")).define("addBall", ["Ball","simulation","ctx"], _addBall);
  main.variable(observer("addBar")).define("addBar", ["Bar","ctx","simulation"], _addBar);
  main.variable(observer("simulation")).define("simulation", ["Simulation","width","height","ctx","ballSet","numStartingBalls","gridSet"], _simulation);
  main.variable(observer("gridSet")).define("gridSet", ["width","gridSize","height","gridHeight","gridWidth","GridCell","ctx"], _gridSet);
  main.variable(observer("ballSet")).define("ballSet", ["Ball","d3","width","height","gridSize","ballSpeed","sizeModifier","ctx"], _ballSet);
  main.variable(observer("Simulation")).define("Simulation", ["d3","fastDetectBallCollision","detectBallWallCollision","detectBarWallCollision","detectBallBarCollision","detectBarToBarCollision","evaluateGrid","endGame","endRound","width","height","mutable areaCleared"], _Simulation);
  main.variable(observer("nextRound")).define("nextRound", ["simulation","mutable areaCleared","mutable currentLives","ballSet","init"], _nextRound);
  main.variable(observer("resetGame")).define("resetGame", ["simulation","mutable areaCleared","mutable currentLives","ballSet","init"], _resetGame);
  main.variable(observer("endGame")).define("endGame", ["sizeModifier"], _endGame);
  main.variable(observer("endRound")).define("endRound", ["sizeModifier"], _endRound);
  main.variable(observer("fastDetectBallCollision")).define("fastDetectBallCollision", ["geometric","elasticCollision"], _fastDetectBallCollision);
  main.variable(observer("detectBallWallCollision")).define("detectBallWallCollision", ["geometric"], _detectBallWallCollision);
  main.variable(observer("detectBarWallCollision")).define("detectBarWallCollision", ["height","lightGray"], _detectBarWallCollision);
  main.variable(observer("detectBallBarCollision")).define("detectBallBarCollision", ["mutable currentLives","geometric"], _detectBallBarCollision);
  main.variable(observer("detectBarToBarCollision")).define("detectBarToBarCollision", ["lightGray"], _detectBarToBarCollision);
  main.variable(observer("evaluateGrid")).define("evaluateGrid", ["gridSize","roundToGrid","gridHeight","floodFill","calculateScore"], _evaluateGrid);
  main.variable(observer("calculateScore")).define("calculateScore", ["mutable areaCleared"], _calculateScore);
  main.variable(observer("floodFill")).define("floodFill", _floodFill);
  main.variable(observer("Ball")).define("Ball", ["d3","width","height","gridSize","ballSpeed","geometric"], _Ball);
  main.variable(observer("Bar")).define("Bar", ["roundToGrid","gridSize","barGrowthSpeed","sizeModifier"], _Bar);
  main.variable(observer("GridCell")).define("GridCell", ["gridSize","darkGray","backgroundColor","lightGray"], _GridCell);
  main.variable(observer("roundToGrid")).define("roundToGrid", ["gridSize"], _roundToGrid);
  main.variable(observer("gridHeight")).define("gridHeight", _gridHeight);
  main.variable(observer("gridWidth")).define("gridWidth", _gridWidth);
  main.variable(observer("gridSize")).define("gridSize", ["baseGridSize"], _gridSize);
  main.variable(observer("baseGridSize")).define("baseGridSize", _baseGridSize);
  main.variable(observer("sizeModifier")).define("sizeModifier", ["gridSize","baseGridSize"], _sizeModifier);
  main.variable(observer("width")).define("width", ["gridWidth","gridSize"], _width);
  main.variable(observer("height")).define("height", ["gridHeight","gridSize"], _height);
  main.variable(observer("backgroundColor")).define("backgroundColor", _backgroundColor);
  main.variable(observer("darkGray")).define("darkGray", _darkGray);
  main.variable(observer("lightGray")).define("lightGray", _lightGray);
  main.variable(observer("fadedBlue")).define("fadedBlue", _fadedBlue);
  main.variable(observer("fadedRed")).define("fadedRed", _fadedRed);
  main.variable(observer("viewof ballSpeed")).define("viewof ballSpeed", ["Range"], _ballSpeed);
  main.variable(observer("ballSpeed")).define("ballSpeed", ["Generators", "viewof ballSpeed"], (G, _) => G.input(_));
  main.variable(observer("viewof barGrowthSpeed")).define("viewof barGrowthSpeed", ["Range"], _barGrowthSpeed);
  main.variable(observer("barGrowthSpeed")).define("barGrowthSpeed", ["Generators", "viewof barGrowthSpeed"], (G, _) => G.input(_));
  const child1 = runtime.module(define1);
  main.import("Range", child1);
  main.import("Radio", child1);
  const child2 = runtime.module(define2);
  main.import("set", child2);
  const child3 = runtime.module(define3);
  main.import("Vector", child3);
  main.import("elasticCollision", child3);
  main.variable(observer("d3")).define("d3", ["require"], _d3);
  main.variable(observer("geometric")).define("geometric", ["require"], _geometric);
  main.variable(observer()).define(["html","width","sizeModifier"], _57);
  main.variable(observer()).define(["html","simulation","barDirection","areaCleared"], _58);
  return main;
}
