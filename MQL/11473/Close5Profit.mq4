//+------------------------------------------------------------------+
//|                                                     Close@Profit |
//|                                        Copyright 2012, ForexBold |
//|                                         http://www.forexbold.com |
//+------------------------------------------------------------------+
#property copyright "Copyright 2014, ForexBold Software Corp."
#property link      "http://www.forexbold.com"
#property version   "1.00"
#property strict

extern bool      UseProfitToClose       = true;
extern double    ProfitToClose          = 100;

extern bool      UseLossToClose         = false;
extern double    LossToClose            = 100;

extern bool      AllSymbols             = true;
extern bool      PendingOrders          = true;

extern double    MaxSlippage=3;

extern color     LegendColor            = White;
extern color     VariableColor          = Gold;

extern int       FontSize=8;

int RowCount=1;
int Tick=0;
double Profit=0;
double ProfitSymbol=0;
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
   ObjectsDeleteAll();
   EventSetTimer(1);
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
   ObjectsDeleteAll();
   EventKillTimer();
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void OnTick()
  {

   FindMyOrders();

   if(Profit>=ProfitToClose && AllSymbols==true)
     {
      CloseOrders();
      Print("Closing All Symbols @ Profit ",Profit);
     }

   if(ProfitSymbol>=ProfitToClose && AllSymbols==false)
     {
      CloseOrders();
      Print("Closing "+Symbol()+" @ Profit ",ProfitSymbol);
     }

   if(Profit<=LossToClose*-1 && AllSymbols==true && UseLossToClose)
     {
      CloseOrders();
      Print("Closing All Symbols @ Loss ",Profit);
     }

   if(ProfitSymbol<=LossToClose*-1 && AllSymbols==false && UseLossToClose)
     {
      CloseOrders();
      Print("Closing "+Symbol()+" @ Loss ",ProfitSymbol);
     }

  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void OnTimer()
  {

   FindMyOrders();
   DrawMenu();

  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void DrawMenu()
  {

   color foo1 = VariableColor;
   color foo2 = VariableColor;

   if(!AllSymbols)
     {
      foo1=Gray;
        } else {
      foo2=Gray;
     }

   DrawItem("totalProfit","AllSymbols Profit",DoubleToStr(Profit,2),foo1);
   DrawItem("symbolProfit",Symbol()+" Profit",DoubleToStr(ProfitSymbol,2),foo2);
   DrawItem("AllSymbols","AllSymbols",BoolToStr(AllSymbols),BoolToColor(AllSymbols));

   DrawItem("sep1","","",0);

   DrawItem("UseProfitToClose","UseProfitToClose",BoolToStr(UseProfitToClose),BoolToColor(UseProfitToClose));
   DrawItem("ProfitToClose","ProfitToClose",DoubleToStr(ProfitToClose,2),BoolToColor(UseProfitToClose));

   DrawItem("sep2","","",0);

   color lossColor=VariableColor;
   if(!UseLossToClose)
     {
      lossColor=Gray;
     }

   DrawItem("UseLossToClose","UseLossToClose",BoolToStr(UseLossToClose),BoolToColor(UseLossToClose));
   DrawItem("LossToClose","LossToClose",DoubleToStr(LossToClose*-1,2),lossColor);

   DrawItem("sep3","","",0);

   DrawItem("PendingOrders","PendingOrders",BoolToStr(PendingOrders),BoolToColor(PendingOrders));
   DrawItem("OrdersTotal","OrdersTotal",IntegerToString(OrdersTotal()),VariableColor);
   DrawItem("Lots","Lots",DoubleToStr(LotsSize(),2),VariableColor);
   RowCount=1;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void FindMyOrders()
  {

   Profit=0;
   ProfitSymbol=0;

   for(int l_pos_0=OrdersTotal()-1; l_pos_0>=0; l_pos_0--)
     {

      bool order=OrderSelect(l_pos_0,SELECT_BY_POS,MODE_TRADES);

      if(!order)
        {
         continue;
        }

      if(OrderType()==OP_BUY || OrderType()==OP_SELL)
        {

         double order_profit=OrderProfit()+OrderSwap()+OrderCommission();
         Profit+=order_profit;

         if(OrderSymbol()==Symbol())
           {
            ProfitSymbol+=order_profit;
           }

        }
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
string BoolToStr(bool val)
  {
   if(val) { return "True"; }
   return "False";
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
color BoolToColor(bool val)
  {
   if(val) { return VariableColor; }
   return Gray;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void DrawItem(string name,string label,string value,color valueColor)
  {

   if(UseLossToClose==false && UseProfitToClose==false)
     {
      valueColor=Gray;
     }

   RowCount++;
   string col1 = name+"Legend";
   string col2 = name+"Value";

   ObjectDelete(col1);
   ObjectDelete(col2);

   ObjectCreate(col1,OBJ_LABEL,0,0,0,0,0);
   ObjectCreate(col2,OBJ_LABEL,0,0,0,0,0);

   ObjectSetText(col1,label,FontSize,"Arial",LegendColor);
   ObjectSet(col1,OBJPROP_XDISTANCE,10*FontSize);
   ObjectSet(col1,OBJPROP_YDISTANCE,FontSize*RowCount*1.5);
   ObjectSet(col1,OBJPROP_CORNER,1);

   ObjectSetText(col2,value,FontSize,"Arial",valueColor);
   ObjectSet(col2,OBJPROP_XDISTANCE,FontSize*3);
   ObjectSet(col2,OBJPROP_YDISTANCE,FontSize*RowCount*1.5);
   ObjectSet(col2,OBJPROP_CORNER,1);

  }
//+------------------------------------------------------------------+

double LotsSize()
  {
   int total=OrdersTotal();
   double lots=0;
   for(int cnt=total-1; cnt>=0; cnt--)
     {
      bool order=OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES);
      if(!order)
        {
         continue;
        }
      if(AllSymbols)
        {
         if(PendingOrders)
            lots+=OrderLots();
         if(!PendingOrders)
            if(OrderType()==OP_BUY || OrderType()==OP_SELL)
               lots+=OrderLots();
        }
      if(!AllSymbols)
        {
         if(OrderSymbol()==Symbol())
           {
            if(PendingOrders)
               lots+=OrderLots();
            if(!PendingOrders)
               if(OrderType()==OP_BUY || OrderType()==OP_SELL)
                  lots+=OrderLots();
           }
        }
     }
   return (lots);
  }
//+------------------------------------------------------------------+

void CloseOrders()
  {

   int cnt;
   RefreshRates();

   for(cnt=OrdersTotal()-1; cnt>=0; cnt--)
     {

      OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES);

      if(AllSymbols==false && OrderSymbol()!=Symbol()) 
        {
         continue;
        }

      if(OrderType()==OP_BUY)
        {
         OrderClose(OrderTicket(),OrderLots(),Bid,MaxSlippage,Violet);
        }
      if(OrderType()==OP_SELL)
        {
         OrderClose(OrderTicket(),OrderLots(),Ask,MaxSlippage,Violet);
        }

      if(PendingOrders)
        {
         if(OrderType()==OP_BUYLIMIT)
           {
            OrderDelete(OrderTicket());
           }
         if(OrderType()==OP_SELLLIMIT)
           {
            OrderDelete(OrderTicket());
           }
         if(OrderType()==OP_BUYSTOP)
           {
            OrderDelete(OrderTicket());
           }
         if(OrderType()==OP_SELLSTOP)
           {
            OrderDelete(OrderTicket());
           }
        }

     } // for

  }
//+------------------------------------------------------------------+
