//+------------------------------------------------------------------+
//|                                        IFS_Sierpinski_Carpet.mq5 |
//|                        Copyright 2011, MetaQuotes Software Corp. |
//|                                              http://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "Copyright 2011, MetaQuotes Software Corp."
#property link      "http://www.mql5.com"
#property version   "1.00"
#include <cIntBMP.mqh>
//-- IFS coefficients of Sierpinski Carpet
double IFS_a[8] = {0.333, 0.333, 0.333, 0.333, 0.333, 0.333, 0.333, 0.333};
double IFS_b[8] = {0.00,  0.00,  0.00,   0.00, 0.00,  0.00,  0.00,   0.00};
double IFS_c[8] = {0.00,  0.00,  0.00,   0.00, 0.00,  0.00,  0.00,   0.00};
double IFS_d[8] = {0.333, 0.333,  0.333,  0.333, 0.333,  0.333,  0.333, 0.333};
double IFS_e[8] = {-0.125, -3.375, -3.375,  3.125, 3.125, -3.375, -0.125, 3.125};
double IFS_f[8] = {6.75, 0.25, 6.75,  0.25, 6.75, 3.5, 0.25, 3.50};
//-- "probabilities" of transforms, multiplied by 1000
double IFS_p[8]={125,125,125,125,125,125,125,125};

double Probs[8];
cIntBMP bmp;
int scale=30;
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

   int points=2500000;

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
            int scY = int (MathRound(YSize/2 + (y-5)*scale));
            if(scX>=0 && scX<XSize && scY>=0 && scY<YSize) { bmp.DrawDot(scX,scY,clrMaroon); }
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