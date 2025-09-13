//+------------------------------------------------------------------+
#property copyright "Copyright © 2013 Matus German, www.MTexperts.net"

#define OP_ALL 10

extern double    DistancePip           = 10;                    
extern double    Lots                  = 0.01;               
extern double    LotsIncrease          = 0.01;
extern int       IncreaseTrade         = 3;
extern int       MaxTrades             = 200; 
extern bool      CloseAllTrades        = false;
extern double    ProfitClose           = 500000;
extern double    MagicNumber           = 8765942;   

extern string    separator9             = "------ Menu settings ------";
extern bool      ShowMenu               = true;
extern int       MenuCorner             = 1;
extern color     FontColor              = White; 
extern int       FontSize               = 10;

double          MaxSlippage            = 3; 

double   minAllowedLot, lotStep, maxAllowedLot,
         pips2dbl, pips2point, pipValue, minGapStop, maxSlippage,
         lots,
         distance, newTakeProfit, newLots;

bool  terminate=false, aos_start=true, stopsChecked, clear=true;

int openTrades=0;

      
//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
int init()
  {
//----   
   Comment("Copyright © 2013 Matus German, www.MTexperts.net");
   
   if (Digits == 5 || Digits == 3)    // Adjust for five (5) digit brokers.
   {            
      pips2dbl = Point*10; pips2point = 10; pipValue = (MarketInfo(Symbol(),MODE_TICKVALUE))*10;
   } 
   else 
   {    
      pips2dbl = Point;   pips2point = 1; pipValue = (MarketInfo(Symbol(),MODE_TICKVALUE))*1;
   }

   if(!GlobalVariableCheck("Collector_"+MagicNumber))
      GlobalVariableSet("Collector_"+MagicNumber, TimeCurrent());

   minGapStop = MarketInfo(Symbol(), MODE_STOPLEVEL)*Point;
   maxSlippage=MaxSlippage*pips2dbl;
   distance=DistancePip*pips2dbl;
   
   lots = Lots;
   minAllowedLot  =  MarketInfo(Symbol(), MODE_MINLOT);    
   lotStep        =  MarketInfo(Symbol(), MODE_LOTSTEP);  
   maxAllowedLot  =  MarketInfo(Symbol(), MODE_MAXLOT );   
 
   if(lots < minAllowedLot)
      lots = minAllowedLot;
   if(lots > maxAllowedLot)
      lots = maxAllowedLot;

   ObjectCreate("buyLevel", OBJ_HLINE, 0, TimeCurrent(), 999999);
   ObjectCreate("sellLevel", OBJ_HLINE, 0, TimeCurrent(), 0);
   ObjectSet("buyLevel", OBJPROP_COLOR, Green); 
   ObjectSet("sellLevel", OBJPROP_COLOR, Red);
   
   if(ShowMenu)
   {
      CreateChart();
      UpdateChart();
   }
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| expert deinitialization function                                 |
//+------------------------------------------------------------------+
int deinit()
  {
//----
   ObjectDelete("buyLevel");
   ObjectDelete("sellLevel");
   ObjectDelete("_trades");
   ObjectDelete("_net_profit");
   ObjectDelete("_lots");
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| expert start function                                            |
//+------------------------------------------------------------------+
int start()
{
//----
   if(ShowMenu)
      UpdateChart();

   if(ProfitCheck()>ProfitClose)
   {
      while(!CloseDeleteAll()) {}
      while(GlobalVariableSet("Collector_"+MagicNumber,TimeCurrent())==0) {}
   }
   
   if(CloseAllTrades)
   {
      while(!CloseDeleteAll()) {}
      while(GlobalVariableSet("Collector_"+MagicNumber,TimeCurrent())==0) {}
      aos_start=true;
      return;   
   }
      
   if(!stopsChecked)
      if(CheckStops())
         stopsChecked = true;
      else return;
     
   if(aos_start)
   {
      RefreshRates();
      MoveLevels(Ask+distance/2, Bid-distance/2);
      aos_start=false;
   } 
   
   if(openTrades!=OpenTradesForMNandPT(MagicNumber, Symbol()))
   {
      while(!CheckHistoryOpen()) 
      {
         return;
      }
         openTrades=OpenTradesForMNandPT(MagicNumber, Symbol());
      
      return;
   }
   
   newTakeProfit=distance;
   if(OpenOrderCheck())
      return;

//----
   return(0);
}

///////////////////////////////////////////////////////////////////////////////////////////////////
void MoveLevels(double buy, double sell)
{
   ObjectSet("buyLevel", OBJPROP_PRICE1, buy);
   ObjectSet("sellLevel", OBJPROP_PRICE1, sell);
}

//////////////////////////////////////////////////////////////////////////////////////////////////  
bool EnterBuyCondition()
{ 
   if(Ask>=ObjectGet("buyLevel", OBJPROP_PRICE1))
   {
      return(true);
   }   
   return (false);   
}

//////////////////////////////////////////////////////////////////////////////////////////////////
bool EnterSellCondition()
{ 
   if(Bid<=ObjectGet("sellLevel", OBJPROP_PRICE1))
      return(true);
   
   return (false);   
}

//////////////////////////////////////////////////////////////////////////////////////////////////
// chceck trades if they do not have set sl and tp than modify trade
bool CheckStops()
{
   double sl=0, tp=0;
   double total=OrdersTotal();
   
   int ticket=-1;
   
   for(int cnt=total-1;cnt>=0;cnt--)
   {
      OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
      if(   OrderType()<=OP_SELL                      
         && OrderSymbol()==Symbol()                  
         && OrderMagicNumber() == MagicNumber)        
      {
         if(OrderType()==OP_BUY)
         {
            if(OrderStopLoss()==0 || OrderTakeProfit()==0)
            { 
               ticket=OrderTicket(); 
               while (!IsTradeAllowed()) Sleep(500); 
               RefreshRates();

               sl = OrderOpenPrice()-distance; 
               tp = OrderOpenPrice()+newTakeProfit;
               
               if(Bid-sl<=minGapStop)
                  sl = Bid-minGapStop*2;
                  
               if(tp-Bid<=minGapStop)
                  tp = Bid+minGapStop*2;
                  
               if(OrderModify(OrderTicket(),OrderOpenPrice(),sl,tp,0,Green)) 
               {
               }
               else
                  return (false);
            }
         }   
         if(OrderType()==OP_SELL)
         {
            if(OrderStopLoss()==0 || OrderTakeProfit()==0)
            {
               ticket=OrderTicket();         
               while (!IsTradeAllowed()) Sleep(500); 
               RefreshRates();  
                           
               sl = OrderOpenPrice()+distance;
               tp = OrderOpenPrice()-newTakeProfit;
         
               if(sl-Ask<=minGapStop)
                  sl = Ask+minGapStop*2;              

               if(Ask-tp<=minGapStop)
                  tp = Ask-minGapStop*2;
                    
               if(OrderModify(OrderTicket(),OrderOpenPrice(),sl,tp,0,Green)) 
               {
               }
               else
                  return (false);
            }
         } 
      }
   }
   return (true);
}

//////////////////////////////////////////////////////////////////////////////////////////////////
bool OpenOrderCheck()
{
   double olots=lots;
   int ticket;
   int orderNumber;
   
   {   
      if(EnterBuyCondition())
      {   
         orderNumber=OrderNumber(MagicNumber, Symbol());
         if(orderNumber==0)
            return(false);
                       
         while (!IsTradeAllowed()) Sleep(500); 
         RefreshRates();
         
         ticket=OrderSend(Symbol(),OP_BUY,olots,Ask,maxSlippage, 0,0,StringConcatenate("-",orderNumber,"-"),MagicNumber,0,Green);
         if(ticket>0)
         {
            if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES)) 
            {
               stopsChecked = false;
               MoveLevels(OrderOpenPrice()+distance, OrderOpenPrice()-distance); 
               Print("BUY order opened : ",OrderOpenPrice());
               return(true);
            }
         }
         else 
         {
            Print("Error opening BUY order : ",GetLastError());   
            return(false);
         }
   
      }
      
      if(EnterSellCondition())   
      {  
         orderNumber=OrderNumber(MagicNumber, Symbol());
         if(orderNumber==0)
            return(false);
            
         while (!IsTradeAllowed()) Sleep(500); 
         RefreshRates();  
         
         ticket=OrderSend(Symbol(),OP_SELL,olots,Bid,maxSlippage, 0,0,StringConcatenate("-",orderNumber,"-"),MagicNumber,0,Red);
         if(ticket>0)
         {               
            if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES))
            { 
               stopsChecked = false;
               MoveLevels(OrderOpenPrice()+distance, OrderOpenPrice()-distance);
               Print("SELL order opened : ",OrderOpenPrice());
               return(true);
            }
         }
         else 
         {
            Print("Error opening SELL order : ",GetLastError());
            return (false); 
         }
      }
   }
   return (false);   
}

