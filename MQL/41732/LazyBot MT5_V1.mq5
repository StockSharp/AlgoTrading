//+------------------------------------------------------------------+
//|                                               LazyBot MT5_V1.mq5 |
//|                                 Copyright 2022, Nguyen Quoc Hung |
//|                                            Hung_tthanh@yahoo.com |
//+------------------------------------------------------------------+
#property copyright "Copyright 2022, Nguyen Quoc Hung"
#property link      "Hung_tthanh@yahoo.com"
#property version   "1.00"

//Import External class
#include <Trade\PositionInfo.mqh>
#include <Trade\Trade.mqh>
#include <Trade\SymbolInfo.mqh>  
#include <Trade\AccountInfo.mqh>
#include <Trade\OrderInfo.mqh>

//--- introduce predefined variables for code readability 
#define Ask    SymbolInfoDouble(_Symbol, SYMBOL_ASK)
#define Bid    SymbolInfoDouble(_Symbol, SYMBOL_BID)

//--- input parameters
input string  EASettings = "---------------------------------------------"; //-------- <EA Settings> --------
input int      InpMagicNumber = 123456;   //Magic Number
input string   InpBotName = "LazyBot_V1"; //Bot Name
input string  TradingSettings = "---------------------------------------------"; //-------- <Trading Settings> --------
input double   Inpuser_lot = 0.01;        //Lots
input double   Inpuser_SL = 5.0;          //Stoploss (in Pips)
input double   InpAddPrice_pip = 0;       //Dist from [H], [L] to OP_Price (in Pips)
input int      Inpuser_SLippage = 3;      // Maximum slippage allow_Pips.
input double   InpMax_spread    = 0;      //Maximum allowed spread (in Pips) (0 = floating)
input string  TimeSettings = "---------------------------------------------"; //-------- <Trading Time Settings> --------
input bool     isTradingTime = true;      //Allow trading time
input int      InpStartHour = 7;          //Start Hour
input int      InpEndHour = 22;           //End Hour
input string  MoneyManagementSettings = "---------------------------------------------"; //-------- <Money Settings> --------
input bool     isVolume_Percent = false;   //Allow Volume Percent
input double   InpRisk = 1;               //Risk Percentage of Balance (%)

//Local parameters
datetime last;
int totalBars;
int     Pips2Points;    // slippage  3 pips    3=points    30=points
double  Pips2Double;    // Stoploss 15 pips    0.015      0.0150
double slippage;
double acSpread;
string strComment = "";

CPositionInfo  m_position;                   // trade position object
CTrade         m_trade;                      // trading object
CSymbolInfo    m_symbol;                     // symbol info object
CAccountInfo   m_account;                    // account info wrapper
COrderInfo     m_order;                      // pending orders object

//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//---
   //3 or 5 digits detection
   //Pip and point
   if(_Digits % 2 == 1)
     {
      Pips2Double  = _Point*10;
      Pips2Points  = 10;
      slippage = 10* Inpuser_SLippage;
     }
   else
     {
      Pips2Double  = _Point;
      Pips2Points  =  1;
      slippage = Inpuser_SLippage;
     }
     
     if(!m_symbol.Name(Symbol())) // sets symbol name
      return(INIT_FAILED);
   RefreshRates();
//---
   m_trade.SetExpertMagicNumber(InpMagicNumber);
   m_trade.SetMarginMode();
   m_trade.SetTypeFillingBySymbol(m_symbol.Name());
   m_trade.SetDeviationInPoints(slippage);
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
     if(TerminalInfoInteger(TERMINAL_TRADE_ALLOWED) == false)
     {
      Comment("LazyBot\nTrade not allowed.");
      return;
     }
     
   //Get trading time,
   // Opening section
   // London 14h - 23h GMT VietNam
   // Newyork 19h - 04h GMT VietNam
   MqlDateTime timeLocal;
   MqlDateTime timeServer;
   
   TimeLocal(timeLocal);
   TimeCurrent(timeServer);   
   
   // do not work on holidays.
   if(timeServer.day_of_week == 0 || timeServer.day_of_week == 6)
      return;
      
   int hourLocal = timeLocal.hour;//TimeHour(TimeLocal());
   int hourCurrent = timeServer.hour;//TimeHour(TimeCurrent());

   acSpread = SymbolInfoInteger(_Symbol, SYMBOL_SPREAD);
   
   strComment = "\nLocal Hour is = " + hourLocal;
   strComment += "\nCurrent Hour is = " + hourCurrent;
   strComment += "\nSpread is = " + (string)acSpread;
   strComment += "\nTotal Bars is = " + (string)totalBars;

   Comment(strComment);

   //Check Trailing
   TrailingSL();

