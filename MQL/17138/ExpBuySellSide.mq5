//+------------------------------------------------------------------+
//|                                               ExpBuySellSide.mq5 |
//|                           Copyright 2016, Roberto Jacobs (3rjfx) |
//|                              https://www.mql5.com/en/users/3rjfx |
//+------------------------------------------------------------------+
#property copyright "Copyright 2016, Roberto Jacobs (3rjfx) ~ By 3rjfx ~ Created: 2016/12/07"
#property link      "http://www.mql5.com"
#property link      "https://www.mql5.com/en/users/3rjfx"
#property version   "1.00"
//--
#property description "The ExpBuySellSide is the MT5 Expert Advisor based on the ATRStops and StepUpDown Indicator."
//---
//+------------------------------------------------------------------+
//|                             Include                              |
//+------------------------------------------------------------------+
#include <Trade\Trade.mqh>
#include <Trade\PositionInfo.mqh>
#include <Trade\SymbolInfo.mqh>
#include <Trade\AccountInfo.mqh>
//---
//--
enum YN
 {
   No,
   Yes
 };
//--
enum mmt
 {  
   FixedLot,
   DynamicLotSize
 };
//--
//---//
//--- input parameters 
input ENUM_TIMEFRAMES   TimeFrames = PERIOD_H1; // Expert TimeFrame
input double        RiskPercentage = 3.0;       // Risk Percentage
input double           TPvsSLRatio = 3.0;       // multiplication SL to TP Ratio
input mmt      MoneyManagementType = DynamicLotSize; // Money Management Type
input string       ManualyLotsSize = "If Set Money Management Type = FixedLot, Input Lots Size below";
input double          TradeLotSize = 0.1;       // Input Lots Size manualy
input YN     CloseByOppositeSignal = Yes;       // Close Order By Opposite Signal, (Yes) or (No)
input YN           UseTrailingStop = No;       // Use Trailing Stop, (Yes) or (No)
input string    InputTrailingvalue = "If Use Trailing Stop (Yes), input Trailing Stop value below";
input int     Trailing_FixedPipsSL = 10;        // Trailing Stop value (in Pips)
input ulong                  Magic = 1234;      // Expert Advisor ID (Max 8 digits)
//---
//---
ulong slip,
 MagicNumber;
//--
double pip; 
//--
int cmnt,
    pmnt,
    opnB,
    opnS;
//--
int cbar,
    pbar,
    ebar=360;
//--
//-- atr buffers
double atrb[];
double smin[];
double smax[];
double atrpos[];
double sideup[];
double sidedn[];
//-- ATR handle
int atrhandle;
//---- atr parameters
int length     = 10,
    atrperiod  = 5;
double Kv      = 2.5;
//--
//-- MA buffers
double sma01ar[],
       mma30ar[];
//--
//--- MA handles
int sma01h,
    mma30h;
//--
int Fast_SMMA=2,
    SLOW_SMMA=30;
//--
bool NoMoney,
     NoOrder,
     exevent;
//--- Price handle
double OPEN[];
double HIGH[];
double LOW[];
double CLOSE[];
//--
string expname;
datetime closetime;
//---
//--
CTrade         *m_forex;
CSymbolInfo    *m_symbol;
CPositionInfo  *m_pos_info; 
CAccountInfo   *m_account;
//---
//---------//
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//---
   //---
   exevent=EventSetTimer(60);
   //--
   MagicNumber=Magic;
   //--
   slip=SymbolInfoInteger(_Symbol,SYMBOL_SPREAD);
   //--
   if(_Digits==3||_Digits==5) 
     {pip=_Point*10;}
   else if(_Digits==2||_Digits==4) {pip=_Point;}
   if((StringSubstr(_Symbol,0,1)=="X")||(StringSubstr(_Symbol,0,1)=="#")) {pip=_Point*10;}
   //---
   //--
   ArrayResize(atrb,ebar);
   ArrayResize(smin,ebar);
   ArrayResize(smax,ebar);
   ArrayResize(atrpos,ebar);
   ArrayResize(sideup,ebar);
   ArrayResize(sidedn,ebar);
   ArrayResize(sma01ar,ebar);
   ArrayResize(mma30ar,ebar);
   ArrayResize(OPEN,ebar);
   ArrayResize(HIGH,ebar);
   ArrayResize(LOW,ebar);
   ArrayResize(CLOSE,ebar);
   //--
   ArraySetAsSeries(atrb,true);
   ArraySetAsSeries(smin,true);
   ArraySetAsSeries(smax,true);
   ArraySetAsSeries(atrpos,true);
   ArraySetAsSeries(sideup,true);
   ArraySetAsSeries(sidedn,true);
   ArraySetAsSeries(sma01ar,true);
   ArraySetAsSeries(mma30ar,true);
   ArraySetAsSeries(OPEN,true);
   ArraySetAsSeries(HIGH,true);
   ArraySetAsSeries(LOW,true);
   ArraySetAsSeries(CLOSE,true);
   //--
   ENUM_TIMEFRAMES tf1=TimeFrames==PERIOD_CURRENT ? _Period : TimeFrames;
   //--
   atrhandle=iATR(_Symbol,tf1,atrperiod);
   if(atrhandle==INVALID_HANDLE) 
     { 
       printf("Failed to create handle of the iATR indicator for ",_Symbol);
       return(INIT_FAILED); 
     } 
   //--
   sma01h=iMA(_Symbol,tf1,Fast_SMMA,0,MODE_SMA,PRICE_TYPICAL);
   if(sma01h==INVALID_HANDLE)
     {
       printf("Error creating sma01h MA indicator for ",_Symbol);
       return(INIT_FAILED);
     }
   mma30h=iMA(_Symbol,tf1,SLOW_SMMA,0,MODE_SMMA,PRICE_MEDIAN);
   if(mma30h==INVALID_HANDLE)
     {
       printf("Error creating mma30h MA indicator for ",_Symbol);
       return(INIT_FAILED);
     }
   //--
   closetime=TimeCurrent();
   cmnt=MqlReturnDateTime(TimeCurrent(),TimeReturn(min));
   pmnt=MqlReturnDateTime(TimeCurrent(),TimeReturn(min));
   expname="ExpBuySellSide";
   //---
