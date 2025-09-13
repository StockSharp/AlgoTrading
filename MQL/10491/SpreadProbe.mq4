//+------------------------------------------------------------------+
//|                                                  SpreadProbe.mq4 |
//|                                                          Lin Xie |
//|                                   http://www.mql4.com/users/linx |
//+------------------------------------------------------------------+
#property copyright "Lin Xie"
#property link      "http://www.mql4.com/users/linx"

extern int Interval = 15;  // spreads are averged over every interval in minutes

static datetime BeginDateTime = 0;
static datetime BNI; // Beginning of next interval (the end of current interval)
static int FileHandle; // file handle?????????? after init, handle is reset to -1;
static double SumSpread = 0.0; // summation of spreads
static int NumTicks = 0;     // num of ticks. sumSpread/numTicks gives the averaged spread

//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
int init()
  {
   if (BeginDateTime>0) return(0); 
      
   // Interval cannot be greater than 60 and must be multiples of 5
   if (Interval>60) {
      Interval = 60;
      Alert("Interval must be a multiple of 5 between [5, 60], it is now reset to " + Interval);
   }
   else if (Interval<5) {
      Interval = 5;
      Alert("Interval must be a multiple of 5 between [5, 60], it is now reset to " + Interval);
   }
   else if (Interval%5!=0) {
      Interval = (Interval/5)*5;
      Alert("Interval must be a multiple of 5 between [5, 60], it is now reset to " + Interval);
   }
  
   Print("Interval = " + Interval);
   
   // Determine the beginning of next interval
   datetime dt = TimeLocal();
   BeginDateTime = dt;
   BNI = dt + (Interval-TimeMinute(dt)%Interval)*60 - TimeSeconds(dt);
        
//---- indicators
   FileHandle = FileOpen(Symbol()+"_"+"BidAskSpread("+Interval+")_"+TimeToStr(dt, TIME_DATE)+".txt", FILE_WRITE, ',');
   if (FileHandle<0)
      Alert("Failed to open data file. Please check if you have write priviledge!");

//----
   return(0);
  }
//+------------------------------------------------------------------+
//| Custom indicator deinitialization function                       |
//+------------------------------------------------------------------+
int deinit()
  {
//----
   if (UninitializeReason()==REASON_CHARTCHANGE) 
      return(0);

   if (FileHandle>0) {
      datetime dt1 = BNI-Interval*60;
      if (dt1<BeginDateTime) dt1 = BeginDateTime;
      
      datetime dt2 = TimeLocal();
      if (NumTicks>0)
         FileWrite(FileHandle, TimeToStr(dt1, TIME_SECONDS), TimeToStr(dt2, TIME_SECONDS), SumSpread/NumTicks);
      else
         FileWrite(FileHandle, TimeToStr(dt1, TIME_SECONDS), TimeToStr(dt2, TIME_SECONDS), 0.0);
                      
      FileClose(FileHandle);
   }
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| Custom indicator iteration function                              |
//+------------------------------------------------------------------+
int start()
  {
   //int    counted_bars=IndicatorCounted();
//----
   if (FileHandle>0) {
      datetime dt = TimeLocal();
      
      if (dt>BNI) { // breaking the current interval
         datetime dt1 = BNI-Interval*60;
         if (dt1<BeginDateTime) dt1 = BeginDateTime;
         
         if (NumTicks>0)
            FileWrite(FileHandle, TimeToStr(dt1, TIME_SECONDS), TimeToStr(BNI, TIME_SECONDS), SumSpread/NumTicks);
         else
            FileWrite(FileHandle, TimeToStr(dt1, TIME_SECONDS), TimeToStr(BNI, TIME_SECONDS), 0.0);
                      
         FileFlush(FileHandle);
     
         SumSpread = (Ask - Bid);
         NumTicks = 1;
         
         BNI = BNI + Interval*60;
      }
      else { // within the current interval
         SumSpread += (Ask - Bid);
         NumTicks++;
      }
   }
//----
   return(0);
  }
//+------------------------------------------------------------------+