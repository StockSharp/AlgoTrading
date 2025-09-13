//+------------------------------------------------------------------+
#property copyright "Copyright © 2013 Matus German www.MTexperts.net"

#define OP_ALL 10
 
extern string    Info               = "Create trendline, name it buy or sell, after crossing will be opened trade";
extern double    MagicNumber        = 0; 
extern double    Lot                = 0.1;
extern double    TakeProfitPip      = 100;
extern double    StopLossPip        = 30;
extern double    TrailingStop       = 20;
extern color     BuyTrendLine       = Lime;
extern color     SellTrendLine      = DeepPink;

 double    MaxSlippage           =3;   
 

double   stopLoss, takeProfit, trailingStop,
         minAllowedLot, lotStep, maxAllowedLot,
         pips2dbl, pips2point, pipValue, minGapStop, maxSlippage,
         lots;         

int      ticket;

bool     startBuy=true, startSell=true,
         openedBuy=false, openedSell=false, 
         buyLineUp, sellLineUp;

string   comm;
      
//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
int init()
  {
//---   
   if (Digits == 5 || Digits == 3)    // Adjust for five (5) digit brokers.
   {            
      pips2dbl = Point*10; pips2point = 10; pipValue = (MarketInfo(Symbol(),MODE_TICKVALUE))*10;
   } 
   else 
   {    
      pips2dbl = Point;   pips2point = 1; pipValue = (MarketInfo(Symbol(),MODE_TICKVALUE))*1;
   }
   
   takeProfit=TakeProfitPip*pips2dbl;
   stopLoss=StopLossPip*pips2dbl;
   trailingStop = TrailingStop*pips2dbl;
   
   maxSlippage = MaxSlippage*pips2dbl;
   minGapStop = MarketInfo(Symbol(), MODE_STOPLEVEL)*Point;
   
   lots = Lot;
   minAllowedLot  =  MarketInfo(Symbol(), MODE_MINLOT);    //IBFX= 0.10
   lotStep        =  MarketInfo(Symbol(), MODE_LOTSTEP);   //IBFX= 0.01
   maxAllowedLot  =  MarketInfo(Symbol(), MODE_MAXLOT );   //IBFX=50.00
     
   if(lots < minAllowedLot)
      return(-1);
   if(lots > maxAllowedLot)
      return(-1);
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| expert deinitialization function                                 |
//+------------------------------------------------------------------+
int deinit()
  {
//----
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| expert start function                                            |
//+------------------------------------------------------------------+
int start()
{
//---- 

   comm="Copyright © 2013 Matus German www.MTexperts.net";    
   if(ObjectFind("buy")!=-1) 
   {
      if(openedBuy==false)
         comm=comm+"\n buy trendline";  
      else
         comm=comm+"\n buy trendline crossed";   
   }
   else
      comm=comm+"\n NO buy trendline";
      
   if(ObjectFind("buySL")!=-1) 
      comm=comm+"\n buySL trendline";  
   if(ObjectFind("buyTP")!=-1) 
      comm=comm+"\n buyTP trendline"; 
     
   if(ObjectFind("sell")!=-1) 
   {
      if(openedSell==false)
         comm=comm+"\n sell trendline";  
      else
         comm=comm+"\n sell trendline crossed";   
   }
   else
      comm=comm+"\n NO sell trendline";
      
   if(ObjectFind("sellSL")!=-1) 
      comm=comm+"\n sellSL trendline";  
   if(ObjectFind("sellTP")!=-1) 
      comm=comm+"\n sellTP trendline";

   Comment(comm);

   if(ObjectFind("buy")==-1)
   {
      startBuy=true;
      openedBuy=false;
   }
   
   if(ObjectFind("sell")==-1)
   {
      startSell=true;
      openedSell=false;
   }
   
   if(startBuy && ObjectFind("buy")!=-1)
   {
      if(ObjectGetValueByShift("buy",0)>Bid)
         buyLineUp=true;
      else
         buyLineUp=false;
      
      ObjectSet("buy",OBJPROP_COLOR,BuyTrendLine);   
      startBuy=false;
   }
   
   if(startSell && ObjectFind("sell")!=-1)
   {
      if(ObjectGetValueByShift("sell",0)>Bid)
         sellLineUp=true;
      else
         sellLineUp=false;
      
      ObjectSet("sell",OBJPROP_COLOR,SellTrendLine);   
      startSell=false;
   }
      
   if(ObjectFind("buy")!=-1 && openedBuy==false
      && ((!buyLineUp && MarketInfo(Symbol(), MODE_BID)<ObjectGetValueByShift("buy",0)) || (buyLineUp && MarketInfo(Symbol(), MODE_BID)>ObjectGetValueByShift("buy",0))))
   {  
      while(!OpenOrder(Symbol(), OP_BUY)) {}
      while(!CheckStops()) {}
      openedBuy=true;
      return;
   }
   if(ObjectFind("sell")!=-1 && openedSell==false
    && ((!sellLineUp && MarketInfo(Symbol(), MODE_BID)<ObjectGetValueByShift("sell",0)) || (sellLineUp && MarketInfo(Symbol(), MODE_BID)>ObjectGetValueByShift("sell",0))))
   {  
      while(!OpenOrder(Symbol(), OP_SELL)) {}
      while(!CheckStops()) {}
      openedSell=true;
      return;
   }  
   
   if(ObjectFind("buyTP")!=-1)
   {
      if(ObjectGetValueByShift("buyTP",0)<Bid)
         CloseOrders(Symbol(), MagicNumber, OP_BUY);
   } 
   if(ObjectFind("buySL")!=-1)
   {
      if(ObjectGetValueByShift("buySL",0)>Bid)
         CloseOrders(Symbol(), MagicNumber, OP_BUY);
   }  
   if(ObjectFind("sellTP")!=-1)
   {
      if(ObjectGetValueByShift("sellTP",0)>Bid)
         CloseOrders(Symbol(), MagicNumber, OP_SELL);
   } 
   if(ObjectFind("sellSL")!=-1)
   {
      if(ObjectGetValueByShift("sellSL",0)<Bid)
         CloseOrders(Symbol(), MagicNumber, OP_SELL);
   } 
   
   if(TrailingStop>0)
      if(!TrailingStopCheck())
         return;
         
//----
   return(0);
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
            if((OrderStopLoss()==0 && stopLoss>0) || (OrderTakeProfit()==0 && takeProfit>0))
            {  
               while (!IsTradeAllowed()) Sleep(500); 
               RefreshRates();
               
               if(OrderStopLoss()==0 && stopLoss>0)
               {
                  sl = OrderOpenPrice()-stopLoss; 
                  if(Bid-sl<=minGapStop)
                     sl = Bid-minGapStop*2;
               }
               else
                  sl = OrderStopLoss();
               
               if(OrderTakeProfit()==0 && takeProfit>0)   
               {
                  tp = OrderOpenPrice()+takeProfit;
                  if(tp-Bid<=minGapStop)
                     tp = Bid+minGapStop*2;
               }
               else
                  tp = OrderTakeProfit();
                     
               if(!OrderModify(OrderTicket(),OrderOpenPrice(),sl,tp,0,Green)) 
                  return (false);
            }
         }   
         if(OrderType()==OP_SELL)
         {
            if((OrderStopLoss()==0 && stopLoss>0) || (OrderTakeProfit()==0 && takeProfit>0))
            {        
               while (!IsTradeAllowed()) Sleep(500); 
               RefreshRates();  
               
               if(OrderStopLoss()==0 && stopLoss>0)    
               {        
                  sl = OrderOpenPrice()+stopLoss;         
                  if(sl-Ask<=minGapStop)
                     sl = Ask+minGapStop*2;              
               }
               else
                  sl = OrderStopLoss();
               
               if(OrderTakeProfit()==0 && takeProfit>0)
               {
                  tp = OrderOpenPrice()-takeProfit;               
                  if(Ask-tp<=minGapStop)
                     tp = Ask-minGapStop*2;
               }
               else
                  tp = OrderTakeProfit();
                       
               if(!OrderModify(OrderTicket(),OrderOpenPrice(),sl,tp,0,Green)) 
                  return (false);
            }
         } 
      }
   }
   return (true);
}