//---
   if(last != iTime(m_symbol.Name(), PERIOD_D1, 0))// && hourCurrent > InpStartHour)
     {
      //Check Trading time
      if(isTradingTime)
        {
         if(hourCurrent >= InpStartHour) // && hourCurrent < InpEndHour){
           {
            DeleteOldOrds();
            //Send Order BUY_STOP va SELL_STOP
            OpenOrder();

            last = iTime(m_symbol.Name(), PERIOD_D1, 0);
           }
        }
      else
        {
         DeleteOldOrds();
         //Send Order BUY_STOP va SELL_STOP
         OpenOrder();
         last = iTime(m_symbol.Name(), PERIOD_D1, 0);
        }
     }


  }
  
//+------------------------------------------------------------------+
string getPendingOrderComment()
{
   string value = "";
   for(int i=OrdersTotal()-1;i>=0;i--) // returns the number of current orders
   {
      if(m_order.SelectByIndex(i))     // selects the pending order by index for further access to its properties
         {
         if(m_order.Symbol() == m_symbol.Name() && m_order.Magic()==InpMagicNumber){
            value = m_order.Comment();
            }
         }
    }
    return value;
}

//+------------------------------------------------------------------+
//| CALCULATE SIGNAL AND SEND ORDER                                  |
//+------------------------------------------------------------------+
void OpenOrder()
  {
      double TP_Buy = 0, TP_Sell = 0;
      double SL_Buy = 0, SL_Sell = 0;
      
      //Check Maximum Spread
      if(InpMax_spread != 0){
         if(acSpread > InpMax_spread){
            Print(__FUNCTION__," > current Spread is greater than user Spread!...");
            return;
         }
      }
         
      double Bar1High = m_symbol.NormalizePrice(iHigh(m_symbol.Name(), PERIOD_D1, 1));
      double Bar1Low = m_symbol.NormalizePrice(iLow(m_symbol.Name(), PERIOD_D1, 1));
      
      //Calculate Lots
      double lot1 = CalculateVolume();
   
      double OpenPrice = m_symbol.NormalizePrice(Bar1High + InpAddPrice_pip * Pips2Double);// + NormalizeDouble((acSpread/Pips2Points) * Pips2Double, Digits);
   
      //For BUY_STOP --------------------------------
      TP_Buy = 0;//Bar1High + NormalizeDouble(min_sl* Pips2Double, Digits);
      SL_Buy = m_symbol.NormalizePrice(OpenPrice - Inpuser_SL * Pips2Double);
   
      totalBars = iBars(m_symbol.Name(), PERIOD_D1);
      string comment = InpBotName + ";" + m_symbol.Name() + ";" + totalBars;
   
         if(CheckVolumeValue(lot1)
            && CheckOrderForFREEZE_LEVEL(ORDER_TYPE_BUY_STOP, OpenPrice)
            && CheckMoneyForTrade(m_symbol.Name(),lot1, ORDER_TYPE_BUY)
            && CheckStopLoss(OpenPrice, SL_Buy))
            {
               if(!m_trade.BuyStop(lot1, OpenPrice, m_symbol.Name(), SL_Buy, TP_Buy, ORDER_TIME_GTC, 0, comment))// use "ORDER_TIME_GTC" when expiration date = 0
               Print(__FUNCTION__, "--> Buy Error");
            }
      
   
      //For SELL_STOP --------------------------------
      OpenPrice = m_symbol.NormalizePrice(Bar1Low - InpAddPrice_pip * Pips2Double);// - NormalizeDouble((acSpread/Pips2Points) * Pips2Double, Digits);
   
      TP_Sell = 0;//Bar1Low - NormalizeDouble(min_sl* Pips2Double, Digits);
      SL_Sell = m_symbol.NormalizePrice(OpenPrice + Inpuser_SL * Pips2Double);
   
         if(CheckVolumeValue(lot1)
            && CheckOrderForFREEZE_LEVEL(ORDER_TYPE_SELL_STOP, OpenPrice)
            && CheckMoneyForTrade(m_symbol.Name(),lot1, ORDER_TYPE_SELL)
            && CheckStopLoss(OpenPrice, SL_Sell))
         {
            if(!m_trade.SellStop(lot1, OpenPrice, m_symbol.Name(), SL_Sell, TP_Sell, ORDER_TIME_GTC, 0, comment))
            Print(__FUNCTION__, "--> Sell Error");
         }  

  }

