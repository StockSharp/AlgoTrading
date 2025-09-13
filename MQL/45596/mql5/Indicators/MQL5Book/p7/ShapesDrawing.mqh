//+------------------------------------------------------------------+
//|                                                ShapesDrawing.mqh |
//|                              Copyright (c) 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#include <MQL5Book/AutoPtr.mqh>
#include <MQL5Book/Defines.mqh>
#include <MQL5Book/MqlError.mqh>
#include <MQL5Book/TypeName.mqh>

#define PROXIMITY 50 // drag hot-spot in pixels

//+------------------------------------------------------------------+
//| Some of possible color mixing rules for overlapping Shapes       |
//+------------------------------------------------------------------+
enum COLOR_EFFECT
{
   PLAIN,
   COMPLEMENT,
   BLENDING_XOR,
   DIMMING_SUM,
   LIGHTEN_OR,
};

class Shape;

//+------------------------------------------------------------------+
//| Basic interface for drawing primitives                           |
//+------------------------------------------------------------------+
interface Drawing
{
   void point(const float x1, const float y1, const uint pixel);
   void line(const int x1, const int y1, const int x2, const int y2, const color clr);
   void rect(const int x1, const int y1, const int x2, const int y2, const color clr);
   Shape *findAt(const int x, const int y, const Shape *priority = NULL);
};

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
   
   virtual void draw(Drawing *drawing) = 0;
   virtual void setup(const int &parameters[]) = 0;
   
   virtual bool contains(const int _x, const int _y)
   {
      return fabs(x - _x) <= PROXIMITY && fabs(y - _y) <= PROXIMITY;
   }
   
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
   
   virtual Shape *resize(const int _x, const int _y)
   {
      return &this;
   }

   string toString() const
   {
      return type + " " + (string)x + " " + (string)y + " " + (string)backgroundColor;
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

   virtual void draw(Drawing *drawing) override
   {
      drawing.rect(x - dx / 2, y - dx / 2, x + dx / 2, y + dy / 2, backgroundColor);
   }

   virtual Shape *resize(const int _x, const int _y) override
   {
      dx += _x;
      dy += _y;
      if(dx < 0) dx = -dx;
      if(dy < 0) dy = -dy;
      return &this;
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
   
   void draw(Drawing *drawing) override
   {
      Rectangle::draw(drawing);
   }

   virtual Shape *resize(const int _x, const int _y) override
   {
      const int d = _x + _y;
      dx += d;
      dy += d;
      if(dx < 0) dx = -dx;
      if(dy < 0) dy = -dy;
      return &this;
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

   void draw(Drawing *drawing) override
   {
      // (x, y) is a center
      // p0: x + dx, y
      // p1: x - dx, y
      // p2: x, y + dy

      const int hh = dy * dy;
      const int ww = dx * dx;
      const int hhww = hh * ww;
      int x0 = dx;
      int step = 0;
      
      // main horizontal diameter
      drawing.line(x - dx, y, x + dx, y, backgroundColor);
      
      // smaller horizontal lines in upper and lower halves keep symmetry
      for(int j = 1; j <= dy; j++)
      {
         for(int x1 = x0 - (step - 1); x1 > 0; --x1)
         {
            if(x1 * x1 * hh + j * j * ww <= hhww)
            {
               step = x0 - x1;
               break;
            }
         }
         x0 -= step;
         
         drawing.line(x - x0, y - j, x + x0, y - j, backgroundColor);
         drawing.line(x - x0, y + j, x + x0, y + j, backgroundColor);
         // TODO: edge smoothing by xx
         // float xx = (float)sqrt((hhww - j * j * ww) / hh);
      }
   }

   virtual Shape *resize(const int _x, const int _y) override
   {
      dx += _x;
      dy += _y;
      if(dx < 0) dx = -dx;
      if(dy < 0) dy = -dy;
      return &this;
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

   void draw(Drawing *drawing) override
   {
      Ellipse::draw(drawing);
   }

   virtual Shape *resize(const int _x, const int _y) override
   {
      const int d = _x + _y;
      dx += d;
      dy += d;
      if(dx < 0) dx = -dx;
      if(dy < 0) dy = -dy;
      return &this;
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

   virtual void draw(Drawing *drawing) override
   {
      if(dx <= 3)
      {
         drawing.point(x, y, ColorToARGB(backgroundColor));
         return;
      }
      
      // (x, y) is a center
      // R = a * sqrt(3) / 3
      // p0: x, y + R
      // p1: x - R * cos(30), y - R * sin(30)
      // p2: x + R * cos(30), y - R * sin(30)
      // height by Pythagorean theorem: dx * dx = dx * dx / 4 + h * h
      // sqrt(dx * dx * 3/4) = h
      const double R = dx * sqrt(3) / 3;
      const double H = sqrt(dx * dx * 3 / 4);
      const double angle = H / (dx / 2);
      
      // main vertical line (triangle height)
      const int base = y + (int)(R - H);
      drawing.line(x, y + (int)R, x, base, backgroundColor);
      
      // go left and right sideways with shorter vertical lines, with symmetry
      for(int j = 1; j <= dx / 2; ++j)
      {
         drawing.line(x - j, y + (int)(R - angle * j), x - j, base, backgroundColor);
         drawing.line(x + j, y + (int)(R - angle * j), x + j, base, backgroundColor);
      }
   }

   virtual Shape *resize(const int _x, const int _y) override
   {
      const int d = _x + _y;
      dx += d;
      if(dx < 0) dx = -dx;
      return &this;
   }
};

//+------------------------------------------------------------------+
//| Our implementation of Drawing interface based on bitmap resource |
//+------------------------------------------------------------------+
class MyDrawing: public Drawing
{
   const string sheet;  // resource
   uint data[];
   int width, height;
   AutoPtr<Shape> shapes[];
   const uint bg;
   COLOR_EFFECT xormode;
   bool smoothing;
   
public:
   MyDrawing(const string resource, uint &array[], const int w, const int h, const uint clr):
      sheet(resource), width(w), height(h), bg(clr),
      xormode(PLAIN), smoothing(false)
   {
      ArraySwap(data, array);
   }
   
   ~MyDrawing()
   {
      ResourceFree(sheet);
   }
   
   void resize(const int w, const int h)
   {
      width = w;
      height = h;
      ArrayResize(data, w * h);
   }
   
   string resource() const
   {
      return sheet;
   }
   
   Shape *push(Shape *shape)
   {
      shapes[EXPAND(shapes)] = shape;
      return shape;
   }
   
   void setColorEffect(const COLOR_EFFECT x)
   {
      xormode = x;
   }
   
   void draw(const bool refresh = false)
   {
      if(refresh)
      {
         ArrayInitialize(data, bg);
      }
      for(int i = 0; i < ArraySize(shapes); ++i)
      {
         shapes[i][].draw(&this);
      }
      ResourceCreate(sheet, data, width, height, 0, 0, width, COLOR_FORMAT_ARGB_NORMALIZE);
      ChartRedraw();
   }

   void _point(const int x1, const int y1, const uint pixel)
   {
      const int index = y1 * width + x1;
      if(index >= 0 && index < ArraySize(data))
      {
         switch(xormode)
         {
         case COMPLEMENT:
            data[index] = (pixel ^ (1 - data[index])); // complementary
            break;
         case BLENDING_XOR:
            data[index] = (pixel & 0xFF000000) | (pixel ^ data[index]); // blending (XOR)
            break;
         case DIMMING_SUM:
            data[index] =  (pixel + data[index]); // dimming (SUM)
            break;
         case LIGHTEN_OR:
            data[index] =  (pixel & 0xFF000000) | (pixel | data[index]); // lighten (OR)
            break;
         case PLAIN:
         default:
            data[index] = pixel;
         }
      }
   }
   
   virtual void point(const float x1, const float y1, const uint pixel) override
   {
      const int x_main = (int)MathRound(x1);
      const int y_main = (int)MathRound(y1);
      _point(x_main, y_main, pixel);
      
      if(smoothing) // test implementation of antialiasing (used in diagonal lines)
      {
         const int factorx = (int)((x1 - x_main) * 255);
         if(fabs(factorx) >= 10)
         {
            _point(x_main + factorx / fabs(factorx), y_main, ((int)fabs(factorx) << 24) | (pixel & 0xFFFFFF));
         }
         const int factory = (int)((y1 - y_main) * 255);
         if(fabs(factory) >= 10)
         {
            _point(x_main, y_main + factory / fabs(factory), ((int)fabs(factory) << 24) | (pixel & 0xFFFFFF));
         }
      }
   }

   virtual void line(const int x1, const int y1, const int x2, const int y2, const color clr) override
   {
      if(x1 == x2) rect(x1, y1, x1, y2, clr);
      else if(y1 == y2) rect(x1, y1, x2, y1, clr);
      else
      {
         smoothing = true;
         const uint pixel = ColorToARGB(clr);
         double angle = 1.0 * (y2 - y1) / (x2 - x1);
         if(fabs(angle) < 1) // step by larger axis, that is x
         {
            const int sign = x2 > x1 ? +1 : -1;
            for(int i = 0; i <= fabs(x2 - x1); ++i)
            {
               const float p = (float)(y1 + sign * i * angle);
               point(x1 + sign * i, p, pixel);
            }
         }
         else // step by y
         {
            const int sign = y2 > y1 ? +1 : -1;
            for(int i = 0; i <= fabs(y2 - y1); ++i)
            {
               const float p = (float)(x1 + sign * i / angle);
               point(p, y1 + sign * i, pixel);
            }
         }
         smoothing = false;
      }
   }
   
   virtual void rect(const int x1, const int y1, const int x2, const int y2, const color clr) override
   {
      // line(x1, y1, x2, y2, clr); // debug
      const uint pixel = ColorToARGB(clr);
      for(int i = fmin(x1, x2); i <= fmax(x1, x2); ++i)
      {
         for(int j = fmin(y1, y2); j <= fmax(y1, y2); ++j)
         {
            point(i, j, pixel);
         }
      }
   }
   
   virtual Shape *findAt(const int x, const int y, const Shape *priority = NULL) override
   {
      Shape *result = NULL;
      for(int i = 0; i < ArraySize(shapes); ++i)
      {
         if(shapes[i][].contains(x, y))
         {
            if(priority == shapes[i][])
            {
               return (Shape *)priority;
            }
            else
            {
               result = shapes[i][];
            }
         }
      }
      return result;
   }
};

//+------------------------------------------------------------------+
//| Static in-class variables (Shape classes self-registration)      |
//+------------------------------------------------------------------+
static Shape::Registrator *Shape::Registrator::regs[] = {};

static MyRegistrator<Rectangle> Rectangle::r;
static MyRegistrator<Square> Square::r;
static MyRegistrator<Ellipse> Ellipse::r;
static MyRegistrator<Circle> Circle::r;
static MyRegistrator<Triangle> Triangle::r;

//+------------------------------------------------------------------+
