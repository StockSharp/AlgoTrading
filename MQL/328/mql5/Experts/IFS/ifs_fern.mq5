//+------------------------------------------------------------------+
//|                                                     IFS_fern.mq5 |
//|                        Copyright 2011, MetaQuotes Software Corp. |
//|                                              http://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "Copyright 2011, MetaQuotes Software Corp."
#property link      "http://www.mql5.com"
#property version   "1.00"
#include <cIntBMP.mqh>
//-- Barnsley Fern IFS coefficients
//-- (a,b,c,d) matricies
double IFS_a[4] = {0.00,  0.85,  0.20,  -0.15};
double IFS_b[4] = {0.00,  0.04, -0.26,   0.28};
double IFS_c[4] = {0.00, -0.04,  0.23,   0.26};
double IFS_d[4] = {0.16,  0.85,  0.22,   0.24};
//-- (e,f) vectors
double IFS_e[4] = {0.00,  0.00,  0.00,   0.00};
double IFS_f[4] = {0.00,  1.60,  1.60,   0.00};
//-- "probabilities" of transforms, multiplied by 1000
double IFS_p[4] = {10,     850,    70,     70};

double Probs[4];
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

   int points=250000;

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
            if(scX>=0 && scX<XSize && scY>=0 && scY<YSize) { bmp.DrawDot(scX,scY,clrForestGreen); }
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