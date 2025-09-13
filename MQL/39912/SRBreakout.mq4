//+------------------------------------------------------------------+
//|                                                      SRBreakout.mq4 |
//|                                                  ALI Hassanzadeh |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "ALI Hassanzadeh"
#property link      "a.hasanzadeh9696@gmail.com"
#property version   "1.00"
#property strict
//#include  <SupportResistanceDetection.mqh>
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+

double lot = 0.1;
extern int RiskPercentage = 1;
extern int Slippage = 1000;

input string Commentary = "MY Favorite EMACRoss";
input int Magic = 25;

int totalBars;
int totalBars1Min;

bool HaveLong = false;
bool HaveShort = false;

bool BuyCross;
bool SellCross;

bool BUYH1CROSS = false;
bool SELLH1CROSS = false;
bool BUYH4CROSS = false;
bool SELLH4CROSS = false;

double HighLow26H4[2];
double HighLow26H1[2];

int OnInit()
  {
   
    totalBars = iBars(_Symbol, PERIOD_CURRENT);
    totalBars1Min = iBars(_Symbol, PERIOD_M1);
//---
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
   
   
   
  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
//---
   int bars = iBars(_Symbol, PERIOD_CURRENT);
   int bars1MIN = iBars(_Symbol, PERIOD_M1);
   if(totalBars != bars){
      totalBars = bars;
      
      SignalGenerator();
   }else{};
   
   
    if(totalBars1Min != bars1MIN){
      totalBars1Min = bars1MIN;
      SRCalculator(HighLow26H4,240,26);
      SRCalculator(HighLow26H1,60,26);
      
      
    }
   
}

void SignalGenerator(){

   

  printf("Checking Resistance/Support Cross");
  
  double PriceH4 = iClose(_Symbol, 240, 1);
  double PriceH4_p = iClose(_Symbol,240,2);
  double PriceH1 = iClose(_Symbol, 60, 1);
  double PriceH1_P = iClose(_Symbol,60,2);
  
  
  if(PriceH1 > HighLow26H1[1] && PriceH1_P <= HighLow26H1[1]){
   string text = _Symbol + " Cross Above Resistance On H1";
   SendNotification(text);
  } else if(PriceH1 < HighLow26H1[0] && PriceH1_P >= HighLow26H1[0]){
   string text = _Symbol + " Cross Below Support On H1";
   SendNotification(text);
  }

  
  
  if(PriceH4 > HighLow26H4[1] && PriceH4_p <= HighLow26H4[1]){
   string text = _Symbol + " Cross Above Resistance On H4";
   SendNotification(text);
  }else if(PriceH4 < HighLow26H4[0] && PriceH4_p >= HighLow26H4[0]){
   string text = _Symbol + " Cross Below Support On H4";
   SendNotification(text);
  }
  
   
   
}



void SRCalculator(double &arr[], double timeframe, double shift){
   double maxprice = iHigh(NULL,timeframe,1); // Initial maximum price is set
   double lowprice = iLow(NULL,timeframe,1);  // Inital minumum price is set
   for(int i =2; i<=shift; i++){ // For loop to calculate the hightest and lowest price in 26 period
   if(iHigh(NULL,timeframe,i) > maxprice) maxprice = iHigh(NULL,timeframe,i);
   if(iLow(NULL,timeframe,i) < lowprice) lowprice = iLow(NULL,timeframe,i);
  }
  arr[0] = lowprice;
  arr[1] = maxprice;
}

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
