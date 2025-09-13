//+------------------------------------------------------------------+
//|                       Starter_v6mod(barabashkakvn's edition).mq5 |
//|                                                           jpkfox |
//+------------------------------------------------------------------+
#property  copyright "jpkfox"
#property version    "1.004"
//---
#include <Trade\PositionInfo.mqh>
#include <Trade\Trade.mqh>
#include <Trade\SymbolInfo.mqh>  
#include <Trade\AccountInfo.mqh>
#include <Expert\Money\MoneyFixedMargin.mqh>
CPositionInfo  m_position;                   // trade position object
CTrade         m_trade;                      // trading object
CSymbolInfo    m_symbol;                     // symbol info object
CAccountInfo   m_account;                    // account info wrapper
CMoneyFixedMargin m_money;
//---
enum enPrices
  {
   pr_close,      // Close
   pr_open,       // Open
   pr_high,       // High
   pr_low,        // Low
   pr_median,     // Median
   pr_typical,    // Typical
   pr_weighted,   // Weighted
   pr_average,    // Average (high+low+open+close)/4
   pr_medianb,    // Average median body (open+close)/2
   pr_tbiased,    // Trend biased price
   pr_tbiased2,   // Trend biased (extreme) price
   pr_haclose,    // Heiken ashi close
   pr_haopen ,    // Heiken ashi open
   pr_hahigh,     // Heiken ashi high
   pr_halow,      // Heiken ashi low
   pr_hamedian,   // Heiken ashi median
   pr_hatypical,  // Heiken ashi typical
   pr_haweighted, // Heiken ashi weighted
   pr_haaverage,  // Heiken ashi average
   pr_hamedianb,  // Heiken ashi median body
   pr_hatbiased,  // Heiken ashi trend biased price
   pr_hatbiased2  // Heiken ashi trend biased (extreme) price
  };
//--- input parameters
sinput string  _0_="Money Management";             // Money management
input bool     InpLotSetManually=false;            // Lot manually: "true" -> manually, "false" - risk in percent
input double   InpLots=0.1;                        // Lots value
input ushort   InpStopLoss=35;                     // Stop Loss (in pips)
input ushort   InpTakeProfit=10;                   // Take Profit (in pips)
input ushort   InpTrailingStop=0;                  // Trailing Stop ("0" - no trailing)(in pips) 
input ushort   InpTrailingStep=5;                  // Trailing Step (in pips)
input double   Risk=5;                             // Risk in percent for a deal from a free margin
input double   InpDecreaseFactor=1.6;              // Decrease lot size after a loss
input uchar    InpMaxLossesPerDay=3;               // Maximum number of losses per day 
input double   InpMargincutoff=800;                // Stop trading if equity level decreases to that level
input double   InpMaxtrades=10;                    // Total number of positions
input ushort   InpStepGrid=30;                     // Step greed
sinput string  _1_="Indicator EMAAngle";           // Indicator EMAAngle
input uint     EMAPeriod=34;
input ENUM_MA_METHOD       MAType=MODE_EMA;
input ENUM_APPLIED_PRICE   MAPrice=PRICE_CLOSE;
input double   AngleThreshold=3.0;
input uint     StartEMAShift=6;
input uint     EndEMAShift=0;
sinput string  _2_="Indicator Laguerre_RSI_with_Laguerre_filter";          // Indicator Laguerre_RSI_with_Laguerre_filter
input double   RsiGamma=0.80;                      // Laguerre RSI gamma
input enPrices RsiPrice=0;                         // Price
input double   RsiSmoothGamma=0.001;               // Laguerre RSI smooth gamma
input int      RsiSmoothSpeed=2;                   // Laguerre RSI smooth speed (min 0, max 6)
input double   FilterGamma=0.60;                   // Laguerre filter gamma
input int      FilterSpeed=2;                      // Laguerre filter speed (min 0, max 6)
input double   LevelUp=0.85;                       // Level up
input double   LevelDown=0.15;                     // Level down
input bool     NoTradeZoneVisible=true;            // Display no trade zone?
input double   NoTradeZoneUp=0.65;                 // No trade zone up
input double   NoTradeZoneDown=0.35;               // No trade zone down
sinput string  _3_="Indicator CCI";                // Indicator CCI
input int      CCI_ma_period=14;                   // CCI averaging period 
sinput string  _4_="Indicators MA";                // Indicators Moving average
input int      MA_One_ma_period=120;               // MA One averaging period 
input int      MA_Two_ma_period=40;                // MA Two averaging period 
input ulong    m_magic=15489;                // magic number
//---
ulong          m_slippage=30;                // slippage

