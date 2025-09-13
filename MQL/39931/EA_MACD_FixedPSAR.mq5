//+------------------------------------------------------------------+
//|                                            EA_MACD_FixedPSAR.mq5 |
//|                                     Copyright 2022, Yossy Nakata |
//|                                           yossy-nakata@gmail.com |
//+------------------------------------------------------------------+
#property copyright "Copyright 2022, Yossy Nakata"
#property link      "yossy-nakata@gmail.com"
#property version     "1.00"

#define MACD_MAGIC 1234321
//---
#include <Trade\Trade.mqh>
#include <Trade\SymbolInfo.mqh>
#include <Trade\PositionInfo.mqh>
#include <Trade\AccountInfo.mqh>

//---
enum ENUM_TRAILING_MODE
  {
   None  = 0, // None
   Fixed  = 1, // Trailing Fixed
   FixedPSAR =  2, // Trailing Fixed PSAR
  };
//---
input double InpLots          =1.0; // Lots
input int    InpTakeProfit    =200;  // Take Profit (in pips)
input int    InpStopLoss      =50;  // Stop Loss (in pips)

input ENUM_TRAILING_MODE    InpTrailngMode    = ENUM_TRAILING_MODE::FixedPSAR;  // Trailing Mode
input int    InpTrailingStop  =30;  // Trailing Stop Level (in pips)
input double InpPSAR_Step   =0.02; // Trailing ParabolicSAR Step
input double InpPSAR_Maximum=0.2;  // Trailing ParabolicSAR Maximum
input int    InpMACDOpenLevel =3;   // MACD open level (in pips)
input int    InpMACDCloseLevel=2;   // MACD close level (in pips)
input int    InpMATrendPeriod =26;  // MA trend period
//---
int ExtTimeOut=10; // time out in seconds between trade operations
//+------------------------------------------------------------------+
//| MACD Sample expert class                                         |
//+------------------------------------------------------------------+
class CSampleExpert
  {
protected:
   double            m_adjusted_point;             // point value adjusted for 3 or 5 points
   CTrade            m_trade;                      // trading object
   CSymbolInfo       m_symbol;                     // symbol info object
   CPositionInfo     m_position;                   // trade position object
   CAccountInfo      m_account;                    // account info wrapper
   //--- indicators
   int               m_handle_macd;                // MACD indicator handle
   int               m_handle_ema;                 // moving average indicator handle
   //--- indicator buffers
   double            m_buff_MACD_main[];           // MACD indicator main buffer
   double            m_buff_MACD_signal[];         // MACD indicator signal buffer
   double            m_buff_EMA[];                 // EMA indicator buffer
   //--- indicator data for processing
   double            m_macd_current;
   double            m_macd_previous;
   double            m_signal_current;
   double            m_signal_previous;
   double            m_ema_current;
   double            m_ema_previous;
   MqlRates          m_last_bar;
   //---
   double            m_macd_open_level;
   double            m_macd_close_level;
   double            m_take_profit;
   double            m_stop_loss;
   ENUM_TRAILING_MODE m_trailing_mode;
   double            m_trailing_stop;
   double            m_trailing_step;
   double            m_trailing_max;

public:
                     CSampleExpert(void);
                    ~CSampleExpert(void);
   bool              Init(void);
   void              Deinit(void);
   bool              Processing(void);

protected:
   bool              InitCheckParameters(const int digits_adjust);
   bool              InitIndicators(void);
   bool              LongClosed(void);
   bool              ShortClosed(void);
   bool              LongModified(void);
   bool              ShortModified(void);
   bool              LongModifiedEx(void);
   bool              ShortModifiedEx(void);
   bool              LongOpened(void);
   bool              ShortOpened(void);
  };
//--- global expert
CSampleExpert ExtExpert;
//+------------------------------------------------------------------+
//| Constructor                                                      |
//+------------------------------------------------------------------+
CSampleExpert::CSampleExpert(void) : m_adjusted_point(0),
   m_handle_macd(INVALID_HANDLE),
   m_handle_ema(INVALID_HANDLE),
   m_macd_current(0),
   m_macd_previous(0),
   m_signal_current(0),
   m_signal_previous(0),
   m_ema_current(0),
   m_ema_previous(0),
   m_macd_open_level(0),
   m_macd_close_level(0),
   m_take_profit(0),
   m_stop_loss(0),
   m_trailing_stop(0),
   m_trailing_mode(ENUM_TRAILING_MODE::None),
   m_trailing_step(0),
   m_trailing_max(0)
  {
   ArraySetAsSeries(m_buff_MACD_main,true);
   ArraySetAsSeries(m_buff_MACD_signal,true);
   ArraySetAsSeries(m_buff_EMA,true);
  }
