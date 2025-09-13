//+------------------------------------------------------------------+
//|                          HTH Trader(barabashkakvn's edition).mq5 |
//|                        Copyright 2017, MetaQuotes Software Corp. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "Copyright 2017, MetaQuotes Software Corp."
#property link      "https://www.mql5.com"
#property version   "1.001"
//---
#include <Trade\PositionInfo.mqh>
#include <Trade\Trade.mqh>
#include <Trade\SymbolInfo.mqh>  
CPositionInfo  m_position;                   // trade position object
CTrade         m_trade;                      // trading object
CSymbolInfo    m_symbol;                     // symbol info object
//--- input parameters
input bool trade=true;
input string InpSymbol_1="EURUSD";
input string InpSymbol_2="USDCHF";
input string InpSymbol_3="GBPUSD";
input string InpSymbol_4="AUDUSD";
input bool show_profit=true;
input bool enable_profit=false;
input bool enable_loss=false;
bool enable_emergency_trading=true;
input int emergency_loss=60;
input int InpProfit=80;
input int InpLoss=40;
input int MagicNumber1=243;
input int MagicNumber2=244;
input int MagicNumber3=245;
input int MagicNumber4=256;
input int E_MagicNumber=257;
input double InpLot=0.01;
//---
double symbol_1_point=0.0;
double symbol_2_point=0.0;
double symbol_3_point=0.0;
double symbol_4_point=0.0;
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//---
   if(!VerifyMarketWatch(InpSymbol_1,InpSymbol_2,InpSymbol_3,InpSymbol_4))
     {
      Print("Error select symbols in Market Watch");
      return(INIT_FAILED);
     }
   ResetLastError();
   if(!SymbolInfoDouble(InpSymbol_1,SYMBOL_POINT,symbol_1_point))
     {
      Print("Error getting \"SYMBOL_POINT\" for the ",InpSymbol_1," #",GetLastError());
      return(INIT_FAILED);
     }
   if(!SymbolInfoDouble(InpSymbol_2,SYMBOL_POINT,symbol_2_point))
     {
      Print("Error getting \"SYMBOL_POINT\" for the ",InpSymbol_2," #",GetLastError());
      return(INIT_FAILED);
     }
   if(!SymbolInfoDouble(InpSymbol_3,SYMBOL_POINT,symbol_3_point))
     {
      Print("Error getting \"SYMBOL_POINT\" for the ",InpSymbol_3," #",GetLastError());
      return(INIT_FAILED);
     }
   if(!SymbolInfoDouble(InpSymbol_4,SYMBOL_POINT,symbol_4_point))
     {
      Print("Error getting \"SYMBOL_POINT\" for the ",InpSymbol_4," #",GetLastError());
      return(INIT_FAILED);
     }
//---
   if(IsFillingTypeAllowed(Symbol(),SYMBOL_FILLING_FOK))
      m_trade.SetTypeFilling(ORDER_FILLING_FOK);
   else if(IsFillingTypeAllowed(Symbol(),SYMBOL_FILLING_IOC))
      m_trade.SetTypeFilling(ORDER_FILLING_IOC);
   else
      m_trade.SetTypeFilling(ORDER_FILLING_RETURN);
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
//---
   double symbol_1_current_0=iClose(0,InpSymbol_1,0);
   double symbol_2_current_0=iClose(0,InpSymbol_2,0);
   double symbol_3_current_0=iClose(0,InpSymbol_3,0);
   double symbol_4_current_0=iClose(0,InpSymbol_4,0);

   double symbol_1_d1_1=iClose(1,InpSymbol_1,PERIOD_D1);
   double symbol_2_d1_1=iClose(1,InpSymbol_2,PERIOD_D1);
   double symbol_3_d1_1=iClose(1,InpSymbol_3,PERIOD_D1);
   double symbol_4_d1_1=iClose(1,InpSymbol_4,PERIOD_D1);

   double symbol_1_d1_2=iClose(2,InpSymbol_1,PERIOD_D1);
   double symbol_2_d1_2=iClose(2,InpSymbol_2,PERIOD_D1);
   double symbol_3_d1_2=iClose(2,InpSymbol_3,PERIOD_D1);
   double symbol_4_d1_2=iClose(2,InpSymbol_4,PERIOD_D1);

   if(symbol_1_current_0==0.0 || symbol_2_current_0==0.0 || symbol_3_current_0==0.0 || symbol_4_current_0==0.0 || 
      symbol_1_d1_1==0.0 || symbol_2_d1_1==0.0 || symbol_3_d1_1==0.0 || symbol_4_d1_1==0.0 ||
      symbol_1_d1_2==0.0 || symbol_2_d1_2==0.0 || symbol_3_d1_2==0.0 || symbol_4_d1_2==0.0)
     {
      return;
     }