double         ExtStopLoss=0.0;
double         ExtTakeProfit=0.0;
double         ExtTrailingStop=0.0;
double         ExtTrailingStep=0.0;
double         ExtStepGrid=0.0;

int            handle_iCustom_EMAAngle;      // variable for storing the handle of the iCustom indicator
int            handle_iCustom_Laguerre_RSI;  // variable for storing the handle of the iCustom indicator 
int            handle_iCCI;                  // variable for storing the handle of the iCCI indicator 
int            handle_iMA_One;               // variable for storing the handle of the iMA indicator 
int            handle_iMA_Two;               // variable for storing the handle of the iMA indicator 
double         m_adjusted_point;             // point value adjusted for 3 or 5 points
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//---
   if(!m_symbol.Name(Symbol())) // sets symbol name
      return(INIT_FAILED);
   RefreshRates();

   string err_text="";
   if(InpLotSetManually)
      if(!CheckVolumeValue(InpLots,err_text))
        {
         Print(err_text);
         return(INIT_PARAMETERS_INCORRECT);
        }
//---
   m_trade.SetExpertMagicNumber(m_magic);
//---
   if(IsFillingTypeAllowed(SYMBOL_FILLING_FOK))
      m_trade.SetTypeFilling(ORDER_FILLING_FOK);
   else if(IsFillingTypeAllowed(SYMBOL_FILLING_IOC))
      m_trade.SetTypeFilling(ORDER_FILLING_IOC);
   else
      m_trade.SetTypeFilling(ORDER_FILLING_RETURN);
//---
   m_trade.SetDeviationInPoints(m_slippage);
//--- tuning for 3 or 5 digits
   int digits_adjust=1;
   if(m_symbol.Digits()==3 || m_symbol.Digits()==5)
      digits_adjust=10;
   m_adjusted_point=m_symbol.Point()*digits_adjust;

   ExtStopLoss=InpStopLoss*m_adjusted_point;
   ExtTakeProfit=InpTakeProfit*m_adjusted_point;
   ExtTrailingStop=InpTrailingStop*m_adjusted_point;
   ExtTrailingStep=InpTrailingStep*m_adjusted_point;
   ExtStepGrid=InpStepGrid*m_adjusted_point;
//---
   if(!InpLotSetManually)
     {
      if(!m_money.Init(GetPointer(m_symbol),Period(),m_symbol.Point()*digits_adjust))
         return(INIT_FAILED);
      m_money.Percent(Risk);
     }
//--- create handle of the indicator iCustom
   handle_iCustom_EMAAngle=iCustom(m_symbol.Name(),Period(),"emaangle",
                                   EMAPeriod,
                                   MAType,
                                   MAPrice,
                                   AngleThreshold,
                                   StartEMAShift,
                                   EndEMAShift
                                   );
//--- if the handle is not created 
   if(handle_iCustom_EMAAngle==INVALID_HANDLE)
     {
      //--- tell about the failure and output the error code 
      PrintFormat("Failed to create handle of the iCustom indicator for the symbol %s/%s, error code %d",
                  m_symbol.Name(),
                  EnumToString(Period()),
                  GetLastError());
      //--- the indicator is stopped early 
      return(INIT_FAILED);
     }
//--- create handle of the indicator iCustom
   handle_iCustom_Laguerre_RSI=iCustom(m_symbol.Name(),Period(),"Laguerre_RSI_with_Laguerre_filter",
                                       RsiGamma,
                                       RsiPrice,
                                       RsiSmoothGamma,
                                       RsiSmoothSpeed,
                                       FilterGamma,
                                       FilterSpeed,
                                       LevelUp,
                                       LevelDown,
                                       NoTradeZoneVisible,
                                       NoTradeZoneUp,
                                       NoTradeZoneDown
                                       );
//--- if the handle is not created 
   if(handle_iCustom_Laguerre_RSI==INVALID_HANDLE)
     {
      //--- tell about the failure and output the error code 
      PrintFormat("Failed to create handle of the iCustom indicator for the symbol %s/%s, error code %d",
                  m_symbol.Name(),
                  EnumToString(Period()),
                  GetLastError());
      //--- the indicator is stopped early 
      return(INIT_FAILED);
     }
//--- create handle of the indicator iCCI
   handle_iCCI=iCCI(m_symbol.Name(),Period(),CCI_ma_period,PRICE_CLOSE);
