//+------------------------------------------------------------------+
//|                                                 SAW_system_1.mq4 |
//|                                              Copyright 2014, SAW |
//|                                http://saw-trade.livejournal.com/ |
//+------------------------------------------------------------------+
#property copyright "Copyright 2014, SAW"
#property link      "http://saw-trade.livejournal.com/"
#property version   "1.00"
#property description "�������� SAW_system_1 ���������� ���������� ������ �� ������� �������������"
#property description "�� N ����. StopLoss ������ ��������������� �� ������� ���������������� ������,"
#property description "��������������, � ���������� ����������� ���������� �� �����, ��� �� ����� ������"
#property description "� ��������������� �����!"
#property description "����-����� �� ����� ��������."
#property strict

//--- input parameters
input double   lot         = 0.01;  // Lot
input int      d           = 5;     // Amount of days (for calculating volatility)
input int      open_hour   = 7;     // Hour installation orders (terminal time)
input int      close_hour  = 10;    // Hour removal orders (terminal time)
input int      sl_rate     = 15;    // Stop-Loss (percentage of the average volatility)
input int      tp_rate     = 30;    // Take-Profit (percentage of the average volatility)
input bool     rev         = false; // Reverse positions
input bool     martin      = false; // Martingale
input double   martin_koef = 2.0;   // Multiplier

//--- global parameters
//int      dig         = 0;
bool     new_day= true;  // new trading day
int      day_week= -1;   // the current day of the week
int      d_average   = 0;     // the value of the average volatility for the selected period
int      sl          = 0;     // Stop Loss
int      tp          = 0;     // Take Profit
int      or          = 0;     // the distance from the stage to order
int      err         = 0;     // variable for counting the number of consecutive errors
bool     mod_order   = false; // orders modification flag

//--- custom functions
void v_calc();
void sl_tp_or_calc();
int send_orders();
int error();
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
   day_week=DayOfWeek();   // ������������� ������� ���� ������ (0 - �����������)

   if(Hour()>=open_hour) // ���� ��� ������, �� � ���� ���� ������ �� �������������
      new_day= false;
   
   v_calc();         // ������ �������������
   sl_tp_or_calc();  // ������������ ����-���� � ����-������

// ������ ���������� dig   
//   if (Digits() == 5)
//      dig = 100000;
//   if (Digits() == 4)
//      dig = 10000;  
//   
//   if (Digits() == 3)
//      dig = 1000;
//   if (Digits() == 2)
//      dig = 100;

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
//--- ��������� ����� ����
   if(new_day==false && day_week!=DayOfWeek())
     {
      v_calc();         // ������������ �������������

      sl_tp_or_calc();  // ������������ ����-���� � ����-������

      //--- ������������� ����������
      day_week= DayOfWeek();
      new_day = true;
      mod_order=false;
     }
//-----------------------------------------------------------------------
//--- ��� ����������� ������ ��� � ��������� ����, ���������� ���� ������
   if(day_week==DayOfWeek() && new_day==true && Hour()==open_hour)
     {
      while(send_orders()==666) // ���� ����� �� �����������, ������������� 5 ���
        {
         switch(error())
           {
            case 666: new_day=false; mod_order=true; Print("!!!!!!��������� ������ �  ",GetLastError()); return;   // if the restart does not help, we derive a mistake and do not sell
            case   1: RefreshRates();
           }
        }
     }
//------------------------------------------------------------------------   
// ���� ���� �� ����� � �. �.
   if(new_day==false && day_week==DayOfWeek() && mod_order==false)
     {
      int tip1      = -1;   // ��� ������� ������
      int tip2      = -1;   // ��� ������� ������
      int tick1     = -1;   // ����� ������� ������
      int tick2     = -1;   // ����� ������� ������
      double price2 = -1.0; // ���� �������� ������� ������
      double sl2    = -1.0; // ����-���� ������� ������
      double tp2    = -1.0; // ����-������ ������� ������

      RefreshRates();

      // ��������� ���������� �������
      if(OrdersTotal()>2)
        {
         Print("������ 2-� ������� �� ����� �����������. ��������� ���!!!");
         close_all_orders();
         return;
        }

      // �������� ���������� �� �������
      for(int i=0; i+1<=OrdersTotal(); i++)
        {

         if(OrderSelect(i,SELECT_BY_POS)==true)
           {
            switch(i)
              {
               case  0: tip1 = OrderType(); break;
               case  1: tip2 = OrderType(); tick2 = OrderTicket();
               price2=OrderOpenPrice(); sl2=OrderStopLoss(); tp2=OrderTakeProfit(); break;
               default: Print("�������� ���������� �������, ���� � ������ �����!!!");
              }
           }
        }

      // ��������� ����� ����� ���������� �������
      if(tip1>1 && tip2>1 && close_hour<=Hour())
        {
         close_all_orders();
         mod_order=true;
         tip1=tip2=-1;
        }

      // ������ �������� ������� �������, ���� �����
      if((tip2==0 || tip2==1) && tip1>1)
        {
         int t;

         if(OrderSelect(0,SELECT_BY_POS)==true)
           {
            tick2=OrderTicket();
            price2=OrderOpenPrice();
            sl2 = OrderStopLoss();
            tp2 = OrderTakeProfit();

            t=tip1;
            tip1 = tip2;
            tip2 = t;
           }

        }

      // ������������ ��� ������� ������, ���� �����
      if((tip1==0 || tip1==1) && tip2>1)
        {
         if(rev==false)
           {
            if(OrderDelete(tick2)==true)
               mod_order=true;
           }

         if(rev==true && martin==true)
           {
            if(OrderDelete(tick2)==true)
               if(OrderSend(Symbol(),tip2,lot*martin_koef,price2,5,sl2,tp2,NULL,0,0,clrNONE)==-1)
                  Print("������ ������������� ������� ������, ��� ���������� ���� �",GetLastError());

            mod_order=true;
           }

         if(rev==true && martin==false)
           {
            mod_order=true;
           }
        }
     }