//+------------------------------------------------------------------+
//| TRAILING STOPLOSS                                                |
//+------------------------------------------------------------------+
void TrailingSL()
  {
   double SL_in_Pip = 0;

   for(int i = PositionsTotal() - 1; i >= 0; i--)
     {         
      if(m_position.SelectByIndex(i))     // selects the orders by index for further access to its properties
        {
         if((m_position.Magic() == InpMagicNumber) && (m_position.Symbol() == m_symbol.Name()))
           {
            double order_stoploss1 = m_position.StopLoss();
            
            // For Buy oder
            if(m_position.PositionType() == POSITION_TYPE_BUY)
              {                  
                  //--Calculate SL when price changed
                  SL_in_Pip = NormalizeDouble((Bid - order_stoploss1), _Digits) / Pips2Double;

                  if(SL_in_Pip > Inpuser_SL)
                    {
                        order_stoploss1 = NormalizeDouble(Bid - (Inpuser_SL * Pips2Double), _Digits);                   
                        m_trade.PositionModify(m_position.Ticket(), order_stoploss1, m_position.TakeProfit());
                    }
              }

            //For Sell Order
            if(m_position.PositionType() == POSITION_TYPE_SELL)
              {
                  //--Calculate SL when price changed
                  SL_in_Pip = NormalizeDouble((order_stoploss1 - Ask), _Digits) / Pips2Double;
                  if(SL_in_Pip > Inpuser_SL)
                    {
                        order_stoploss1 = NormalizeDouble(Ask + (Inpuser_SL * Pips2Double), _Digits);
                     
                        m_trade.PositionModify(m_position.Ticket(), order_stoploss1, m_position.TakeProfit());
            
                    }
              }
           }
        }
     }
  }

//+------------------------------------------------------------------+
//| Delele Old Orders                                                |
//+------------------------------------------------------------------+
void DeleteOldOrds()
  {
   string sep=";";                // A separator as a character
   ushort u_sep;                  // The code of the separator character
   string result[];               // An array to get strings

   for(int i = OrdersTotal() - 1; i >= 0; i--)  // returns the number of current orders
     {
      if(m_order.SelectByIndex(i))              // selects the pending order by index for further access to its properties
        {
         //--- Get the separator code
         u_sep = StringGetCharacter(sep, 0);
         string Ordcomment = m_order.Comment();

         //Split OrderComment (EAName;Symbol;totalBar) to get Ordersymbol
         int k = StringSplit(Ordcomment, u_sep, result);

         if(k > 2)
           {
            string sym = m_symbol.Name();
            if((m_order.Magic() == InpMagicNumber) && (sym == result[1]))
              {
                  m_trade.OrderDelete(m_order.Ticket());
              }
           }
        }
     }

  }

