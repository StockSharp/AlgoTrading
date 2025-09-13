//+------------------------------------------------------------------+
//|                                                  TradeReport.mqh |
//|                              Copyright (c) 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#include <MQL5Book/DealFilter.mqh>
#include <MQL5Book/AutoPtr.mqh>

//+------------------------------------------------------------------+
//| Main class for trade statistics calculation                      |
//+------------------------------------------------------------------+
class TradeReport
{
   double balance;  // on-the-fly totaling
   double floating; // on-the-fly change

   double data[];   // entire period balance curve
   datetime moments[];
   
public:
   struct DrawDown
   {
      double maxpeak;
      double minpeak;
      
      double
      series_start,
      series_min,
      series_dd,
      series_dd_percent,
      series_dd_relative_percent,
      series_dd_relative;
      
      DrawDown()
      {
         reset();
      }
      
      void reset()
      {
         ZeroMemory(this);
         series_min = DBL_MAX;
      }
      
      void calcDrawdown(const double &data[])
      {
         reset();
         for(int i = 0; i < ArraySize(data); ++i)
         {
            calcDrawdown(data[i]);
         }
      }
      
      void calcDrawdown(const double amount)
      {
         if(series_start == 0.0) series_start = amount;
         if(amount < series_min) series_min = amount;
      
         if(maxpeak == 0.0) maxpeak = amount;
         if(minpeak == 0.0) minpeak = amount;
      
         // check of extremum condition
         if(amount > maxpeak || amount < minpeak)
         {
            if(amount > maxpeak) maxpeak = amount;
            minpeak = amount;

            const double drawdown = maxpeak - minpeak;
            const double drawdown_percent = drawdown / maxpeak * 100.0;
      
            if(series_dd_relative_percent < drawdown_percent)
            {
               series_dd_relative_percent = drawdown_percent;
               series_dd_relative = drawdown;
            }
      
            if(series_dd < drawdown)
            {
               series_dd = drawdown;
               series_dd_percent = drawdown_percent;
            }
         }
      }

      void print() const
      {
         DrawDown temp[1];
         temp[0] = this;
         ArrayPrint(temp, 2);
      }
   };
   
   struct GenericStats: public DrawDown
   {
      long deals;
      long trades;
      long buy_trades;
      long wins;
      long buy_wins;
      long sell_wins;
      
      double profits;
      double losses;
      double net;
      double pf;
      double average_trade;
      double recovery;
      
      double max_profit;
      double max_loss;
      
      double sharpe;
      
      GenericStats()
      {
         ZeroMemory(this);
      }
      
      void fillByTester()
      {
         deals = (long)TesterStatistics(STAT_DEALS);
         trades = (long)TesterStatistics(STAT_TRADES);
         buy_trades = (long)TesterStatistics(STAT_LONG_TRADES);
         wins = (long)TesterStatistics(STAT_PROFIT_TRADES);
         buy_wins = (long)TesterStatistics(STAT_PROFIT_LONGTRADES);
         sell_wins = (long)TesterStatistics(STAT_PROFIT_SHORTTRADES);
         
         profits = TesterStatistics(STAT_GROSS_PROFIT);
         losses = TesterStatistics(STAT_GROSS_LOSS);
         net = TesterStatistics(STAT_PROFIT);
         pf = TesterStatistics(STAT_PROFIT_FACTOR);
         average_trade = TesterStatistics(STAT_EXPECTED_PAYOFF);
         recovery = TesterStatistics(STAT_RECOVERY_FACTOR);
         sharpe = TesterStatistics(STAT_SHARPE_RATIO);
         
         max_profit = TesterStatistics(STAT_MAX_PROFITTRADE);
         max_loss = TesterStatistics(STAT_MAX_LOSSTRADE);
         
         series_start = TesterStatistics(STAT_INITIAL_DEPOSIT);
         series_min = TesterStatistics(STAT_EQUITYMIN);
         series_dd = TesterStatistics(STAT_EQUITY_DD);
         series_dd_percent = TesterStatistics(STAT_EQUITYDD_PERCENT);
         series_dd_relative_percent = TesterStatistics(STAT_EQUITY_DDREL_PERCENT);
         series_dd_relative = TesterStatistics(STAT_EQUITY_DD_RELATIVE);
      }
      
      void print() const
      {
         GenericStats temp[1];
         temp[0] = this;
         ArrayPrint(temp, 2);
      }
   };
   
   TradeReport()
   {
      balance = AccountInfoDouble(ACCOUNT_BALANCE);
   }

