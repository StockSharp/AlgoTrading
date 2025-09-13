//+------------------------------------------------------------------+
//|                                                LiveAlligator.mq4 |
//|          Copyright © 2011,  http://www.mql4.com/ru/users/rustein |
//-------------------------------------------------------------------+

// Trend Detection function
#define BULL 1
#define BEAR 2
// Input variables
//+------------------------------------------------------------------+
extern int    Slippage        = 5;
//-------------------------------------------------------------------+
extern int    Magic           = 55;
//-------------------------------------------------------------------+
extern int    StopLoss        = 75;
//-------------------------------------------------------------------+
extern bool   MoneyManagement = true;
//-------------------------------------------------------------------+
extern double Lots            = 0.1;
//-------------------------------------------------------------------+
extern double MaximumRisk     = 1.3;
//-------------------------------------------------------------------+
extern int    AlligatorPeriod = 5;
//+------------------------------------------------------------------+
extern int    AlliggatorMODE  = 2; // 0=SMA,1=EMA,2=SSMA,3=LWMA
//-------------------------------------------------------------------+
extern int    LivePeriod      = 46;
//-------------------------------------------------------------------+
extern int    TrailPeriod     = 113;
//-------------------------------------------------------------------+
extern bool   CheckHour       = false;
//-------------------------------------------------------------------+
extern int    StartHour       = 17;
//+------------------------------------------------------------------+
extern int    EndHour         = 1;
//-------------------------------------------------------------------+
extern int    TotalOrders     = 1;
//-------------------------------------------------------------------+
string OrderComments = "LiveAlligator";
//-----
color  BuyColor      = CLR_NONE;
color  SellColor     = CLR_NONE;
// Global variables
int Cnt = 0;              // counter variable, used in for() loops
bool InitVariables;       // init variable when program starts
datetime PreviousBar;     // record the candle/bar time
int LastTrendDirection;   // record the previous trends direction
int FileHandle;           // variable for file handling
//-------------------------------------------------------------------+
 
//+------------------------------------------------------------------+
//|            DO NOT MODIFY ANYTHING BELOW THIS LINE!!!             |
//+------------------------------------------------------------------+
 