//////////////////////////////////////////////////////////////////////////////////////////////////
bool OpenOrder(int orderType, int orderNumber)
{
   int ticket;
               
      if(orderType==OP_BUY) 
      { 
         while (!IsTradeAllowed()) Sleep(500); 
         RefreshRates(); 
         ticket=OrderSend(Symbol(),OP_BUY,newLots,Ask,maxSlippage, 0,0,StringConcatenate("-",orderNumber,"-"),MagicNumber,0,Green);
         if(ticket>0)
         {
            if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES)) 
            {
               stopsChecked = false;
               Print("BUY order opened : ",OrderOpenPrice());
               return(true);
            }
         }
         else 
         {
            Print("Error opening BUY order : ",GetLastError());   
            return(false);
         }
   
      }

      if(orderType==OP_SELL)   
      {  
         while (!IsTradeAllowed()) Sleep(500); 
         RefreshRates();  
         
         ticket=OrderSend(Symbol(),OP_SELL,newLots,Bid,maxSlippage, 0,0,StringConcatenate("-",orderNumber,"-"),MagicNumber,0,Red);
         if(ticket>0)
         {               
            if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES))
            { 
               stopsChecked = false;
               Print("SELL order opened : ",OrderOpenPrice());
               return(true);
            }
         }
         else 
         {
            Print("Error opening SELL order : ",GetLastError());
            return (false); 
         }
      }
   return (true);   
}