//--- if the handle is not created 
   if(handle_iCCI==INVALID_HANDLE)
     {
      //--- tell about the failure and output the error code 
      PrintFormat("Failed to create handle of the iCCI indicator for the symbol %s/%s, error code %d",
                  m_symbol.Name(),
                  EnumToString(Period()),
                  GetLastError());
      //--- the indicator is stopped early 
      return(INIT_FAILED);
     }
//--- create handle of the indicator iMA
   handle_iMA_One=iMA(m_symbol.Name(),Period(),MA_One_ma_period,0,MODE_EMA,PRICE_MEDIAN);
//--- if the handle is not created 
   if(handle_iMA_One==INVALID_HANDLE)
     {
      //--- tell about the failure and output the error code 
      PrintFormat("Failed to create handle of the iMA indicator for the symbol %s/%s, error code %d",
                  m_symbol.Name(),
                  EnumToString(Period()),
                  GetLastError());
      //--- the indicator is stopped early 
      return(INIT_FAILED);
     }
//--- create handle of the indicator iMA
   handle_iMA_Two=iMA(m_symbol.Name(),Period(),MA_Two_ma_period,0,MODE_EMA,PRICE_MEDIAN);
//--- if the handle is not created 
   if(handle_iMA_Two==INVALID_HANDLE)
     {
      //--- tell about the failure and output the error code 
      PrintFormat("Failed to create handle of the iMA indicator for the symbol %s/%s, error code %d",
                  m_symbol.Name(),
                  EnumToString(Period()),
                  GetLastError());
      //--- the indicator is stopped early 
      return(INIT_FAILED);
     }
//---
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
//---

  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
   if(m_account.FreeMargin()<InpMargincutoff)
      return;
   if(CalculateAllPositions()>=InpMaxtrades)
      return;
   if(!RefreshRates())
      return;
   int NumberSLToday=NumberSLToday();
   if(NumberSLToday>=InpMaxLossesPerDay || NumberSLToday==-1)
      return;
   int donttrade=0;
   int allexit=0;
//--- friday Exits 
   MqlDateTime str1;
   TimeToStruct(TimeCurrent(),str1);
   if(str1.day_of_week==5 && str1.hour>=18)
      donttrade=1;
   if(str1.day_of_week==5 && str1.hour>=20)
      allexit=1;
//--- condition statements                                         
   double Laguerre=iCustomGet(handle_iCustom_Laguerre_RSI,2,0);
//double Laguerreprevious =iCustomGet(handle_iCustom_Laguerre_RSI,2,1);
   double Alpha            =iCCIGet(0);
   double MA               =iMAGet(handle_iMA_One,0);
   double MAprevious       =iMAGet(handle_iMA_One,1);
   double MA2              =iMAGet(handle_iMA_Two,0);
   double MAprevious2      =iMAGet(handle_iMA_Two,1);

   CheckOpenPositions(allexit,Laguerre);  // open position controls  
//---
   int      count_buys        =0;
   double   price_lowest_buy  =DBL_MAX;
   int      count_sells       =0;
   double   price_highest_sell=DBL_MIN;
   CalculateAllPositions(count_buys,price_lowest_buy,
                         count_sells,price_highest_sell);  // check for open positions
   bool you_can_open_BUY=(count_buys!=0)?true:false;
   bool you_can_open_SELL=(count_sells!=0)?true:false;
//--- in opposite direction
   double emaanlgle=iCustomGet(handle_iCustom_EMAAngle,0,1);
   int  EMAAngle=0;
   if(emaanlgle<-AngleThreshold)
     {
      EMAAngle=1;          // up trend
      if(count_sells==0) // make sure No Sells open
        {
         you_can_open_BUY=true;
         you_can_open_SELL=false;
        }
     }
   if(emaanlgle>AngleThreshold)
     {
      EMAAngle=-1;            // down trend
      if(count_buys==0) // make sure no Buys open
        {
         you_can_open_BUY=false;
         you_can_open_SELL=true;
        }
     }
   if(EMAAngle==0) // market is flat
     {
      you_can_open_BUY=false;
      you_can_open_SELL=false;
     }
//--- 
   if(CheckBuyCondition(/*Laguerreprevious,*/Laguerre,MAprevious,MA,MAprevious2,MA2,Alpha)==1 && donttrade==0 && you_can_open_BUY)
     {
      if(price_lowest_buy-m_symbol.Ask()<ExtStepGrid)
         return;
      double sl=(InpStopLoss==0)?0.0:m_symbol.Ask()-ExtStopLoss;
      double tp=(InpTakeProfit==0)?0.0:m_symbol.Ask()+ExtTakeProfit;
      OpenBuy(sl,tp,NumberSLToday);
     }
   if(CheckSellCondition(/*Laguerreprevious,*/Laguerre,MAprevious,MA,MAprevious2,MA2,Alpha)==1 && donttrade==0 && you_can_open_SELL)
     {
      if(m_symbol.Bid()-price_highest_sell<ExtStepGrid)
         return;
      double sl=(InpStopLoss==0)?0.0:m_symbol.Bid()+ExtStopLoss;
      double tp=(InpTakeProfit==0)?0.0:m_symbol.Bid()-ExtTakeProfit;
      OpenSell(sl,tp,NumberSLToday);
     }