//---
   return(INIT_SUCCEEDED);
  }
//---------//
  
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
//---
//--- Release our indicator handles
   IndicatorRelease(sma01h);
   IndicatorRelease(mma30h);
   IndicatorRelease(atrhandle);
   //--
   EventKillTimer();
   GlobalVariablesDeleteAll();
//---
  }
//---------//

//+------------------------------------------------------------------+
//|                  "Timer" event handler function                  |
//+------------------------------------------------------------------+
void OnTimer()
  {
//----
    //---
    ResetLastError();
    cmnt=MqlReturnDateTime(TimeCurrent(),TimeReturn(min));
    //--
    if(cmnt!=pmnt)
      {
        //--
        m_forex    = new CTrade();
        m_symbol   = new CSymbolInfo();   
        m_pos_info = new CPositionInfo();
        m_account  = new CAccountInfo();
        //--
        int reqact=0,
            opsbuy=0,
            opssell=1;
        //--
        CheckOpenPosition();
        datetime order_close=CheckOrderCloseTime();
        int HR=MqlReturnDateTime(TimeCurrent(),TimeReturn(hour));
        int HR_CLOSE=MqlReturnDateTime(order_close,TimeReturn(hour));
        //--
        int tSIGNAL=0;
        int trade=tradeFX();
        //--
        if((trade==1)&&(opnB==0)&&(opnS==0)&&(HR_CLOSE!=HR)) tSIGNAL=1;
        if((trade==1)&&(opnB==0)&&(opnS>0)&&(CloseByOppositeSignal==Yes)) tSIGNAL=2;
        //--
        if((trade==-1)&&(opnS==0)&&(opnB==0)&&(HR_CLOSE!=HR)) tSIGNAL=-1;
        if((trade==-1)&&(opnS==0)&&(opnB>0)&&(CloseByOppositeSignal==Yes)) tSIGNAL=-2;
        //--
        if((trade==1)&&(opnB==0)&&(opnS>0)&&(CloseByOppositeSignal==No)) tSIGNAL=3;
        if((trade==-1)&&(opnS==0)&&(opnB>0)&&(CloseByOppositeSignal==No)) tSIGNAL=-3;
        //--
        //---
        switch(tSIGNAL)
          {
            case 1: 
              {
                OpenPosition(reqact,opsbuy);
                //--
                break;
              }
            //--
            case -1: 
              {
                OpenPosition(reqact,opssell);
                //--
                break;
              }
            //--
            case 2: 
              {
                OpenPosition(reqact,opsbuy); 
                Sleep(1000); 
                ClosePosition(opssell);
                //--
                break;
              }
            //--
            case -2: 
              {
                OpenPosition(reqact,opssell); 
                Sleep(1000); 
                ClosePosition(opsbuy);
                //--
                break;
              }
            //--
            case 3: 
              {
                OpenPosition(reqact,opsbuy);
                //--
                break;
              }
            //--
            case -3: 
              {
                OpenPosition(reqact,opssell);
                //--
                break;
              }
            //--
          }
        //--
        //---
        if(UseTrailingStop==1) ModifySLTP();
        //--
        if(UseTrailingStop==0)
          {
            int clsprofit=CloseOrderProfit();
            if(clsprofit==1) ClosePosition(opsbuy);
            if(clsprofit==-1) ClosePosition(opssell);
          }
        //--
        pmnt=cmnt;
        //---
      }
//----
  } //-end OnTimer()
//---------//

//+------------------------------------------------------------------+
//|                           open positions                         |
//+------------------------------------------------------------------+
void OpenPosition(int reqaction,
                  int reqtype)
  { 
//--- prepare the request
    //--
    ResetLastError();
    //--
    ENUM_TRADE_REQUEST_ACTIONS action_req = TradeAction(reqaction);
    ENUM_ORDER_TYPE type_req              = GetOrderType(reqtype);
    //---
    double sl=0.0;
    double tp=0.0;
    double lots=0.0;
    double sltp =0.0;
    double req_price=0.0;
    //--
    if(type_req==ORDER_TYPE_BUY||
       type_req==ORDER_TYPE_BUY_LIMIT||
       type_req==ORDER_TYPE_BUY_STOP)
      {
        lots = TradeSize(reqtype);
        sltp = CalculateSLTP(lots);
        req_price = RequestPrice(reqtype);
        tp = NormalizeDouble(req_price+(sltp*TPvsSLRatio),_Digits);
        sl = NormalizeDouble(req_price-sltp,_Digits);
      }
    //--
    else if(type_req==ORDER_TYPE_SELL||
            type_req==ORDER_TYPE_SELL_LIMIT||
            type_req==ORDER_TYPE_SELL_STOP)
      {
        lots = TradeSize(reqtype);
        sltp = CalculateSLTP(lots);
        req_price = RequestPrice(reqtype);
        tp = NormalizeDouble(req_price-(sltp*TPvsSLRatio),_Digits);
        sl = NormalizeDouble(req_price+sltp,_Digits);
      }
    //--
    string comm;
    string entry = "entry_in";
    string ortype = ReturnsOrderType(type_req);
    if(MagicNumber!=0)
      {comm = expname+" :#"+ortype+"# "+string(MagicNumber);}
    else {comm = expname+" :#"+ortype+"#";}
    slip=SymbolInfoInteger(_Symbol,SYMBOL_SPREAD);
    //--
    MqlTradeRequest req={0};
    MqlTradeResult  res={0};
    //--
    req.action      = action_req; 
    req.symbol      = _Symbol; 
    req.magic       = MagicNumber; 
    req.volume      = lots;
    req.type        = type_req; 
    req.price       = req_price;
    req.sl          = sl;
    req.tp          = tp;
    req.deviation   = slip; 
    req.comment     = comm;
    //--
    bool success=OrderSend(req,res);
    int error=GetLastError();
    int answer=(int)res.retcode;
    int ticket=(int)res.order;
    if(OrderSendResult(reqtype,error,answer,ticket,entry))
      {
        PlaySound("gun.wav.");
      }
    //--
//----
    return;
  } //-end OpenPosition()