////////////////////////////////////////////////////////////////////////////////////////////////////////
bool CloseDeleteAll()
{
    int total  = OrdersTotal();
      for (int cnt = total-1 ; cnt >=0 ; cnt--)
      {
         OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES);
         if (OrderSymbol()==Symbol() && OrderMagicNumber() == MagicNumber)
         {
            while(IsTradeContextBusy()) Sleep(100);
            RefreshRates();
            
            if(OrderType()==OP_BUY)
               if(!OrderClose(OrderTicket(),OrderLots(),Bid,maxSlippage,Violet)) 
               {
                  Print("Error closing " + OrderType() + " order : ",GetLastError());
                  return (false);
               }
            if(OrderType()==OP_SELL)   
               if(!OrderClose(OrderTicket(),OrderLots(),Ask,maxSlippage,Violet)) 
               {
                  Print("Error closing " + OrderType() + " order : ",GetLastError());
                  return (false);
               }
            
            if(OrderType()==OP_BUYSTOP || OrderType()==OP_SELLSTOP || OrderType()==OP_BUYLIMIT || OrderType()==OP_SELLLIMIT)
               if(!OrderDelete(OrderTicket()))
               { 
                  Print("Error deleting " + OrderType() + " order : ",GetLastError());
                  return (false);
               }
         }
      }
      return (true);
}

//////////////////////////////////////////////////////////////////////////////////////////////////
int OpenTradesForMNandPT(int iMN, string sOrderSymbol)
{
   int icnt, itotal, retval;

   retval=0;
   itotal=OrdersTotal();

      for(icnt=itotal-1;icnt>=0;icnt--) 
      {
         OrderSelect(icnt, SELECT_BY_POS, MODE_TRADES);
         if (OrderSymbol()== sOrderSymbol)
         {
            if (OrderMagicNumber()==iMN) 
               retval++;             
         } 
      } 

   return(retval);
}