//---
  }
//+------------------------------------------------------------------+
//| TradeTransaction function                                        |
//+------------------------------------------------------------------+
void OnTradeTransaction(const MqlTradeTransaction &trans,
                        const MqlTradeRequest &request,
                        const MqlTradeResult &result)
  {
//---

  }
//+------------------------------------------------------------------+
//| Refreshes the symbol quotes data                                 |
//+------------------------------------------------------------------+
bool RefreshRates(void)
  {
//--- refresh rates
   if(!m_symbol.RefreshRates())
     {
      Print("RefreshRates error");
      return(false);
     }
//--- protection against the return value of "zero"
   if(m_symbol.Ask()==0 || m_symbol.Bid()==0)
      return(false);
//---
   return(true);
  }
//+------------------------------------------------------------------+
//| Check the correctness of the order volume                        |
//+------------------------------------------------------------------+
bool CheckVolumeValue(double volume,string &error_description)
  {
//--- minimal allowed volume for trade operations
// double min_volume=m_symbol.LotsMin();
   double min_volume=SymbolInfoDouble(Symbol(),SYMBOL_VOLUME_MIN);
   if(volume<min_volume)
     {
      error_description=StringFormat("Volume is less than the minimal allowed SYMBOL_VOLUME_MIN=%.2f",min_volume);
      return(false);
     }

//--- maximal allowed volume of trade operations
// double max_volume=m_symbol.LotsMax();
   double max_volume=SymbolInfoDouble(Symbol(),SYMBOL_VOLUME_MAX);
   if(volume>max_volume)
     {
      error_description=StringFormat("Volume is greater than the maximal allowed SYMBOL_VOLUME_MAX=%.2f",max_volume);
      return(false);
     }

//--- get minimal step of volume changing
// double volume_step=m_symbol.LotsStep();
   double volume_step=SymbolInfoDouble(Symbol(),SYMBOL_VOLUME_STEP);

   int ratio=(int)MathRound(volume/volume_step);
   if(MathAbs(ratio*volume_step-volume)>0.0000001)
     {
      error_description=StringFormat("Volume is not a multiple of the minimal step SYMBOL_VOLUME_STEP=%.2f, the closest correct volume is %.2f",
                                     volume_step,ratio*volume_step);
      return(false);
     }
   error_description="Correct volume value";
   return(true);
  }
//+------------------------------------------------------------------+ 
//| Checks if the specified filling mode is allowed                  | 
//+------------------------------------------------------------------+ 
bool IsFillingTypeAllowed(int fill_type)
  {
//--- Obtain the value of the property that describes allowed filling modes 
   int filling=m_symbol.TradeFillFlags();
//--- Return true, if mode fill_type is allowed 
   return((filling & fill_type)==fill_type);
  }
//+------------------------------------------------------------------+
//| Lot Check                                                        |
//+------------------------------------------------------------------+
double LotCheck(double lots)
  {
//--- calculate maximum volume
   double volume=NormalizeDouble(lots,2);
   double stepvol=m_symbol.LotsStep();
   if(stepvol>0.0)
      volume=stepvol*MathFloor(volume/stepvol);
//---
   double minvol=m_symbol.LotsMin();
   if(volume<minvol)
      volume=0.0;
//---
   double maxvol=m_symbol.LotsMax();
   if(volume>maxvol)
      volume=maxvol;
   return(volume);
  }
//+------------------------------------------------------------------+
//| Get value of buffers for the iCustom                             |
//|  the buffer numbers are the following:                           |
//+------------------------------------------------------------------+
double iCustomGet(int handle,const int buffer,const int index)
  {
   double Custom[1];
//--- reset error code 
   ResetLastError();
//--- fill a part of the iCustom array with values from the indicator buffer that has 0 index 
   if(CopyBuffer(handle,buffer,index,1,Custom)<0)
     {
      //--- if the copying fails, tell the error code 
      PrintFormat("Failed to copy data from the iCustom indicator, error code %d",GetLastError());
      //--- quit with zero result - it means that the indicator is considered as not calculated 
      return(0.0);
     }
   return(Custom[0]);
  }
