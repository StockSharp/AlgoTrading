//+------------------------------------------------------------------+
//|                                        IFS_Sierpinski_Gasket.mq5 |
//|                        Copyright 2011, MetaQuotes Software Corp. |
//|                                              http://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "Copyright 2011, MetaQuotes Software Corp."
#property link      "http://www.mql5.com"
#property version   "1.00"

//-- include file with cIntBMP class
#include <cIntBMP.mqh>

//-- Sierpinski Gasket IFS coefficients
//-- (a,b,c,d) matricies
double IFS_a[3] = {0.50,  0.50,  0.50};
double IFS_b[3] = {0.00,  0.00,  0.00};
double IFS_c[3] = {0.00,  0.00,  0.00};
double IFS_d[3] = {0.50,  0.50,  0.50};
//-- (e,f) vectors
double IFS_e[3] = {0.00,  0.00,  0.50};
double IFS_f[3] = {0.00,  0.50,  0.50};
//-- "probabilities" of transforms, multiplied by 1000
double IFS_p[3]={333,333,333};

double Probs[3]; // Probs array - used to choose IFS transforms
cIntBMP bmp;     // cIntBMP class instance
int scale=350;  // scale coefficient
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//-- Prepare Probs array
   double m=0;
   for(int i=0; i<ArraySize(IFS_p); i++)
     {
      Probs[i]=IFS_p[i]+m;
      m=m+IFS_p[i];
     }
//-- size of BMP image
   int XSize=500;
   int YSize=400;
//-- create bmp image XSizexYSize with clrSeashell background color
   bmp.Create(XSize,YSize,clrSeashell);
//-- image rectangle
   bmp.DrawRectangle(0,0,XSize-1,YSize-1,clrBlack);

//-- point coordinates (will be used in construction of set)
   double x0=0;
   double y0=0;
   double x,y;
//-- number of points to calculate (more points - detailed image)
   int points=1500000;
//-- calculate set
   for(int i=0; i<points; i++)
     {
      // choose IFS tranform with probabilities, proportional to defined
      double prb=1000*(rand()/32767.0);
      for(int k=0; k<ArraySize(IFS_p); k++)
        {
         if(prb<=Probs[k])
           {
            // affine transformation
            x = IFS_a[k] * x0 + IFS_b[k] * y0 + IFS_e[k];
            y = IFS_c[k] * x0 + IFS_d[k] * y0 + IFS_f[k];
            // update previous coordinates
            x0 = x;
            y0 = y;
            // convert to BMP image coordinates
            // (note the Y axis in cIntBMP)
            int scX = int (MathRound(XSize/2 + (x-0.5)*scale));
            int scY = int (MathRound(YSize/2 + (y-0.5)*scale));
            // if the point coordinates inside the image, draw the dot
            if(scX>=0 && scX<XSize && scY>=0 && scY<YSize) { bmp.DrawDot(scX,scY,clrDarkBlue); }
            break;
           }
        }
     }
//-- save image to file
   bmp.Save("bmpimg",true);
//-- plot image on the chart
   bmp.Show(0,0,"bmpimg","IFS");
//---
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
//--- delete image from the chart
   ObjectDelete(0,"IFS");
//--- delete file
   bmp.Delete("bmpimg",true);
  }
//+------------------------------------------------------------------+