//This shows the relative length of the down trend.

//+------------------------------------------------------------------+
//|                                             RelDownTrLen_....mq4 |
//|                                                   Author: Martes |
//|               http://championship.mql4.com/2007/ru/users/Martes/ |
//+------------------------------------------------------------------+
#property copyright "Martes"
#property link      "http://championship.mql4.com/2007/ru/users/Martes/"

//Fix minimum (0) and maximum (1) levels manually. Levels below do not work.
#property indicator_level1 0
#property indicator_level2 1
#property indicator_levelcolor LimeGreen
#property indicator_levelwidth 2
#property indicator_levelstyle 0

#property indicator_separate_window

#property indicator_buffers 1
#property indicator_color1 MidnightBlue
#property indicator_width1 2

extern int barsToProcess=100;
extern int StartingBarNumber=0;
extern bool DrawForCandleBodies=true;

//---- buffers
double ExtMapBuffer1[];
//----

datetime ProcessedTime=0;

double CandleBodyMinimum(int CandleNumber)
{
   return(MathMin(Open[CandleNumber], Close[CandleNumber]));
}

double CandleBodyMaximum(int CandleNumber)
{
   return(MathMax(Open[CandleNumber], Close[CandleNumber]));
}


// isLeft(): tests if a point is Left|On|Right of an infinite line.
//    Input:  three points P0=(x0,y0), P1=(x1,y1), and P2=(x2,y2).
//    Return: >0 for P2 left of the line through P0 and P1
//            =0 for P2 on the line
//            <0 for P2 right of the line
double isLeft( double x0, double y0, double x1, double y1, double x2, double y2) {
    return( (x1-x0)*(y2-y0) - (x2-x0)*(y1-y0) );
}