//+------------------------------------------------------------------+
//| Get value of buffers for the iCCI                                |
//+------------------------------------------------------------------+
double iCCIGet(const int index)
  {
   double CCI[1];
//--- reset error code 
   ResetLastError();
//--- fill a part of the iCCIBuffer array with values from the indicator buffer that has 0 index 
   if(CopyBuffer(handle_iCCI,0,index,1,CCI)<0)
     {
      //--- if the copying fails, tell the error code 
      PrintFormat("Failed to copy data from the iCCI indicator, error code %d",GetLastError());
      //--- quit with zero result - it means that the indicator is considered as not calculated 
      return(0.0);
     }
   return(CCI[0]);
  }
//+------------------------------------------------------------------+
//| Get value of buffers for the iMA                                 |
//+------------------------------------------------------------------+
double iMAGet(int handle_iMA,const int index)
  {
   double MA[1];
//--- reset error code 
   ResetLastError();
//--- fill a part of the iMABuffer array with values from the indicator buffer that has 0 index 
   if(CopyBuffer(handle_iMA,0,index,1,MA)<0)
     {
      //--- if the copying fails, tell the error code 
      PrintFormat("Failed to copy data from the iMA indicator, error code %d",GetLastError());
      //--- quit with zero result - it means that the indicator is considered as not calculated 
      return(0.0);
     }
   return(MA[0]);
  }
//+------------------------------------------------------------------+
//| Calculate all positions                                          |
//+------------------------------------------------------------------+
int CalculateAllPositions()
  {
   int total=0;

   for(int i=PositionsTotal()-1;i>=0;i--)
      if(m_position.SelectByIndex(i)) // selects the position by index for further access to its properties
         if(m_position.Symbol()==m_symbol.Name() && m_position.Magic()==m_magic)
            total++;
//---
   return(total);
  }
//+------------------------------------------------------------------+
//| Calculate all positions                                          |
//+------------------------------------------------------------------+
void CalculateAllPositions(int &count_buys,double &price_lowest_buy,
                           int &count_sells,double &price_highest_sell)
  {
   count_buys  =0;   price_lowest_buy  =DBL_MAX;
   count_sells =0;   price_highest_sell=DBL_MIN;
   for(int i=PositionsTotal()-1;i>=0;i--)
      if(m_position.SelectByIndex(i)) // selects the position by index for further access to its properties
         if(m_position.Symbol()==m_symbol.Name() && m_position.Magic()==m_magic)
           {
            if(m_position.PositionType()==POSITION_TYPE_BUY)
              {
               count_buys++;
               if(m_position.PriceOpen()<price_lowest_buy) // the lowest position of "BUY" is found
                  price_lowest_buy=m_position.PriceOpen();
               continue;
              }
            else if(m_position.PositionType()==POSITION_TYPE_SELL)
              {
               count_sells++;
               if(m_position.PriceOpen()>price_highest_sell) // the highest position of "SELL" is found
                  price_highest_sell=m_position.PriceOpen();
               continue;
              }
           }
  }
//+------------------------------------------------------------------+
//| CheckExitCondition                                               |
//| Check if Leguerre has proper value                               |
//| return 0 for exit condition not met                              |
//| return 1 for exit condition met                                  |
//+------------------------------------------------------------------+
int CheckExitCondition(string type,double &Laguerre)
  {
   if(type=="BUY")
     {
      if(Laguerre>LevelUp)
         return(1);
      return(0);
     }
   if(type=="SELL")
     {
      if(Laguerre<LevelDown)
         return(1);
     }
   return(0);
  }
//+------------------------------------------------------------------+
//| CheckBuyCondition                                                |
//|   return 0 for exit condition not met                            |
//|   return 1 for exit condition met                                |
//+------------------------------------------------------------------+
int CheckBuyCondition(/*double &Laguerreprevious,*/double &Laguerre,
                      double &MAprevious,double &MA,
                      double &MAprevious2,double &MA2,
                      double &Alpha)
  {
//if(Laguerreprevious<=0 && Laguerre<=0 && MA>MAprevious && MA2>MAprevious2 && Alpha<-5)
   if(Laguerre<LevelDown && MA<MAprevious && MA2<MAprevious2 && Alpha<0.0)
      return(1);
//---
   return(0);
  }
