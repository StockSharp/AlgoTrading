//+------------------------------------------------------------------+
//|                                                Basket Manager EA |
//|                                  Copyright 2025, Yashar Seyyedin |
//|                    https://www.mql5.com/en/users/yashar.seyyedin |
//+------------------------------------------------------------------+
#include <Trade\Trade.mqh>
CTrade trade;

input int n=3;

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void OnTick()
  {
   if(PositionsTotal()>=n)
     {
      if(AccountInfoDouble(ACCOUNT_EQUITY)>AccountInfoDouble(ACCOUNT_BALANCE))
        {
         while(PositionsTotal()>0)
            trade.PositionClose(PositionGetTicket(0));
        }
     }
  }

#include <Trade\DealInfo.mqh>
CDealInfo      m_deal;

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void OnTradeTransaction(const MqlTradeTransaction& trans,
                        const MqlTradeRequest& request,
                        const MqlTradeResult& result)
  {
   if(PositionsTotal()!=n)
      return;
   if(AccountInfoDouble(ACCOUNT_EQUITY)<AccountInfoDouble(ACCOUNT_BALANCE))
      return;
   if(trans.type==TRADE_TRANSACTION_DEAL_ADD)
     {
      if(HistoryDealSelect(trans.deal))
         m_deal.Ticket(trans.deal);
      else
         return;

      long deal_entry=-1;
      m_deal.InfoInteger(DEAL_ENTRY,deal_entry);

      long ticket=0;
      m_deal.InfoInteger(DEAL_POSITION_ID, ticket);

      if(deal_entry==DEAL_ENTRY_IN)
        {
         while(PositionsTotal()>1)
            if(PositionGetTicket(0)!=ticket)
               trade.PositionClose(PositionGetTicket(0));
        }
     }
  }
//+------------------------------------------------------------------+
