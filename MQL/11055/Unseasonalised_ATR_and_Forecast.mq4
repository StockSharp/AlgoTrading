//+------------------------------------------------------------------+
//|            Unseasonalised Average Trading Range and Forecast.mq4 |
//|                                                    Gary Thompson |
//|                                       GaryThompsons.blogspot.com |
//+------------------------------------------------------------------+
#property copyright "Gary Thompson"
#property link      "GaryOThompson@gmail.com"

//---Including Standard Headers
#include <stderror.mqh>
#include <stdlib.mqh>
#include <WinUser32.mqh>


/* A pseudocode to explain this EA:
   
   Define the sample size (amount of candlesticks (on any timeframe) to use for calculations).
   Find and store the trading range of each candlestick by:
      Repeating from the current candlestick counting backwards by the defined sample size:
         A subtraction from the highest price of each candlestick, of its lowest price,
         Transform the difference into pips,
         Store each result as a value on the list of trading ranges to be averaged later.
   Find the average of the aforementioned list of trading ranges by:
      Adding all the stored trading ranges and dividing the sum by the defined sample size.
      Store the result as the Average Trading Range; it is to be displayed later.
   Find the standard deviation of the list of trading ranges by:
      Subtracting each of the stored trading ranges from the average,
      Square each of those differences,
      Find the average of those squared differences by: 
         Summing the squared differences and dividing by the number entered by the user,
      Find the non-negative square root of the average of the squared differences.
      Store the result as the Standard Deviation.
   
   Allow the user to enter their desired range in pips.
   Find the corresponding confidence interval for that entry by:
      Subtracting the entered range from the Average Trading Range,and
      Dividing the difference by the Standard Deviation,
      If the result is >= -1 & <= 1, Confidenc Interval = 68.26%
      Else If the result is >= -2 & <= 2, Confidence Interval =95.44%  
      Else If the result is >= -3 & <= 3, Confidence Interval = 99.74%.
   
   Display the following:
      The Average Trading Range of the selected sample.
      The Standard Deviation of the selected sample.
      The Confidence Interval for the desired range.
 */
      
   
extern double Desired_Pips;

int      Sample_Counter;
int      Array_Pointer;
int      Trend_Analysis_Counter;

double   Array_of_Ranges [40];
double   Sum_Total = 0;
double   Difference = 0;
double   Difference_Total = 0;
double   Difference_Total_in_Pips;

double   ATR_Above_Open;
double   ATR_Below_Open;
double   ATR_in_Pips;
double   ATR = 0;


double   LTF_Above_Open;
double   LTF_Below_Open;
double   LTF_MAPE_Above_Open;
double   LTF_MAPE_Below_Open;


double   Trend_Forecasted_Range;
double   Trend_Forecasted_MAPE;
double   M = 0;
double   C = 0;

double   Sum_of_XY;
double   Sum_of_XSquared;
double   Sum_of_X;      
double   Mean_of_X;
double   Sum_of_APE;

double   Seasonal_Forecasted_Range;
double   Seasonal_Forecasted_MAPE;

double   Confidence_Interval;
double   Standard_Deviation;



//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+

int init()
  {
   if(IsExpertEnabled()== true && ATR == 0)
   {
      RefreshRates();
      ObjectDelete("Average Trading Range Bullish");
      ObjectDelete("Average Trading Range Bearish");
      ObjectDelete("Linear Trend Range Forecast Bullish"); 
      ObjectDelete("Linear Trend Range Forecast Bearish");
      ObjectDelete("Linear Trend Range Forecast with MAPE. Bullish"); 
      ObjectDelete("Linear Trend Range Forecast with MAPE. Bearish");
      

   //Defining the sample size based on current Month.
   Sample_Counter = Month()+24; //Two years plus num of months past this year.
   Array_Pointer = Sample_Counter-1;
   
      ArrayResize(Array_of_Ranges[40],Sample_Counter);
   
      //Populating the array of ranges with the ranges of each monthly candlestick.
      while (Array_Pointer > 0 && ATR == 0)
      {
      Array_of_Ranges[Array_Pointer] = NormalizeDouble((High[Array_Pointer] - Low[Array_Pointer]),Digits); 
      Array_Pointer--;
      }
    
      //Summing and averaging the ranges.
      while (Array_Pointer < Sample_Counter && ATR == 0)
      {
      Sum_Total += Array_of_Ranges[Array_Pointer];
      Array_Pointer++;
      }
      
      ATR = (Sum_Total * 10000) / (Sample_Counter-1);      
      ATR_in_Pips = NormalizeDouble(ATR,2);
     
   }
   
   return(0);
 }



//+------------------------------------------------------------------+
//| expert deinitialization function                                 |
//+------------------------------------------------------------------+
int deinit()
  {                                
  return(0);
  }


//+------------------------------------------------------------------+
//| expert start function                                            |
//+------------------------------------------------------------------+