//+------------------------------------------------------------------+
//| CheckSellCondition                                               |
//|   return 0 for exit condiotion not met                           |
//|   return 1 for exit condition met                                |
//+------------------------------------------------------------------+
int CheckSellCondition(/*double &Laguerreprevious,*/double &Laguerre,
                       double &MAprevious,double &MA,
                       double &MAprevious2,double &MA2,
                       double &Alpha)
  {
//if((Laguerreprevious>=1) && (Laguerre>=1) && (MA<MAprevious) && (MA2<MAprevious2) && (Alpha>5))
   if(Laguerre>LevelUp && MA>MAprevious && MA2>MAprevious2 && Alpha>0.0)
      return(1);
//---
   return(0);
  }
//+------------------------------------------------------------------+
//| Check Open Position Controls                                     |
//+------------------------------------------------------------------+
void CheckOpenPositions(int allexits,double &Laguerre)
  {
   int ExitCondition_Buy=CheckExitCondition("BUY",Laguerre);
   int ExitCondition_Sell=CheckExitCondition("SELL",Laguerre);

   for(int i=PositionsTotal()-1;i>=0;i--)
      if(m_position.SelectByIndex(i)) // selects the position by index for further access to its properties
         if(m_position.Symbol()==m_symbol.Name() && m_position.Magic()==m_magic)
           {
            if(m_position.PositionType()==POSITION_TYPE_BUY)
              {
               int ExitCondition=ExitCondition_Buy;   // first check if indicators cause exit
               if(allexits==1) // then check if Friday
                  ExitCondition=1;
               if(ExitCondition==1)
                 {
                  m_trade.PositionClose(m_position.Ticket()); // close position
                  continue;
                 }
               if(InpTrailingStop>0)
                  if(m_position.PriceCurrent()-m_position.PriceOpen()>ExtTrailingStop+ExtTrailingStep)
                     if(m_position.StopLoss()<m_position.PriceCurrent()-(ExtTrailingStop+ExtTrailingStep))
                       {
                        if(!m_trade.PositionModify(m_position.Ticket(),
                           m_symbol.NormalizePrice(m_position.PriceCurrent()-ExtTrailingStop),
                           m_position.TakeProfit()))
                           Print("Modify ",m_position.Ticket(),
                                 " Position -> false. Result Retcode: ",m_trade.ResultRetcode(),
                                 ", description of result: ",m_trade.ResultRetcodeDescription());
                       }
              }

            if(m_position.PositionType()==POSITION_TYPE_SELL)
              {
               int ExitCondition=ExitCondition_Sell;  // first check if indicators cause exit
               if(allexits==1) // then check if Friday
                  ExitCondition=1;
               if(ExitCondition==1)
                 {
                  m_trade.PositionClose(m_position.Ticket()); // close position
                  continue;
                 }
               if(InpTrailingStop>0)
                  if(m_position.PriceOpen()-m_position.PriceCurrent()>ExtTrailingStop+ExtTrailingStep)
                     if((m_position.StopLoss()>(m_position.PriceCurrent()+(ExtTrailingStop+ExtTrailingStep))) || 
                        (m_position.StopLoss()==0))
                       {
                        if(!m_trade.PositionModify(m_position.Ticket(),
                           m_symbol.NormalizePrice(m_position.PriceCurrent()+ExtTrailingStop),
                           m_position.TakeProfit()))
                           Print("Modify ",m_position.Ticket(),
                                 " Position -> false. Result Retcode: ",m_trade.ResultRetcode(),
                                 ", description of result: ",m_trade.ResultRetcodeDescription());
                       }
              }
           }
//---
   return;
  }