//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
int init()
  {
//---- indicators
   SetIndexStyle(0,DRAW_LINE);
   SetIndexBuffer(0,ExtMapBuffer1);
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| Custom indicator deinitialization function                       |
//+------------------------------------------------------------------+
int deinit()
  {
//----
 
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| Custom indicator iteration function                              |
//+------------------------------------------------------------------+
int start()
  {
   int    counted_bars=IndicatorCounted();
//----
   int limit;
   //Variables for finding best down-trend
   int Length, StartIdx, EndIdx,
       BestLength=0, BestStartIdx, BestEndIdx;
   datetime BestStartTime, BestEndTime;
   double BestStartPrice, BestEndPrice;   
   
   //Do nothing if the current candle is already processed.
   if(ProcessedTime==Time[StartingBarNumber])
   {
     return(0);
   }
   else //Process.
   {
    ProcessedTime=Time[StartingBarNumber];
    if(barsToProcess<=2)
    {
     if(barsToProcess<=1)
     {
      Print("Too few bars to calculate a down-trend");
      return(0);
     }
     else
     {
      if(DrawForCandleBodies)
      {
       BestStartPrice=CandleBodyMaximum(StartingBarNumber+2);
       BestEndPrice=CandleBodyMaximum(StartingBarNumber+1);
      }
      else
      {
       BestStartPrice=High[StartingBarNumber+2];
       BestEndPrice=High[StartingBarNumber+1];
      }
      if(BestStartPrice<=BestEndPrice)
      {
         ExtMapBuffer1[0]=0;
         Print("No down trend");
         return(0);
      }
      BestStartTime=Time[StartingBarNumber+2];
      BestEndTime=Time[StartingBarNumber+1];
     }
    }
    else //Find a Down-Trend with a convex hull.
    {
      ProcessedTime=Time[StartingBarNumber]; 
      int NumbersOfConvexHullVertices[], //To store indices (numbers) of Vertices in Convex Hull
          ConvexHullIdx, //This points to the last valid element of NumbersOfConvexHullVertices
          i;
      ArrayResize(NumbersOfConvexHullVertices, barsToProcess);
     
      double CandleBodyMaxima[],
             x0, y0,
             x1, y1,
             x2, y2;
      ArrayResize(CandleBodyMaxima, barsToProcess);
      //Fill array CandleBodyMaxima[] with values from 
      //the range StartingBarNumber+1 ... StartingBarNumber+barsToProcess of the chart
      for(i=1; i<=barsToProcess; i++)
      {
         if(DrawForCandleBodies)
         {
          CandleBodyMaxima[i-1]=CandleBodyMaximum(StartingBarNumber+i);
         }
         else
         {
          CandleBodyMaxima[i-1]=High[StartingBarNumber+i];
         }
      }
      //Find the lower part of the convex hull for points (i, CandleBodyMaxima[i]) of
      //the plane
      NumbersOfConvexHullVertices[0]=0;//This is the index of CandleBodyMaxima[0]
      ConvexHullIdx=0;//This says that NumbersOfConvexHullVertices[ConvexHullIdx] is valid.
      NumbersOfConvexHullVertices[1]=1;//This is the index of CandleBodyMaxima[1]
      ConvexHullIdx=1;//NumbersOfConvexHullVertices[ConvexHullIdx] is still thought to be valid.
      for(i=2; i<barsToProcess; i++)
      {
       ConvexHullIdx++;
       NumbersOfConvexHullVertices[ConvexHullIdx]=i;//The new point is added in the hull.
       //Now we will make the hull convex
          x0=NumbersOfConvexHullVertices[ConvexHullIdx-2];
          y0=CandleBodyMaxima[NumbersOfConvexHullVertices[ConvexHullIdx-2]];
          x1=NumbersOfConvexHullVertices[ConvexHullIdx-1];
          y1=CandleBodyMaxima[NumbersOfConvexHullVertices[ConvexHullIdx-1]];
          x2=NumbersOfConvexHullVertices[ConvexHullIdx];
          y2=CandleBodyMaxima[NumbersOfConvexHullVertices[ConvexHullIdx]];
       while( (isLeft(x0, y0, x1, y1, x2, y2)>=0) && (ConvexHullIdx>=2))
       {
          NumbersOfConvexHullVertices[ConvexHullIdx-1]=NumbersOfConvexHullVertices[ConvexHullIdx];
          ConvexHullIdx--;
          if(ConvexHullIdx>=2)//Enough points in the hull to process.
          {
           x0=NumbersOfConvexHullVertices[ConvexHullIdx-2];
           y0=CandleBodyMaxima[NumbersOfConvexHullVertices[ConvexHullIdx-2]];
           x1=NumbersOfConvexHullVertices[ConvexHullIdx-1];
           y1=CandleBodyMaxima[NumbersOfConvexHullVertices[ConvexHullIdx-1]];
           x2=NumbersOfConvexHullVertices[ConvexHullIdx];
           y2=CandleBodyMaxima[NumbersOfConvexHullVertices[ConvexHullIdx]];
          }//if
       }//while
      }//for
      //Find the longest down-trend
      for(i=0; i<ConvexHullIdx; i++)
      {
       StartIdx=NumbersOfConvexHullVertices[i];
       EndIdx=NumbersOfConvexHullVertices[i+1];
       Length=EndIdx-StartIdx;
       if(Length>BestLength &&
          CandleBodyMaxima[StartIdx]<CandleBodyMaxima[EndIdx])
       {
          BestStartIdx=StartIdx;
          BestEndIdx=EndIdx;
          BestLength=Length;
       }//if
      }//for
      if(BestLength==0)
      {
         ExtMapBuffer1[0]=0;
         Print("No down trend");
         return(0);
      } 
      BestStartTime=Time[BestEndIdx+StartingBarNumber+1];
      BestEndTime=Time[BestStartIdx+StartingBarNumber+1];
      BestStartPrice=CandleBodyMaxima[BestEndIdx];
      BestEndPrice=CandleBodyMaxima[BestStartIdx];
    }//else //Find an Up-Trend with a convex hull.
    
    ExtMapBuffer1[0]=(1.0*BestLength)/barsToProcess;
    // Вариант: ExtMapBuffer1[0]=BestEndIdx/barsToProcess;
    
   }//else Process
   
   
//----
   return(0);
  }
//+------------------------------------------------------------------+