int start()
  {
    
      RefreshRates();     
    
      // Finding the standard deviation of the list of trading ranges.
      while (Array_Pointer > 0 && Standard_Deviation == 0)
      {
      Difference = ATR - (Array_of_Ranges[Array_Pointer] * 10000);
      Difference_Total = Difference_Total +(Difference * Difference);
      Array_Pointer--;
      }
     
      Difference_Total_in_Pips = (Difference_Total / (Sample_Counter - 1));
     
      Standard_Deviation = MathSqrt(Difference_Total_in_Pips); 
               
      
      //Finding the confidence interval for desired pips entered by user.
      Confidence_Interval =(ATR_in_Pips - Desired_Pips) / Standard_Deviation;
      
      if(Confidence_Interval >= -1 && Confidence_Interval <= 1)
      {
       Confidence_Interval = 68.26;
      } 
      else if(Confidence_Interval >= -2 && Confidence_Interval <= 2)
      {
       Confidence_Interval =95.44;
      }   
      else //if(Confidence_Interval >= -3 && Confidence_Interval <= 3)
      {
       Confidence_Interval = 99.74;
      } 
        
      ATR_Above_Open = NormalizeDouble((Open[0] + (ATR/10000)), Digits);
      ATR_Below_Open = NormalizeDouble((Open[0] - (ATR/10000)), Digits); 
    
      ObjectCreate("Average Trading Range Bullish",1,0,0,ATR_Above_Open,0,ATR_Above_Open); 
      ObjectSet("Average Trading Range Bullish", 6, Blue);
      ObjectCreate("Average Trading Range Bearish",1,0,0,ATR_Below_Open,0,ATR_Below_Open);
      ObjectSet("Average Trading Range Bearish", 6, Blue);
      
      //Linear Trend Analysis and Forecast with MAPE below here:
      Trend_Analysis_Counter = 1;
      
      while (Trend_Analysis_Counter < Sample_Counter && Trend_Forecasted_Range == 0)
      {
      Sum_of_XY += (Sample_Counter - Trend_Analysis_Counter)*(Array_of_Ranges[Trend_Analysis_Counter] * 10000);
      Sum_of_XSquared += (Trend_Analysis_Counter * Trend_Analysis_Counter);
      Sum_of_X += Trend_Analysis_Counter;      
      Trend_Analysis_Counter++;
      }
 
      Mean_of_X = (Sum_of_X /(Sample_Counter - 1));
          
      M = (Sum_of_XY - ((Sample_Counter - 1)*(Mean_of_X * ATR))) 
          /(Sum_of_XSquared - ((Sample_Counter -1)*(Mean_of_X * Mean_of_X)));
      
      C = NormalizeDouble((ATR - (M * Mean_of_X)),2);

      Trend_Forecasted_Range = NormalizeDouble(((M * Sample_Counter) + C),2);

      while (Trend_Analysis_Counter > 1 && Trend_Forecasted_Range > 0)
      {
      Trend_Analysis_Counter--;
      if(((Array_of_Ranges[Trend_Analysis_Counter] * 10000) - (M *(Sample_Counter - Trend_Analysis_Counter) + C)) < 0)
         {
         Sum_of_APE +=((((Array_of_Ranges[Trend_Analysis_Counter] * 10000) - (M *(Sample_Counter - Trend_Analysis_Counter) + C)) * (-1))
                       /(Array_of_Ranges[Trend_Analysis_Counter] * 10000)) * 100;
         }
      else if(((Array_of_Ranges[Trend_Analysis_Counter] * 10000) - (M *(Sample_Counter - Trend_Analysis_Counter) + C)) > 0)
         {
         Sum_of_APE +=(((Array_of_Ranges[Trend_Analysis_Counter] * 10000) - (M *(Sample_Counter - Trend_Analysis_Counter) + C))
                       /(Array_of_Ranges[Trend_Analysis_Counter] * 10000)) * 100;
         }         
      }
    
      Trend_Forecasted_MAPE = NormalizeDouble((Sum_of_APE / (Sample_Counter - 1)),2);      
      
      LTF_Above_Open = NormalizeDouble((Open[0] + (Trend_Forecasted_Range/10000)), Digits);
      LTF_Below_Open = NormalizeDouble((Open[0] - (Trend_Forecasted_Range/10000)), Digits);
      LTF_MAPE_Above_Open = NormalizeDouble((Open[0] + ((Trend_Forecasted_Range*(1 - (Trend_Forecasted_MAPE /100)))/10000)), Digits);
      LTF_MAPE_Below_Open = NormalizeDouble((Open[0] - ((Trend_Forecasted_Range*(1 - (Trend_Forecasted_MAPE /100)))/10000)), Digits);
                  
    
      ObjectCreate("Linear Trend Range Forecast Bullish",1,0,0,LTF_Above_Open,0,LTF_Above_Open); 
      ObjectSet("Linear Trend Range Forecast Bullish", 6, Black);
      ObjectCreate("Linear Trend Range Forecast Bearish",1,0,0,LTF_Below_Open,0,LTF_Below_Open);
      ObjectSet("Linear Trend Range Forecast Bearish", 6, Black);
      
      ObjectCreate("Linear Trend Range Forecast with MAPE. Bullish",1,0,0,LTF_MAPE_Above_Open,0,LTF_MAPE_Above_Open); 
      ObjectSet("Linear Trend Range Forecast with MAPE. Bullish", 6, Gray);
      ObjectCreate("Linear Trend Range Forecast with MAPE. Bearish",1,0,0,LTF_MAPE_Below_Open,0,LTF_MAPE_Below_Open);
      ObjectSet("Linear Trend Range Forecast with MAPE. Bearish", 6, Gray);


      Comment("Average Trading Range: ", ATR_in_Pips,"\n", "Standard Deviation:", Standard_Deviation,
              "\n","Confidence Interval for desired pips of ", Desired_Pips, " is: ", Confidence_Interval,
              "\n","The Linear Trend Forecast Range is: ", Trend_Forecasted_Range, "\n",
              "The MAPE of Linear Forecast is: ",Trend_Forecasted_MAPE,"%");
     
     //Consider adding a print that repeats this information for future reference.
    
  return(0);
  }
//+------------------------------------------------------------------+


//End of EA.
          
          
//+------------------------------------------------------------------+