//---------//

//+------------------------------------------------------------------+
//|                  modify positions trailing stops                 |
//+------------------------------------------------------------------+
void ModifySLTP()
  { 
//---
    //--
//--- declare and initialize the trade request and result of trade request
    MqlTradeRequest req={0};
    MqlTradeResult  res={0};
    MqlTradeCheckResult check={0};
    int total = PositionsTotal(); // number of open positions
    double vtp  = NormalizeDouble(Trailing_FixedPipsSL*TPvsSLRatio*pip,_Digits);
    double vts  = NormalizeDouble(Trailing_FixedPipsSL*pip,_Digits);
    //--
 //--- iterate over all open positions
    for(int i=total-1; i>=0; i--)
      {
        //--- parameters of the order
        ulong  position_ticket = PositionGetTicket(i);               // ticket of the position
        string position_symbol = PositionGetString(POSITION_SYMBOL); // symbol 
        ulong  magic           = PositionGetInteger(POSITION_MAGIC); // MagicNumber of the position
        //---
        if(position_symbol==_Symbol && (magic==MagicNumber||magic==NULL))
          { 
            //--- calculate the current price levels
            double price            = PositionGetDouble(POSITION_PRICE_OPEN);
            double bid              = SymbolInfoDouble(position_symbol,SYMBOL_BID);
            double ask              = SymbolInfoDouble(position_symbol,SYMBOL_ASK);
            double sl               = PositionGetDouble(POSITION_SL);     // Stop Loss of the position
            double tp               = PositionGetDouble(POSITION_TP);     // Take Profit of the position
            int freeze_level        = m_symbol.FreezeLevel(); // Freeze Level distance
            int stop_freeze_level   = freeze_level<=0 ? 15 : freeze_level+=5;
            double price_level      = stop_freeze_level*pip;
            ENUM_POSITION_TYPE type = (ENUM_POSITION_TYPE)PositionGetInteger(POSITION_TYPE);  // type of the position
            //--- calculation and rounding of the Stop Loss and Take Profit values
            if(vts<price_level) 
              {
                vts = price_level;
                vtp = vts*TPvsSLRatio;
              }
            //--
            if(type==POSITION_TYPE_BUY)
              {
                if((bid-price>vts)&&(tp-ask>price_level))
                  {
                    if(sl<bid-vts)
                      {
                        sl = NormalizeDouble(bid-vts,_Digits);
                        if(ask+vtp>tp) tp = NormalizeDouble(ask+vtp,_Digits);
                        //--- setting the operation parameters
                        req.action     = TRADE_ACTION_SLTP; // type of trade operation
                        req.position   = position_ticket;   // ticket of the position
                        req.symbol     = position_symbol;   // symbol 
                        req.sl         = sl;                // Stop Loss of the position
                        req.tp         = tp;                // Take Profit of the position
                        req.magic      = MagicNumber;       // MagicNumber of the position
                        //--- output information about the modification
                        PrintFormat("Modify #%I64d %s %s",position_ticket,position_symbol,EnumToString(type));
                        //--- send the request
                        bool success=OrderSend(req,res);
                        if(!success)
                          {
                            m_forex.CheckResult(check);
                            Print("PositionModify() ",EnumToString(type)," ",_Symbol," FAILED!!. Return code= "
                                  ,m_forex.ResultRetcode(), ". Code description: [",m_forex.ResultRetcodeDescription(),"]");
                          }
                        break;
                      }
                  }
              }
            //--
            if(type==POSITION_TYPE_SELL)
              {
                if((price-ask>vts)&&(bid-tp>price_level))
                  {
                    if(sl>ask+vts)
                      {
                        sl = NormalizeDouble(ask+vts,_Digits);
                        if(bid-vtp<tp) tp = NormalizeDouble(bid-vtp,_Digits);
                        //--- setting the operation parameters
                        req.action     = TRADE_ACTION_SLTP; // type of trade operation
                        req.position   = position_ticket;   // ticket of the position
                        req.symbol     = position_symbol;   // symbol 
                        req.sl         = sl;                // Stop Loss of the position
                        req.tp         = tp;                // Take Profit of the position
                        req.magic      = MagicNumber;       // MagicNumber of the position
                        //--- output information about the modification
                        PrintFormat("Modify #%I64d %s %s",position_ticket,position_symbol,EnumToString(type));
                        //--- send the request
                        bool success=OrderSend(req,res);
                        if(!success)
                          {
                            m_forex.CheckResult(check);
                            Print("PositionModify() ",EnumToString(type)," ",_Symbol," FAILED!!. Return code= "
                                 ,m_forex.ResultRetcode(), ". Code description: [",m_forex.ResultRetcodeDescription(),"]");
                          }
                        break;
                      }
                  }
              }
          }
      }
//----
  } //-end ModifySLTP()
//---------//    

