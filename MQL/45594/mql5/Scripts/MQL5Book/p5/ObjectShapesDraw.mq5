//+------------------------------------------------------------------+
//|                                             ObjectShapesDraw.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#include <MQL5Book/AutoPtr.mqh>
#include <MQL5Book/TypeName.mqh>

#define FIGURES 21

class Shape;

//+------------------------------------------------------------------+
//| Base shape class for drawing                                     |
//+------------------------------------------------------------------+
class Shape
{
public:
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
      
      virtual Shape *create(const int px, const int py, const color back,
         const int &parameters[]) = 0;
   };
   
protected:
   int x, y;
   color backgroundColor;
   const string type;
   string name;

   Shape(int px, int py, color back, string t) :
      x(px), y(py),
      backgroundColor(back),
      type(t)
   {
   }
   
public:
   ~Shape()
   {
      ObjectDelete(0, name);
   }

   virtual void draw() = 0;
   virtual void setup(const int &parameters[]) = 0;
   // TODO: read from and write to a file
   //virtual void load(const int handle) = 0;
   //virtual void save(const int handle) = 0;
   
   Shape *setColor(const color c)
   {
      backgroundColor = c;
      return &this;
   }

   Shape *moveX(const int _x)
   {
      x += _x;
      return &this;
   }

   Shape *moveY(const int _y)
   {
      y += _y;
      return &this;
   }
   
   Shape *move(const int _x, const int _y)
   {
      x += _x;
      y += _y;
      return &this;
   }

   string toString() const
   {
      return type + " " + (string)x + " " + (string)y;
   }
};

//+------------------------------------------------------------------+
//| Templatized helper class for Shape-derived classes registration  |
//+------------------------------------------------------------------+
template<typename T>
class MyRegistrator : public Shape::Registrator
{
public:
   MyRegistrator() : Registrator(TYPENAME(T))
   {
   }
   
   virtual Shape *create(const int px, const int py, const color back,
      const int &parameters[]) override
   {
      T *temp = new T(px, py, back);
      temp.setup(parameters);
      return temp;
   }
};

//+------------------------------------------------------------------+
//| Rectangle shape                                                  |
//+------------------------------------------------------------------+
class Rectangle : public Shape
{
   static MyRegistrator<Rectangle> r;
   
protected:
   int dx, dy; // size (width, height)
   
   Rectangle(int px, int py, color back, string t) :
      Shape(px, py, back, t), dx(1), dy(1)
   {
   }

public:
   Rectangle(int px, int py, color back) :
      Shape(px, py, back, TYPENAME(this)), dx(1), dy(1)
   {
      name = TYPENAME(this) + (string)r.increment();
   }
   
   virtual void setup(const int &parameters[]) override
   {
      if(ArraySize(parameters) < 2)
      {
         Print("Insufficient parameters for Rectangle");
         return;
      }
      dx = parameters[0];
      dy = parameters[1];
   }

   void draw() override
   {
      // Print("Drawing rectangle");
      int subw;
      datetime t;
      double p;
      ChartXYToTimePrice(0, x, y, subw, t, p);
      ObjectCreate(0, name, OBJ_RECTANGLE, 0, t, p);
      ChartXYToTimePrice(0, x + dx, y + dy, subw, t, p);
      ObjectSetInteger(0, name, OBJPROP_TIME, 1, t);
      ObjectSetDouble(0, name, OBJPROP_PRICE, 1, p);
      
      ObjectSetInteger(0, name, OBJPROP_COLOR, backgroundColor);
      ObjectSetInteger(0, name, OBJPROP_FILL, true);
   }
};

//+------------------------------------------------------------------+
//| Square shape                                                     |
//+------------------------------------------------------------------+
class Square : public Rectangle
{
   static MyRegistrator<Square> r;
public:
   Square(int px, int py, color back) :
      Rectangle(px, py, back, TYPENAME(this))
   {
      name = TYPENAME(this) + (string)r.increment();
   }
   
   virtual void setup(const int &parameters[]) override
   {
      if(ArraySize(parameters) < 1)
      {
         Print("Insufficient parameters for Square");
         return;
      }
      dx = dy = parameters[0];
   }
   
   void draw() override
   {
      // Print("Drawing square (delegated to parent)");
      Rectangle::draw();
   }
};

//+------------------------------------------------------------------+
//| Ellipse shape                                                    |
//+------------------------------------------------------------------+
class Ellipse : public Shape
{
   static MyRegistrator<Ellipse> r;
protected:
   int dx, dy; // large and small radiuses

   Ellipse(int px, int py, color back, string t) :
      Shape(px, py, back, t), dx(1), dy(1)
   {
   }

public:
   Ellipse(int px, int py, color back) :
      Shape(px, py, back, TYPENAME(this)), dx(1), dy(1)
   {
      name = TYPENAME(this) + (string)r.increment();
   }
   
   virtual void setup(const int &parameters[]) override
   {
      if(ArraySize(parameters) < 2)
      {
         Print("Insufficient parameters for Ellipse");
         return;
      }
      dx = parameters[0]; // first radius
      dy = parameters[1]; // second radius
   }

   void draw() override
   {
      // Print("Drawing ellipse");
      int subw;
      datetime t;
      double p;
      
      // (x, y) is a center
      // p0: x + dx, y
      // p1: x - dx, y
      // p2: x, y + dy
      
      ChartXYToTimePrice(0, x + dx, y, subw, t, p);
      ObjectCreate(0, name, OBJ_ELLIPSE, 0, t, p);
      ChartXYToTimePrice(0, x - dx, y, subw, t, p);
      ObjectSetInteger(0, name, OBJPROP_TIME, 1, t);
      ObjectSetDouble(0, name, OBJPROP_PRICE, 1, p);
      ChartXYToTimePrice(0, x, y + dy, subw, t, p);
      ObjectSetInteger(0, name, OBJPROP_TIME, 2, t);
      ObjectSetDouble(0, name, OBJPROP_PRICE, 2, p);
      
      ObjectSetInteger(0, name, OBJPROP_COLOR, backgroundColor);
      ObjectSetInteger(0, name, OBJPROP_FILL, true);
   }
};

