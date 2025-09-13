//+------------------------------------------------------------------+
//|                                                TestMnistOnnx.mq5 |
//|                                  Copyright 2023, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "Copyright 2023, MetaQuotes Ltd."
#property link      "https://www.mql5.com"
#property version   "1.00"

#resource "mnist.onnx" as uchar ExtMnistOnnx[]
long ExtModel=INVALID_HANDLE;

#include <Canvas\Canvas.mqh>

#define SHIFT_X   100
#define SHIFT_Y   100
#define SIZE_X    420
#define SIZE_Y    300
#define SIZE_CAPT 20
#define SIZE_280  (SIZE_Y-SIZE_CAPT)
#define GRID_STEP (SIZE_280/28)

CCanvas ExtCanvas;
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//--- load ONNX model
   ExtModel=OnnxCreateFromBuffer(ExtMnistOnnx,ONNX_DEFAULT);
   if(ExtModel==INVALID_HANDLE)
     {
      Print("OnInit failed, OnnxCreateFromBuffer error ",GetLastError());
      return(INIT_FAILED);
     }

//--- allow mouse events for current chart
   ChartSetInteger(0,CHART_EVENT_MOUSE_MOVE,true);
//--- create canvas
   ExtCanvas.CreateBitmapLabel(0,0,"CanvasMNIST",SHIFT_X,SHIFT_Y,SIZE_X+2,SIZE_Y+2,COLOR_FORMAT_XRGB_NOALPHA);
   ExtCanvas.Erase(clrWhite);
   ExtCanvas.Rectangle(0,0,SIZE_X+1,SIZE_Y+1,clrBlack);
//--- caption
   ExtCanvas.LineHorizontal(0,SIZE_X+1,SIZE_CAPT,clrBlack);
//--- close button
   ExtCanvas.LineVertical(SIZE_X-SIZE_CAPT,0,SIZE_CAPT,clrBlack);
   ExtCanvas.FillRectangle(SIZE_X-SIZE_CAPT+1,1,SIZE_X-1,SIZE_CAPT-1,clrAliceBlue);
   ExtCanvas.Line(SIZE_X-SIZE_CAPT+6,5,SIZE_X-4,SIZE_CAPT-5,clrGray);
   ExtCanvas.Line(SIZE_X-4,5,SIZE_X-SIZE_CAPT+6,SIZE_CAPT-5,clrGray);
   ExtCanvas.Line(SIZE_X-SIZE_CAPT+5,5,SIZE_X-5,SIZE_CAPT-5,clrBlack);
   ExtCanvas.Line(SIZE_X-5,5,SIZE_X-SIZE_CAPT+5,SIZE_CAPT-5,clrBlack);
//--- "erase input" button
   ExtCanvas.FillRectangle(SIZE_280+10,40,SIZE_X-10,70,clrAliceBlue);
   ExtCanvas.Rectangle(SIZE_280+10,40,SIZE_X-10,70,clrBlack);
   ExtCanvas.FontSet("Arial",18);
   ExtCanvas.TextOut(SIZE_280+(SIZE_X-SIZE_280)/2,55,"ERASE INPUT",clrBlack,TA_CENTER|TA_VCENTER);
//--- "classify" button
   ExtCanvas.FillRectangle(SIZE_280+10,SIZE_Y-50,SIZE_X-10,SIZE_Y-20,clrAliceBlue);
   ExtCanvas.Rectangle(SIZE_280+10,SIZE_Y-50,SIZE_X-10,SIZE_Y-20,clrBlack);
   ExtCanvas.FontSet("Arial",18);
   ExtCanvas.TextOut(SIZE_280+(SIZE_X-SIZE_280)/2,SIZE_Y-35,"CLASSIFY",clrBlack,TA_CENTER|TA_VCENTER);
//--- image canvas 28 x 28
   EraseGrid();
   ExtCanvas.Update();
//---
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
   if(ExtModel!=INVALID_HANDLE)
      OnnxRelease(ExtModel);

   ExtCanvas.Destroy();
  }