//---
   double d_c1,d_c2,d_c3,d_c4,d_c11,d_c22,d_c33,d_c44;
   d_c1=(100*symbol_1_current_0/symbol_1_d1_1)-100;
   d_c2=(100*symbol_2_current_0/symbol_2_d1_1)-100;
   d_c3=(100*symbol_3_current_0/symbol_3_d1_1)-100;
   d_c4=(100*symbol_4_current_0/symbol_4_d1_1)-100;

   d_c11=(100*symbol_1_d1_1/symbol_1_d1_2)-100;
   d_c22=(100*symbol_2_d1_1/symbol_2_d1_2)-100;
   d_c33=(100*symbol_3_d1_1/symbol_3_d1_2)-100;
   d_c44=(100*symbol_4_d1_1/symbol_4_d1_2)-100;
//--- check for InpProfit in PIP, and close if the goal is reached
   if(show_profit==true)
     {
      int profit1=0,profit2=0,profit3=0,profit4=0,e_profit=0;
      int totalprofit=0;

      for(int i=PositionsTotal()-1;i>=0;i--)
         if(m_position.SelectByIndex(i)) // selects the position by index for further access to its properties
           {
            double point=0.0;
            string symbol=m_position.Symbol();
            if(symbol==InpSymbol_1)
               point=symbol_1_point;
            else if(symbol==InpSymbol_2)
               point=symbol_2_point;
            else if(symbol==InpSymbol_3)
               point=symbol_3_point;
            else if(symbol==InpSymbol_4)
               point=symbol_4_point;
            else
               return;

            MqlTick  tick;
            if(!SymbolInfoTick(symbol,tick))
               return;

            long magic=m_position.Magic();

            if(magic==MagicNumber1)
              {
               if(m_position.PositionType()==POSITION_TYPE_BUY)
                  profit1=(int)((tick.bid-m_position.PriceOpen())/point);

               if(m_position.PositionType()==POSITION_TYPE_SELL)
                  profit1=(int)((m_position.PriceOpen()-tick.ask)/point);
              }
            if(magic==MagicNumber2)
              {
               if(m_position.PositionType()==POSITION_TYPE_BUY)
                  profit2=(int)((tick.bid-m_position.PriceOpen())/point);

               if(m_position.PositionType()==POSITION_TYPE_SELL)
                  profit2=(int)((m_position.PriceOpen()-tick.ask)/point);
              }
            if(magic==MagicNumber3)
              {
               if(m_position.PositionType()==POSITION_TYPE_BUY)
                  profit3=(int)((tick.bid-m_position.PriceOpen())/point);

               if(m_position.PositionType()==POSITION_TYPE_SELL)
                  profit3=(int)((m_position.PriceOpen()-tick.ask)/point);
              }
            if(magic==MagicNumber4)
              {
               if(m_position.PositionType()==POSITION_TYPE_BUY)
                  profit4=(int)((tick.bid-m_position.PriceOpen())/point);

               if(m_position.PositionType()==POSITION_TYPE_SELL)
                  profit4=(int)((m_position.PriceOpen()-tick.ask)/point);
              }
            if(magic==E_MagicNumber)// check InpProfit of emergency trades
              {
               if(m_position.PositionType()==POSITION_TYPE_BUY)
                  e_profit+=(int)((tick.bid-m_position.PriceOpen())/point);

               if(m_position.PositionType()==POSITION_TYPE_SELL)
                  e_profit+=(int)((m_position.PriceOpen()-tick.ask)/point);
              }
           }
      //---
      totalprofit=profit1+profit2+profit3+profit4+e_profit;
      if(enable_emergency_trading && totalprofit<=-emergency_loss)
         DoublePositions();

      if(enable_profit && totalprofit>=InpProfit)
         ClosePositions();

      if(enable_loss && totalprofit<=-InpLoss)
         ClosePositions();
      //--- end check for InpProfit
      Comment("\n",
              InpSymbol_1+" Deviation: "+DoubleToString(d_c1,5)+" | Previous Deviation: "+DoubleToString(d_c11,5),
              "\n",InpSymbol_2+" Deviation: "+DoubleToString(d_c2,5)+" | Previous Deviation: "+DoubleToString(d_c22,5),
              "\n",InpSymbol_3+" Deviation: "+DoubleToString(d_c3,5)+" | Previous Deviation: "+DoubleToString(d_c33,5),
              "\n",InpSymbol_4+" Deviation: "+DoubleToString(d_c4,5)+" | Previous Deviation: "+DoubleToString(d_c44,5),
              "\n",
              "\n",InpSymbol_1+"   "+InpSymbol_2+" Pair Deviation: "+DoubleToString(d_c1+d_c2,5),
              "\n",InpSymbol_1+"   "+InpSymbol_3+" Pair Deviation: "+DoubleToString(d_c1-d_c3,5),
              "\n",InpSymbol_1+"   "+InpSymbol_4+" Pair Deviation: "+DoubleToString(d_c1-d_c4,5),
              "\n",InpSymbol_2+"   "+InpSymbol_3+" Pair Deviation: "+DoubleToString(d_c2+d_c3,5),
              "\n",InpSymbol_3+"   "+InpSymbol_4+" Pair Deviation: "+DoubleToString(d_c3-d_c4,5),
              "\n",InpSymbol_2+"   "+InpSymbol_4+" Pair Deviation: "+DoubleToString(d_c2+d_c4,5),
              "\n",
              "\n",InpSymbol_1+"/"+InpSymbol_2+" vs. "+InpSymbol_3+"/"+InpSymbol_4+" Pair Deviation: "+DoubleToString((d_c1+d_c2)+(d_c3-d_c4),5),
              "\n","PIP InpProfit: "+IntegerToString(totalprofit));
     }
