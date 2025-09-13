//+------------------------------------------------------------------+
//|                                                  MultiMartin.mq5 |
//|                                    Copyright (c) 2019, Marketeer |
//|                          https://www.mql5.com/en/users/marketeer |
//+------------------------------------------------------------------+
#property copyright "Copyright (c) 2019, Marketeer"
#property link "https://www.mql5.com/en/users/marketeer"
#property version "1.4"
#property description "Multi-currency expert adviser based on reversal strategy with martingale. Original idea is taken from ExpMartin for MT4.\n"


#include <MT4Bridge/MT4MarketInfo.mqh>
#include <MT4Bridge/MT4Time.mqh>
#include <MT4Bridge/MT4Account.mqh>
#include <MT4Bridge/MT4Orders.mqh>  // https://www.mql5.com/en/code/16006


enum BAD_TIME
{
  bt_NONE = 0,                  // none
  bt_SECOND = 1,                // second
  bt_MINUTE = 60,               // minute (M1)
  bt_HOUR = 60 * 60,            // hour (H1)
  bt_SESSION = 60 * 60 * 4,     // session (H4)
  bt_DAY = 60 * 60 * 24,        // day (D1)
  bt_MONTH = 60 * 60 * 24 * 30, // month (MN)
  bt_YEAR = 60 * 60 * 24 * 365, // year
  bt_FOREVER = UINT_MAX         // forever
};

enum TRAIL_TYPE
{
  tt_NONE,      // none
  tt_BREAKEVEN, // break-even
  tt_STRAIGHT   // straight
};


sinput string _G1 = "";      // S Y M B O L   S P E C I F I C   S E T T I N G S
input bool _UseTime = true;  // · UseTime (HourStart and HourEnd)
input int _HourStart = 2;    // · HourStart (0...23)
input int _HourEnd = 22;     // · HourEnd (0...23)
input double _Lots = 0.01;   // · Lots (initial)
input double _Factor = 2.0;  // · Factor (lot multiplication)
input int _Limit = 5;        // · Limit (max number of multiplications)
input int _StopLoss = 500;   // · StopLoss (points)
input int _TakeProfit = 500; // · TakeProfit (points)
input int _StartType = 0;    // · StartType (first order type: 0-BUY, 1-SELL)
sinput string _G2 = "";      // C O M M O N   S E T T I N G S
sinput int _Magic = 1000;    // · Magic
input BAD_TIME _SkipBadTime = bt_SECOND; // · SkipBadTime
input string _WorkSymbols = ""; // · WorkSymbols (name±lots*factor^limit(sl,tp)[start,stop];...)
input TRAIL_TYPE _Trail = tt_NONE; // · Trail stop


#define BIND_CLAZZ(clazz,field)  clazz *set##field(){this.field = _##field; return &this;}
#define BIND_SETTING(F)          BIND_CLAZZ(Settings,F)
#define _SET(field)              set##field()


class Settings // this should be a struct, but MQL doesn't support pointers to structs
{
  public:
    bool UseTime;
    int HourStart;
    int HourEnd;
    double Lots;
    double Factor;
    int Limit;
    int StopLoss;
    int TakeProfit;
    int StartType;
    int Magic;
    BAD_TIME SkipBadTime;
    string Symbol;
    TRAIL_TYPE Trail;

    void defaults()
    {
      UseTime = false;
      HourStart = HourEnd = 0;
      Lots = 0.01;
      Factor = 1;
      Limit = 1;
      StopLoss = 1000;
      TakeProfit = 1000;
      StartType = 0;
      Magic = 0;
      SkipBadTime = bt_NONE;
      Symbol = _Symbol;
      Trail = tt_NONE;
    }
    
    Settings()
    {
      defaults();
    }
    