//+------------------------------------------------------------------+
//| CALCULATE VOLUME                                                 |
//+------------------------------------------------------------------+
// We define the function to calculate the position size and return the lot to order.
double CalculateVolume()
  {
   double LotSize = 0;
   int n;

   if(isVolume_Percent == false)
     {
      LotSize = Inpuser_lot;
     }
   else
     {

      LotSize = (InpRisk) * m_account.FreeMargin();
      LotSize = LotSize /100000;
      n = MathFloor(LotSize/Inpuser_lot);
      //Comment((string)n);
      LotSize = n * Inpuser_lot;

      if(LotSize < Inpuser_lot)
         LotSize = Inpuser_lot;

      if(LotSize > m_symbol.LotsMax())
         LotSize = m_symbol.LotsMax();

      if(LotSize < m_symbol.LotsMin())
         LotSize = m_symbol.LotsMin();
     }

   return(LotSize);
  }
  
//+------------------------------------------------------------------+
//| CHECK FREEZE LEVEL                                               |
//+------------------------------------------------------------------+
bool CheckOrderForFREEZE_LEVEL(ENUM_ORDER_TYPE type, double price)//change name of this function
{
  int freeze_level = (int)SymbolInfoInteger(_Symbol,  SYMBOL_TRADE_FREEZE_LEVEL);

   bool check = false;
   
//--- check only two order types
   switch(type)
     {
         //--- Buy operation
         case ORDER_TYPE_BUY_STOP:
         {  
           //--- check the distance from the opening price to the activation price
           check = ((price-Ask) > freeze_level*_Point);
           //--- return the result of checking
            return(check);
         }
      //--- Sell operation
      case ORDER_TYPE_SELL_STOP:
         {
         //--- check the distance from the opening price to the activation price
            check = ((Bid-price)>freeze_level*_Point);

            //--- return the result of checking
            return(check);
         }
         break;
     }
//--- a slightly different function is required for pending orders
   return false;
}
   
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+

bool CheckMoneyForTrade(string symb,double lots,ENUM_ORDER_TYPE type)
  {
//--- Getting the opening price
   MqlTick mqltick;
   SymbolInfoTick(symb,mqltick);
   double price=mqltick.ask;
   if(type==ORDER_TYPE_SELL)
      price=mqltick.bid;
//--- values of the required and free margin
   double margin,free_margin=AccountInfoDouble(ACCOUNT_MARGIN_FREE);
   //--- call of the checking function
   if(!OrderCalcMargin(type,symb,lots,price,margin))
     {
      //--- something went wrong, report and return false
      Print("Error in ",__FUNCTION__," code=",GetLastError());
      return(false);
     }
   //--- if there are insufficient funds to perform the operation
   if(margin>free_margin)
     {
      //--- report the error and return false
      Print("Not enough money for ",EnumToString(type)," ",lots," ",symb," Error code=",GetLastError());
      return(false);
     }
//--- checking successful
   return(true);
}
  
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+

bool CheckStopLoss(double price, double SL)
{
//--- get the SYMBOL_TRADE_STOPS_LEVEL level
   int stops_level = (int)SymbolInfoInteger(m_symbol.Name(), SYMBOL_TRADE_STOPS_LEVEL);
   if(stops_level != 0)
     {
      PrintFormat("SYMBOL_TRADE_STOPS_LEVEL=%d: StopLoss and TakeProfit must"+
                  " not be nearer than %d points from the closing price", stops_level, stops_level);
     }
//---
   bool SL_check=false;

   //--- check the StopLoss
  return SL_check = MathAbs(price - SL) > (stops_level * m_symbol.Point());  
}

//+------------------------------------------------------------------+
//| Check the correctness of the order volume                        |
//+------------------------------------------------------------------+
bool CheckVolumeValue(double volume)
{
//--- minimal allowed volume for trade operations
  double min_volume = m_symbol.LotsMin();
  
//--- maximal allowed volume of trade operations
   double max_volume = m_symbol.LotsMax();

//--- get minimal step of volume changing
   double volume_step = m_symbol.LotsStep();
   
   if(volume < min_volume || volume>max_volume)
     {
      return(false);
     }

   int ratio = (int)MathRound(volume/volume_step);
   if(MathAbs(ratio*volume_step-volume)>0.0000001)
     {
      return(false);
     }

   return(true);
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
   if(Ask==0 || Bid==0)
      return(false);
//---
   return(true);
  }
  
  //I need Upgrade trailing stop, If you have any idea please comment on youtube. thank you!