// �������� �� ������ ������������ ����� ������ �� �������
   if(mod_order==true && OrdersTotal()==1 && Hour()>=close_hour)
      if(OrderSelect(0,SELECT_BY_POS)==true)
         if(OrderType()>1)
            if(!OrderDelete(OrderTicket()))
               Print("������ �������� ������! GetLastError = " + (string)GetLastError());

// �������������� �������� �� ������ ���������� ���������
   if(new_day==true && day_week!=DayOfWeek())
     {
      day_week=DayOfWeek();
      mod_order=false;

      if(Hour() >= open_hour)
         new_day = false;
     }

  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void v_calc() // ������ �������������
  {
   double h;   // ������������ ���� ����
   double l;   // ����������� ���� ����
   double av=0;  // ����� ���� ������������� �� ������

                 // ������ ������� ������������� �� ������
   for(int i=1; i<=d; i++)
     {
      h = NormalizeDouble (iHigh(NULL, PERIOD_D1, i), Digits);
      l = NormalizeDouble (iLow(NULL, PERIOD_D1, i), Digits);
      av= av+((h-l)/Point());
     }

   d_average=(int)(av/d);

   Print("d_average = ",d_average);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void sl_tp_or_calc() // ������ ����� � �������
  {
   sl = d_average * sl_rate / 100;
   tp = d_average * tp_rate / 100;

   or=sl/2;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int send_orders() // ��������� �������
  {
   double price_up = -1.0; // ������� ���� ����
   double price_dn = -1.0; // ������� ���� ����
   double tp_up = -1.0;    // ���� �������� ������
   double tp_dn = -1.0;    // ���� ������� ������

   RefreshRates();

// ������ �������
   price_up = Bid + or * Point();
   price_dn = Bid - or * Point();
   tp_up    = price_up + tp * Point();
   tp_dn    = price_dn - tp * Point();

   Print ("price_up = ", price_up);
   Print ("price_dn = ", price_dn);
   Print ("tp_up = ", tp_up);
   Print ("tp_dn = ", tp_dn);

// ��������� ������ BUYSTOP
   if(OrderSend(Symbol(),OP_BUYSTOP,lot,price_up,5,price_dn,tp_up,NULL,0,0,clrNavy)==-1)
     {
      return(666);
     }

// ��������� ������ SELLSTOP
   if(OrderSend(Symbol(),OP_SELLSTOP,lot,price_dn,5,price_up,tp_dn,NULL,0,0,clrRed)==-1)
     {
      return(666);
     }

   new_day=false;
   err=0;

   return(0);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int error() // ������� ��������� ������
  {
   if(err>=5)
     {
      close_all_orders();
      err=0;
      return(666);
     }
   else
     {
      close_all_orders();
      err++;
      return(1);
     }

  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void close_all_orders() // ������� � ������� ��� ������
  {
   int   tip=-1;        // ���� ���������� ������
   bool  cl=true;  // ��������� �������� ���� �������
   int   i= 5;          // ���������� ������� �������� �������
   int   tick = -1;     // ����� ������

   while(OrdersTotal()>0 && i>0)
     {
      RefreshRates();

      if(OrderSelect(0,SELECT_BY_POS)==true)
        {
         tip=OrderType();
         tick=OrderTicket();

         switch(tip)
           {
            case 0: cl = OrderClose(tick, OrderLots(), Bid, 5); break;
            case 1: cl = OrderClose(tick, OrderLots(), Ask, 5); break;
            case 2: cl = OrderDelete(tick); break;
            case 3: cl = OrderDelete(tick); break;
            case 4: cl = OrderDelete(tick); break;
            case 5: cl = OrderDelete(tick); break;
           }
        }
      else
        {
         Print("������ ������ ������");
         cl=false;
        }

      if(cl==false)
         i--;

     }

   if(cl==false)
     {
      Print("������ �������� ���� �������");
      return;
     }
  }
//+------------------------------------------------------------------+