//+------------------------------------------------------------------+
//| Destructor                                                       |
//+------------------------------------------------------------------+
CSampleExpert::~CSampleExpert(void)
  {
  }
//+------------------------------------------------------------------+
//| Initialization and checking for input parameters                 |
//+------------------------------------------------------------------+
bool CSampleExpert::Init(void)
  {
//--- initialize common information
   m_symbol.Name(Symbol());                  // symbol
   m_trade.SetExpertMagicNumber(MACD_MAGIC); // magic
   m_trade.SetMarginMode();
   m_trade.SetTypeFillingBySymbol(Symbol());
//--- tuning for 3 or 5 digits
   int digits_adjust=1;
   if(m_symbol.Digits()==3 || m_symbol.Digits()==5)
      digits_adjust=10;
   m_adjusted_point=m_symbol.Point()*digits_adjust;
//--- set default deviation for trading in adjusted points
   m_macd_open_level =InpMACDOpenLevel*m_adjusted_point;
   m_macd_close_level=InpMACDCloseLevel*m_adjusted_point;
   m_trailing_stop    =InpTrailingStop*m_adjusted_point;
   m_take_profit     =InpTakeProfit*m_adjusted_point;
   m_stop_loss       = InpStopLoss*m_adjusted_point;
//--- set default deviation for trading in adjusted points
   m_trade.SetDeviationInPoints(3*digits_adjust);
//---
   if(!InitCheckParameters(digits_adjust))
      return(false);
   if(!InitIndicators())
      return(false);
//--- succeed
   return(true);
  }
//+------------------------------------------------------------------+
//| Checking for input parameters                                    |
//+------------------------------------------------------------------+
bool CSampleExpert::InitCheckParameters(const int digits_adjust)
  {
//--- initial data checks
   if(InpTakeProfit>0 && InpTakeProfit*digits_adjust<m_symbol.StopsLevel())
     {
      printf("Take Profit must be greater than %d",m_symbol.StopsLevel());
      return(false);
     }
   if(InpStopLoss>0 && InpStopLoss*digits_adjust<m_symbol.StopsLevel())
     {
      printf("Stop Loss must be greater than %d",m_symbol.StopsLevel());
      return(false);
     }
   if(InpTrailingStop>0 && InpTrailingStop*digits_adjust<m_symbol.StopsLevel())
     {
      printf("Trailing Stop must be greater than %d",m_symbol.StopsLevel());
      return(false);
     }

//--- check for right lots amount
/*
   if(InpLots<m_symbol.LotsMin() || InpLots>m_symbol.LotsMax())
     {
      printf("Lots amount must be in the range from %f to %f",m_symbol.LotsMin(),m_symbol.LotsMax());
      return(false);
     }
     
   if(MathAbs(InpLots/m_symbol.LotsStep()-MathRound(InpLots/m_symbol.LotsStep()))>1.0E-10)
     {
      printf("Lots amount is not corresponding with lot step %f",m_symbol.LotsStep());
      return(false);
     }
*/
//--- succeed
   return(true);
  }
//+------------------------------------------------------------------+
//| Initialization of the indicators                                 |
//+------------------------------------------------------------------+
bool CSampleExpert::InitIndicators(void)
  {
//--- create MACD indicator
   if(m_handle_macd==INVALID_HANDLE)
      if((m_handle_macd=iMACD(NULL,0,12,26,9,PRICE_CLOSE))==INVALID_HANDLE)
        {
         printf("Error creating MACD indicator");
         return(false);
        }
//--- create EMA indicator and add it to collection
   if(m_handle_ema==INVALID_HANDLE)
      if((m_handle_ema=iMA(NULL,0,InpMATrendPeriod,0,MODE_EMA,PRICE_CLOSE))==INVALID_HANDLE)
        {
         printf("Error creating EMA indicator");
         return(false);
        }
//--- succeed
   return(true);
  }
