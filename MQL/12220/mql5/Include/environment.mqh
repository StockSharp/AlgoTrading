//+------------------------------------------------------------------+
//|                                                  Environment.mqh |
//|                                 Copyright 2015, Vasiliy Sokolov. |
//|                                              http://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "Copyright 2015, Vasiliy Sokolov."
#property link      "http://www.mql5.com"
#include <Prototypes.mqh>
//+------------------------------------------------------------------+
//|                                                  Environment.mqh |
//|                                 Copyright 2015, Vasiliy Sokolov. |
//|                                              http://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "Copyright 2015, Vasiliy Sokolov."
#property link      "http://www.mql5.com"
#include <Prototypes.mqh>
//+------------------------------------------------------------------+
//| Environment.mqh                                                  |
//| Copyright 2015, Vasiliy Sokolov.                                 |
//| http://www.mql5.com                                              |
//+------------------------------------------------------------------+
#property copyright "Copyright 2015, Vasiliy Sokolov."
#property link "http://www.mql5.com"
#include <Prototypes.mqh>
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
/*
   The class provides a convenient interface to a variety of trade statistics,
   to get expert. For example it can be used to learn
   the number of long or short positions (GetPositionsLongTotal/GetPositionsShortTotal)
   the average weighted price of entry all positions belonging to the expert (AveragePrice())
   and other statistics.
*/

//+------------------------------------------------------------------+
//| The class contains the current environment expert.               |
//+------------------------------------------------------------------+
class CEnvironment
  {
private:
   uint              m_magic;                // Magic number.
   int               m_positions_long;       // Total number long positions.
   int               m_positions_short;      // Total number of short positions.
   datetime          m_bar_time_open;        // the Last memorized the opening time of the bar for NewBarDetected.
   ulong             m_last_id;              // Ticket last position.
   double            m_avrg_price;           // Average opening price of all positions.
   datetime          m_first_pos_open;       // Time of opening of the first position.
   double            m_equity_high;          // Maximum stored value of equity all positions. 
   double            m_curr_profit;          // the Profit of all current positions.
   int               GetDynamicPips(void);
public:
                     CEnvironment();
   void              Refresh();
   void              SetMagic(uint mg);
   uint              GetMagic(void);
   bool              NewBarDetected(void);
   int               GetPositionsTotal(void);
   int               GetPositionsLongTotal(void);
   int               GetPositionsShortTotal(void);
   bool              IsMainPosition(void);
   bool              IsLastPositionLost(void);
   ulong             LastPositionId();
   double            AveragePrice(void);
   datetime          FirstPosOpen(void);
   double            GetHighEquity();
   double            CurrentProfit();
   ENUM_DIRECTION_TYPE CurrentDirection();
   bool              DoubleEquals(const double a,const double b);
  };
//+------------------------------------------------------------------+
//| Constructor by default,                                          |
//+------------------------------------------------------------------+
CEnvironment::CEnvironment()
  {
//---
   m_magic=0;
   m_positions_long=0;
   m_positions_short=0;
   m_bar_time_open=0;
   m_last_id=0;
   m_avrg_price=0.0;
   m_first_pos_open=0;
   m_equity_high = 0.0;
   m_curr_profit = 0.0;
//---
  }
//+------------------------------------------------------------------+
//| Sets the magic number.                                           |
//+------------------------------------------------------------------+
void CEnvironment::SetMagic(uint mg)
  {
   m_magic=mg;
  };
//+------------------------------------------------------------------+
//| Sets the magic number.                                           |
//+------------------------------------------------------------------+
uint CEnvironment::GetMagic(void)
  {
   return m_magic;
  };
//+------------------------------------------------------------------+
//| The function returns true if the current position belongs        |
//| the expert and they can be processed. Returns false if not       |
//| case.                                                            | 
//+------------------------------------------------------------------+
bool CEnvironment::IsMainPosition(void)
  {
//---
   if(TransactionType() != TRANS_HEDGE_POSITION)return false;
   if(HedgePositionGetString(HEDGE_POSITION_SYMBOL) != Symbol())return false;
   if(HedgePositionGetInteger(HEDGE_POSITION_MAGIC) != m_magic)return false;
   return true;
//---
  }