//+------------------------------------------------------------------+
//| Counting the number of SLs for today                             |
//+------------------------------------------------------------------+
int NumberSLToday()
  {
   int losses=0;

   MqlDateTime str1;
   TimeToStruct(TimeCurrent(),str1);
//--- request trade history 
   datetime to_date=TimeCurrent()+60*60*24;
   datetime from_date=0;
   str1.hour=0;
   str1.min=0;
   str1.sec=0;
   from_date=StructToTime(str1);
   HistorySelect(from_date,to_date);
//---
   uint     total=HistoryDealsTotal();
   ulong    ticket=0;
   long     position_id=0;
//--- for all deals 
   for(uint i=0;i<total;i++)
     {
      //--- try to get deals ticket 
      if((ticket=HistoryDealGetTicket(i))>0)
        {
         //--- get deals properties 
         long     deal_ticket       =0;
         long     deal_order        =0;
         long     deal_time         =0;
         long     deal_time_msc     =0;
         long     deal_type         =-1;
         long     deal_entry        =-1;
         long     deal_magic        =0;
         long     deal_reason       =-1;
         long     deal_position_id  =0;
         double   deal_volume       =0.0;
         double   deal_price        =0.0;
         double   deal_commission   =0.0;
         double   deal_swap         =0.0;
         double   deal_profit       =0.0;
         string   deal_symbol       ="";
         string   deal_comment      ="";
         string   deal_external_id  ="";
         if(HistoryDealSelect(ticket))
           {
            deal_ticket       =HistoryDealGetInteger(ticket,DEAL_TICKET);
            deal_order        =HistoryDealGetInteger(ticket,DEAL_ORDER);
            deal_time         =HistoryDealGetInteger(ticket,DEAL_TIME);
            deal_time_msc     =HistoryDealGetInteger(ticket,DEAL_TIME_MSC);
            deal_type         =HistoryDealGetInteger(ticket,DEAL_TYPE);
            deal_entry        =HistoryDealGetInteger(ticket,DEAL_ENTRY);
            deal_magic        =HistoryDealGetInteger(ticket,DEAL_MAGIC);
            deal_reason       =HistoryDealGetInteger(ticket,DEAL_REASON);
            deal_position_id  =HistoryDealGetInteger(ticket,DEAL_POSITION_ID);

            deal_volume       =HistoryDealGetDouble(ticket,DEAL_VOLUME);
            deal_price        =HistoryDealGetDouble(ticket,DEAL_PRICE);
            deal_commission   =HistoryDealGetDouble(ticket,DEAL_COMMISSION);
            deal_swap         =HistoryDealGetDouble(ticket,DEAL_SWAP);
            deal_profit       =HistoryDealGetDouble(ticket,DEAL_PROFIT);

            deal_symbol       =HistoryDealGetString(ticket,DEAL_SYMBOL);
            deal_comment      =HistoryDealGetString(ticket,DEAL_COMMENT);
            deal_external_id  =HistoryDealGetString(ticket,DEAL_EXTERNAL_ID);
           }
         else
            return(-1);
         //if(deal_reason!=-1)
         //   DebugBreak();
         if(deal_symbol==m_symbol.Name() && deal_magic==m_magic)
            if(deal_entry==DEAL_ENTRY_OUT)
              {
               if(deal_commission+deal_swap+deal_profit<0.0)
                 {
                  losses++;
                 }
              }
        }
     }
//---
   return(losses);
  }
//+------------------------------------------------------------------+
//| Open Buy position                                                |
//+------------------------------------------------------------------+
void OpenBuy(double sl,double tp,int &NumberSLToday)
  {
   sl=m_symbol.NormalizePrice(sl);
   tp=m_symbol.NormalizePrice(tp);

   double check_open_long_lot=0.0;
   if(!InpLotSetManually)
     {
      check_open_long_lot=m_money.CheckOpenLong(m_symbol.Ask(),sl);
      Print("sl=",DoubleToString(sl,m_symbol.Digits()),
            ", CheckOpenLong: ",DoubleToString(check_open_long_lot,2),
            ", Balance: ",    DoubleToString(m_account.Balance(),2),
            ", Equity: ",     DoubleToString(m_account.Equity(),2),
            ", FreeMargin: ", DoubleToString(m_account.FreeMargin(),2));
      if(check_open_long_lot==0.0)
         return;
     }
   else
      check_open_long_lot=InpLots;

   if(NumberSLToday>0)
     {
      check_open_long_lot=LotCheck(check_open_long_lot/InpDecreaseFactor);
      if(check_open_long_lot==0.0)
         return;
     }
//--- check volume before OrderSend to avoid "not enough money" error (CTrade)
   double check_volume_lot=m_trade.CheckVolume(m_symbol.Name(),check_open_long_lot,m_symbol.Ask(),ORDER_TYPE_BUY);

   if(check_volume_lot!=0.0)
      if(check_volume_lot>=check_open_long_lot)
        {
         if(m_trade.Buy(check_open_long_lot,NULL,m_symbol.Ask(),sl,tp))
           {
            if(m_trade.ResultDeal()==0)
              {
               Print("#1 Buy -> false. Result Retcode: ",m_trade.ResultRetcode(),
                     ", description of result: ",m_trade.ResultRetcodeDescription());
               //PrintResult(m_trade,m_symbol);
              }
            else
              {
               Print("#2 Buy -> true. Result Retcode: ",m_trade.ResultRetcode(),
                     ", description of result: ",m_trade.ResultRetcodeDescription());
               //PrintResult(m_trade,m_symbol);
              }
           }
         else
           {
            Print("#3 Buy -> false. Result Retcode: ",m_trade.ResultRetcode(),
                  ", description of result: ",m_trade.ResultRetcodeDescription());
            //PrintResult(m_trade,m_symbol);
           }
        }
//---
  }
