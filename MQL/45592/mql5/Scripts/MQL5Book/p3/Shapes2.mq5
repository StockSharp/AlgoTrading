//+------------------------------------------------------------------+
//|                                                      Shapes2.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#include <MQL5Book/TypeName.mqh>

struct Pair
{
   int x, y;
   Pair(int a, int b): x(a), y(b) { }
};

//+------------------------------------------------------------------+
//| Base shape class for drawing                                     |
//+------------------------------------------------------------------+
class Shape
{
protected:
   Pair coordinates;       // center coordinates (embedded object)
   color backgroundColor;
   const string type;

   Shape() :
      coordinates(0, 0),
      backgroundColor(clrNONE)
   {
   }

   Shape(int px, int py, color back, string t) :
      coordinates(px, py),
      backgroundColor(back),
      type(t)
   {
   }

public:

   void draw()
   {
   }

   Shape *setColor(const color c)
   {
      backgroundColor = c;
      return &this;
   }

   Shape *moveX(const int x)
   {
      coordinates.x += x;
      return &this;
   }

   Shape *moveY(const int y)
   {
      coordinates.y += y;
      return &this;
   }

   string toString() const
   {
      return type + " " + (string)coordinates.x + " " + (string)coordinates.y;
   }
};

//+------------------------------------------------------------------+
//| Rectangle shape                                                  |
//+------------------------------------------------------------------+
class Rectangle : public Shape
{
protected:
   int dx, dy; // size (width, height)

   Rectangle(int px, int py, int sx, int sy, color back, string t) :
      Shape(px, py, back, t), dx(sx), dy(sy)
   {
   }

public:
   Rectangle(int px, int py, int sx, int sy, color back) :
      Shape(px, py, back, TYPENAME(this)), dx(sx), dy(sy)
   {
   }
};

//+------------------------------------------------------------------+
//| Square shape                                                     |
//+------------------------------------------------------------------+
class Square : public Rectangle
{
public:
   Square(int px, int py, int sx, color back) :
      Rectangle(px, py, sx, sx, back, TYPENAME(this))
   {
   }
};

//+------------------------------------------------------------------+
//| Ellipse shape                                                    |
//+------------------------------------------------------------------+
class Ellipse : public Shape
{
protected:
   int dx, dy; // large and small radiuses

   Ellipse(int px, int py, int rx, int ry, color back, string t) :
      Shape(px, py, back, t), dx(rx), dy(ry)
   {
   }

public:
   Ellipse(int px, int py, int rx, int ry, color back) :
      Shape(px, py, back, TYPENAME(this)), dx(rx), dy(ry)
   {
   }
};

//+------------------------------------------------------------------+
//| Circle shape                                                     |
//+------------------------------------------------------------------+
class Circle : public Ellipse
{
public:
   Circle(int px, int py, int rx, color back) :
      Ellipse(px, py, rx, rx, back, TYPENAME(this))
   {
   }
};

//+------------------------------------------------------------------+
//| Triangle shape                                                   |
//+------------------------------------------------------------------+
class Triangle: public Shape
{
   int dx;  // single side
public:
   Triangle(int px, int py, int side, color back) :
      Shape(px, py, back, TYPENAME(this)), dx(side)
   {
   }
};

//+------------------------------------------------------------------+
//| Return random number in a range                                  |
//+------------------------------------------------------------------+
int random(int range)
{
   return (int)(rand() / 32767.0 * range);
}

//+------------------------------------------------------------------+
//| Create a random shape                                            |
//+------------------------------------------------------------------+
Shape *addRandomShape()
{
   enum SHAPES
   {
      RECTANGLE,
      ELLIPSE,
      TRIANGLE,
      SQUARE,
      CIRCLE,
      NUMBER_OF_SHAPES
   };

   SHAPES type = (SHAPES)random(NUMBER_OF_SHAPES);
   int cx = random(500), cy = random(500), dx = random(200), dy = random(200);
   color clr = (color)((random(256) << 16) | (random(256) << 8) | random(256));
   switch(type)
   {
   case RECTANGLE:
      return new Rectangle(cx, cy, dx, dy, clr);
   case ELLIPSE:
      return new Ellipse(cx, cy, dx, dy, clr);
   case TRIANGLE:
      return new Triangle(cx, cy, dx, clr);
   case SQUARE:
      return new Square(cx, cy, dx, clr);
   case CIRCLE:
      return new Circle(cx, cy, dx, clr);
   }
   return NULL;
}

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   Shape *shapes[10];

   // emulate arbitrary set of shapes created by user
   for(int i = 0; i < 10; ++i)
   {
      shapes[i] = addRandomShape();
   }

   // process all shapes: currenly just logging
   for(int i = 0; i < 10; ++i)
   {
      Print(i, ": ", shapes[i].toString());
      delete shapes[i];
   }
}
//+------------------------------------------------------------------+