//+------------------------------------------------------------------+
//| Updates the environment expert                                   |
//+------------------------------------------------------------------+
CEnvironment::Refresh(void)
  {
//---
   m_positions_long=0;
   m_positions_short=0;
   m_avrg_price=0.0;
   m_first_pos_open=0;
   m_curr_profit=0.0;
   m_last_id=0;
   double count=0.0;
   double equity=0.0;
   if(TransactionsTotal()==0)
      m_equity_high=0.0;
   FOREACH_POSITION
     {
      if(!TransactionSelect(i))continue;
      if(TransactionType()!=TRANS_HEDGE_POSITION)continue;
      if(HedgePositionGetString(HEDGE_POSITION_SYMBOL) != Symbol())continue;
      if(HedgePositionGetInteger(HEDGE_POSITION_MAGIC) != m_magic)continue;
      equity+=HedgePositionGetDouble(HEDGE_POSITION_PROFIT_CURRENCY);
      datetime open=(datetime)(HedgePositionGetInteger(HEDGE_POSITION_ENTRY_TIME_EXECUTED_MSC)/1000);
      if(m_first_pos_open == 0 || m_first_pos_open > open)
         m_first_pos_open = open;
      ulong curr_id=HedgePositionGetInteger(HEDGE_POSITION_ENTRY_ORDER_ID);
      double vol=HedgePositionGetDouble(HEDGE_POSITION_VOLUME);
      m_avrg_price+=HedgePositionGetDouble(HEDGE_POSITION_PRICE_OPEN)*vol;
      count+=vol;
      if(curr_id>m_last_id)
         m_last_id=curr_id;
      IF_LONG
      m_positions_long++;
      IF_SHORT
      m_positions_short++;
     }
   if(equity>m_equity_high)
      m_equity_high=equity;
   m_curr_profit= equity;
   m_avrg_price = DoubleEquals(count,0.0) ? 0.0 : NormalizeDouble(m_avrg_price/count,Digits());
//---
  }
//+------------------------------------------------------------------+
//| Compares two values of type double.                              |
//| RESULT                                                           |
//| Returns true if the values are equal and                         |
//| false in the opposite case.                                      |
//+------------------------------------------------------------------+
bool CEnvironment::DoubleEquals(const double a,const double b)
  {
//---
   return(fabs(a-b)<=16*DBL_EPSILON*fmax(fabs(a),fabs(b)));
//---
  }
//+------------------------------------------------------------------+
//| Returns the number of long positions expert.                     |
//+------------------------------------------------------------------+
int CEnvironment::GetPositionsTotal(void)
  {
   return m_positions_long + m_positions_short;
  }
//+------------------------------------------------------------------+
//| Returns the number of long positions expert.                     |
//+------------------------------------------------------------------+
int CEnvironment::GetPositionsLongTotal(void)
  {
   return m_positions_long;
  }
//+------------------------------------------------------------------+
//| Returns the number of short positions expert.                    |
//+------------------------------------------------------------------+
int CEnvironment::GetPositionsShortTotal(void)
  {
   return m_positions_short;
  }
//+------------------------------------------------------------------+
//| Returns the current direction of all of your opened positions.   |
//| If no position is returns DIRECTION_UNDEFINED                    |
//+------------------------------------------------------------------+
ENUM_DIRECTION_TYPE CEnvironment::CurrentDirection(void)
  {
   if(m_positions_long)return DIRECTION_LONG;
   else if(m_positions_short)return DIRECTION_SHORT;
   return DIRECTION_UNDEFINED;
  }
//+------------------------------------------------------------------+
//| Returns the ID of the last open position. If                     |
//| active positions not, returns null.                              |
//+------------------------------------------------------------------+
ulong CEnvironment::LastPositionId(void)
  {
   if(GetPositionsTotal()==0)
      m_last_id=0;
   return m_last_id;
  }
//+------------------------------------------------------------------+
//| Returns true, if you have defined a new bar. |
//| Returns false otherwise. | 
//+------------------------------------------------------------------+
bool CEnvironment::NewBarDetected(void)
  {
//---
   datetime time[];
   if(CopyTime(Symbol(),PERIOD_CURRENT,0,1,time)>0 && 
      m_bar_time_open==time[0])
     {
      return false;
     }
   m_bar_time_open=time[0];
   return true;
//---
  }
//+------------------------------------------------------------------+
//| Returns the average price of all positions belonging to the expert |
//+------------------------------------------------------------------+
double CEnvironment::AveragePrice(void)
  {
   return m_avrg_price;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
datetime CEnvironment::FirstPosOpen(void)
  {
   return m_first_pos_open;
  }
//+------------------------------------------------------------------+
//| Returns the maximum equity that has been made within             |
//| observation positions.                                           |
//+------------------------------------------------------------------+
double CEnvironment::GetHighEquity(void)
  {
   return m_equity_high;
  }
//+------------------------------------------------------------------+
//| Returns the current value of profit for all open positions       |
//| expert.                                                          |
//+------------------------------------------------------------------+
double CEnvironment::CurrentProfit()
  {
   return m_curr_profit;
  }
//+------------------------------------------------------------------+
