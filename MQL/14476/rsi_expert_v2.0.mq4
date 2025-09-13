//+------------------------------------------------------------------+
//|                                              RSI_Expert_v2.0.mq4 |
//|                                                             Joni |
//|                                                  JoniH88@mail.ru |
//+------------------------------------------------------------------+
#property copyright     "Joni"
#property link          "JoniH88@mail.ru"
#property version       "2.00"
#property description   "JoniH88@mail.ru"
#property strict
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
enum enum_ma_trade 
  {
   OFF,     //���������
   FORWARD, //������
   REVERS   //��������
  };
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
enum enum_lot 
  {
   FIX,//�������������
   FLOAT,//������� �� �������
   MARTIN
  };
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
enum enum_trade 
  {
   UP,
   DOWN,
   ALWAYS,
   FOUL
  };
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
enum enum_order 
  {
   BUY_MINUS,
   PLUS,
   SELL_MINUS,
  };

input string               RSI            = "--=========RSI==========--";
input int                  periodRSI      = 21;          //������:
input double               levelUpRSI     = 70.0;        //������� �������:
input double               levelDownRSI   = 30.0;        //������ �������:
input ENUM_APPLIED_PRICE   priceRSI       = PRICE_CLOSE; //��������� �:
input string               FastMovingAverage="--====Fast Moving Average=====--";
input int                  periodFastMA   = 50;          //������:
input ENUM_MA_METHOD       modeFastMA     = MODE_SMA;    //����� ��:
input ENUM_APPLIED_PRICE   priceFastMA    = PRICE_CLOSE; //��������� �:
input string               SlowMovingAverage="--====Slow Moving Average=====--";
input int                  periodSlowMA   = 200;         //������:
input ENUM_MA_METHOD       modeSlowMA     = MODE_SMA;    //����� ��:
input ENUM_APPLIED_PRICE   priceSlowMA    = PRICE_CLOSE; //��������� �:
input string               Expert         = "--======Expert Settings======--";
input enum_ma_trade        typeTradeMA    = FORWARD;     //�������� �� MA
input enum_lot             typeLot        = FIX;         //��� ����
input double               lotPoz         = 10;          //������ ���� � ��������� �� �������
input double               lot            = 0.01;        //���
input int                  SL             = 0;           //Stop Loss (� �������)
input int                  TP             = 0;           //Take Profit (� �������)
input int                  stepTrall      = 0;           //������� ���� (� �������)
input int                  Slippage       = 3;           //��������������� (� �������)
input int                  magic          = 25;

