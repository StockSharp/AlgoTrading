//+------------------------------------------------------------------+
//|                                           ResourceShapesDraw.mq5 |
//|                              Copyright (c) 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property script_show_inputs

#include <MQL5Book/AutoPtr.mqh>
#include <MQL5Book/Defines.mqh>
#include <MQL5Book/MqlError.mqh>
#include <MQL5Book/TypeName.mqh>

#define FIGURES 21

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

input color BackgroundColor = clrNONE;
input COLOR_EFFECT ColorEffect = PLAIN;
input bool SaveImage = false;

//+------------------------------------------------------------------+
//| Basic interface for drawing primitives                           |
//+------------------------------------------------------------------+
interface Drawing
{
   void point(const float x1, const float y1, const uint pixel);
   void line(const int x1, const int y1, const int x2, const int y2, const color clr);
   void rect(const int x1, const int y1, const int x2, const int y2, const color clr);
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

   void draw(Drawing *drawing) override
   {
      drawing.rect(x - dx / 2, y - dy / 2, x + dx / 2, y + dy / 2, backgroundColor);
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
};

//+------------------------------------------------------------------+
//| Our implementation of Drawing interface based on bitmap resource |
//+------------------------------------------------------------------+
class MyDrawing: public Drawing
{
   const string object; // bitmap object
   const string sheet;  // resource
   uint data[];
   int width, height;
   AutoPtr<Shape> shapes[];
   const uint bg;
   COLOR_EFFECT xormode;
   bool smoothing;
   
public:
   MyDrawing(const uint background = 0, const string s = NULL) :
      object((s == NULL ? "Drawing" : s)),
      sheet("::" + (s == NULL ? "D" + (string)ChartID() : s)),
      bg(background), xormode(PLAIN), smoothing(false)
   {
      width = (int)ChartGetInteger(0, CHART_WIDTH_IN_PIXELS);
      height = (int)ChartGetInteger(0, CHART_HEIGHT_IN_PIXELS);
      ArrayResize(data, width * height);
      ArrayInitialize(data, background);

      ResourceCreate(sheet, data, width, height, 0, 0, width, COLOR_FORMAT_ARGB_NORMALIZE);
      
      ObjectCreate(0, object, OBJ_BITMAP_LABEL, 0, 0, 0);
      ObjectSetInteger(0, object, OBJPROP_XDISTANCE, 0);
      ObjectSetInteger(0, object, OBJPROP_YDISTANCE, 0);
      ObjectSetInteger(0, object, OBJPROP_XSIZE, width);
      ObjectSetInteger(0, object, OBJPROP_YSIZE, height);
      ObjectSetString(0, object, OBJPROP_BMPFILE, sheet);
   }
   
   ~MyDrawing()
   {
      ResourceFree(sheet);
      ObjectDelete(0, object);
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
   
   void draw()
   {
      for(int i = 0; i < ArraySize(shapes); ++i)
      {
         shapes[i][].draw(&this);
      }
      ResourceCreate(sheet, data, width, height, 0, 0, width, COLOR_FORMAT_ARGB_NORMALIZE);
      ChartRedraw();
   }
   
   void shake()
   {
      ArrayInitialize(data, bg);
      for(int i = 0; i < ArraySize(shapes); ++i)
      {
         shapes[i][].move(random(20) - 10, random(20) - 10);
      }
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
   ChartSetInteger(0, CHART_SHOW, false);
   
   MyDrawing raster(BackgroundColor != clrNONE ? ColorToARGB(BackgroundColor) : 0);
   raster.setColorEffect(ColorEffect);
   
   // prepare a random set of shapes for drawing
   for(int i = 0; i < FIGURES; ++i)
   {
      raster.push(addRandomShape());
   }

   raster.draw(); // show initial state

   const int n = Shape::Registrator::getTypeCount();
   for(int i = 0; i < n; ++i)
   {
      Print(Shape::Registrator::get(i).type, " ",
            Shape::Registrator::get(i).getShapeCount());
   }
   
   // keep small random movement of shapes until user stops it
   const ulong start = TerminalInfoInteger(TERMINAL_KEYSTATE_ESCAPE);
   while(!IsStopped()
   && TerminalInfoInteger(TERMINAL_KEYSTATE_ESCAPE) == start)
   {
      Sleep(250);
      raster.shake();
      raster.draw();
   }

   if(SaveImage)
   {
      const string filename = EnumToString(ColorEffect) + ".bmp";
      if(ResourceSave(raster.resource(), filename))
      {
         Print("Bitmap image saved: ", filename);
      }
      else
      {
         Print("Can't save image ", filename, ", ", E2S(_LastError));
      }
   }

   ChartSetInteger(0, CHART_SHOW, true);
}
//+------------------------------------------------------------------+
