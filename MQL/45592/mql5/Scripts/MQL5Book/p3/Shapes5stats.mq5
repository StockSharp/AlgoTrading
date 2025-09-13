//+------------------------------------------------------------------+
//|                                                 Shapes5stats.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#include <MQL5Book/TypeName.mqh>

//+------------------------------------------------------------------+
//| Base shape class for drawing                                     |
//+------------------------------------------------------------------+
class Shape
{
public:
   struct Pair
   {
      int x, y;
      Pair(int a, int b): x(a), y(b) { }
   };

protected:
   class Registrator
   {
      static Registrator *regs[];
      int shapeCount;
   public:
      const string type;
      Registrator(const string t) : type(t), shapeCount(0)
      {
         const int n = ArraySize(regs);
         ArrayResize(regs, n + 1);
         regs[n] = &this;
      }
      
      static int getTypeCount()
      {
         return ArraySize(regs);
      }
      
      static Registrator *get(const int i)
      {
         return regs[i];
      }
      
      int increment()
      {
         return ++shapeCount;
      }
      
      int getShapeCount() const
      {
         return shapeCount;
      }
   };
   
   Pair coordinates;
   color backgroundColor;
   const string type;

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

// the following macro can be used for declaration of specific registrator class
#define REGISTRATOR(A) \
   class A##Registrator : public Registrator \
   { \
   public: \
      A##Registrator() : Registrator(#A) { } \
   }; \
   static A##Registrator r;

//+------------------------------------------------------------------+
//| Rectangle shape                                                  |
//+------------------------------------------------------------------+
class Rectangle : public Shape
{
   // example of how the macro can be used inside a Shape class:
   REGISTRATOR(Rectangle)
   // it will be expanded to RectangleRegistrator class, but
   // NB: definition of 'r'-variable must be specified outside the class
   // (see below)
   
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
      // this is another way of creating shape registrator instead of REGISTRATOR macro
      // static Registrator r(TYPENAME(this));
      r.increment();
   }

   void draw() override
   {
      Print("Drawing rectangle");
   }
};

//+------------------------------------------------------------------+
//| Square shape                                                     |
//+------------------------------------------------------------------+
class Square : public Rectangle
{
   REGISTRATOR(Square)
public:
   Square(int px, int py, int sx, color back) :
      Rectangle(px, py, sx, sx, back, TYPENAME(this))
   {
      // static Registrator r(TYPENAME(this));
      r.increment();
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
      // the macro is not used in this class, so create registrator as local static
      static Registrator r(TYPENAME(this));
      r.increment();
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
      static Registrator r(TYPENAME(this));
      r.increment();
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
      static Registrator r(TYPENAME(this));
      r.increment();
   }

   void draw()
   {
      Print("Drawing triangle");
   }
};

static Shape::Registrator *Shape::Registrator::regs[] = {};
// for classes where the macro was used we need to define 'r' static variable
static Rectangle::RectangleRegistrator Rectangle::r;
static Square::SquareRegistrator Square::r;

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
   // emulate drawing of random set of shapes
   for(int i = 0; i < 10; ++i)
   {
      Shape *shape = addRandomShape();
      // emulate shifting all shapes
      shape.move(Shape::Pair(100, 100));
      shape.draw();
      delete shape;
   }

   const int n = Shape::Registrator::getTypeCount();
   for(int i = 0; i < n; ++i)
   {
      Print(Shape::Registrator::get(i).type, " ",
            Shape::Registrator::get(i).getShapeCount());
   }
}
//+------------------------------------------------------------------+