datetime gBar;
int      gSlippage;
double   gPoint;
bool     gTradeEnable;
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int OnInit() 
  {
   Comment("");
   gTradeEnable=true;
   checkingSet();
   if(Digits==5) gPoint=0.0001;
   else 
     {
      if(Digits==3) gPoint=0.01;
      else gPoint=Point;
     }
   gSlippage=(int)NormalizeDouble(Slippage*gPoint/Point,0);
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void OnTick() 
  {
   if(gTradeEnable) 
     {
      if(gBar!=Time[0])
        {
         gBar=Time[0];
         trade(1);
         trall(stepTrall);
        }
        } else {
      Comment("�������� �� �������");
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void trade(int _numBar)
  {
   int _typeTrade=typeTrade();
   double _lot=calcLot(_typeTrade);
   if(_typeTrade==1) 
     {
      if(_lot>0) openOrder(OP_BUY,_lot,SL,TP);
      closeAllSell();
     }
   if(_typeTrade==-1) 
     {
      if(_lot>0) openOrder(OP_SELL,_lot,SL,TP);
      closeAllBuy();
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int typeTrade()
  {
   enum_trade tradeRsi= tradeRsi();
   enum_trade tradeMa = tradeMa();
   if(tradeRsi == UP && (tradeMa == UP || tradeMa == ALWAYS)) return 1;
   if(tradeRsi == DOWN && (tradeMa == DOWN || tradeMa == ALWAYS)) return -1;
   return 0;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
enum_trade tradeRsi() 
  {
   double rsi_1 = iRSI(Symbol(), PERIOD_CURRENT, periodRSI, PRICE_CLOSE, 1);
   double rsi_2 = iRSI(Symbol(), PERIOD_CURRENT, periodRSI, PRICE_CLOSE, 2);
   if(rsi_1 > levelDownRSI && rsi_2 < levelDownRSI)   return UP;
   if(rsi_1 < levelUpRSI && rsi_2 > levelUpRSI)       return DOWN;
   return FOUL;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
enum_trade tradeMa() 
  {
   double maFast = iMA(Symbol(), PERIOD_CURRENT, periodFastMA, 0, modeFastMA, priceFastMA, 1);
   double maSlow = iMA(Symbol(), PERIOD_CURRENT, periodSlowMA, 0, modeSlowMA, priceSlowMA, 1);
   switch(typeTradeMA) 
     {
      case OFF:
         return ALWAYS;
      case FORWARD:
         if(maFast > maSlow) return UP;
         if(maFast < maSlow) return DOWN;
         break;
      case REVERS:
         if(maFast < maSlow) return UP;
         if(maFast > maSlow) return DOWN;
         break;
     }
   return FOUL;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double calcLot(int _typeTrade) 
  {
   double _lot=0.01;
   double minLot = MarketInfo(Symbol(), MODE_MINLOT);
   double maxLot = MarketInfo(Symbol(), MODE_MAXLOT);
   double needForOneLot=MarketInfo(Symbol(),MODE_MARGINREQUIRED); //������ ��������� �������, ����������� ��� �������� 1 ���� �� �������         
   if(typeLot==FLOAT) 
     {
      _lot=AccountFreeMargin()*lotPoz/100.0/needForOneLot;
      if(_lot<minLot) 
        {
         _lot=0;
        }
      if(_lot>maxLot) 
        {
         _lot=maxLot;
        }
        } else if(typeLot==MARTIN) {
      if((typeLastOrder()==BUY_MINUS && _typeTrade>0) || (typeLastOrder()==SELL_MINUS && _typeTrade<0)) 
        {
         _lot=sizeLotLastOrder()*2;
        }
     }
   return _lot;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void trall(int step)
  {
   if(MarketInfo(Symbol(), MODE_STOPLEVEL) > step * gPoint / Point) return;
   int ordersTotal=OrdersTotal();
   for(int i=ordersTotal-1; i>=0; i --) 
     {
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES)) 
        {
         if(OrderSymbol()==Symbol() && OrderMagicNumber()==magic)
           {
            setSl(step);
           }
        }
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void setSl(int sl)
  {
   double _sl=0.0;
   if(OrderType()==OP_BUY)
     {
      _sl=ND(Bid -(double)sl*gPoint);
      if(_sl <= OrderStopLoss()) return;
     }
   if(OrderType()==OP_SELL)
     {
      _sl=ND(Ask+(double)sl*gPoint);
      //Print("sl = ", sl, "  ordersl = ", OrderStopLoss());
      if(OrderStopLoss() != 0.0 && _sl >= OrderStopLoss()) return;
     }
   bool f=OrderModify(OrderTicket(),OrderOpenPrice(),_sl,OrderTakeProfit(),0);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
enum_order typeLastOrder() 
  {
   if(OrderSelect(OrdersTotal()-1,SELECT_BY_POS,MODE_TRADES))
     {
      if(OrderSymbol()==Symbol() && OrderMagicNumber()==magic) 
        {
         if(OrderType()==OP_BUY) 
           {
            if(OrderProfit() >= 0) return PLUS;
            else return BUY_MINUS;
              } else if(OrderType()==OP_SELL) {
            if(OrderProfit() >= 0) return PLUS;
            else return SELL_MINUS;
           }
        }
     }
   return PLUS;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double sizeLotLastOrder() 
  {
   if(OrderSelect(OrdersTotal()-1,SELECT_BY_POS,MODE_TRADES))
     {
      if(OrderSymbol()==Symbol() && OrderMagicNumber()==magic)
         return OrderLots();
     }
   return 0;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void closeAllBuy()
  {
   int i;
   int ordersTotal=OrdersTotal();
   for(i=ordersTotal-1; i>=0; i --) 
     {
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES))
        {
         if(OrderSymbol()==Symbol() && OrderMagicNumber()==magic && OrderType()==OP_BUY) 
           {
            if(OrderStopLoss()==0 || OrderStopLoss()-OrderOpenPrice()<0) closeOpder(OP_BUY);
           }
        }
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void closeAllSell()
  {
   int i;
   int ordersTotal=OrdersTotal();
   for(i=ordersTotal-1; i>=0; i --)
     {
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES))
        {
         if(OrderSymbol()==Symbol() && OrderMagicNumber()==magic && OrderType()==OP_SELL)
           {
            if(OrderStopLoss()==0 || OrderOpenPrice()-OrderStopLoss()<0) closeOpder(OP_SELL);
           }
        }
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int openOrder(int cmd,double lots,int _stop_loss,int _take_profit) 
  {
   double sl = 0;
   double tp = 0;
   int ticket= 0;
   int error = 0;
   RefreshRates();
   ResetLastError();
   while(true) 
     {
      if(cmd==OP_SELL) 
        {
         if(_stop_loss==0) sl=0;
         else sl=NormalizeDouble(Ask+_stop_loss*gPoint,Digits);
         if(_take_profit==0) tp=0;
         else tp= NormalizeDouble(Ask-_take_profit * gPoint,Digits);
         ticket = OrderSend(Symbol(),OP_SELL,lots,NormalizeDouble(Bid,Digits),gSlippage,sl,tp,NULL,magic,0,Red);
        }
      if(cmd==OP_BUY) 
        {
         if(_stop_loss==0) sl=0;
         else sl=NormalizeDouble(Bid-_stop_loss*gPoint,Digits);
         if(_take_profit==0) tp=0;
         else tp= NormalizeDouble(Bid+_take_profit * gPoint,Digits);
         ticket = OrderSend(Symbol(),OP_BUY,lots,NormalizeDouble(Ask,Digits),gSlippage,sl,tp,NULL,magic,0,Blue);
        }
      if(ticket>0) break;
      error=GetLastError();
      switch(error) 
        {
         case 135: Print("���� ����������. ������ ��� ...");
         RefreshRates();
         continue;
         case 136: Print("��� ���. ��� ����� ��� ...");
         while(RefreshRates()==false)
            Sleep(1);
         continue;
         case 146: Print("���������� �������� ������. ������ ��� ...");
         Sleep(500);
         RefreshRates();
         continue;
         case 138: Print("���� ��������. ������ ��� ...");
         Sleep(500);
         RefreshRates();
         continue;
         case 129: Print("������������ ���� ��� ������� ������� �����. ������ ��� ...");
         Sleep(5000);
         RefreshRates();
         continue;
        }
      switch(error) // ����������� ������
        {
         case 2 : Print("����� ������.");
         break;
         case 5 : Print("������ ������ ����������� ���������.");
         break;
         case 64: Print("���� ������������.");
         break;
         case 133:Print("�������� ���������");
         break;
         case 130:Print("������� ��������� �� ��� ��");
         break;
         case 134:Print("�� ������� �������");
         break;
         default: Print("�������� ������: ",error);// ������ ��������   
        }
      break;                                    // ����� �� �����
     }
   return ticket;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void closeAll() 
  {
   int i;
   int ordersTotal=OrdersTotal();
   if(ordersTotal>0) 
     {
      for(i=ordersTotal-1; i>=0; i --) 
        {
         if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES)) 
           {
            if(OrderSymbol()==Symbol() && OrderMagicNumber()==magic && OrderType()==OP_SELL) 
              {
               if(OrderStopLoss()==0 || OrderOpenPrice()-OrderStopLoss()<0) closeOpder(OP_SELL);
              }
            if(OrderSymbol()==Symbol() && OrderMagicNumber()==magic && OrderType()==OP_BUY) 
              {
               if(OrderStopLoss()==0 || OrderStopLoss()-OrderOpenPrice()<0) closeOpder(OP_BUY);
              }
           }
        }
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void closeOpder(int cmd) 
  {
   bool er=false;
   int error=0;
   RefreshRates();
   ResetLastError();
   while(true) 
     {
      if(cmd==OP_BUY) 
        {
         er = OrderClose(OrderTicket(),OrderLots(),Bid,gSlippage,Blue);
        }
      if(cmd==OP_SELL) 
        {
         er = OrderClose(OrderTicket(),OrderLots(),Ask,gSlippage,Red);
        }
      if(er == true) break;
      error = GetLastError();
      switch(error) // ����������� ������
        {
         case 135: Print("���� ����������. ������ ��� ...");
         RefreshRates();
         continue;
         case 136: Print("��� ���. ��� ����� ��� ...");
         while(RefreshRates()==false)
            Sleep(1);
         continue;
         case 146: Print("���������� �������� ������. ������ ��� ...");
         Sleep(500);
         RefreshRates();
         continue;
         case 129: Print("������������ ���� ��� ������� ������� �����. ������ ���...");
         Sleep(5000);
         RefreshRates();
         continue;
        }
      switch(error) 
        {
         case 2 : Print("����� ������.");
         break;
         case 5 : Print("������ ������ ����������� ���������.");
         break;
         case 64: Print("���� ������������.");
         break;
         case 133: Print("�������� ���������");
         break;
         default: Print("�������� ������: ",error);
        }
      break;
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double ND(double value)
  {
   return NormalizeDouble(value, Digits);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
enum enum_error 
  {
   ERROR_RSI,
   ERROR_MA
  };
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void checkingSet() 
  {
   if(levelUpRSI<=levelDownRSI) 
     {
      gTradeEnable=false;
      showError(ERROR_RSI);
     }
   if(periodSlowMA<=periodFastMA) 
     {
      gTradeEnable = false;
      showError(ERROR_MA);
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void showError(enum_error error) 
  {
   switch(error) 
     {
      case ERROR_RSI:
         MessageBox("������� ���������� ������ RSI!\n����������, ���������� ������� ������� ���� �������");
         break;
      case ERROR_MA:
         MessageBox("������� ���������� ������� ��!\n����������, �������� ������ ��������� ��\n��� ��������� ������ ������� ��");
         break;
     }
  }
//+------------------------------------------------------------------+