//+------------------------------------------------------------------+
//| Check for long position closing                                  |
//+------------------------------------------------------------------+
bool CSampleExpert::LongClosed(void)
  {
   bool res=false;
//--- should it be closed?
   if(m_macd_current>0)
      if(m_macd_current<m_signal_current && m_macd_previous>m_signal_previous)
         if(m_macd_current>m_macd_close_level)
           {
            //--- close position
            if(m_trade.PositionClose(Symbol()))
               printf("Long position by %s to be closed",Symbol());
            else
               printf("Error closing position by %s : '%s'",Symbol(),m_trade.ResultComment());
            //--- processed and cannot be modified
            res=true;
           }
//--- result
   return(res);
  }
//+------------------------------------------------------------------+
//| Check for short position closing                                 |
//+------------------------------------------------------------------+
bool CSampleExpert::ShortClosed(void)
  {
   bool res=false;
//--- should it be closed?
   if(m_macd_current<0)
      if(m_macd_current>m_signal_current && m_macd_previous<m_signal_previous)
         if(MathAbs(m_macd_current)>m_macd_close_level)
           {
            //--- close position
            if(m_trade.PositionClose(Symbol()))
               printf("Short position by %s to be closed",Symbol());
            else
               printf("Error closing position by %s : '%s'",Symbol(),m_trade.ResultComment());
            //--- processed and cannot be modified
            res=true;
           }
//--- result
   return(res);
  }
//+------------------------------------------------------------------+
//| Check for long position modifying                                |
//+------------------------------------------------------------------+
bool CSampleExpert::LongModified(void)
  {
   bool res=false;
//--- check for trailing stop
   if(InpTrailingStop>0)
     {
      if(m_symbol.Bid()-m_position.PriceOpen()>m_adjusted_point*InpTrailingStop)
        {
         double sl=NormalizeDouble(m_symbol.Bid()-m_trailing_stop,m_symbol.Digits());
         double tp=m_position.TakeProfit();
         if(m_position.StopLoss()<sl || m_position.StopLoss()==0.0)
           {
            //--- modify position
            if(m_trade.PositionModify(Symbol(),sl,tp))
               printf("Long position by %s to be modified",Symbol());
            else
              {
               printf("Error modifying position by %s : '%s'",Symbol(),m_trade.ResultComment());
               printf("Modify parameters : SL=%f,TP=%f",sl,tp);
              }
            //--- modified and must exit from expert
            res=true;
           }
        }
     }
//--- result
   return(res);
  }
//+------------------------------------------------------------------+
//| Check for short position modifying                               |
//+------------------------------------------------------------------+
bool CSampleExpert::ShortModified(void)
  {
   bool   res=false;
//--- check for trailing stop
   if(InpTrailingStop>0)
     {
      if((m_position.PriceOpen()-m_symbol.Ask())>(m_adjusted_point*InpTrailingStop))
        {
         double sl=NormalizeDouble(m_symbol.Ask()+m_trailing_stop,m_symbol.Digits());
         double tp=m_position.TakeProfit();
         if(m_position.StopLoss()>sl || m_position.StopLoss()==0.0)
           {
            //--- modify position
            if(m_trade.PositionModify(Symbol(),sl,tp))
               printf("Short position by %s to be modified",Symbol());
            else
              {
               printf("Error modifying position by %s : '%s'",Symbol(),m_trade.ResultComment());
               printf("Modify parameters : SL=%f,TP=%f",sl,tp);
              }
            //--- modified and must exit from expert
            res=true;
           }
        }
     }
//--- result
   return(res);
  }