///////////////////////////////////////////////////////////////////////////////////////////////////////////
int OrderNumber(int iMN, string sOrderSymbol)
{
   int icnt, itotal, orderTicket;
   bool isOpen=false;

   itotal=OrdersTotal();
   for(int j=1;j<=MaxTrades;j++)
   {
      isOpen=false;
      for(icnt=itotal-1;icnt>=0;icnt--) 
      {
         OrderSelect(icnt, SELECT_BY_POS, MODE_TRADES);
         if (OrderSymbol()== sOrderSymbol)
         {
            if (OrderMagicNumber()==iMN && StringFind(OrderComment(),StringConcatenate("-",j,"-"),0)>=0) 
               isOpen=true;             
         } 
      } 
      if(isOpen)
         continue;
         
      orderTicket=LastHistoryOrderTicket(MagicNumber, Symbol(), j);
      OrderSelect(orderTicket,SELECT_BY_TICKET, MODE_HISTORY);
      if(orderTicket==-1 || OrderProfit()>0)
         return(j);
   }
   return(0);
}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////
int LastHistoryOrderTicket(int magicNumber, string symbol, int orderNumber)
{
   datetime orderTime=0;
   int orderTicket=-1;
   int total=OrdersHistoryTotal();
   for(int i=0;i<total;i++)
   {
      OrderSelect(i, SELECT_BY_POS,MODE_HISTORY);
      if(OrderSymbol()==symbol && OrderMagicNumber()==magicNumber && OrderOpenTime()>orderTime && OrderOpenTime()>=GlobalVariableGet("Collector_"+magicNumber) 
         && StringFind(OrderComment(),StringConcatenate("-",orderNumber,"-"),0)>=0)
      {
         orderTicket=OrderTicket();
         orderTime=OrderOpenTime();
      }
   } 
   
   return(orderTicket); 
}

///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
bool CheckHistoryOpen()
{
   int itotal, orderTicket;
   itotal=OrdersTotal();
   
   bool isOpen=false;
   
   for(int j=MaxTrades;j>0;j--)
   {
      isOpen=false;
      for(int icnt=itotal-1;icnt>=0;icnt--)
      {
         OrderSelect(icnt, SELECT_BY_POS, MODE_TRADES);
         if (OrderSymbol()== Symbol())
         {
            if (OrderMagicNumber()==MagicNumber && StringFind(OrderComment(),StringConcatenate("-",j,"-"),0)>=0) 
               isOpen=true;             
         } 
      }
      if(isOpen)
         continue;
         
      orderTicket=LastHistoryOrderTicket(MagicNumber, Symbol(), j);
      OrderSelect(orderTicket,SELECT_BY_TICKET, MODE_HISTORY);
      if(OrderProfit()<0)
      {
         if(OrderType()==OP_BUY)
         {
            newTakeProfit=OrderTakeProfit()-OrderOpenPrice()+distance;
            if(MathMod(j,IncreaseTrade)==0)
               newLots=OrderLots()+LotsIncrease;
            else
               newLots=lots;
            OpenOrder(OP_SELL,j);
               
               return(false);
         }
         if(OrderType()==OP_SELL)
         {
            newTakeProfit=OrderOpenPrice()-OrderTakeProfit()+distance;
            if(MathMod(j,IncreaseTrade)==0)
               newLots=OrderLots()+LotsIncrease;
            else
               newLots=lots;
            OpenOrder(OP_BUY,j);
               
               return(false);
         }
      }
   }
   return(true);
}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
double ProfitCheck()
{
   double profit=0;
   int total  = OrdersTotal();
      for (int cnt = total-1 ; cnt >=0 ; cnt--)
      {
         OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES);
         if (OrderSymbol()==Symbol() && (OrderMagicNumber() == MagicNumber))
            profit+=OrderProfit()+OrderSwap();
      }
      
      total=OrdersHistoryTotal();
      for(cnt = total-1 ; cnt >=0 ; cnt--)
      {
         OrderSelect(cnt,SELECT_BY_POS,MODE_HISTORY);
         if (OrderSymbol()==Symbol() && (OrderMagicNumber() == MagicNumber) && OrderOpenTime()>=GlobalVariableGet("Collector_"+MagicNumber))
            profit+=OrderProfit()+OrderSwap();
      }
      
      Print("***open time> ",OrderOpenTime());
      Print("***global time> ",GlobalVariableGet("Collector_"+MagicNumber));
   return(profit);       
}