//+------------------------------------------------------------------+
//|              Close positions by opposite positions               |
//+------------------------------------------------------------------+
void ClosePosition(const int popstype)
   {
 //---
    //---
    ResetLastError();
    //--
    int total=PositionsTotal(); // number of open positions
    ENUM_POSITION_TYPE closetype = ENUM_POSITION_TYPE(PositionGetInteger(POSITION_TYPE));
    ENUM_ORDER_TYPE type_req=0;
    //--
    string ortype;
    int reqtype=0;
    //--
    if(popstype==0) 
      {
        reqtype=popstype+1;
        type_req  = GetOrderType(popstype+1);
        ortype=ReturnsOrderType(popstype+1);
        closetype = POSITION_TYPE_BUY;
      }
    if(popstype==1)
      {
        reqtype=popstype-1;
        type_req  = GetOrderType(popstype-1);
        ortype=ReturnsOrderType(popstype-1);
        closetype = POSITION_TYPE_SELL;
      }
    //--
    string entry = "entry_out";
    string comm = expname+" :#"+ortype+"#Out";
    slip=SymbolInfoInteger(_Symbol,SYMBOL_SPREAD);
    //--
    MqlTradeRequest req={0};
    MqlTradeResult  res={0};
    //---
    //--- iterate over all open positions
    for(int i=total-1; i>=0; i--)
      {
        //--- parameters of the order
        string position_Symbol   = PositionGetSymbol(i);                // symbol of the position
        ulong  magic             = PositionGetInteger(POSITION_MAGIC);  // MagicNumber of the position
        ENUM_POSITION_TYPE  type = (ENUM_POSITION_TYPE)PositionGetInteger(POSITION_TYPE);
        //--- if the MagicNumber matches
        if((position_Symbol==_Symbol)&&(magic==MagicNumber))
          { 
            //--
            if(type==closetype)
              {
                //--
                ulong  position_ticket = PositionGetTicket(i);  // ticket of the the opposite position
                double volume          = PositionGetDouble(POSITION_VOLUME);                                 
                //--- set the position_ticket of the opposite position
                req.action    = TRADE_ACTION_DEAL;  // type of trade operation
                req.position  = position_ticket;    // ticket of the position
                req.symbol    = position_Symbol;    // symbol 
                req.volume    = volume;             // volume of the position
                req.sl        = 0.0;                // stop loss
                req.tp        = 0.0;                // take profit
                req.deviation = slip;               // allowed deviation from the price
                req.magic     = MagicNumber;        // MagicNumber of the position
                req.comment   = comm;               // comment
                //--- set the price and order type depending on the position type
                if(type==POSITION_TYPE_BUY)
                  {
                    req.price = RequestPrice(1);
                    req.type  = type_req;
                  }
                if(type==POSITION_TYPE_SELL)
                  {
                    req.price = RequestPrice(0);
                    req.type  = type_req;
                  }
                //--- output information about the closure
                PrintFormat("Close #%I64d %s %s",position_ticket,position_Symbol,EnumToString(type));
                //--- send the request
                bool success=OrderSend(req,res);
                int error=GetLastError();
                int answer=(int)res.retcode;
                int ticket=(int)res.order;
                if(OrderSendResult(reqtype,error,answer,ticket,entry)) 
                  {
                     PlaySound("gun.wav.");
                  }
                //---
              }
            //--
          }
      }
   //---
   return;
//----
   } //-end ClosePosition()
//---------//

void CheckOpenPosition()
   {
//----
    //--
    ResetLastError();
    //--
    opnB=0;
    opnS=0;
    //--
    long pos_magic=0;
    string position_symbol;
    //---
    int total=PositionsTotal();
    if(PositionSelect(_Symbol))
      {
        for(int i=total-1; i>=0; i--)
          {
            position_symbol = PositionGetSymbol(i);
            if(position_symbol==_Symbol)
              {
                 pos_magic = PositionGetInteger(POSITION_MAGIC);
                 if(pos_magic==MagicNumber)
                   {
                     ENUM_POSITION_TYPE opstype=ENUM_POSITION_TYPE(PositionGetInteger(POSITION_TYPE));
                     //--
                     if(opstype == POSITION_TYPE_BUY) opnB++;
                     if(opstype == POSITION_TYPE_SELL) opnS++;
                   }
              }
          }
      }
    //---
    return;
//----
   } //-end CheckOpenPosition()
//---------//

datetime CheckOrderCloseTime()
   {
//---
    //--
    ResetLastError();
    //--
    datetime from_date=7*PeriodSeconds(PERIOD_D1); // from the 7 days ago
    datetime to_date=TimeCurrent();                // till the current moment 
    //---
    HistorySelect(from_date,to_date); 
    //--- total number in the list of deals 
    int deals=HistoryDealsTotal();
    int order=HistoryOrdersTotal();
    //---
    datetime close_time=0;    // time of a deal execution
    ulong deal_ticket;        // deal ticket 
    int deal_entry=0;         // enum deal entry
    long deal_ID=0;           // position ID
    double deal_close=0;      // deal CLOSE price
    double deal_profit=0;     // deal profit
    double deal_swap=0;       // deal swap
    double deal_comm=0;       // deal commission
    string deal_symbol;       // symbol of the deal
    //--
    ulong order_ticket=0;     // ticket of the order the deal
    string order_symbol;      // OPEN order symbol 
    long order_magic;         // OPEN order magic
    long order_ID=0;          // OPEN order ID
    int  order_type=0;        // OPEN order type
    string ortype;
    //---
    //--
    if(order>0)
      {
        for(int x=order-1; x>=0; x--)
          {
            order_ticket = HistoryOrderGetTicket(x);
            order_symbol = HistoryOrderGetString(order_ticket,ORDER_SYMBOL);
            order_magic  = HistoryOrderGetInteger(order_ticket,ORDER_MAGIC); 
            //--
            if((order_symbol==_Symbol)&&(order_magic==MagicNumber))
              {
                order_ID    = HistoryOrderGetInteger(order_ticket,ORDER_POSITION_ID);
                order_type  = (int)HistoryOrderGetInteger(order_ID,ORDER_TYPE);
                ortype      = ReturnsOrderType(order_type);
                break;
              }
          }
        //--
        int y=0;
        while(y<1)
          {
            for(int z=deals-1; z>=0; z--)
              {
                deal_ticket = HistoryDealGetTicket(z); 
                deal_symbol = HistoryDealGetString(deal_ticket,DEAL_SYMBOL);
                deal_entry  = (int)HistoryDealGetInteger(deal_ticket,DEAL_ENTRY);
                deal_ID     = HistoryDealGetInteger(deal_ticket,DEAL_POSITION_ID);
                //--
                if(deal_symbol==order_symbol && deal_ID==order_ID)
                  {
                    if((deal_entry==DEAL_ENTRY_OUT)||(deal_entry==DEAL_ENTRY_OUT_BY))
                      {
                        deal_close  = HistoryDealGetDouble(deal_ticket,DEAL_PRICE);
                        close_time  = (datetime)HistoryDealGetInteger(deal_ticket,DEAL_TIME);
                        deal_comm   = HistoryDealGetDouble(deal_ticket,DEAL_COMMISSION);
                        deal_swap   = HistoryDealGetDouble(deal_ticket,DEAL_SWAP);
                        deal_profit = HistoryDealGetDouble(deal_ticket,DEAL_PROFIT);
                        break;
                      }
                  }
              }
           y++;
          }
      }
    //--
    if((close_time>0)&&(close_time!=closetime))
      {
        double profit_loss = deal_profit+deal_comm+deal_swap;
        if(profit_loss>0) 
          {
            Print("Close Order ",ortype," ",_Symbol," at price : ",DoubleToString(deal_close,_Digits),
                  " OrderCloseTime(): ",TimeToString(close_time,TIME_DATE|TIME_MINUTES)," in profit : ",DoubleToString(profit_loss,2)); 
            PlaySound("ping.wav");
          }
        if(profit_loss<=0) 
          {
            Print("Close Order ",ortype," ",_Symbol," at price : ",DoubleToString(deal_close,_Digits),
                  " OrderCloseTime(): ",TimeToString(close_time,TIME_DATE|TIME_MINUTES)," in loss : ",DoubleToString(profit_loss,2)); 
            PlaySound("ping.wav");
          }
        //--
      }
    //--
    closetime=close_time; 
    //---
    return(close_time);
//----
   } //-end CheckOrderCloseTime()
