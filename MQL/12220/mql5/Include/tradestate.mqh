//+------------------------------------------------------------------+
//|                                                   TradeState.mqh |
//|                                 Copyright 2015, Vasiliy Sokolov. |
//|                                              http://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "Copyright 2015, Vasiliy Sokolov."
#property link      "http://www.mql5.com"

/*
   In MetaTrader 5, after the trade order (the order)
   marks, runs for some time (1-2 msec.) before
   he enters the story warrants. Therefore, to obtain information
   on the newly opened position is not possible. To
   to correct this situation, use the class CTradeState. He
   remembers trading environment, and expects from 2 to 30,000 milliseconds
   waiting for the change in the trading environment (function ChangedState()).
*/
//+------------------------------------------------------------------+
//| Class stores the number of transactions (method RememberDeals), and |
//| if the trading environment changes, the method ChangedState      |
//| returns true.                                                    |
//+------------------------------------------------------------------+
class CTradeState
{
private:
   int      m_deals_count;
   int      m_limit_msc;
   bool     m_is_remember;
   bool     m_is_print;
   datetime StartTime(void);
public:
   CTradeState();
   void SetLimitWaitMsc(int limit);
   int GetLimitWaitMsc();
   void RememberDeals(void);
   bool ChangedState();
   void PrintSleeping(bool isPrint);
};
//+------------------------------------------------------------------+
//| The default constructor.                                         |
//+------------------------------------------------------------------+
CTradeState::CTradeState()
{
   m_deals_count = 0;
   m_limit_msc = 3000;
   m_is_remember = false;
   m_is_print = false;
}
//+------------------------------------------------------------------+
//| Sets reprintable number of nasypany.                             |
//+------------------------------------------------------------------+
void CTradeState::PrintSleeping(bool isPrint)
{
   m_is_print = isPrint;
}

//+-------------------------------------------------------------------+
//| Sets the limit in milliseconds for which the function             |
//| ChangedState expects changes in the trade environment.            |
//+-------------------------------------------------------------------+
void CTradeState::SetLimitWaitMsc(int limit_msc)
{
   if(limit_msc < 0)
      m_limit_msc = 3000;
   if(limit_msc > 30000)
   {
      printf("Too large limit. Set 30000 msec.");
      m_limit_msc = 3000;
   }
   m_limit_msc = limit_msc;
}
//+------------------------------------------------------------------+
//| Returns the limit in milliseconds for which the function         |
//| ChangedState expects changes in the trade environment.           |
//+------------------------------------------------------------------+
int CTradeState::GetLimitWaitMsc(void)
{
   return m_limit_msc;
}
//+------------------------------------------------------------------+
//| Returns the start time of the EA/indicator/library.              |
//+------------------------------------------------------------------+
datetime CTradeState::StartTime(void)
{
   return TimeCurrent() - GetTickCount()/1000;
}
//+------------------------------------------------------------------+
//| Stores the number of transactions.                               |
//+------------------------------------------------------------------+
void CTradeState::RememberDeals(void)
{
//---   
   HistorySelect(StartTime(), TimeCurrent());
   m_deals_count = HistoryDealsTotal();
   m_is_remember = true;
//---   
}
//+------------------------------------------------------------------+
//| Returns true if trading environment within .                     |
//+------------------------------------------------------------------+
bool CTradeState::ChangedState(void)
{
   if(!m_is_remember)
   {
      printf("State is not remember");
      return false;
   }
   for(int i = 1;;i++)
   {
      HistorySelect(StartTime(), TimeCurrent());
      int sleep = (int)MathPow(2, i);
      if(HistoryDealsTotal() != m_deals_count ||
         sleep > m_limit_msc)
      {
         m_is_remember = false;
         m_deals_count = 0;
         return sleep < m_limit_msc;
      }
      if(m_is_print)
         printf("Sleep " + (string)sleep + " milliseconds...");
      Sleep(sleep);
   }
   return false;
}
