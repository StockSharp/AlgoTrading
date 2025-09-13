//+------------------------------------------------------------------+
//|                                                      LiveRSI.mq4 |
//|                      Copyright © 2011, MetaQuotes Software Corp. |
//|                             http://www.mql4.com/ru/users/rustein |
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
extern bool   MoneyManagement = true;
//-------------------------------------------------------------------+
extern double Lots            = 0.1;
//-------------------------------------------------------------------+
extern double MaximumRisk     = 1.5;
//-------------------------------------------------------------------+
extern int    StopLoss        = 40;
//-------------------------------------------------------------------+
extern int    RsiPeriod       = 30;
//-------------------------------------------------------------------+
extern double SarStep         = 0.08;
//-------------------------------------------------------------------+
extern bool   CheckHour       = false;
//-------------------------------------------------------------------+
extern int    StartHour       = 17;
//+------------------------------------------------------------------+
extern int    EndHour         = 1;
//-------------------------------------------------------------------+
extern int    TotalOrders     = 1;
//-------------------------------------------------------------------+
string OrderComments = "LiveRSI";
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
  FileHandle=FileOpen("LiveRSI.txt",FILE_BIN | FILE_READ);
  if(FileHandle<1)
  {
    Print("LiveRSI.txt not found.");
    FileHandle=FileOpen("LiveRSI.txt",FILE_BIN | FILE_WRITE);
    if(FileHandle>0)
    {
      LastTrendDirection=0;
      FileWriteInteger(FileHandle,LastTrendDirection,SHORT_VALUE);
      FileClose(FileHandle);
      Print("LiveRSI.txt has been successfully created.");
    }
      else
      {
        FileClose(FileHandle);
        Print("Failed to create LiveRSI.txt file.");
      }  
    }
    else
    {
      LastTrendDirection=FileReadInteger(FileHandle,SHORT_VALUE);
      Print("LiveRSI variables loaded from file.");
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
    FileHandle=FileOpen("LiveRSI.txt",FILE_BIN | FILE_WRITE);
    if(FileHandle<1)
    {
      Print("LiveRSI.txt not found.");
      FileHandle=FileOpen("LiveRSI.txt",FILE_BIN | FILE_WRITE);
      if(FileHandle>0)
      {
        FileWriteInteger(FileHandle,LastTrendDirection,SHORT_VALUE);
        FileClose(FileHandle);
        Print("LiveRSI.txt has been successfully created.");
      }  
      else
      {
      FileClose(FileHandle);
      Print("Failed to create LiveRSI.txt file.");
      }
    }
    else
    {
      FileWriteInteger(FileHandle,LastTrendDirection,SHORT_VALUE);
      FileClose(FileHandle);
      Print("LiveRSI Variables successfully saved to file.");
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
//|-----------------------// Trail By SAR //-------------------------|
//+------------------------------------------------------------------+
  int Orders = OrdersTotal();
  for (int i=0; i<Orders; i++)
  {
    if(!(OrderSelect(i, SELECT_BY_POS, MODE_TRADES))) continue;
    if(OrderSymbol() != Symbol()) continue;
    {
//+-----
      double SL = NormalizeDouble(iSAR(NULL,0,SarStep,0.1,1),Digits);
//+-----
      if(OrderType() == OP_BUY && OrderMagicNumber()==Magic && OrderStopLoss() != SL
      && SL > 0 && SL!=EMPTY_VALUE && (OrderStopLoss() == 0 || SL > OrderStopLoss()))
      {
        OrderModify(OrderTicket(),OrderOpenPrice(),SL,OrderTakeProfit(),0,CLR_NONE);
      }
//-----
      if(OrderType() == OP_SELL && OrderMagicNumber()==Magic&& OrderStopLoss() != SL
      && SL > 0 && SL!=EMPTY_VALUE && (OrderStopLoss() == 0 || SL < OrderStopLoss()))
      {
        OrderModify(OrderTicket(),OrderOpenPrice(),SL,OrderTakeProfit(),0,CLR_NONE);
      }
//-----
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
        if(StopLoss > 0)
        {
          OrderSend(Symbol(),OP_BUY,Lots,NormalizeDouble(Ask,Digits),Slippage,NormalizeDouble(Ask,Digits)-(StopLoss*Point),0,OrderComments,Magic,0,BuyColor);
        }
        if(StopLoss == 0)
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
        if(StopLoss > 0)
        {
          OrderSend(Symbol(),OP_SELL,Lots,NormalizeDouble(Bid,Digits),Slippage,NormalizeDouble(Bid,Digits)+(StopLoss*Point),0,OrderComments,Magic,0,SellColor);
        }
        if(StopLoss == 0)
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
//-----
  double SL = iSAR(NULL,0,SarStep,0.1,1);
//+----
  double RsiClose = iRSI(NULL,0,RsiPeriod,PRICE_CLOSE,1);
  double Weighted = iRSI(NULL,0,RsiPeriod,PRICE_WEIGHTED,1);
  double Typical = iRSI(NULL,0,RsiPeriod,PRICE_TYPICAL,1);
  double Median = iRSI(NULL,0,RsiPeriod,PRICE_MEDIAN,1);
  double RsiOpen = iRSI(NULL,0,RsiPeriod,PRICE_OPEN,1);
//-------------------------------------------------------------------+
//BULL trend
  if(RsiClose > Weighted && Weighted > Typical && Typical > Median && Median > RsiOpen && Close[1] > SL && RsiClose > 50)
  {
    if((CheckHour != true) || (Hour() > StartHour && Hour() < EndHour))
    {
      return(BULL);
    }
  }
//BEAR trend
  if(RsiClose < Weighted && Weighted < Typical && Typical < Median && Median < RsiOpen && Close[1] < SL && RsiClose < 50)
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