//////////////////////////////////////////////////////////////////////////////////////////////////
bool OpenOrder(string symbol, int orderType)
{
   double olots=lots;
   {           
         while (!IsTradeAllowed()) Sleep(300); 
         
         if(orderType==OP_BUY)
         {
            ticket=OrderSend(symbol, OP_BUY, olots, MarketInfo(symbol, MODE_ASK),maxSlippage, 0,0,"",MagicNumber,0,Green);
         }
         if(orderType==OP_SELL)
         {
            ticket=OrderSend(symbol, OP_SELL, olots, MarketInfo(symbol, MODE_BID),maxSlippage, 0,0,"",MagicNumber,0,Red);
         }
            
         if(ticket>0)
         {
            return(true);              
         }
         else 
         {
            return(false);
         }
   }
   return (false);   
}

// cmd = OP_ALL // OP_ALL = OP_BUY || OP_SELL || OP_BUYSTOP || OP_SELLSTOP || OP_BUYLIMIT || OP_SELLLIMIT
////////////////////////////////////////////////////////////////////////////////////////////////////////
bool CloseOrders(string symbol, int magic, int cmd)
{
    int total  = OrdersTotal();
      for (int cnt = total-1 ; cnt >=0 ; cnt--)
      {
         OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES);
         if (OrderSymbol()==symbol && OrderMagicNumber() == magic)
         {
            while(IsTradeContextBusy()) Sleep(100);
            if((cmd==OP_BUY || cmd==OP_ALL) && OrderType()==OP_BUY)
            {
               if(!OrderClose(OrderTicket(),OrderLots(),MarketInfo(symbol,MODE_BID),maxSlippage,Violet)) 
               {
                  Print("Error closing " + OrderType() + " order : ",GetLastError());
                  return (false);
               }
            }
            if((cmd==OP_SELL || cmd==OP_ALL) && OrderType()==OP_SELL)
            {  
               if(!OrderClose(OrderTicket(),OrderLots(),MarketInfo(symbol,MODE_ASK),maxSlippage,Violet)) 
               {
                  Print("Error closing " + OrderType() + " order : ",GetLastError());
                  return (false);
               }
            }
            if(cmd==OP_ALL && (OrderType()==OP_BUYSTOP || OrderType()==OP_SELLSTOP || OrderType()==OP_BUYLIMIT || OrderType()==OP_SELLLIMIT))
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
// trailing stop function
bool TrailingStopCheck()
{  
   double newStopLoss;
   int total=OrdersTotal();
   for(int cnt=total-1;cnt>=0;cnt--)
   {
      OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
      if(OrderSymbol()==Symbol() && OrderMagicNumber() == MagicNumber)
      {
         if(OrderType()==OP_BUY)
         {
            newStopLoss = Bid-trailingStop;                   
            if(newStopLoss>OrderOpenPrice() && newStopLoss>OrderStopLoss()+trailingStop)
            {
               while (!IsTradeAllowed()) Sleep(500);
               RefreshRates();
               if(OrderModify(OrderTicket(),OrderOpenPrice(),newStopLoss,OrderTakeProfit(),0,Green)) // modify position
               {
                   return (true);
               }
               else 
               {       
                  return(false);
               } 
            } 
         }
         if(OrderType()==OP_SELL)
         {
            // should it be modified? 
            newStopLoss = Ask+trailingStop;        
            if(newStopLoss<OrderOpenPrice() && newStopLoss<OrderStopLoss()-trailingStop)
            {
               while (!IsTradeAllowed()) Sleep(500);
               RefreshRates();
               if(OrderModify(OrderTicket(),OrderOpenPrice(),newStopLoss,OrderTakeProfit(),0,Green)) // modify position
               {
                  return (true);
               }
               else 
               {      
                  return(false);
               } 
            }          
         }
      }
   }
   return (true);
}