//---------//

int CloseOrderProfit()
   {
//---
    //--
    ResetLastError();
    //--
    int cls=0;
    //--
    double pos_profit = 0.0;
    double pos_swap   = 0.0;
    double pos_comm   = 0.0;
    double cur_profit = 0.0;
    //--
    if(PositionSelect(_Symbol))
      {
        pos_profit = PositionGetDouble(POSITION_PROFIT);
        pos_swap   = PositionGetDouble(POSITION_SWAP);
        pos_comm   = m_pos_info.Commission();
        cur_profit = pos_profit+pos_swap+pos_comm;
      }
    //--
    int stepud=StpUpDn();
    //--
    if((opnB>0)&&(stepud==-1)&&(cur_profit>0.0)) cls=1;
    if((opnS>0)&&(stepud==1)&&(cur_profit>0.0)) cls=-1;
    //---
    return(cls);
//----
   } //-end CloseOrderProfit()
//---------//

//+-------------------------------------------------------------------------+
//|                             Money Managment                             |   
//+-------------------------------------------------------------------------+   
double TradeSize(const int opstype) 
  {
    //---
    ResetLastError();
    double size=0.0;
    //--
    ENUM_ORDER_TYPE ortype=GetOrderType(opstype);
    int Opentotal=PositionsTotal()+1 < 5 ? 5 : PositionsTotal()+1;
    //--
    double lots_min  = SymbolInfoDouble(_Symbol,SYMBOL_VOLUME_MIN);
    double lots_max  = SymbolInfoDouble(_Symbol,SYMBOL_VOLUME_MAX);
    double cont_size = SymbolInfoDouble(_Symbol,SYMBOL_TRADE_CONTRACT_SIZE);
    double lots_step = SymbolInfoDouble(_Symbol,SYMBOL_VOLUME_STEP);
    long   leverage  = AccountInfoInteger(ACCOUNT_LEVERAGE);
    //--
    double final_account = MathMin(AccountInfoDouble(ACCOUNT_BALANCE),AccountInfoDouble(ACCOUNT_EQUITY));
    int lot_Digits = 0;
    double lots    = 0.0;
    //--
    if(lots_step == 0.01) {lot_Digits = 2;}
    if(lots_step == 0.1)  {lot_Digits = 1;}
    //--
    lots = (final_account*(RiskPercentage/100.0))/(cont_size/leverage);
    lots = NormalizeDouble(lots/Opentotal,lot_Digits);
    //--
    if (lots < lots_min) {lots = lots_min;}
    if (lots > lots_max) {lots = lots_max;}
    //--
    if(MoneyManagementType == 1) {size=lots;}
    else {size=TradeLotSize;}
    //--
    //---
    double cprice=0.0;
    //--
    if(ortype==ORDER_TYPE_BUY)  {cprice = NormalizeDouble(SymbolInfoDouble(_Symbol,SYMBOL_ASK),_Digits);}
    //--
    if(ortype==ORDER_TYPE_SELL) {cprice = NormalizeDouble(SymbolInfoDouble(_Symbol,SYMBOL_BID),_Digits);}
    //--
    string sType=ReturnsOrderType(ortype);
    //--
    if((m_account.FreeMarginCheck(_Symbol,ortype,size,cprice)<=0)||(GetLastError()==10019))
      {
        Print("--Warning----OPEN NEW "+sType+" ORDER MARKET for ",_Symbol," Lots size "+DoubleToString(lots,lot_Digits)+" - NOT ENOUGH MONEY..!");
        Alert("--Warning----OPEN NEW "+sType+" ORDER MARKET for ",_Symbol," Lots size "+DoubleToString(lots,lot_Digits)+" - NOT ENOUGH MONEY..!");
        ResetLastError();
        NoMoney=true;
        NoOrder=true;
        return(-1);
      }
    else {NoMoney=false; NoOrder=false; return(size);}
    //---
//----
  }
//---------//

//+------------------------------------------------------------------+
//|          Calculate optimal Take Profit & Stop Loss size          |
//+------------------------------------------------------------------+
double CalculateSLTP(double lots) 
  {
    //---
    ResetLastError();
    double valst=0.0;
    //--
    double balance       = MathMin(AccountInfoDouble(ACCOUNT_BALANCE),AccountInfoDouble(ACCOUNT_EQUITY));
    double moneyrisk     = balance*RiskPercentage/100;
    double sprd          = (double)SymbolInfoInteger(Symbol(),SYMBOL_SPREAD);
    double point         = SymbolInfoDouble(_Symbol,SYMBOL_POINT);
    double ticksize      = SymbolInfoDouble(_Symbol,SYMBOL_TRADE_TICK_SIZE);
    long   leverage      = AccountInfoInteger(ACCOUNT_LEVERAGE);
    double leveragerisk  = NormalizeDouble(leverage/100,0);
    double tickvalue     = ValNotZero(SymbolInfoDouble(_Symbol,SYMBOL_TRADE_TICK_VALUE));
    double tickvaluesize = tickvalue*point/ticksize;
    if(leveragerisk<1.0) leveragerisk = 1.0;
    //--
    double sltp = (moneyrisk/(lots*tickvaluesize)/leveragerisk)+sprd;
    //--
    valst = NormalizeDouble(sltp*point,_Digits);
    if(valst<38*pip)
       valst=NormalizeDouble(38*pip,_Digits);
    //--
    return(valst);
    //---
//---
  }