//+------------------------------------------------------------------+
//| Check for long position modifying(for trailing fixed PSAR)       |
//+------------------------------------------------------------------+
bool CSampleExpert::LongModifiedEx(void)
  {
   bool res=false;
//--- check for trailing stop
   if(m_trailing_max < m_last_bar.high)
     {
      double tp=m_position.TakeProfit();
      double sl=m_position.StopLoss();
      //--- calcurate ParabolicSAR
      m_trailing_max=m_last_bar.high;
      m_trailing_step=fmin(InpPSAR_Maximum,m_trailing_step+InpPSAR_Step);
      double sar_stop =sl + (m_trailing_max - sl)*m_trailing_step;
      sar_stop=NormalizeDouble(sar_stop,m_symbol.Digits());
      //---

      if((sl==0.0 || sl < sar_stop) && sar_stop < m_symbol.Bid())
        {
         //--- modify position
         if(m_trade.PositionModify(Symbol(),sar_stop,tp))
            printf("Long position by %s to be modified",Symbol());
         else
           {
            printf("Error modifying position by %s : '%s'",Symbol(),m_trade.ResultComment());
            printf("Modify parameters : SL=%f,TP=%f",sar_stop,tp);
           }
         //--- modified and must exit from expert
         res=true;
        }
     }
//--- result
   return(res);
  }
//+------------------------------------------------------------------+
//| Check for short position modifying(for trailing fixed PSAR)      |
//+------------------------------------------------------------------+
bool CSampleExpert::ShortModifiedEx(void)
  {
   bool   res=false;
//--- check for trailing stop
   if(m_trailing_max > m_last_bar.low)
     {
      double tp=m_position.TakeProfit();
      double sl=m_position.StopLoss();
      //--- calcurate ParabolicSAR
      m_trailing_max=m_last_bar.low;
      m_trailing_step=fmin(InpPSAR_Maximum,m_trailing_step+InpPSAR_Step);
      double sar_stop = sl - (sl - m_trailing_max)*m_trailing_step;
      sar_stop=NormalizeDouble(sar_stop,m_symbol.Digits());
      //---
      if((sl==0.0 || sl > sar_stop) && sar_stop > m_symbol.Ask())
        {
         //--- modify position
         if(m_trade.PositionModify(Symbol(),sar_stop,tp))
            printf("Short position by %s to be modified",Symbol());
         else
           {
            printf("Error modifying position by %s : '%s'",Symbol(),m_trade.ResultComment());
            printf("Modify parameters : SL=%f,TP=%f",sar_stop,tp);
           }
         //--- modified and must exit from expert
         res=true;
        }
     }
//--- result
   return(res);
  }

//+------------------------------------------------------------------+
//| Check for long position opening                                  |
//+------------------------------------------------------------------+
bool CSampleExpert::LongOpened(void)
  {
   bool res=false;
//--- check for long position (BUY) possibility
   if(m_macd_current<0)
      if(m_macd_current>m_signal_current && m_macd_previous<m_signal_previous)
         if(MathAbs(m_macd_current)>(m_macd_open_level) && m_ema_current>m_ema_previous)
           {
            double sl=0.0,tp=0.0;
            double price=m_symbol.Ask();
            if(InpStopLoss > 0)
               sl = NormalizeDouble(m_symbol.Bid()-m_stop_loss,m_symbol.Digits());
            if(InpTakeProfit > 0)
               tp = m_symbol.Bid()+m_take_profit;
            //--- check for free money
            if(m_account.FreeMarginCheck(Symbol(),ORDER_TYPE_BUY,InpLots,price)<0.0)
               printf("We have no money. Free Margin = %f",m_account.FreeMargin());
            else
              {
               //--- open position
               if(m_trade.PositionOpen(Symbol(),ORDER_TYPE_BUY,InpLots,price,sl,tp))
                 {
                  m_trailing_max = price;
                  m_trailing_step = InpPSAR_Step;
                  printf("Position by %s to be opened",Symbol());
                 }
               else
                 {
                  printf("Error opening BUY position by %s : '%s'",Symbol(),m_trade.ResultComment());
                  printf("Open parameters : price=%f,TP=%f",price,tp);
                 }
              }
            //--- in any case we must exit from expert
            res=true;
           }
//--- result
   return(res);
  }
