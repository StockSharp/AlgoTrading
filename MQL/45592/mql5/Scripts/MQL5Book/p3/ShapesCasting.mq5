//+------------------------------------------------------------------+
//|                                                ShapesCasting.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#include <MQL5Book/TypeName.mqh>

#define PRT(A) Print(#A, "=", (A))

//+------------------------------------------------------------------+
//| Base shape class for drawing                                     |
//+------------------------------------------------------------------+
class Shape
{
public:
   enum SHAPES
   {
      RECTANGLE,
      ELLIPSE,
      TRIANGLE,
      SQUARE,
      CIRCLE,
      NUMBER_OF_SHAPES
   };

   struct Pair
   {
      int x, y;
      Pair(int a, int b): x(a), y(b) { }
   };

protected:
   Pair coordinates;
   color backgroundColor;
   const string type;  // remove 'const' here to make Shape's copyable
                           // without custom operator= implementation

   Shape(int px, int py, color back, string t) :
      coordinates(px, py),
      backgroundColor(back),
      type(t)
   {
   }

public:
   virtual void draw()
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
   
   Shape *move(const Pair &pair)
   {
      coordinates.x += pair.x;
      coordinates.y += pair.y;
      return &this;
   }

   string toString() const
   {
      return type + " " + (string)coordinates.x + " " + (string)coordinates.y;
   }
};

class Square;
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

   /*
   // Uncomment this to fix the ERROR caused by
   // default Rectangle::operator= is deleted duy to 'const' field in Shape
   Rectangle *operator=(const Rectangle &r)
   {
      coordinates.x = r.coordinates.x;
      coordinates.y = r.coordinates.y;
      backgroundColor = r.backgroundColor;
      dx = r.dx;
      dy = r.dy;
      return &this;
   }
   */
   
   bool amISquare() const
   {
      return dx == dy;
   }
   
   void draw() override
   {
      ::Print("Drawing rectangle"); // will print via global Print(...)
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

   void draw()
   {
      Print("Drawing square");
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

   void draw()
   {
      Print("Drawing ellipse");
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

   void draw()
   {
      Print("Drawing circle");
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

   void draw()
   {
      Print("Drawing triangle");
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
Shape *addRandomShape(Shape::SHAPES t = Shape::SHAPES::NUMBER_OF_SHAPES)
{
   Shape::SHAPES type = ((t == Shape::SHAPES::NUMBER_OF_SHAPES) ?
      (Shape::SHAPES)random(Shape::SHAPES::NUMBER_OF_SHAPES): t);
   
   int cx = random(500), cy = random(500), dx = random(200), dy = random(200);
   color clr = (color)((random(256) << 16) | (random(256) << 8) | random(256));
   
   switch(type)
   {
   case Shape::SHAPES::RECTANGLE:
      return new Rectangle(cx, cy, dx, dy, clr);
   case Shape::SHAPES::ELLIPSE:
      return new Ellipse(cx, cy, dx, dy, clr);
   case Shape::SHAPES::TRIANGLE:
      return new Triangle(cx, cy, dx, clr);
   case Shape::SHAPES::SQUARE:
      return new Square(cx, cy, dx, clr);
   case Shape::SHAPES::CIRCLE:
      return new Circle(cx, cy, dx, clr);
   }

   return NULL;
}

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   Rectangle *r = addRandomShape(Shape::SHAPES::RECTANGLE);
   Square *s = addRandomShape(Shape::SHAPES::SQUARE);
   Circle *c = NULL;

   // OBJECTS by value
   // ERROR:
   // 
   // attempting to reference deleted function 'void Rectangle::operator=(const Rectangle&)'
   //    function 'void Rectangle::operator=(const Rectangle&)' was implicitly deleted
   //    because it invokes deleted function 'void Shape::operator=(const Shape&)'
   // function 'void Shape::operator=(const Shape&)' was implicitly deleted
   //    because member 'type' has 'const' modifier
   //
   // Rectangle r3(100, 100, 10, 10, clrWhite);
   // r3 = s; // copy and conversion by value
   //
   //    will work if remove 'const' for 'string type' in Shape,
   //           or if you provide an explicit Rectangle::operator= (see above)
   
   Shape *p;
   Rectangle *r2;
   
   // OK
   p = c;   // Circle -> Shape
   p = s;   // Square -> Shape
   r = p;
   p = r;   // Rectangle -> Shape
   r2 = p;  // Shape -> Rectangle
   r2.amISquare();

   // COMPILE ERRORs
   // p.amISquare(); // undeclared identifier: amISquare is in Rectangle
   // Circle and Rectangle are in different branches of hierarchy
   // c = r; // type mismatch
   
   Square *s2;
   // RUNTIME ERROR
   // s2 = p; // Incorrect casting of pointers
   s2 = dynamic_cast<Square *>(p); // attempt to cast, NULL on failure
   Print(s2); // 0
   
   c = dynamic_cast<Circle *>(r); // attempt to cast, NULL on failure
   Print(c); // 0

   void *v;
   v = s;    // point to a Square object
   PRT(dynamic_cast<Shape *>(v));
   PRT(dynamic_cast<Rectangle *>(v));
   PRT(dynamic_cast<Square *>(v));
   PRT(dynamic_cast<Circle *>(v));    // 0
   PRT(dynamic_cast<Triangle *>(v));  // 0
   
   delete r;
   delete s;
}
//+------------------------------------------------------------------+