//---------//

int StpUpDn() // StepUpDown code
  {
//---
    //--
    int trn=0,
        mlHi=0,
        mlLo=0;
    int i,j,pr=9;
    //--
    if(!FillArrayBuffer(sma01ar,0,sma01h,ebar)) return(0);
    if(!FillArrayBuffer(mma30ar,0,mma30h,ebar)) return(0);
    //--
    bool ups=false;
    bool dns=false;
    //--
    double divma520=0;
    double divma521=0;
    double MAHi=sma01ar[1];
    double MALo=sma01ar[1];
    //--
    for(i=pr; i>=0; i--)
      {
       if(sma01ar[i]>MAHi) {MAHi=sma01ar[i]; mlHi=i;}
       if(sma01ar[i]<MALo) {MALo=sma01ar[i]; mlLo=i;}
       divma520=sma01ar[i]-mma30ar[i];
       divma521=sma01ar[i+1]-mma30ar[i+1];
      }
    //---
    ENUM_TIMEFRAMES tf=TimeFrames==PERIOD_CURRENT ? _Period : TimeFrames;
    cbar=Bars(_Symbol,tf);
    int pre=cbar!=pbar ? ebar : pr;
    //--
    for(i=0,j=pre-1; i<pre; i++,j--)
      {
        //--
        if((mlHi>mlLo)&&(sma01ar[j]>MALo))
           {ups=true; dns=false;}
        if((mlHi>mlLo)&&(divma520<divma521))
           {dns=true; ups=false;}
        if((mlHi<mlLo)&&(sma01ar[j]<MAHi))
           {dns=true; ups=false;}
        if((mlHi<mlLo)&&(divma520>divma521))
           {ups=true; dns=false;}
        //--
      }
    //--
    pbar=cbar;
    //--
    if(ups==true) {trn=1;}
    if(dns==true) {trn=-1;}
    //---
    return(trn);
//---
  } //-end StpUpDn()
//---------//

int ATRStops(void) // ATR stop code
  {
    //---
    int i,br,
        vatr=0,
        xmin=-1,
        xmax=fabs(xmin);
    //--
    double min=-100000,
           max=fabs(min);
    //--
    int period=TimeFrames==PERIOD_CURRENT ? ReverseTF(PERIOD_CURRENT) : ReverseTF(TimeFrames);
    if(!FillArrayBuffer(atrb,0,atrhandle,ebar)) return(0);
    if(!FillArraysOHLC(period,ebar)) return(0);
    //--
    int bar=ebar-length-1;
    //--
    for(br=bar; br>=0; br--)
      {
       //---
       smin[br]=min; 
       smax[br]=max;
       //--
       for(i=length-1; i>=0; i--)
         {
          int brx=br+i;
          smin[br]=fmax(smin[br],HIGH[brx]-(Kv*atrb[brx]));
          smax[br]=fmin(smax[br],LOW[brx]+(Kv*atrb[brx]));
         }
       //--
       atrpos[br]=atrpos[br+1];
       if(CLOSE[br]>smax[br+1]) atrpos[br]=xmax;
       if(CLOSE[br]<smin[br+1]) atrpos[br]=xmin;
       //---
       if(atrpos[br]>0)
         {
          if(smin[br]<smin[br+1]) smin[br]=smin[br+1];
            sideup[br]=smin[br];
            sidedn[br]=0.0;
         }
       //--
       if(atrpos[br]<0)
         {
          if(smax[br]>smax[br+1]) smax[br]=smax[br+1];
            sidedn[br]=smax[br];
            sideup[br]=0.0;
         }
       //---
      }
    //--
    if((sidedn[1]>0.0)&&(sidedn[0]==0.0)&&(sideup[0]>0.0)) vatr=1;
    if((sideup[1]>0.0)&&(sideup[0]==0.0)&&(sidedn[0]>0.0)) vatr=-1;
    //Print("sideup Value : ",DoubleToString(sideup[0],_Digits));
    //Print("sidedn Value : ",DoubleToString(sidedn[0],_Digits));
    //Print("ATR Value : ",string(vatr));
    //--
    return(vatr);
    //---
  } //-end ATRStops()
//---------//

int tradeFX()
  {
//---
    //--
    int tfx=0;
    //--
    int stepud=StpUpDn();
    int atrval=ATRStops();
    //--
    if((atrval==1)&&(stepud==1)) tfx=1;
    if((atrval==-1)&&(stepud==-1)) tfx=-1;
    //---
    return(tfx);
//---
  } //-end tradeFX()
//---------//

//+------------------------------------------------------------------+
//| Filling indicator buffers from the MA indicator                  |
//+------------------------------------------------------------------+
bool FillArrayBuffer(double &values[], // values of indicator buffer to fill
                      int shift,        // shift
                      int ind_handle,   // handle of the iMA indicator
                      int amount)       // number of copied values
  {
//--- reset error code
   ResetLastError();
//--- fill a part of the iMABuffer array with values from the indicator buffer that has 0 index
   if(CopyBuffer(ind_handle,shift,0,amount,values)<1)
     {
       //--- if the copying fails, tell the error code
       PrintFormat("Failed to copy data from the iMA indicator, error code %d",GetLastError());
       //--- quit with zero result - it means that the indicator is considered as not calculated
       return(false);
     }
//--- everything is fine
   return(true);
  }
//---------//