//+------------------------------------------------------------------+
//| Check for short position opening                                 |
//+------------------------------------------------------------------+
bool CSampleExpert::ShortOpened(void)
  {
   bool res=false;
//--- check for short position (SELL) possibility
   if(m_macd_current>0)
      if(m_macd_current<m_signal_current && m_macd_previous>m_signal_previous)
         if(m_macd_current>(m_macd_open_level) && m_ema_current<m_ema_previous)
           {
            double sl=0.0,tp=0.0;
            double price=m_symbol.Bid();
            if(InpStopLoss>0)
               sl = NormalizeDouble(m_symbol.Ask()+m_stop_loss,m_symbol.Digits());
            if(InpTakeProfit>0)
               tp =m_symbol.Ask()-m_take_profit;
            //--- check for free money
            if(m_account.FreeMarginCheck(Symbol(),ORDER_TYPE_SELL,InpLots,price)<0.0)
               printf("We have no money. Free Margin = %f",m_account.FreeMargin());
            else
              {
               //--- open position
               if(m_trade.PositionOpen(Symbol(),ORDER_TYPE_SELL,InpLots,price,sl,tp))
                 {
                  m_trailing_max = price;
                  m_trailing_step = InpPSAR_Step;
                  printf("Position by %s to be opened",Symbol());
                 }
               else
                 {
                  printf("Error opening SELL position by %s : '%s'",Symbol(),m_trade.ResultComment());
                  printf("Open parameters : price=%f,TP=%f",price,tp);
                 }
              }
            //--- in any case we must exit from expert
            res=true;
           }
//--- result
   return(res);
  }
//+------------------------------------------------------------------+
//| main function returns true if any position processed             |
//+------------------------------------------------------------------+
bool CSampleExpert::Processing(void)
  {
//--- refresh rates
   if(!m_symbol.RefreshRates())
      return(false);
//--- refresh indicators
   if(BarsCalculated(m_handle_macd)<2 || BarsCalculated(m_handle_ema)<2)
      return(false);

   if(CopyBuffer(m_handle_macd,0,0,2,m_buff_MACD_main)  !=2 ||
      CopyBuffer(m_handle_macd,1,0,2,m_buff_MACD_signal)!=2 ||
      CopyBuffer(m_handle_ema,0,0,2,m_buff_EMA)         !=2)
      return(false);

//--- get last bar info
   MqlRates rt[1];
   if(CopyRates(NULL,0,1,1,rt)!=1)
      return(false);
   m_last_bar = rt[0];
//---

//   m_indicators.Refresh();
//--- to simplify the coding and speed up access
//--- data are put into internal variables
   m_macd_current   =m_buff_MACD_main[0];
   m_macd_previous  =m_buff_MACD_main[1];
   m_signal_current =m_buff_MACD_signal[0];
   m_signal_previous=m_buff_MACD_signal[1];
   m_ema_current    =m_buff_EMA[0];
   m_ema_previous   =m_buff_EMA[1];
//--- it is important to enter the market correctly,
//--- but it is more important to exit it correctly...
//--- first check if position exists - try to select it
   if(m_position.Select(Symbol()))
     {
      if(m_position.PositionType()==POSITION_TYPE_BUY)
        {
         //--- try to close or modify long position
         if(LongClosed())
            return(true);
         if(InpTrailngMode==ENUM_TRAILING_MODE::Fixed)
            if(LongModified())
               return(true);
         if(InpTrailngMode==ENUM_TRAILING_MODE::FixedPSAR)
            if(LongModifiedEx())
               return(true);

        }
      else
        {
         //--- try to close or modify short position
         if(ShortClosed())
            return(true);
         if(InpTrailngMode==ENUM_TRAILING_MODE::Fixed)
            if(ShortModified())
               return(true);
         if(InpTrailngMode==ENUM_TRAILING_MODE::FixedPSAR)
            if(ShortModifiedEx())
               return(true);
        }
     }
//--- no opened position identified
   else
     {
      //--- check for long position (BUY) possibility
      if(LongOpened())
         return(true);
      //--- check for short position (SELL) possibility
      if(ShortOpened())
         return(true);
     }
//--- exit without position processing
   return(false);
  }
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit(void)
  {
//--- create all necessary objects
   if(!ExtExpert.Init())
      return(INIT_FAILED);
//--- secceed
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Expert new tick handling function                                |
//+------------------------------------------------------------------+
void OnTick(void)
  {
   static datetime limit_time=0; // last trade processing time + timeout
//--- don't process if timeout
   if(TimeCurrent()>=limit_time)
     {
      //--- check for data
      if(Bars(Symbol(),Period())>2*InpMATrendPeriod)
        {
         //--- change limit time by timeout in seconds if processed
         if(ExtExpert.Processing())
            limit_time=TimeCurrent()+ExtTimeOut;
        }
     }
  }
//+------------------------------------------------------------------+