//-- close positions after one Day
   MqlDateTime str1;
   TimeToStruct(TimeCurrent(),str1);
   if(str1.hour>=23)
     {
      ClosePositions();
      return;
     }
//--- end close positions

//--- check for opened positions, do not continue if positions are opened
   for(int i=PositionsTotal()-1;i>=0;i--) // returns the number of open positions
      if(m_position.SelectByIndex(i)) // selects the position by index for further access to its properties
        {
         long magic=m_position.Magic();
         if(magic==MagicNumber1 ||magic==MagicNumber2  ||magic==MagicNumber3  ||
            magic==E_MagicNumber ||magic==MagicNumber4)
           {
            return;
           }
        }

   if(str1.hour>=0 && str1.hour<1 && (str1.min>=5 && str1.min<=12)) // start of a new day
     {
      //--- turn on emergency_exit
      enable_emergency_trading=true;
      MqlTick  tick;

      if(trade && d_c11>0 && IsTradeAllowed()) // Previous Day's Deviation is Positive
        {
         //--- LONG EURUSD
         if(!SymbolInfoTick(InpSymbol_1,tick))
            return;
         m_trade.SetExpertMagicNumber(MagicNumber1);
         if(m_trade.Buy(InpLot,InpSymbol_1,tick.ask,0.0,0.0,"Hedge"+InpSymbol_1))
           {
            if(m_trade.ResultDeal()==0)
               Print(InpSymbol_1," Buy -> false. Result Retcode: ",m_trade.ResultRetcode(),
                     ", description of result: ",m_trade.ResultRetcodeDescription());
            else
               Print(InpSymbol_1," Buy -> true. Result Retcode: ",m_trade.ResultRetcode(),
                     ", description of result: ",m_trade.ResultRetcodeDescription());
           }
         else
            Print(InpSymbol_1," Buy -> false. Result Retcode: ",m_trade.ResultRetcode(),
                  ", description of result: ",m_trade.ResultRetcodeDescription());
         //--- LONG USDCHF
         if(!SymbolInfoTick(InpSymbol_2,tick))
            return;
         m_trade.SetExpertMagicNumber(MagicNumber2);
         if(m_trade.Buy(InpLot,InpSymbol_2,tick.ask,0.0,0.0,"Hedge"+InpSymbol_2))
           {
            if(m_trade.ResultDeal()==0)
               Print(InpSymbol_2," Buy -> false. Result Retcode: ",m_trade.ResultRetcode(),
                     ", description of result: ",m_trade.ResultRetcodeDescription());
            else
               Print(InpSymbol_2," Buy -> true. Result Retcode: ",m_trade.ResultRetcode(),
                     ", description of result: ",m_trade.ResultRetcodeDescription());
           }
         else
            Print(InpSymbol_2," Buy -> false. Result Retcode: ",m_trade.ResultRetcode(),
                  ", description of result: ",m_trade.ResultRetcodeDescription());
         //--- SHORT GBPUSD
         if(!SymbolInfoTick(InpSymbol_3,tick))
            return;
         m_trade.SetExpertMagicNumber(MagicNumber3);
         if(m_trade.Sell(InpLot,InpSymbol_3,tick.bid,0.0,0.0,"Hedge"+InpSymbol_3))
           {
            if(m_trade.ResultDeal()==0)
               Print(InpSymbol_3," Sell -> false. Result Retcode: ",m_trade.ResultRetcode(),
                     ", description of result: ",m_trade.ResultRetcodeDescription());
            else
               Print(InpSymbol_3," Sell -> true. Result Retcode: ",m_trade.ResultRetcode(),
                     ", description of result: ",m_trade.ResultRetcodeDescription());
           }
         else
            Print(InpSymbol_3," Sell -> false. Result Retcode: ",m_trade.ResultRetcode(),
                  ", description of result: ",m_trade.ResultRetcodeDescription());
         //--- LONG AUDUSD
         if(!SymbolInfoTick(InpSymbol_4,tick))
            return;
         m_trade.SetExpertMagicNumber(MagicNumber4);
         if(m_trade.Buy(InpLot,InpSymbol_4,tick.ask,0.0,0.0,"Hedge"+InpSymbol_4))
           {
            if(m_trade.ResultDeal()==0)
               Print(InpSymbol_4," Buy -> false. Result Retcode: ",m_trade.ResultRetcode(),
                     ", description of result: ",m_trade.ResultRetcodeDescription());
            else
               Print(InpSymbol_4," Buy -> true. Result Retcode: ",m_trade.ResultRetcode(),
                     ", description of result: ",m_trade.ResultRetcodeDescription());
           }
         else
            Print(InpSymbol_4," Buy -> false. Result Retcode: ",m_trade.ResultRetcode(),
                  ", description of result: ",m_trade.ResultRetcodeDescription());
        }

      if(trade && d_c11<0 && IsTradeAllowed()) // Previous Day's Deviation is Negative
        {
         //--- LONG EURUSD
         if(!SymbolInfoTick(InpSymbol_1,tick))
            return;
         m_trade.SetExpertMagicNumber(MagicNumber1);
         if(m_trade.Sell(InpLot,InpSymbol_1,tick.bid,0.0,0.0,"Hedge"+InpSymbol_1))
           {
            if(m_trade.ResultDeal()==0)
               Print(InpSymbol_1," Sell -> false. Result Retcode: ",m_trade.ResultRetcode(),
                     ", description of result: ",m_trade.ResultRetcodeDescription());
            else
               Print(InpSymbol_1," Sell -> true. Result Retcode: ",m_trade.ResultRetcode(),
                     ", description of result: ",m_trade.ResultRetcodeDescription());
           }
         else
            Print(InpSymbol_1," Sell -> false. Result Retcode: ",m_trade.ResultRetcode(),
                  ", description of result: ",m_trade.ResultRetcodeDescription());
         //--- LONG USDCHF
         if(!SymbolInfoTick(InpSymbol_2,tick))
            return;
         m_trade.SetExpertMagicNumber(MagicNumber2);
         if(m_trade.Sell(InpLot,InpSymbol_2,tick.bid,0.0,0.0,"Hedge"+InpSymbol_2))
           {
            if(m_trade.ResultDeal()==0)
               Print(InpSymbol_2," Sell -> false. Result Retcode: ",m_trade.ResultRetcode(),
                     ", description of result: ",m_trade.ResultRetcodeDescription());
            else
               Print(InpSymbol_2," Sell -> true. Result Retcode: ",m_trade.ResultRetcode(),
                     ", description of result: ",m_trade.ResultRetcodeDescription());
           }
         else
            Print(InpSymbol_2," Sell -> false. Result Retcode: ",m_trade.ResultRetcode(),
                  ", description of result: ",m_trade.ResultRetcodeDescription());
         //--- SHORT GBPUSD
         if(!SymbolInfoTick(InpSymbol_3,tick))
            return;
         m_trade.SetExpertMagicNumber(MagicNumber3);
         if(m_trade.Buy(InpLot,InpSymbol_3,tick.ask,0.0,0.0,"Hedge"+InpSymbol_3))
           {
            if(m_trade.ResultDeal()==0)
               Print(InpSymbol_3," Buy -> false. Result Retcode: ",m_trade.ResultRetcode(),
                     ", description of result: ",m_trade.ResultRetcodeDescription());
            else
               Print(InpSymbol_3," Buy -> true. Result Retcode: ",m_trade.ResultRetcode(),
                     ", description of result: ",m_trade.ResultRetcodeDescription());
           }
         else
            Print(InpSymbol_3," Buy -> false. Result Retcode: ",m_trade.ResultRetcode(),
                  ", description of result: ",m_trade.ResultRetcodeDescription());
         //--- LONG AUDUSD
         if(!SymbolInfoTick(InpSymbol_4,tick))
            return;
         m_trade.SetExpertMagicNumber(MagicNumber4);
         if(m_trade.Sell(InpLot,InpSymbol_4,tick.bid,0.0,0.0,"Hedge"+InpSymbol_4))
           {
            if(m_trade.ResultDeal()==0)
               Print(InpSymbol_4," Sell -> false. Result Retcode: ",m_trade.ResultRetcode(),
                     ", description of result: ",m_trade.ResultRetcodeDescription());
            else
               Print(InpSymbol_4," Sell -> true. Result Retcode: ",m_trade.ResultRetcode(),
                     ", description of result: ",m_trade.ResultRetcodeDescription());
           }
         else
            Print(InpSymbol_4," Sell -> false. Result Retcode: ",m_trade.ResultRetcode(),
                  ", description of result: ",m_trade.ResultRetcodeDescription());
        }
     }