//+------------------------------------------------------------------+ 
//| Filling price buffers                                            | 
//+------------------------------------------------------------------+ 
bool FillArraysOHLC(int period,         // timeframes
                    int amount)         // number of copied values
  { 
//--- reset error code 
   ResetLastError();
   //--
   ENUM_TIMEFRAMES timeframe=TFmt5(period);
   //--
//--- fill a part of the OHLC array with price values 
   if(CopyOpen(_Symbol,timeframe,0,amount,OPEN)<1)
     { 
       //--- if the copying fails, tell the error code 
       PrintFormat("Failed to copy data OPEN price, error code %d",GetLastError());
       //--- quit with zero result
       return(false); 
     } 
    //--
   if(CopyHigh(_Symbol,timeframe,0,amount,HIGH)<1)
     { 
       //--- if the copying fails, tell the error code 
       PrintFormat("Failed to copy data HIGH price, error code %d",GetLastError());
       //--- quit with zero result
       return(false); 
     } 
    //--
   if(CopyLow(_Symbol,timeframe,0,amount,LOW)<1)
     { 
       //--- if the copying fails, tell the error code 
       PrintFormat("Failed to copy data LOW price, error code %d",GetLastError());
       //--- quit with zero result
       return(false); 
     } 
    //--
   if(CopyClose(_Symbol,timeframe,0,amount,CLOSE)<1)
     { 
       //--- if the copying fails, tell the error code 
       PrintFormat("Failed to copy data CLOSE price, error code %d",GetLastError());
       //--- quit with zero result
       return(false); 
     } 
    //--
//--- everything is fine 
   return(true); 
  }
//---------//

//+------------------------------------------------------------------+
//|                      Switch Time Frames                          |   
//+------------------------------------------------------------------+  
ENUM_TIMEFRAMES TFmt5(int tf)
  {
//----
    switch(tf)
     {
      case 0:     return(PERIOD_CURRENT);
      case 1:     return(PERIOD_M1);
      case 5:     return(PERIOD_M5);
      case 15:    return(PERIOD_M15);
      case 30:    return(PERIOD_M30);
      case 60:    return(PERIOD_H1);
      case 240:   return(PERIOD_H4);
      case 1440:  return(PERIOD_D1);
      case 10080: return(PERIOD_W1);
      case 43200: return(PERIOD_MN1);
      default:    return(PERIOD_CURRENT);
     }
//----
  }
//---------//

int ReverseTF(ENUM_TIMEFRAMES TF)
  {
//----
   int val=0;
   //--
   switch(TF)
     {
       //--
       case (PERIOD_CURRENT): val=0; break;
       case (PERIOD_M1):      val=1; break;
       case (PERIOD_M5):      val=5; break;
       case (PERIOD_M15):     val=15; break;
       case (PERIOD_M30):     val=30; break;
       case (PERIOD_H1):      val=60; break;
       case (PERIOD_H4):      val=240; break;
       case (PERIOD_D1):      val=1440; break;
       case (PERIOD_W1):      val=10080; break;
       case (PERIOD_MN1):     val=43200; break;
       //--
     }
   return(val);
//----
  }
//---------//

ENUM_ORDER_TYPE GetOrderType(int type)
  { 
//----
   ENUM_ORDER_TYPE or_type=(ENUM_ORDER_TYPE)OrderGetInteger(ORDER_TYPE);
   switch(type) 
     { 
      case 0: or_type=ORDER_TYPE_BUY; break;
      case 1: or_type=ORDER_TYPE_SELL; break;
      case 2: or_type=ORDER_TYPE_BUY_LIMIT; break;
      case 3: or_type=ORDER_TYPE_SELL_LIMIT; break;
      case 4: or_type=ORDER_TYPE_BUY_STOP; break;
      case 5: or_type=ORDER_TYPE_SELL_STOP; break;
      case 6: or_type=ORDER_TYPE_BUY_STOP_LIMIT; break;
      case 7: or_type=ORDER_TYPE_SELL_STOP_LIMIT; break;
      case 8: or_type=ORDER_TYPE_CLOSE_BY; break;
     } 
   return(or_type);
//----
  }
//---------//

string ReturnsOrderType(int ordtype)
  { 
   string str_type;
   switch(ordtype) 
     { 
      case (ORDER_TYPE_BUY):             str_type="BUY"; break;
      case (ORDER_TYPE_SELL):            str_type="SELL"; break; 
      case (ORDER_TYPE_BUY_LIMIT):       str_type="BUY LIMIT"; break; 
      case (ORDER_TYPE_SELL_LIMIT):      str_type="SELL LIMIT"; break; 
      case (ORDER_TYPE_BUY_STOP):        str_type="BUY STOP"; break; 
      case (ORDER_TYPE_SELL_STOP):       str_type="SELL STOP"; break; 
      case (ORDER_TYPE_BUY_STOP_LIMIT):  str_type="BUY STOP LIMIT"; break; 
      case (ORDER_TYPE_SELL_STOP_LIMIT): str_type="SELL STOP LIMIT"; break;
      case (ORDER_TYPE_CLOSE_BY):        str_type="ORDER CLOSE BY"; break;
     } 
   return(str_type);
//----
  }
//---------//

ENUM_TRADE_REQUEST_ACTIONS TradeAction(int action)
  { 
//----
   switch(action) 
     { 
      case 0:  return(TRADE_ACTION_DEAL);
      case 1:  return(TRADE_ACTION_PENDING);
      case 2:  return(TRADE_ACTION_SLTP);
      case 3:  return(TRADE_ACTION_MODIFY);
      case 4:  return(TRADE_ACTION_REMOVE);
      case 5:  return(TRADE_ACTION_CLOSE_BY);
      default: return(TRADE_ACTION_DEAL);
     }
//----
  }
//---------//