//+------------------------------------------------------------------+
//| Circle shape                                                     |
//+------------------------------------------------------------------+
class Circle : public Ellipse
{
   static MyRegistrator<Circle> r;
public:
   Circle(int px, int py, color back) :
      Ellipse(px, py, back, TYPENAME(this))
   {
      name = TYPENAME(this) + (string)r.increment();
   }
   
   virtual void setup(const int &parameters[]) override
   {
      if(ArraySize(parameters) < 1)
      {
         Print("Insufficient parameters for Circle");
         return;
      }
      dx = dy = parameters[0];
   }

   void draw() override
   {
      // Print("Drawing circle (delegated to parent)");
      Ellipse::draw();
   }
};

//+------------------------------------------------------------------+
//| Triangle shape                                                   |
//+------------------------------------------------------------------+
class Triangle: public Shape
{
   static MyRegistrator<Triangle> r;
protected:
   int dx;  // single side (equilateral)
public:
   Triangle(int px, int py, color back) :
      Shape(px, py, back, TYPENAME(this)), dx(1)
   {
      name = TYPENAME(this) + (string)r.increment();
   }
   
   virtual void setup(const int &parameters[]) override
   {
      if(ArraySize(parameters) < 1)
      {
         Print("Insufficient parameters for Triangle");
         return;
      }
      dx = parameters[0];
   }

   void draw()
   {
      // Print("Drawing triangle");
      int subw;
      datetime t;
      double p;
      
      const double R = dx * sqrt(3) / 3;
      // (x, y) is a center
      // R = a * sqrt(3) / 3
      // p0: x, y + R
      // p1: x - R * cos(30), y - R * sin(30)
      // p2: x + R * cos(30), y - R * sin(30)
      
      ChartXYToTimePrice(0, x, y + (int)R, subw, t, p);
      ObjectCreate(0, name, OBJ_TRIANGLE, 0, t, p);
      ChartXYToTimePrice(0, x - (int)(R * cos(30 * M_PI / 180)),
         y - (int)(R * sin(30 * M_PI / 180)), subw, t, p);
      ObjectSetInteger(0, name, OBJPROP_TIME, 1, t);
      ObjectSetDouble(0, name, OBJPROP_PRICE, 1, p);
      ChartXYToTimePrice(0, x + (int)(R * cos(30 * M_PI / 180)),
         y - (int)(R * sin(30 * M_PI / 180)), subw, t, p);
      ObjectSetInteger(0, name, OBJPROP_TIME, 2, t);
      ObjectSetDouble(0, name, OBJPROP_PRICE, 2, p);
      
      ObjectSetInteger(0, name, OBJPROP_COLOR, backgroundColor);
      ObjectSetInteger(0, name, OBJPROP_FILL, true);
   }
};

//+------------------------------------------------------------------+
//| Static in-class variables                                        |
//+------------------------------------------------------------------+
static Shape::Registrator *Shape::Registrator::regs[] = {};

static MyRegistrator<Rectangle> Rectangle::r;
static MyRegistrator<Square> Square::r;
static MyRegistrator<Ellipse> Ellipse::r;
static MyRegistrator<Circle> Circle::r;
static MyRegistrator<Triangle> Triangle::r;

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
   const int w = (int)ChartGetInteger(0, CHART_WIDTH_IN_PIXELS);
   const int h = (int)ChartGetInteger(0, CHART_HEIGHT_IN_PIXELS);
   
   const int n = random(Shape::Registrator::getTypeCount());
   
   int cx = 1 + w / 4 + random(w / 2), cy = 1 + h / 4 + random(h / 2);
   int clr = ((random(256) << 16) | (random(256) << 8) | random(256));

   int custom[] = {1 + random(w / 4), 1 + random(h / 4)};
   return Shape::Registrator::get(n).create(cx, cy, clr, custom);
}

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   // adjust chart settings
   const int scale = (int)ChartGetInteger(0, CHART_SCALE);
   ChartSetInteger(0, CHART_SCALEFIX_11, true);
   ChartSetInteger(0, CHART_SCALE, 0); // most compressed view: 1 bar = 1 pixel
   ChartSetInteger(0, CHART_SHOW, false);
   ChartRedraw();
   
   AutoPtr<Shape> shapes[FIGURES];
   
   // prepare a random set of shapes for drawing
   for(int i = 0; i < FIGURES; ++i)
   {
      Shape *shape = shapes[i] = addRandomShape();
      shape.draw();
   }

   ChartRedraw(); // show initial state

   const int n = Shape::Registrator::getTypeCount();
   for(int i = 0; i < n; ++i)
   {
      Print(Shape::Registrator::get(i).type, " ",
            Shape::Registrator::get(i).getShapeCount());
   }
   
   // keep small random movement of shapes until user stops it
   while(!IsStopped())
   {
      Sleep(250);
      for(int i = 0; i < FIGURES; ++i)
      {
         shapes[i][].move(random(20) - 10, random(20) - 10);
         shapes[i][].draw();
      }
      ChartRedraw();
   }

   ChartSetInteger(0, CHART_SCALEFIX, false); // CHART_SCALEFIX_11
   ChartSetInteger(0, CHART_SCALE, scale);
   ChartSetInteger(0, CHART_SHOW, true);
}
//+------------------------------------------------------------------+