    Settings(const string &line)
    {
      defaults();

      // syntax: name±lots*factor^limit(sl,tp)[start,stop];... both braces/brackets are optional
      // examples: EURUSD+0.01*2^5(500,1000)[2,22]
      //           EURUSD+0.01*2.0^7(500,500)[2,22];AUDJPY+0.01*2.0^8(300,500)[2,22];GBPCHF+0.01*1.7^8(1000,2000)[2,22]

      int p = StringFind(line, "+");
      if(p == -1) p = StringFind(line, "-");
      if(p == -1) return;
      
      Symbol = StringSubstr(line, 0, p);
      StartType = StringGetCharacter(line, p) == '+' ? 0 : 1;
      int q = StringFind(line, "*", ++p);
      if(q == -1) return;
      Lots = StringToDouble(StringSubstr(line, p, q - p));
      p = q + 1;
      q = StringFind(line, "^", p);
      if(q == -1) return;
      Factor = StringToDouble(StringSubstr(line, p, q - p));
      p = q + 1;
      q = StringFind(line, "(", p);
      if(q == -1)
      {
        Limit = (int)StringToInteger(StringSubstr(line, p));
        return;
      }
      Limit = (int)StringToInteger(StringSubstr(line, p, q - p));
      p = q + 1;
      q = StringFind(line, ")", p);
      if(q == -1) return;
      string sltp = StringSubstr(line, p, q - p);
      string sltps[];
      int r = StringSplit(sltp, ',', sltps);
      if(r == 2)
      {
        StopLoss = (int)StringToInteger(sltps[0]);
        TakeProfit = (int)StringToInteger(sltps[1]);
      }
      p = q + 1;
      q = StringFind(line, "[", p);
      if(q == -1) return;
      p = q + 1;
      q = StringFind(line, "]", p);
      if(q == -1) return;
      string hours = StringSubstr(line, p, q - p);
      string hourss[];
      r = StringSplit(hours, ',', hourss);
      if(r == 2)
      {
        HourStart = (int)StringToInteger(hourss[0]);
        HourEnd = (int)StringToInteger(hourss[1]);
        UseTime = true;
      }
    }

    // setters' sugar  
    BIND_SETTING(UseTime);
    BIND_SETTING(HourStart);
    BIND_SETTING(HourEnd);
    BIND_SETTING(Lots);
    BIND_SETTING(Factor);
    BIND_SETTING(Limit);
    BIND_SETTING(StopLoss);
    BIND_SETTING(TakeProfit);
    BIND_SETTING(StartType);
    BIND_SETTING(Magic);
    BIND_SETTING(SkipBadTime);
    BIND_SETTING(Trail);

    virtual bool validate()
    {
      if(TakeProfit <= 0)
      {
        TakeProfit = 1000;
        Print("Default TakeProfit applied: ", TakeProfit);
      }

      if(StopLoss <= 0)
      {
        StopLoss = TakeProfit;
        Print("Default StopLoss applied: ", StopLoss);
      }
      
      double minLot = MarketInfo(Symbol, MODE_MINLOT);
      if(Lots < minLot)
      {
        Lots = minLot;
        Print("Minimal lot ", (float)minLot, " is applied for ", Symbol);
      }
      
      double maxLot = MarketInfo(Symbol, MODE_MAXLOT);
      if(Lots > maxLot)
      {
        Lots = maxLot;
        Print("Maximal lot ", (float)maxLot, " is applied for ", Symbol);
      }
      
      
      // TODO: other check-ups
      
      MqlRates rates[1];
      bool success = CopyRates(Symbol, PERIOD_CURRENT, 0, 1, rates) > -1;
      if(!success)
      {
        Print("Unknown symbol: ", Symbol);
      }
      return success;
    }
    
    void print() const
    {
      Print(Symbol, (StartType == 0 ? "+" : "-"), (float)Lots,
        "*", (float)Factor,
        "^", Limit,
        "(", StopLoss, ",", TakeProfit, ")",
        UseTime ? "[" + (string)HourStart + "," + (string)HourEnd + "]": "");
    }
};


class SettingsParser
{
  private:
    Settings *settings[];

