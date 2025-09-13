//+------------------------------------------------------------------+
//|                                                      Shapes1.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#include <MQL5Book/TypeName.mqh>

struct Pair
{
   int x, y;
   Pair(int a, int b): x(a), y(b)
   {
      Print(__FUNCSIG__, " ", x, " ", y);
   }
   ~Pair()
   {
      Print(__FUNCSIG__, " ", x, " ", y);
   }
};

//+------------------------------------------------------------------+
//| Base shape class for drawing                                     |
//+------------------------------------------------------------------+
class Shape
{
protected:
   // int x, y;            // center coordinates
   Pair coordinates;       // the same in the form of embedded object
   color backgroundColor;
   const string type;

public:
   Shape() :               // default constructor
      // x(0), y(0),
      coordinates(0, 0),
      backgroundColor(clrNONE),
      type(TYPENAME(this))
   {
      Print(__FUNCSIG__, " ", &this);
   }

   Shape(int px, int py, color back, string t = NULL) :
      // x(px), y(py),
      coordinates(px, py),
      backgroundColor(back),
      type(t != NULL ? t : TYPENAME(Shape))
   {
      Print(__FUNCSIG__, " ", &this);
   }

   Shape(const Shape &source) : // copy-constructor
      coordinates(source.coordinates.x, source.coordinates.y),
      backgroundColor(source.backgroundColor),
      type(source.type)
   {
   }

   ~Shape()
   {
      Print(__FUNCSIG__, " ", &this);
   }

   // unreal implementation of draw:
   //   just to demonstrate 'this' dereferencing
   // warning: declaration of 'backgroundColor' hides member
   void draw(string backgroundColor = NULL)
   {
      // ... use the string, maybe lookup color by description

      // misused name
      // warning: implicit conversion from 'number' to 'string'
      backgroundColor = clrBlue;
      this.backgroundColor = clrBlue; // ok

      {
         // warning: declaration of 'backgroundColor' hides local variable
         bool backgroundColor = false;
         // ... do something
         this.backgroundColor = clrRed; // ok
      }
      // ...
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

public:
   Rectangle(int px, int py, int sx, int sy, color back) :
      Shape(px, py, back, TYPENAME(this)), dx(sx), dy(sy)
   {
      Print(__FUNCSIG__, " ", &this);
   }
   Rectangle(const Rectangle &other) :
      Shape(other), dx(other.dx), dy(other.dy)
   {
   }
   ~Rectangle()
   {
      Print(__FUNCSIG__, " ", &this);
   }
};

//+------------------------------------------------------------------+
//| Ellipse shape                                                    |
//+------------------------------------------------------------------+
class Ellipse : public Shape
{
protected:
   int dx, dy; // large and small radiuses

public:
   Ellipse(int px, int py, int rx, int ry, color back) :
      Shape(px, py, back, TYPENAME(this)), dx(rx), dy(ry)
   {
      Print(__FUNCSIG__, " ", &this);
   }
   ~Ellipse()
   {
      Print(__FUNCSIG__, " ", &this);
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
      Print(__FUNCSIG__, " ", &this);
   }
   ~Triangle()
   {
      Print(__FUNCSIG__, " ", &this);
   }
};

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   Shape s;
   // Print(s);   // 's' - objects are passed by reference only
   Print(&s);     // ok: &s means a pointer to s
   // errors: cannot access to protected member declared in class 'Shape'
   // Print(s.x, " ", s.y);

   // method chaining through 'this'
   s.setColor(clrWhite).moveX(80).moveY(-50);

   Rectangle r(100, 200, 75, 50, clrBlue);
   Ellipse e(200, 300, 100, 150, clrRed);

   /*
   // copy-construction
   Shape s2(s);            // ok: syntax 1
   Shape s3 = s;           // ok: syntax 2

   // error: attempting to reference deleted function
   //        'void Shape::operator=(const Shape&)'
   //        function 'void Shape::operator=(const Shape&)' was implicitly
   //        deleted because member 'type' has 'const' modifier
   // Shape s4;
   // s4 = s;

   Shape s5(r);            // ok: copy derived to base

   // error: 'Rectangle' - no one of the overloads
   //        can be applied to the function call
   Rectangle r4(s);
   */

   // Print(TYPENAME(s), " ", TYPENAME(r)); // Shape Rectangle

   Print(s.toString());
   Print(r.toString());
   Print(e.toString());
}
//+------------------------------------------------------------------+