//---
   return;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void DoublePositions()
  {
   for(int i=PositionsTotal()-1;i>=0;i--)
      if(m_position.SelectByIndex(i)) // selects the position by index for further access to its properties
        {
         string symbol=m_position.Symbol();
         if(symbol!=InpSymbol_1 || symbol!=InpSymbol_2 || symbol!=InpSymbol_3 || symbol!=InpSymbol_4)
            continue;

         MqlTick  tick;
         if(!SymbolInfoTick(symbol,tick))
            return;

         long magic=m_position.Magic();
         if(magic==MagicNumber1 ||magic==MagicNumber2  ||magic==MagicNumber3  ||
            magic==E_MagicNumber ||magic==MagicNumber4)
            if(m_position.Profit()>0.0)
              {
               m_trade.SetExpertMagicNumber(E_MagicNumber);
               if(m_position.PositionType()==POSITION_TYPE_BUY)
                  m_trade.Buy(m_position.Volume(),symbol,tick.ask,0.0,0.0,"Emergency Double");

               if(m_position.PositionType()==POSITION_TYPE_SELL)
                  m_trade.Sell(m_position.Volume(),symbol,tick.bid,0.0,0.0,"Emergency Double");
              }
        }
   enable_emergency_trading=false;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool VerifyMarketWatch(const string name_1,const string name_2,const string name_3,const string name_4)
  {
   bool result=false;
   SymbolSelect(name_1,true);
   SymbolSelect(name_2,true);
   SymbolSelect(name_3,true);
   SymbolSelect(name_4,true);
   Sleep(1000);
   if(!SymbolSelect(name_1,true) || !SymbolSelect(name_2,true) || 
      !SymbolSelect(name_3,true) || !SymbolSelect(name_4,true))
      return(result);
   else
      return(true);
  }
//+------------------------------------------------------------------+ 
//| Get Close for specified bar index                                | 
//+------------------------------------------------------------------+ 
double iClose(const int index,string symbol=NULL,ENUM_TIMEFRAMES timeframe=PERIOD_CURRENT)
  {
   if(symbol==NULL)
      symbol=Symbol();
   if(timeframe==0)
      timeframe=Period();
   double Close[1];
   double close=0;
   int copied=CopyClose(symbol,timeframe,index,1,Close);
   if(copied>0)
      close=Close[0];
   return(close);
  }
//+------------------------------------------------------------------+
//| Gets the information about permission to trade                   |
//+------------------------------------------------------------------+
bool IsTradeAllowed()
  {
   if(!TerminalInfoInteger(TERMINAL_TRADE_ALLOWED))
     {
      Alert("Check if automated trading is allowed in the terminal settings!");
      return(false);
     }
   if(!TerminalInfoInteger(TERMINAL_TRADE_ALLOWED))
     {
      Alert("Check if automated trading is allowed in the terminal settings!");
      return(false);
     }
   else
     {
      if(!MQLInfoInteger(MQL_TRADE_ALLOWED))
        {
         Alert("Automated trading is forbidden in the program settings for ",__FILE__);
         return(false);
        }
     }
   if(!AccountInfoInteger(ACCOUNT_TRADE_EXPERT))
     {
      Alert("Automated trading is forbidden for the account ",AccountInfoInteger(ACCOUNT_LOGIN),
            " at the trade server side");
      return(false);
     }
   if(!AccountInfoInteger(ACCOUNT_TRADE_ALLOWED))
     {
      Comment("Trading is forbidden for the account ",AccountInfoInteger(ACCOUNT_LOGIN),
              ".\n Perhaps an investor password has been used to connect to the trading account.",
              "\n Check the terminal journal for the following entry:",
              "\n\'",AccountInfoInteger(ACCOUNT_LOGIN),"\': trading has been disabled - investor mode.");
      return(false);
     }
//---
   return(true);
  }
//+------------------------------------------------------------------+
//| Close Positions                                                  |
//+------------------------------------------------------------------+
void ClosePositions()
  {
   for(int i=PositionsTotal()-1;i>=0;i--) // returns the number of open positions
      if(m_position.SelectByIndex(i)) // selects the position by index for further access to its properties
        {
         long magic=m_position.Magic();
         if(magic==MagicNumber1 ||magic==MagicNumber2  ||magic==MagicNumber3  ||
            magic==E_MagicNumber ||magic==MagicNumber4)
           {
            m_trade.PositionClose(m_position.Ticket());
           }
        }
  }
//+------------------------------------------------------------------+ 
//| Checks if the specified filling mode is allowed                  | 
//+------------------------------------------------------------------+ 
bool IsFillingTypeAllowed(string symbol,int fill_type)
  {
//--- Obtain the value of the property that describes allowed filling modes 
   int filling=(int)SymbolInfoInteger(symbol,SYMBOL_FILLING_MODE);
//--- Return true, if mode fill_type is allowed 
   return((filling & fill_type)==fill_type);
  }
//+------------------------------------------------------------------+
