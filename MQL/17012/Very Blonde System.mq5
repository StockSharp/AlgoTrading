//+------------------------------------------------------------------+
//|                  Very Blonde System(barabashkakvn's edition).mq5 |
//|                                                            David |
//|                                               Broker77@gmail.com |
//+------------------------------------------------------------------+
#property copyright "David"
#property link      "broker77@gmail.com"
#property version   "1.001"

#define MODE_LOW 1
#define MODE_HIGH 2

#include <Trade\PositionInfo.mqh>
#include <Trade\Trade.mqh>
#include <Trade\SymbolInfo.mqh>  
#include <Trade\AccountInfo.mqh>
#include <Trade\DealInfo.mqh>
#include <Trade\OrderInfo.mqh>
CPositionInfo  m_position;                   // trade position object
CTrade         m_trade;                      // trading object
CSymbolInfo    m_symbol;                     // symbol info object
CAccountInfo   m_account;                    // account info wrapper
CDealInfo      m_deal;                       // deals object
COrderInfo     m_order;                      // pending orders object

#define MAGICMA  20081109

input ushort   CountBars      = 10;    // CountBars
input ushort   InpLimit       = 240;   // Change in price
input ushort   InpGrid        = 35;    // Grid
input double   Amount         = 40;    // Amount 
input ushort   InpLockDown    = 0;     // LockDown

double ExtGrid                = 0.0;
double ExtLockDown            = 0.0;
double ExtLimit               = 0.0;
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//---
   if(Bars(Symbol(),Period())<100)
     {
      Print("On graphics less than 100 bars");
      return(INIT_FAILED);
     }
//---
   m_symbol.Name(Symbol());                  // sets symbol name
   m_trade.SetExpertMagicNumber(MAGICMA);    // sets magic number
   if(!RefreshRates())
     {
      Print("Error RefreshRates. Bid=",DoubleToString(m_symbol.Bid(),Digits()),
            ", Ask=",DoubleToString(m_symbol.Ask(),Digits()));
      return(INIT_FAILED);
     }

//--- tuning for 3 or 5 digits
   int digits_adjust=1;
   if(m_symbol.Digits()==3 || m_symbol.Digits()==5)
      digits_adjust=10;

   ExtGrid        = InpGrid*digits_adjust;
   ExtLockDown    = InpLockDown*digits_adjust;
   ExtLimit       = InpLimit*digits_adjust;

   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
//--- check for history and trading
   if(!IsTradeAllowed())
      return;
//--- calculate open positions by current symbol
   int count_pos=0;
   for(int i=PositionsTotal()-1;i>=0;i--)
      if(m_position.SelectByIndex(i))
         if(m_position.Symbol()==Symbol() && m_position.Magic()==MAGICMA)
            count_pos++;

   if(count_pos==0)
      CheckForOpen();
   else
      CheckForClose();
