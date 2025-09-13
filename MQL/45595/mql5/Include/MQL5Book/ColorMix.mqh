//+------------------------------------------------------------------+
//|                                                     ColorMix.mqh |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

//+------------------------------------------------------------------+
//| Color converters between RGB and HSV, color mixer and blender    |
//+------------------------------------------------------------------+
namespace ColorMix
{
// handy local macros
// RGB bytes order 012 in uint, which is actually BGR
#define RGB(R,G,B) (color)((uint)(R) | ((uint)(G) << 8) | ((uint)(B) << 16))
#define CLR_R(C) ((C) & 0xFF)
#define CLR_G(C) (((C) >> 8) & 0xFF)
#define CLR_B(C) (((C) >> 16) & 0xFF)

//+------------------------------------------------------------------+
//| Convert Hue/Saturation/Value into color (RGB components)         |
//| H = 0..360 (angle), S = 0..1, V = 0..1                           |
//+------------------------------------------------------------------+
color HSVtoRGB(double H, double S = 1.0, double V = 1.0)
{
   int r = 0, g = 0, b = 0;

   if(S < 0 || S > 1) S = 1;
   if(V < 0 || V > 1) V = 1;

   if(S == 0.0)
   {
      r = (int)(V * 255);
      g = (int)(V * 255);
      b = (int)(V * 255);
   }
   else
   {
      if(H >= 360.0 || H < 0) H = 0.0;
      else H /= 60;
      int i = (int)H;
      double f = H - i;
      double m, n, k;
      m = V * (1 - S);
      n = V * (1 - S * f);
      k = V * (1 - S * (1 - f));

      switch(i)
      {
      case 0:
         r = (int)(V * 255);
         g = (int)(k * 255);
         b = (int)(m * 255);
         break;
      case 1:
         r = (int)(n * 255);
         g = (int)(V * 255);
         b = (int)(m * 255);
         break;
      case 2:
         r = (int)(m * 255);
         g = (int)(V * 255);
         b = (int)(k * 255);
         break;
      case 3:
         r = (int)(m * 255);
         g = (int)(n * 255);
         b = (int)(V * 255);
         break;
      case 4:
         r = (int)(k * 255);
         g = (int)(m * 255);
         b = (int)(V * 255);
         break;
      case 5:
         r = (int)(V * 255);
         g = (int)(m * 255);
         b = (int)(n * 255);
         break;
      }
   }
   return (color)RGB(r, g, b);
}

//+------------------------------------------------------------------+
//| Find max of 3 RGB components                                     |
//+------------------------------------------------------------------+
int RGBmax(int R, int G, int B)
{
   if(R > G)
   {
      if(R > B) return R;
      else return B;
   }
   else
   {
      if(G > B) return G;
      else return B;
   }
}

//+------------------------------------------------------------------+
//| Find min of 3 RGB components                                     |
//+------------------------------------------------------------------+
int RGBmin(int R, int G, int B)
{
   if(R < G)
   {
      if(R < B) return R;
      else return B;
   }
   else
   {
      if(G < B) return G;
      else return B;
   }
}

//+------------------------------------------------------------------+
//| Convert RGB color in Hue/Saturation/Value                        |
//| H = 0..360 (angle), S = 0..1, V = 0..1                           |
//+------------------------------------------------------------------+
void RGBtoHSV(color rgb, double &H, double &S, double &V)
{
   int r = CLR_R(rgb);
   int g = CLR_G(rgb);
   int b = CLR_B(rgb);
  
   H = 0;

   // determine the value (brightness)
   V = RGBmax(r, g, b);

   // determine saturation
   double temp = RGBmin(r, g, b);

   if(V == 0)
   {
      S = 0;
   }
   else
   {
      S = (V - temp) / V;
   }

   // determine the hue
   if(S == 0)
   {
      H = 0;
   }
   else
   {
      double Cr, Cg, Cb;
      Cr = (V - r) / (V - temp);
      Cg = (V - g) / (V - temp);
      Cb = (V - b) / (V - temp);
      // the color is between yellow and magenta
      if(r == V)
         H = Cb - Cg;
      // the color is between cyan and yellow
      if(g == V)
         H = 2 + Cr - Cb;
      // the color is between magenta and cyan
      if(b == V)
         H = 4 + Cg - Cr;
      // convert to degrees
      H = 60 * H;
      // prevent negative value
      if(H < 0)
         H += 360;
   }
   V /= 255;
}

//+------------------------------------------------------------------+
//| Construct a color (one of several grades) between 2 given colors |
//| using hue circle or linear manner                                |
//+------------------------------------------------------------------+
color RotateColors(const color clr1, const color clr2, const int gradeCount, const int i, const bool colourScheme = true)
{
   color clr;

   if(colourScheme)
   {
      double hh, s, v;
      double h1, s1, v1;
      double h2, s2, v2;

      RGBtoHSV(clr1, h1, s1, v1);
      RGBtoHSV(clr2, h2, s2, v2);

      hh = h1 + ((h2 - h1) / (gradeCount)) * i;
      s = s1 + ((s2 - s1) / (gradeCount)) * i;
      v = v1 + ((v2 - v1) / (gradeCount)) * i;
      if(hh < 0) hh += 360;
      if(hh > 360) hh -= 360;
      clr = HSVtoRGB(hh, s, v);
   }
   else
   {
      int r, g, b;
      int r1 = CLR_R(clr1), g1 = CLR_G(clr1), b1 = CLR_B(clr1);
      int r2 = CLR_R(clr2), g2 = CLR_G(clr2), b2 = CLR_B(clr2);

      r = (int)(r1 + (((r2 - r1) / (1.0 * gradeCount)) * i));
      g = (int)(g1 + (((g2 - g1) / (1.0 * gradeCount)) * i));
      b = (int)(b1 + (((b2 - b1) / (1.0 * gradeCount)) * i));
      clr = RGB(r, g, b);
   }
    
   return clr;
}

//+------------------------------------------------------------------+
//| Mix 2 colors linear manner                                       |
//+------------------------------------------------------------------+
color BlendColors(const int col1, const int col2, const double a = 0.5)
{
   int r1 = CLR_R(col1), g1 = CLR_G(col1), b1 = CLR_B(col1),
       r2 = CLR_R(col2), g2 = CLR_G(col2), b2 = CLR_B(col2);
   return RGB(r1 + a * (r2 - r1), g1 + a * (g2 - g1), b1 + a * (b2 - b1));
}

#undef RGB
#undef CLR_R
#undef CLR_G
#undef CLR_B

} // namespace ColorMix
//+------------------------------------------------------------------+