double RequestPrice(const int ordtype)
  { 
//----
   int distance=20;
   double price=0.0;
   double pricetmp=0.0;
   ENUM_ORDER_TYPE type=GetOrderType(ordtype);
   m_symbol.RefreshRates();
   //--
   switch(type) 
     { 
       case (ORDER_TYPE_BUY):             price=NormalizeDouble(SymbolInfoDouble(_Symbol,SYMBOL_ASK),_Digits); break;
       case (ORDER_TYPE_SELL):            price=NormalizeDouble(SymbolInfoDouble(_Symbol,SYMBOL_BID),_Digits); break;
       case (ORDER_TYPE_BUY_LIMIT):       
                                          {
                                            pricetmp=SymbolInfoDouble(_Symbol,SYMBOL_BID)-distance*pip;
                                            price=NormalizeDouble(pricetmp,_Digits);  break; 
                                          }
       case (ORDER_TYPE_SELL_LIMIT):      
                                          {
                                            pricetmp=SymbolInfoDouble(_Symbol,SYMBOL_ASK)+distance*pip;
                                            price=NormalizeDouble(pricetmp,_Digits); break;
                                          }
       case (ORDER_TYPE_BUY_STOP):       
                                          {
                                            pricetmp=SymbolInfoDouble(_Symbol,SYMBOL_ASK)+distance*pip;
                                            price=NormalizeDouble(pricetmp,_Digits); break;  
                                          }
       case (ORDER_TYPE_SELL_STOP):       
                                          {
                                            pricetmp=SymbolInfoDouble(_Symbol,SYMBOL_BID)-distance*pip;
                                            price=NormalizeDouble(pricetmp,_Digits); break;
                                          }
     }
   //---
   return(price);
//----
  }
//---------//

bool OrderSendResult(int tradeops,
                     int error,
                     int rescode,
                     int ticket,
                     string r_entry)
  { 
//--- reset the last error code to zero 
   ResetLastError();
   //--
   ENUM_ORDER_TYPE ortype=GetOrderType(tradeops);
   string ost=ReturnsOrderType(ortype);
   //---
   if(rescode==10009 || rescode==10008) //Request is completed or order placed
     {
       Print("-- Opened New "+ost+" ORDER for ",_Symbol," ticket: ",ticket," ",r_entry,", - OK!");
     }
   else
     { 
      //--
      Print("TradeLog: Trade request failed. Error = ",error); 
      switch(rescode) 
        { 
         //--- requote 
         case 10004: 
           { 
            Print("TRADE_RETCODE_REQUOTE"); 
            break; 
           } 
         //--- order is not accepted by the server 
         case 10006: 
           { 
            Print("TRADE_RETCODE_REJECT");
            break; 
           } 
         //--- invalid price 
         case 10015: 
           { 
            Print("TRADE_RETCODE_INVALID_PRICE");
            break; 
           } 
         //--- invalid SL and/or TP 
         case 10016: 
           { 
            Print("TRADE_RETCODE_INVALID_STOPS"); 
            break; 
           } 
         //--- invalid volume 
         case 10014: 
           { 
            Print("TRADE_RETCODE_INVALID_VOLUME");
            break; 
           } 
         //--- not enough money for a trade operation  
         case 10019: 
           { 
            Print("TRADE_RETCODE_NO_MONEY");
            break; 
           } 
         //--- some other reason, output the server response code  
         default: 
           { 
            Print("Other answer = ",rescode);
           } 
        } 
      //--- notify about the unsuccessful result of the trade request by returning false
       return(false); 
     } 
//--- OrderSendResult() returns true -
   return(true);
//----
   } //-end OrderSendResult()
//---------//

double ValNotZero(double not_zero)
  {
    if(not_zero==0) return(NormalizeDouble(1/100000,_Digits));
    else return(not_zero);
  }
//---------//

double iOpen(string symbol,
             int tf,
             int index)
  {
    if(index < 0) return(-1);
    double Arr[];
    ENUM_TIMEFRAMES timeframe=TFmt5(tf);
    if(CopyOpen(symbol,timeframe,index,1,Arr)>0)
    return(Arr[0]);
    else return(-1);
  }
//---------//

double iHigh(string symbol,
             int tf,
             int index)
  {
    if(index < 0) return(-1);
    double Arr[];
    ENUM_TIMEFRAMES timeframe=TFmt5(tf);
    if(CopyHigh(symbol,timeframe,index,1,Arr)>0)
    return(Arr[0]);
    else return(-1);
  }
//---------//

double iLow(string symbol,
            int tf,
            int index)
  {
    if(index < 0) return(-1);
    double Arr[];
    ENUM_TIMEFRAMES timeframe=TFmt5(tf);
    if(CopyLow(symbol,timeframe,index,1,Arr)>0)
    return(Arr[0]);
    else return(-1);
  }
//---------//

double iClose(string symbol,
              int tf,
              int index)
  {
    if(index < 0) return(-1);
    double Arr[];
    ENUM_TIMEFRAMES timeframe=TFmt5(tf);
    if(CopyClose(symbol,timeframe,index,1,Arr)>0)
    return(Arr[0]);
    else return(-1);
  }
//---------//

datetime iTime(string symbol,
               int tf,
               int index)
  {
    if(index < 0) return(-1);
    datetime Arr[];
    ENUM_TIMEFRAMES timeframe=TFmt5(tf);
    if(CopyTime(symbol,timeframe,index,1,Arr)>0)
    return(Arr[0]);
    else return(-1);
  }
//---------//

enum TimeReturn
  {
    year        = 0,   // Year 
    mon         = 1,   // Month 
    day         = 2,   // Day 
    hour        = 3,   // Hour 
    min         = 4,   // Minutes 
    sec         = 5,   // Seconds 
    day_of_week = 6,   // Day of week (0-Sunday, 1-Monday, ... ,6-Saturday) 
    day_of_year = 7    // Day number of the year (January 1st is assigned the number value of zero) 
  };
//---------//

int MqlReturnDateTime(datetime reqtime,
                      const int mode) 
  {
    MqlDateTime mqltm;
    TimeToStruct(reqtime,mqltm);
    int valdate=0;
    //--
    switch(mode)
      {
        case 0: valdate=mqltm.year; break;        // Return Year 
        case 1: valdate=mqltm.mon;  break;        // Return Month 
        case 2: valdate=mqltm.day;  break;        // Return Day 
        case 3: valdate=mqltm.hour; break;        // Return Hour 
        case 4: valdate=mqltm.min;  break;        // Return Minutes 
        case 5: valdate=mqltm.sec;  break;        // Return Seconds 
        case 6: valdate=mqltm.day_of_week; break; // Return Day of week (0-Sunday, 1-Monday, ... ,6-Saturday) 
        case 7: valdate=mqltm.day_of_year; break; // Return Day number of the year (January 1st is assigned the number value of zero) 
      }
    return(valdate);
  }
//---------//
//+----------------------------------------------------------------------------+