//-------------------------------------------------------------------+
int init()
{
  if(IsTesting()==false)
  {
  InitVariables = true;  // Allows us to init variables in the start function because 
                         // we cannot do this in the init function because the variables 
                         // use values from the chart 
  //create data file if it does not exist and load variables if file does exist
  FileHandle=FileOpen("LiveAlligator.txt",FILE_BIN | FILE_READ);
  if(FileHandle<1)
  {
    Print("LiveAlligator.txt not found.");
    FileHandle=FileOpen("LiveAlligator.txt",FILE_BIN | FILE_WRITE);
    if(FileHandle>0)
    {
      LastTrendDirection=0;
      FileWriteInteger(FileHandle,LastTrendDirection,SHORT_VALUE);
      FileClose(FileHandle);
      Print("LiveAlligator.txt has been successfully created.");
    }
      else
      {
        FileClose(FileHandle);
        Print("Failed to create LiveAlligator.txt file.");
      }  
    }
    else
    {
      LastTrendDirection=FileReadInteger(FileHandle,SHORT_VALUE);
      Print("LiveAlligator variables loaded from file.");
      FileClose(FileHandle);
    }
  }      
  return(0);
}
//+------------------------------------------------------------------+
//|-----------------------//  Save Variables  //---------------------|
//+------------------------------------------------------------------+
int SaveVariables()
{
  if(IsTesting()==false)
  {
    //save variables to file
    FileHandle=FileOpen("LiveAlligator.txt",FILE_BIN | FILE_WRITE);
    if(FileHandle<1)
    {
      Print("LiveAlligator.txt not found.");
      FileHandle=FileOpen("LiveAlligator.txt",FILE_BIN | FILE_WRITE);
      if(FileHandle>0)
      {
        FileWriteInteger(FileHandle,LastTrendDirection,SHORT_VALUE);
        FileClose(FileHandle);
        Print("LiveAlligator.txt has been successfully created.");
      }  
      else
      {
      FileClose(FileHandle);
      Print("Failed to create LiveAlligator.txt file.");
      }
    }
    else
    {
      FileWriteInteger(FileHandle,LastTrendDirection,SHORT_VALUE);
      FileClose(FileHandle);
      Print("LiveAlligator Variables successfully saved to file.");
    }  
  }
  return(0);
}
//+------------------------------------------------------------------+ 
int deinit()
{
  return(0);
} 
//+------------------------------------------------------------------+
int start()
{
//-----
  double MinLot=MarketInfo(Symbol(),MODE_MINLOT);
  int LotsDigits;
  if(MinLot<=0.0001)
  LotsDigits = 4;
  if(MinLot<=0.001)
  LotsDigits = 3;
  if(MinLot<=0.01)
  LotsDigits = 2;
  if(MinLot>=0.1)
  LotsDigits = 1;
  double MMLot = NormalizeDouble(AccountBalance()*MaximumRisk/100.00/100.00,LotsDigits);
  if(MoneyManagement == true && MMLot > MinLot)
  Lots = MMLot;
  else Lots = Lots;
//-----
  // init variables when the expert advisor first starts running
  if(InitVariables == true)
  {
    PreviousBar = Time[0];     // record the current canle/bar open time
                                 // place code here that you only wnat to run one time
    InitVariables = false;    // change to false so we only init variable once
  }
  // record trends direction
  if(LastTrendDirection==0)
  {
    if(TrendDetection()==BULL)
    {
      LastTrendDirection=BULL;
    }
    if(TrendDetection()==BEAR)
    {
      LastTrendDirection=BEAR;
    }
  }
//+------------------------------------------------------------------+
//|------------------------// Trail By MA //-------------------------|
//+------------------------------------------------------------------+
  double SL = NormalizeDouble(iMA(NULL,0,TrailPeriod,0,MODE_SMMA,PRICE_CLOSE,1),Digits);
//-----
  int Orders = OrdersTotal();
  for (int i=0; i<Orders; i++)
  {
    if(!(OrderSelect(i, SELECT_BY_POS, MODE_TRADES))) continue;
    if(OrderSymbol() != Symbol()) continue;
    {
      if(OrderType() == OP_BUY && OrderMagicNumber()==Magic && SL > OrderOpenPrice() && NormalizeDouble(Ask,Digits) > SL
      && OrderStopLoss() != SL && SL > 0 && SL!=EMPTY_VALUE && SL > OrderStopLoss())
      {
        OrderModify(OrderTicket(),OrderOpenPrice(),SL,OrderTakeProfit(),0,CLR_NONE);
      }
      if(OrderType() == OP_SELL && OrderMagicNumber()==Magic && SL < OrderOpenPrice() && NormalizeDouble(Bid,Digits) < SL
      && OrderStopLoss() != SL && SL > 0 && SL!=EMPTY_VALUE && SL < OrderStopLoss())
      {
        OrderModify(OrderTicket(),OrderOpenPrice(),SL,OrderTakeProfit(),0,CLR_NONE);
      }
    }
  }
//+------------------------------------------------------------------+
//|------------------------//  Open Orders  //-----------------------|
//+------------------------------------------------------------------+
  // perform analysis and open orders on new candle/bar 
  if(NewBar() == true)
  {
    // only perform analysis and open new order if we have not reached our TotalOpenOrders max
    if(TotalOpenOrders() < TotalOrders)
    {
//----- // Open BUY
      if(TrendDetection() == BULL && LastTrendDirection == BEAR)
      {
        if(StopLoss>0)
        {
          OrderSend(Symbol(),OP_BUY,Lots,NormalizeDouble(Ask,Digits),Slippage,NormalizeDouble(Ask,Digits)-(StopLoss*Point),0,OrderComments,Magic,0,BuyColor);
        }
        if(StopLoss==0)
        {
          OrderSend(Symbol(),OP_BUY,Lots,NormalizeDouble(Ask,Digits),Slippage,0,0,OrderComments,Magic,0,BuyColor);           
        }
        LastTrendDirection=BULL;
        // save variables to file
        SaveVariables();
      }
//----- // Open SELL
      if(TrendDetection() == BEAR && LastTrendDirection == BULL)
      {
        if(StopLoss>0)
        {
          OrderSend(Symbol(),OP_SELL,Lots,NormalizeDouble(Bid,Digits),Slippage,NormalizeDouble(Bid,Digits)+(StopLoss*Point),0,OrderComments,Magic,0,SellColor);
        }
        if(StopLoss==0)
        {
          OrderSend(Symbol(),OP_SELL,Lots,NormalizeDouble(Bid,Digits),Slippage,0,0,OrderComments,Magic,0,SellColor);
        }
        LastTrendDirection=BEAR;
        // save variables to file
        SaveVariables();
      }
    }
  }
  return(0);
}
//+------------------------------------------------------------------+
//|-----------------------//  Orders Count  //-----------------------|
//+------------------------------------------------------------------+
// This function returns the total amount of orders the expert advisor has open  
int TotalOpenOrders()
{
  Cnt=OrdersTotal();
  int TotalOpenOrders = 0;
  if(Cnt==0)
  {
    return(0);
  }
    else
    {
    for(;Cnt>=0;Cnt--)
    {
      RefreshRates();
      OrderSelect(Cnt,SELECT_BY_POS);
      if(OrderMagicNumber()==Magic)
      {
      TotalOpenOrders++;
      }
    }
  }
  return(TotalOpenOrders);
}
//+------------------------------------------------------------------+
//|--------------------------//  New Bar  //-------------------------|
//+------------------------------------------------------------------+
// This function return the value true if the current bar/candle was just formed
bool NewBar()
{
  if(PreviousBar<Time[0])
  {
    PreviousBar = Time[0];
    return(true);
  }
  else
  {
    return(false);
  }
  return(false);    // in case if - else statement is not executed
}
//+------------------------------------------------------------------+
//|---------------------//  Trend Detection  //----------------------|
//+------------------------------------------------------------------+
int TrendDetection()
{
//-------------------------------------------------------------------+
  double A1=MathRound(AlligatorPeriod*1.61803398874989);
  double A2=MathRound(A1*1.61803398874989);
  double A3=MathRound(A2*1.61803398874989);
//-----
  double LipsNow = iAlligator(NULL,0,A3,A2,A2,A1,A1,AlligatorPeriod,AlliggatorMODE,PRICE_MEDIAN,MODE_GATORLIPS,0);
  double LipsPre = iAlligator(NULL,0,A3,A2,A2,A1,A1,AlligatorPeriod,AlliggatorMODE,PRICE_MEDIAN,MODE_GATORLIPS,1);
  double JawsNow = iAlligator(NULL,0,A3,A2,A2,A1,A1,AlligatorPeriod,AlliggatorMODE,PRICE_MEDIAN,MODE_GATORJAW,0);
  double JawsPre = iAlligator(NULL,0,A3,A2,A2,A1,A1,AlligatorPeriod,AlliggatorMODE,PRICE_MEDIAN,MODE_GATORJAW,1);
  double TeethNow = iAlligator(NULL,0,A3,A2,A2,A1,A1,AlligatorPeriod,AlliggatorMODE,PRICE_MEDIAN,MODE_GATORTEETH,0);
//-----
  double MA1 = iMA(NULL,0,LivePeriod,0,MODE_EMA,PRICE_CLOSE,0);
  double MA2 = iMA(NULL,0,LivePeriod,0,MODE_EMA,PRICE_WEIGHTED,0);
  double MA3 = iMA(NULL,0,LivePeriod,0,MODE_EMA,PRICE_TYPICAL,0);
  double MA4 = iMA(NULL,0,LivePeriod,0,MODE_EMA,PRICE_MEDIAN,0);
  double MA5 = iMA(NULL,0,LivePeriod,0,MODE_EMA,PRICE_OPEN,0);
//-------------------------------------------------------------------+
//BULL trend
  if(LipsNow > JawsNow && TeethNow < JawsNow && LipsPre < JawsPre && MA1 > MA2 && MA2 > MA3 && MA3 > MA4 && MA4 > MA5)
  {
    if((CheckHour != true) || (Hour() > StartHour && Hour() < EndHour))
    {
      return(BULL);
    }
  }
//BEAR trend
  if(LipsNow < JawsNow && TeethNow > JawsNow && LipsPre > JawsPre && MA1 < MA2 && MA2 < MA3 && MA3 < MA4 && MA4 < MA5)
  {
    if((CheckHour != true) || (Hour() > StartHour && Hour() < EndHour))
    {
      return(BEAR);
    }
  }
  return(0);
}
//+------------------------------------------------------------------+
//|---------------------------// END //------------------------------|
//+------------------------------------------------------------------+