//---
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CheckForOpen()
  {
   double L = iLowest(Symbol(),Period(),MODE_LOW,CountBars,0);
   double H = iHighest(Symbol(),Period(),MODE_HIGH,CountBars,0);
   double Lots=MathRound(m_account.Balance()/100)/1000;
   Lots=LotCheck(Lots);
   if(Lots==0)
      return;

   if(!RefreshRates())
      return;

   if((H-m_symbol.Bid()>ExtLimit*Point()))
     {
      if(m_trade.Buy(Lots,Symbol(),m_symbol.Ask()))
         for(int i=1; i<5; i++)
           {
            double volume=MathPow(2,i)*Lots;
            volume=LotCheck(volume);
            if(volume==0)
               return;
            m_trade.BuyLimit(volume,m_symbol.Ask()-i*ExtGrid*Point(),Symbol());
           }
      return;
     }

   if((m_symbol.Bid()-L>ExtLimit*Point()))
     {
      if(m_trade.Sell(Lots,Symbol(),m_symbol.Bid()))
         for(int j=1; j<5; j++)
           {
            double volume=MathPow(2,j)*Lots;
            volume=LotCheck(volume);
            if(volume==0)
               return;
            m_trade.SellLimit(volume,m_symbol.Bid()+j*ExtGrid*Point(),Symbol());
           }
      return;
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CheckForClose()
  {
   if(getProfit()>=Amount)
      CloseAll();

   if(ExtLockDown<=0)
      return;

   for(int i=PositionsTotal()-1;i>=0;i--)
      if(m_position.SelectByIndex(i))
         if(m_position.Symbol()==Symbol() && m_position.Magic()==MAGICMA)
           {
            if(!RefreshRates())
               return;
            if(m_position.PositionType()==POSITION_TYPE_BUY)
               if((m_symbol.Bid()-m_position.PriceOpen()>Point()*ExtLockDown) && (m_position.StopLoss()==0))
                 {
                  m_trade.PositionModify(m_position.Ticket(),m_position.PriceOpen()+Point(),m_position.TakeProfit());
                 }
            if(m_position.PositionType()==POSITION_TYPE_SELL)
               if((m_position.PriceOpen()-m_symbol.Ask()>Point()*ExtLockDown) && (m_position.StopLoss()==0))
                 {
                  m_trade.PositionModify(m_position.Ticket(),m_position.PriceOpen()-Point(),m_position.TakeProfit());
                 }
           }
  }
//+------------------------------------------------------------------+
//| Get profit for positions                                         |
//+------------------------------------------------------------------+
double getProfit()
  {
   double Profit=0;
   for(int i=PositionsTotal()-1;i>=0;i--)
      if(m_position.SelectByIndex(i))
         if(m_position.Symbol()==Symbol() && m_position.Magic()==MAGICMA)
            Profit+=m_position.Profit();
   return (Profit);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CloseAll()
  {
   for(int i=PositionsTotal()-1;i>=0;i--)
      if(m_position.SelectByIndex(i))
         if(m_position.Symbol()==Symbol() && m_position.Magic()==MAGICMA)
           {
            if(!m_trade.PositionClose(m_position.Ticket()))
              {
               uint     retcode=m_trade.ResultRetcode();
               string   retcode_description = m_trade.ResultRetcodeDescription();
               string   comment             = m_trade.ResultComment();
               Print("Error position close. Ticket ",m_position.Ticket(),
                     ", Retcode ",retcode,
                     ", RetcodeDescription ",retcode_description,
                     ", Comment ",comment);
              }
           }

   for(int i=OrdersTotal()-1;i>=0;i--)
      if(m_order.SelectByIndex(i))
         if(m_order.Symbol()==Symbol() && m_order.Magic()==MAGICMA)
           {
            if(!m_trade.OrderDelete(m_order.Ticket()))
              {
               uint     retcode=m_trade.ResultRetcode();
               string   retcode_description = m_trade.ResultRetcodeDescription();
               string   comment             = m_trade.ResultComment();
               Print("Error order delete. Ticket ",m_position.Ticket(),
                     ", Retcode ",retcode,
                     ", RetcodeDescription ",retcode_description,
                     ", Comment ",comment);
              }
           }
  }
//+------------------------------------------------------------------+
//| Refreshes the symbol quotes data                                 |
//+------------------------------------------------------------------+
bool RefreshRates()
  {
//--- refresh rates
   if(!m_symbol.RefreshRates())
      return(false);
//--- protection against the return value of "zero"
   if(m_symbol.Ask()==0 || m_symbol.Bid()==0)
      return(false);
//---
   return(true);
  }
//+------------------------------------------------------------------+
//|  Gets the information about permission to trade                  |
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
//| Lot Check                                                        |
//+------------------------------------------------------------------+
double LotCheck(double lots)
  {
//--- calculate maximum volume
   double volume=NormalizeDouble(lots,2);
   double stepvol=SymbolInfoDouble(Symbol(),SYMBOL_VOLUME_STEP);
   if(stepvol>0.0)
      volume=stepvol*MathFloor(volume/stepvol);
//---
   double minvol=SymbolInfoDouble(Symbol(),SYMBOL_VOLUME_MIN);
   if(volume<minvol)
      volume=0.0;
//---
   double maxvol=SymbolInfoDouble(Symbol(),SYMBOL_VOLUME_MAX);
   if(volume>maxvol)
      volume=maxvol;
   return(volume);
  }
//+------------------------------------------------------------------+