  public:
    SettingsParser(const string &line)
    {
      string symbols[];
      int n = StringSplit(line, ';', symbols);
      ArrayResize(settings, n);
      
      string hash = "";
      
      for(int i = 0, k = 0; i < n; i++, k++)
      {
        settings[i] = new Settings(symbols[k]);
        string signature = "^" + settings[i].Symbol + "$";
        bool duplicate = StringFind(hash, signature) > -1;
        if(!duplicate && settings[i].validate())
        {
          settings[i].print();
          hash += signature;
        }
        else
        {
          if(duplicate)
          {
            Print("Duplicate of symbol ", settings[i].Symbol, " skipped, only unique ones are allowed");
          }
          delete settings[i];
          i--;
          n--;
        }
      }
      ArrayResize(settings, n);
    }
    
    ~SettingsParser()
    {
      for(int i = 0; i < ArraySize(settings); i++)
      {
        if(CheckPointer(settings[i]) != POINTER_INVALID) delete settings[i];
      }
    }
    
    int size() const
    {
      return ArraySize(settings);
    }

    Settings *operator[](const int k)
    {
      if(ArraySize(settings) <= k) return NULL;
      return settings[k];
    }
};


class ExpMartin
{
  protected:
    Settings settings;
    
    double lots_step;
    
    long ticket_buy;
    long ticket_sell;
    double lots_test;
    double take_profit, stop_loss;
    double point;
    int symb_digits;
    
    bool bad_times;

  public:
    ExpMartin()
    {
    }
    
    ExpMartin(const Settings &state)
    {
      init(state);
    }
    
    int init(const Settings &state)
    {
      settings = state;
      bad_times = false;

      ticket_buy = -1;
      ticket_sell = -1;
  
      lots_step = MarketInfo(settings.Symbol, MODE_LOTSTEP);
      symb_digits = (int)SymbolInfoInteger(settings.Symbol, SYMBOL_DIGITS);

      lots_test = settings.Lots;
      
      for(int pos = 0; pos < settings.Limit; pos++)
        lots_test = MathFloor((lots_test * settings.Factor) / lots_step) * lots_step;

      double maxLot = MarketInfo(settings.Symbol, MODE_MAXLOT);
      if(lots_test > maxLot)
        lots_test = maxLot;

      // pick up existing orders (if any)
      for(int pos = OrdersTotal() - 1; pos >= 0; pos--)
      {
        if(OrderSelect(pos, SELECT_BY_POS) && OrderMagicNumber() == settings.Magic && OrderSymbol() == settings.Symbol)
        {
          if(OrderType() == OP_BUY)
          {
            ticket_buy = OrderTicket();
            break;
          }

          if(OrderType() == OP_SELL)
          {
            ticket_sell = OrderTicket();
            break;
          }
        }
      }
      
      point = MarketInfo(settings.Symbol, MODE_POINT);
      take_profit = settings.TakeProfit * point;
      stop_loss = settings.StopLoss * point;
  
      return 0;
    }

    int deinit()
    {
      return 0;
    }