//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//
//  Menu view functions
//
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void CreateChart()
{   
   ObjectDelete("_trades");
   ObjectCreate("_trades", OBJ_LABEL, 0, 0, 0);
   ObjectSet("_trades", OBJPROP_CORNER, MenuCorner);
   ObjectSet("_trades", OBJPROP_XDISTANCE, 2*FontSize);
   ObjectSet("_trades", OBJPROP_YDISTANCE, 15+FontSize+4);

   ObjectDelete("_lots");
   ObjectCreate("_lots", OBJ_LABEL, 0, 0, 0);
   ObjectSet("_lots", OBJPROP_CORNER, MenuCorner);
   ObjectSet("_lots", OBJPROP_XDISTANCE, 2*FontSize);
   ObjectSet("_lots", OBJPROP_YDISTANCE, 15+2*(FontSize+4)); 

   ObjectDelete("_net_profit");
   ObjectCreate("_net_profit", OBJ_LABEL, 0, 0, 0);
   ObjectSet("_net_profit", OBJPROP_CORNER, MenuCorner);
   ObjectSet("_net_profit", OBJPROP_XDISTANCE, 2*FontSize);
   ObjectSet("_net_profit", OBJPROP_YDISTANCE, 15+3*(FontSize+4));
}

///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void UpdateChart()
{
   double net_profit, draw_lots=Lots();

   ObjectSetText("_trades", "Trades open: " + StringConcatenate("",Opened(Symbol(), MagicNumber, OP_ALL)), FontSize, "Arial", FontColor);

   net_profit = ProfitCheck();

   if(net_profit<=0) 
      ObjectSetText("_net_profit", "Net profit: " + DoubleToStr(net_profit,1) + AccountCurrency(), FontSize, "Arial", Red);
   else 
      ObjectSetText("_net_profit", "Net profit: " + DoubleToStr(net_profit,1) + AccountCurrency(), FontSize, "Arial", Lime);
 
   ObjectSetText("_lots", StringConcatenate("Lots open: ",draw_lots), FontSize, "Arial", FontColor);   
}

// cmd = OP_ALL // OP_ALL = OP_BUY || OP_SELL
//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
int Opened(string symbol, int magic, int cmd)
{
    int total  = OrdersTotal();
    int cnt, count = 0;

    if(cmd==OP_ALL)
    {
      for (cnt = total-1 ; cnt >=0 ; cnt--)
      {
         OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES);
         if (OrderSymbol()==symbol && OrderMagicNumber() == magic)
             if(OrderType()==OP_BUY || OrderType()==OP_SELL)
               count++;
      }
    }
    if(cmd==OP_BUY)
    {
      for (cnt = total-1 ; cnt >=0 ; cnt--)
      {
         OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES);
         if (OrderSymbol()==symbol && OrderMagicNumber() == magic)
             if(OrderType()==OP_BUY)
               count++;
      }
    }    
    if(cmd==OP_SELL)
    {
      for (cnt = total-1 ; cnt >=0 ; cnt--)
      {
         OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES);
         if (OrderSymbol()==symbol && OrderMagicNumber() == magic)
             if(OrderType()==OP_SELL)
               count++;
      }
    }
        
    return (count);
}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////
double Lots()
{
    int total  = OrdersTotal();
    double lots = 0;
      for (int cnt = total-1 ; cnt >=0 ; cnt--)
      {
         OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES);
         if (OrderSymbol()==Symbol() && (OrderMagicNumber() == MagicNumber))
             if(OrderType()==OP_BUY || OrderType()==OP_SELL)
               lots+=OrderLots();
      }
    return (lots);
}