   void resetFloatingPL()
   {
      floating = 0;
   }

   void addFloatingPL(const double pl)
   {
      floating += pl;
   }
   
   void addBalance(const double pl)
   {
      balance += pl;
   }
   
   double getCurrent() const
   {
      return balance + floating;
   }
   
   static double calcSharpe(const double &data[], const double riskFreeRate = 0)
   {
      const int limit = ArraySize(data);
      if(limit < 3)
      {
          Print("Too short array for Sharpe calculation: ", limit);
          return 0;
      }
      
      double AHPR = 0, Std = 0;
      double HPR[];
      ArrayResize(HPR, limit);
      const int n = limit - 1;
      for(int i = 1; i < limit; i++)
      {
         if(data[i - 1] != 0) 
         {
            HPR[i - 1] = (data[i] - data[i - 1]) / data[i - 1];
            AHPR += HPR[i - 1];
         }
      }
      AHPR = AHPR / n;
 
      for(int i = 0; i < n - 1; i++)
      {
         Std += (AHPR - HPR[i]) * (AHPR - HPR[i]);
      }
      Std = sqrt(Std / (n - 1));
      if(Std == 0) return 0;
      return (AHPR - riskFreeRate) / Std;
   }

   GenericStats calcStatistics(DealFilter &filter, const double start = 0, const datetime origin = 0, const double riskFreeRate = 0)
   {
      GenericStats stats;
      ArrayResize(data, 0);
      ArrayResize(moments, 0);
      ulong tickets[];
      if(!filter.select(tickets)) return stats;
      
      balance = start;
      PUSH(data, balance);
      PUSH(moments, origin);
      
      for(int i = 0; i < ArraySize(tickets); ++i)
      {
         DealMonitor m(tickets[i]);
         if(m.get(DEAL_TYPE) == DEAL_TYPE_BALANCE) // deposit/withdrawal
         {
            balance += m.get(DEAL_PROFIT);
            PUSH(data, balance);
            PUSH(moments, (datetime)m.get(DEAL_TIME));
         }
         else if(m.get(DEAL_TYPE) == DEAL_TYPE_BUY || m.get(DEAL_TYPE) == DEAL_TYPE_SELL)
         {
            const double profit = m.get(DEAL_PROFIT) + m.get(DEAL_SWAP) + m.get(DEAL_COMMISSION) + m.get(DEAL_FEE);
            balance += profit;
            
            stats.deals++;
            if(m.get(DEAL_ENTRY) == DEAL_ENTRY_OUT || m.get(DEAL_ENTRY) == DEAL_ENTRY_INOUT
               || m.get(DEAL_ENTRY) == DEAL_ENTRY_OUT_BY)
            {
               PUSH(data, balance);
               PUSH(moments, (datetime)m.get(DEAL_TIME));
               stats.trades++;
               if(m.get(DEAL_TYPE) == DEAL_TYPE_SELL)
               {
                  stats.buy_trades++; // close made in opposite direction
               }
               if(profit >= 0)
               {
                  stats.wins++;
                  if(m.get(DEAL_TYPE) == DEAL_TYPE_BUY)
                  {
                     stats.sell_wins++;
                  }
                  else
                  {
                     stats.buy_wins++;
                  }
               }
            }
            else if(!TU::Equal(profit, 0))
            {
               PUSH(data, balance); // entry commission (if any)
               PUSH(moments, (datetime)m.get(DEAL_TIME));
            }
            
            if(profit >= 0)
            {
               stats.profits += profit;
               stats.max_profit = fmax(profit, stats.max_profit);
            }
            else
            {
               stats.losses += profit;
               stats.max_loss = fmin(profit, stats.max_loss);
            }
         }
      }
      
      if(stats.trades > 0)
      {
         stats.net = stats.profits + stats.losses;
         stats.pf = -stats.losses > DBL_EPSILON ? stats.profits / -stats.losses : MathExp(10000.0); // NaN(+inf)
         stats.average_trade = stats.net / stats.trades;
         stats.sharpe = calcSharpe(data, riskFreeRate);
         stats.calcDrawdown(data);             // fills all fields from DrawDown sub-struct
         stats.recovery = stats.series_dd > DBL_EPSILON ? stats.net / stats.series_dd : MathExp(10000.0);
      }
      
      return stats;
   }
   
   void getCurve(double &output[], datetime &timeline[])
   {
      ArraySwap(output, data);
      ArraySwap(timeline, moments);
   }
};
//+------------------------------------------------------------------+