    int trade() // the former start() function of the MT4's EA
    {
      double price;
      int slip;
      static bool once = true;
  
      if(ticket_buy > 0 && ticket_sell > 0)
      {
        if(once)
        {
          Print("> > > Incorrect state, the lock: ", ticket_buy, " ", ticket_sell);
          once = false;
        }
        return 0;
      }
      
      once = true;

      // virtual stop for BUY order
      if(ticket_buy > 0)
      {
        if(OrderSelect(ticket_buy, SELECT_BY_TICKET) && OrderCloseTime() == 0)
        {
          price = MarketInfo(settings.Symbol, MODE_BID);
          if(settings.Trail != tt_NONE && take_profit >= stop_loss) // trailing
          {
            const double step = MarketInfo(settings.Symbol, MODE_SPREAD) * point;
            
            if((settings.Trail == tt_STRAIGHT || price - OrderOpenPrice() > stop_loss || OrderStopLoss() == 0)
            && price - OrderStopLoss() > stop_loss + step)
            {
              OrderModify(OrderTicket(), OrderOpenPrice(), NormalizeDouble(price - stop_loss, symb_digits), OrderTakeProfit(), 0, 0);
            }
            return 0;
          }
          else
          {
            // const double passed = (double)(TimeCurrent() - OrderOpenTime()) / bt_DAY;
            const double coef = 1.0; // MathMax(1, MathPow(passed, 1.0 / settings.Factor));
            
            slip = MarketInfo(settings.Symbol, MODE_SPREAD, 0) * 2;
            if(OrderOpenPrice() + take_profit / coef <= price
            || OrderOpenPrice() - stop_loss / coef >= price)
            {
              OrderClose(ticket_buy, OrderLots(), price, slip, Blue);
            }
            else
            {
              return 0;
            }
          }
        }
      }
  
      // virtual stop for SELL order
      if(ticket_sell > 0)
      {
        if(OrderSelect(ticket_sell, SELECT_BY_TICKET) && OrderCloseTime() == 0)
        {
          price = MarketInfo(settings.Symbol, MODE_ASK);
          if(settings.Trail != tt_NONE && take_profit >= stop_loss) // trailing
          {
            const double step = MarketInfo(settings.Symbol, MODE_SPREAD) * point;
            
            if((settings.Trail == tt_STRAIGHT || OrderOpenPrice() - price > stop_loss || OrderStopLoss() == 0)
            && (OrderStopLoss() - price > stop_loss + step || OrderStopLoss() == 0))
            {
              OrderModify(OrderTicket(), OrderOpenPrice(), NormalizeDouble(price + stop_loss, symb_digits), OrderTakeProfit(), 0, 0);
            }
            return 0;
          }
          else
          {
            // const double passed = (double)(TimeCurrent() - OrderOpenTime()) / bt_DAY;
            const double coef = 1.0; // MathMax(1, MathPow(passed, 1.0 / settings.Factor));

            slip = MarketInfo(settings.Symbol, MODE_SPREAD, 0) * 2;
            if(OrderOpenPrice() - take_profit / coef >= price
            || OrderOpenPrice() + stop_loss / coef <= price)
            {
              OrderClose(ticket_sell, OrderLots(), price, slip, Red);
            }
            else
            {
              return 0;
            }
          }
        }
      }
  
  
      static datetime badDay = 0;
      
      if(settings.SkipBadTime > 0 && badDay == TimeCurrent() / settings.SkipBadTime * settings.SkipBadTime)
      {
        return 0;
      }
  
      // work hours
      if(settings.UseTime && !(Hour() >= settings.HourStart && Hour() < settings.HourEnd))
      {
        return 0;
      }
      
  
      double lots;
      long ticket;
  
      if(ticket_buy < 0 && ticket_sell < 0)
      {
        // open new BUY order
        if(settings.StartType == 0)
        {
          ticket = openBuy(settings.Lots);
  
          if(ticket > 0)
            ticket_buy = ticket;
        }
        else
        // open new SELL order
        if(settings.StartType == 1)
        {
          ticket = openSell(settings.Lots);
  
          if(ticket > 0)
            ticket_sell = ticket;
        }
      }
      else
      // open next order...
      if(ticket_buy > 0 && OrderSelect(ticket_buy, SELECT_BY_TICKET) && OrderCloseTime() > 0)
      {
        if(OrderProfit() >= 0.0) // BUY in case of previous profitable BUY
        {
          ticket = openBuy(settings.Lots);

          if(ticket > 0)
            ticket_buy = ticket;
        }
        else
        if(OrderProfit() < 0.0) // SELL in case of previous lossy BUY
        {
          lots = MathFloor((OrderLots() * settings.Factor) / lots_step) * lots_step;

          if(lots_test < lots)
          {
            lots = settings.Lots;
          }

          ticket = openSell(lots);

          if(ticket > 0)
          {
            ticket_sell = ticket;
            ticket_buy = -1;
            bad_times = false;
          }
          else
          {
            badDay = TimeCurrent() / settings.SkipBadTime * settings.SkipBadTime;
            if(!bad_times)
            {
              Print("Skipping bad time: ", badDay);
              bad_times = true;
            }
          }
        }
      }
      else
      // open next order...
      if(ticket_sell > 0 && OrderSelect(ticket_sell, SELECT_BY_TICKET) && OrderCloseTime() > 0)
      {
        if(OrderProfit() >= 0.0) // SELL in case of previous profitable SELL
        {
          ticket = openSell(settings.Lots);

          if(ticket > 0)
            ticket_sell = ticket;
        }
        else
        if(OrderProfit() < 0.0) // BUY in case of previous lossy SELL
        {
          lots = MathFloor((OrderLots() * settings.Factor) / lots_step) * lots_step;

          if(lots_test < lots)
          {
            lots = settings.Lots;
          }

          ticket = openBuy(lots);

          if(ticket > 0)
          {
            ticket_buy = ticket;
            ticket_sell = -1;
            bad_times = false;
          }
          else
          {
            badDay = TimeCurrent() / settings.SkipBadTime * settings.SkipBadTime;
            if(!bad_times)
            {
              Print("Skipping bad time: ", badDay);
              bad_times = true;
            }
          }
        }
      }
  
      return 0;
    }