//+------------------------------------------------------------------+
//| Open Sell position                                               |
//+------------------------------------------------------------------+
void OpenSell(double sl,double tp,int &NumberSLToday)
  {
   sl=m_symbol.NormalizePrice(sl);
   tp=m_symbol.NormalizePrice(tp);

   double check_open_short_lot=0.0;
   if(!InpLotSetManually)
     {
      check_open_short_lot=m_money.CheckOpenShort(m_symbol.Bid(),sl);
      Print("sl=",DoubleToString(sl,m_symbol.Digits()),
            ", CheckOpenLong: ",DoubleToString(check_open_short_lot,2),
            ", Balance: ",    DoubleToString(m_account.Balance(),2),
            ", Equity: ",     DoubleToString(m_account.Equity(),2),
            ", FreeMargin: ", DoubleToString(m_account.FreeMargin(),2));
      if(check_open_short_lot==0.0)
         return;
     }
   else
      check_open_short_lot=InpLots;

   if(NumberSLToday>0)
     {
      check_open_short_lot=LotCheck(check_open_short_lot/InpDecreaseFactor);
      if(check_open_short_lot==0.0)
         return;
     }
//--- check volume before OrderSend to avoid "not enough money" error (CTrade)
   double check_volume_lot=m_trade.CheckVolume(m_symbol.Name(),check_open_short_lot,m_symbol.Bid(),ORDER_TYPE_SELL);

   if(check_volume_lot!=0.0)
      if(check_volume_lot>=check_open_short_lot)
        {
         if(m_trade.Sell(check_open_short_lot,NULL,m_symbol.Bid(),sl,tp))
           {
            if(m_trade.ResultDeal()==0)
              {
               Print("#1 Sell -> false. Result Retcode: ",m_trade.ResultRetcode(),
                     ", description of result: ",m_trade.ResultRetcodeDescription());
               //PrintResult(m_trade,m_symbol);
              }
            else
              {
               Print("#2 Sell -> true. Result Retcode: ",m_trade.ResultRetcode(),
                     ", description of result: ",m_trade.ResultRetcodeDescription());
               //PrintResult(m_trade,m_symbol);
              }
           }
         else
           {
            Print("#3 Sell -> false. Result Retcode: ",m_trade.ResultRetcode(),
                  ", description of result: ",m_trade.ResultRetcodeDescription());
            //PrintResult(m_trade,m_symbol);
           }
        }
//---
  }
//+------------------------------------------------------------------+
//| Trailing                                                         |
//+------------------------------------------------------------------+
void Trailing()
  {
   if(ExtTrailingStop==0)
      return;
   for(int i=PositionsTotal()-1;i>=0;i--) // returns the number of open positions
      if(m_position.SelectByIndex(i))
         if(m_position.Symbol()==m_symbol.Name() && m_position.Magic()==m_magic)
           {
            if(m_position.PositionType()==POSITION_TYPE_BUY)
              {
               if(m_position.PriceCurrent()-m_position.PriceOpen()>ExtTrailingStop+ExtTrailingStep)
                  if(m_position.StopLoss()<m_position.PriceCurrent()-(ExtTrailingStop+ExtTrailingStep))
                    {
                     if(!m_trade.PositionModify(m_position.Ticket(),
                        m_symbol.NormalizePrice(m_position.PriceCurrent()-ExtTrailingStop),
                        m_position.TakeProfit()))
                        Print("Modify ",m_position.Ticket(),
                              " Position -> false. Result Retcode: ",m_trade.ResultRetcode(),
                              ", description of result: ",m_trade.ResultRetcodeDescription());
                     continue;
                    }
              }
            else
              {
               if(m_position.PriceOpen()-m_position.PriceCurrent()>ExtTrailingStop+ExtTrailingStep)
                  if((m_position.StopLoss()>(m_position.PriceCurrent()+(ExtTrailingStop+ExtTrailingStep))) || 
                     (m_position.StopLoss()==0))
                    {
                     if(!m_trade.PositionModify(m_position.Ticket(),
                        m_symbol.NormalizePrice(m_position.PriceCurrent()+ExtTrailingStop),
                        m_position.TakeProfit()))
                        Print("Modify ",m_position.Ticket(),
                              " Position -> false. Result Retcode: ",m_trade.ResultRetcode(),
                              ", description of result: ",m_trade.ResultRetcodeDescription());
                     return;
                    }
              }

           }
  }
//+------------------------------------------------------------------+