//+------------------------------------------------------------------+
//| ChartEvent function                                              |
//+------------------------------------------------------------------+
void OnChartEvent(const int id,
                  const long &lparam,
                  const double &dparam,
                  const string &sparam)
  {
   static bool classified=false;

   if(id==CHARTEVENT_MOUSE_MOVE)
     {
      int  mouse_x=(int)lparam-SHIFT_X;
      int  mouse_y=(int)dparam-SHIFT_Y;
      bool left_button=(sparam[0]=='1');

      //--- close
      if(left_button && mouse_x>SIZE_X-SIZE_CAPT && mouse_x<SIZE_X && mouse_y>0 && mouse_y<SIZE_CAPT)
        {
         ExpertRemove();
         return;
        }
      //--- erase
      if(left_button && mouse_x>SIZE_280+10 && mouse_x<SIZE_X-10 && mouse_y>40 && mouse_y<70)
        {
         classified=false;
         EraseGrid();
         ExtCanvas.FillRectangle(SIZE_280+10,80,SIZE_X-10,SIZE_Y-60,clrWhite);
         ExtCanvas.Update();
         return;
        }
      //--- classify
      if(left_button && mouse_x>SIZE_280+10 && mouse_x<SIZE_X-10 && mouse_y>SIZE_Y-50 && mouse_y<SIZE_Y-20)
        {
         //--- not classified yet
         if(!classified)
           {
            //--- prevent second click
            classified=true;
            //--- get predicted number
            int predict=PredictNumber();
            string str=IntegerToString(predict);
            //--- print predicted number
            ExtCanvas.FillRectangle(SIZE_280+10,80,SIZE_X-10,SIZE_Y-60,clrWhite);
            ExtCanvas.FontSet("Arial",72);
            ExtCanvas.TextOut(SIZE_280+(SIZE_X-SIZE_280)/2,SIZE_280/2+SIZE_CAPT,str,clrBlack,TA_CENTER|TA_VCENTER);
            ExtCanvas.Update();
           }
         return;
        }
      //--- out of canvas
      if(mouse_x<=0 || mouse_x>=SIZE_280)
        {
         //--- enable chart scrolling
         ChartSetInteger(0,CHART_MOUSE_SCROLL,true);
         return;
        }
      if(mouse_y<=SIZE_CAPT || mouse_y>=SIZE_Y)
        {
         //--- enable chart scrolling
         ChartSetInteger(0,CHART_MOUSE_SCROLL,true);
         return;
        }
      //--- left mouse button released
      if(!left_button)
         return;

      classified=false;
      //--- disable chart scrolling
      ChartSetInteger(0,CHART_MOUSE_SCROLL,false);
      //--- draw on canvas
      for(int ix=-10; ix<=10; ix++)
         for(int iy=-10; iy<=10; iy++)
           {
            if(mouse_x+ix<SIZE_280 && mouse_y+iy>SIZE_CAPT)
               ExtCanvas.PixelSet(mouse_x+ix,mouse_y+iy,clrBlack);
           }
      ExtCanvas.Update();
     }
  }
//+------------------------------------------------------------------+
//| Erase drawings and redraw 28x28 grid on canvas                   |
//+------------------------------------------------------------------+
void EraseGrid(void)
  {
   ExtCanvas.FillRectangle(0,SIZE_CAPT,SIZE_280+1,SIZE_Y+1,clrWhite);
   for(int i=1; i<28; i++)
     {
      ExtCanvas.LineVertical(i*GRID_STEP,SIZE_CAPT,SIZE_Y,clrLightGray);
      ExtCanvas.LineHorizontal(0,SIZE_280,i*GRID_STEP+SIZE_CAPT,clrLightGray);
     }
   ExtCanvas.Rectangle(0,SIZE_CAPT,SIZE_280+1,SIZE_Y+1,clrBlack);
  }
//+------------------------------------------------------------------+
//| Predict drawn number                                             |
//+------------------------------------------------------------------+
int PredictNumber(void)
  {
   static matrixf image(28,28);
   static vectorf result(10);

   PrepareMatrix(image);

   if(!OnnxRun(ExtModel,ONNX_DEFAULT,image,result))
     {
      Print("OnnxRun error ",GetLastError());
      return(-1);
     }

   result.Activation(result,AF_SOFTMAX);
   int predict=int(result.ArgMax());
   if(result[predict]<0.8)
      Print(result);
   Print("value ",predict," predicted with probability ",result[predict]);

   return(predict);
  }
//+------------------------------------------------------------------+
//| Get drawn image and prepare input matrix                         |
//+------------------------------------------------------------------+
void PrepareMatrix(matrixf& image)
  {
   static uchar canvas[SIZE_280][SIZE_280];

//--- get pixels from canvas
   for(int i=0; i<SIZE_280; i++)
     {
      for(int j=0; j<SIZE_280; j++)
        {
         int   x=j+1;
         int   y=i+SIZE_CAPT+1;
         color clr=(color)ExtCanvas.PixelGet(x,y);
         if(clr==clrBlack)
            canvas[i][j]=255;
         else
            canvas[i][j]=0;
        }
     }

   //string out_line="";
//--- average pooling in each grid cell
   for(int i=0; i<28; i++)
     {
      for(int j=0; j<28; j++)
        {
         int sum=0;
         for(int ix=0; ix<GRID_STEP; ix++)
            for(int jy=0; jy<GRID_STEP; jy++)
               sum+=canvas[i*GRID_STEP+ix][j*GRID_STEP+jy];
         image[i][j]=(float)(sum/(GRID_STEP*GRID_STEP));
         //out_line+=IntegerToString(int(image[i][j]),3)+" ";
        }
      //Print(out_line);
      //out_line="";
     }

//--- return normalized to 0...1 result matrix
   image/=255;
  }
//+------------------------------------------------------------------+