  protected:
    bool checkFreeMargin(const int type, double &lots) const
    {
      if(AccountFreeMarginCheck(settings.Symbol, type, lots) <= 0)
      {
        // Fallback to starting lot due to insufficient margin
        lots = settings.Lots;
        if(AccountFreeMarginCheck(settings.Symbol, type, lots) <= 0)
        {
          return false;
        }
      }
      return true;
    }
    
    long openBuy(double lots) const
    {
      const double price = MarketInfo(settings.Symbol, MODE_ASK);
      const int slip = MarketInfo(settings.Symbol, MODE_SPREAD, 0) * 2;
      
      if(!checkFreeMargin(OP_BUY, lots)) return -1;
  
      return OrderSend(settings.Symbol, OP_BUY, lots, price, slip, 0.0, 0.0, "", settings.Magic, 0, Blue);
    }
    
    long openSell(double lots) const
    {
      const double price = MarketInfo(settings.Symbol, MODE_BID);
      const int slip = MarketInfo(settings.Symbol, MODE_SPREAD, 0) * 2;

      if(!checkFreeMargin(OP_SELL, lots)) return -1;
  
      return OrderSend(settings.Symbol, OP_SELL, lots, price, slip, 0.0, 0.0, "", settings.Magic, 0, Red);
    }
};


class ExpMartinPool
{
  private:
    ExpMartin *pool[];

  public:
    ExpMartinPool(const int reserve = 0)
    {
      ArrayResize(pool, 0, reserve);
    }

    ExpMartinPool(ExpMartin *instance)
    {
      push(instance);
    }
    
    ~ExpMartinPool()
    {
      for(int i = 0; i < ArraySize(pool); i++)
      {
        pool[i].deinit();
        delete pool[i];
      }
    }

    void push(ExpMartin *instance)
    {
      int n = ArraySize(pool);
      ArrayResize(pool, n + 1);
      pool[n] = instance;
    }
    
    void trade()
    {
      for(int i = 0; i < ArraySize(pool); i++)
      {
        pool[i].trade();
      }
    }
};


ExpMartinPool *pool;


int OnInit()
{
  pool = NULL;
  if(_WorkSymbols == "")
  {
    Print("Input settings:");

    Settings settings;
    settings._SET(UseTime)._SET(HourStart)._SET(HourEnd)
            ._SET(Lots)._SET(Factor)._SET(Limit)
            ._SET(StopLoss)._SET(TakeProfit)._SET(StartType)
            ._SET(Magic)._SET(SkipBadTime)._SET(Trail).validate();

    settings.print();
    pool = new ExpMartinPool(new ExpMartin(settings));
  }
  else
  {
    Print("Parsed settings:");

    SettingsParser parser(_WorkSymbols);
    const int n = parser.size();
    pool = new ExpMartinPool(n);
    for(int i = 0; i < n; i++)
    {
      pool.push(new ExpMartin(parser[i]._SET(Magic)._SET(SkipBadTime)._SET(Trail)));
    }
  }
  
  return 0;
}

void OnDeinit(const int r)
{
  if(pool != NULL)
  {
    delete pool;
  }
}

void OnTick()
{
  if(pool != NULL)
  {
    pool.trade();
  }
}
