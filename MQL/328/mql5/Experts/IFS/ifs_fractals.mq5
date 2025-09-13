//+------------------------------------------------------------------+
//|                                                 IFS_Fractals.mq5 |
//|                        Copyright 2011, MetaQuotes Software Corp. |
//|                                              http://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "Copyright 2011, MetaQuotes Software Corp."
#property link      "http://www.mql5.com"
#property version   "1.00"
#include <cIntBMP.mqh>
//-- IFS coefficients of the "fractal word"
double IFS_a[28]=
  {
   0.00, 0.03,  0.00, 0.09, 0.00, 0.03, -0.00, 0.07, 0.00, 0.07, 0.03,  0.03,  0.03,  0.00,
   0.04, 0.04, -0.00, 0.09, 0.03, 0.03,  0.03, 0.03, 0.03, 0.00, 0.05, -0.00,  0.05,  0.00
  };

double IFS_b[28]=
  {
   -0.11, 0.00, 0.07, 0.00, -0.07, 0.00, -0.11,  0.00, -0.07,  0.00, -0.11,  0.11, 0.00, -0.14,
   -0.12, 0.12,-0.11, 0.00, -0.11, 0.11,  0.00, -0.11,  0.11, -0.11,  0.00, -0.07, 0.00, -0.07
  };

double IFS_c[28]=
  {
   0.12,  0.00,  0.08,  -0.00,  0.08,  0.00,  0.12,  0.00,  0.04,  0.00,  0.12,  -0.12, 0.00,  0.12,
   0.06,  -0.06,  0.10,  0.00,  0.12,  -0.12,  0.00,  0.12,  -0.12,  0.12, 0.00,  0.04,  0.00,  0.12
  };

double IFS_d[28]=
  {
   0.00,  0.05,  0.00,  0.07,  0.00,  0.05,  0.00,  0.07,  0.00,  0.07,  0.00,  0.00,  0.07,  0.00,
   0.00,  0.00,  0.00,  0.07,  0.00,  0.00,  0.07,  0.00,  0.00,  0.00,  0.07,  0.00,  0.07,  0.00
  };

double IFS_e[28]=
  {
   -4.58,  -5.06, -5.16, -4.70, -4.09, -4.35, -3.73, -3.26, -2.76,  -3.26, -2.22, -1.86, -2.04, -0.98,
   -0.46,  -0.76,  0.76,  0.63,  1.78,  2.14,  1.96,  3.11,  3.47,  4.27,  4.60,  4.98,   4.60, 5.24
  };

double IFS_f[28]=
  {
   1.26,  0.89,  1.52,  2.00,  1.52,  0.89,  1.43,  1.96,  1.69,  1.24,  1.43,  1.41,  1.11,  1.43,
   1.79,  1.05,  1.32,  1.96,  1.43,  1.41,  1.11,  1.43,  1.41,  1.43,  1.42,  1.16,  0.71,  1.43
  };

//-- "probabilities" of transforms, multiplied by 1000
double IFS_p[28]=
  {
   35,  35,  35,  35,  35,  35,  35,  35,  35,  35,  35,  35,  35,  35,  
   35,  35,  35,  35,  35,  35,  35,  35,  35,  35,  35,  35,  35,  35
  };

double Probs[28];
cIntBMP bmp;
int scale=50;
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
   double m=0;
   for(int i=0; i<ArraySize(IFS_p); i++)
     {
      Probs[i]=IFS_p[i]+m;
      m=m+IFS_p[i];
     }

   int XSize=600;
   int YSize=600;

   bmp.Create(XSize,YSize,clrSeashell);

   bmp.DrawRectangle(0,0,XSize-1,YSize-1,clrBlack);

   double x0=0;
   double y0=0;
   double x,y;

   int points=1250000;

   for(int i=0; i<points; i++)
     {
      double prb=1000*(rand()/32767.0);
      for(int k=0; k<ArraySize(IFS_p); k++)
        {
         if(prb<=Probs[k])
           {
            x = IFS_a[k] * x0 + IFS_b[k] * y0 + IFS_e[k];
            y = IFS_c[k] * x0 + IFS_d[k] * y0 + IFS_f[k];
            x0 = x;
            y0 = y;
            int scX = int (MathRound(XSize/2 + (x)*scale));
            int scY = int (MathRound(YSize/2 + (y-1)*scale));
            if(scX>=0 && scX<XSize && scY>=0 && scY<YSize) { bmp.DrawDot(scX,scY,clrMidnightBlue); }
            break;
           }
        }
     }
   bmp.Save("bmpimg",true);
   bmp.Show(0,0,"bmpimg","IFS");
//---
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
   ObjectDelete(0,"IFS");
   bmp.Delete("bmpimg",true);
  }
//+------------------------------------------------------------------+ 