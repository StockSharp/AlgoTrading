//+------------------------------------------------------------------+
//|                                                Bill_Williams.mq4 |
//|                                                     Alexey_Zykov |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "Alexey_Zykov"
#property link      "https://www.mql5.com"
#property version   "1.00"
#property strict
//---
input double    Risk=0;                          // Percent
input double    Fix_Lot=0.01;                    // Lot
input double    filter=30;                       // Filter
input int       Magic=70100;                     // Magic
input string    comment="Bill_Williams";         // Comment
input double    Gator_Div_slow=250;              // Delta Lips_Gator & Teeth_Gator
input double    Gator_Div_fast=150;              // Delta Teeth_Gator & Jaw_Gator
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int OnInit()
  {
//---
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
   bool static Error=true; // Flag fatal error
   if(Error==false)
     {
      Comment("Fatal error, EA does not work!");
      return;
     }
   int OrderBuy=0,OrderSell=0;
//---
   for(int i=0;i<OrdersTotal();i++) // 
     {
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES)!=true) // Choose orders
         Error=Error();
      //---
      if(OrderType()==OP_BUY && OrderSymbol()==_Symbol && OrderMagicNumber()==Magic) // Choose type
         OrderBuy++;
      if(OrderType()==OP_SELL && OrderSymbol()==_Symbol && OrderMagicNumber()==Magic)
         OrderSell++;
     }
//---  
   if(OrderBuy==0 && OrderSell==0)
     {
      int Signal=Signal_Bill_Williams(); // Check Signal
      if(Signal!=0)
        {
         //---
         for(int kol_poputok=0;5>kol_poputok;kol_poputok++) // Open Order
           {
            int OpenOrder=OpenOrder(Signal); // Check error
            if(OpenOrder<0)
               Error=Error();
            else  break;
           }
         //---      
        }
      else Comment("EA is waiting for a signal for opening an order");
     }
   else
     {
      bool TrailingStop=TrailingStop(OrderBuy,OrderSell); // Start trailing stop
      if(TrailingStop!=true)
         Error=Error();
     }
   return;
  }
//+------------------------------------------------------------------+
//| Check signal                                                     |
//+------------------------------------------------------------------+
int Signal_Bill_Williams()
  {
   int Sign=0;
   int i,j;
   double fractal_up=0,fractal_down=0;
   double Gator_Jaw=iAlligator(_Symbol,PERIOD_CURRENT,13,8,8,5,5,3,MODE_SMMA,PRICE_MEDIAN,MODE_GATORJAW,1);             // Gator_Jaw
   double Gator_Teeth=iAlligator(_Symbol,PERIOD_CURRENT,13,8,8,5,5,3,MODE_SMMA,PRICE_MEDIAN,MODE_GATORTEETH,1);         // Gator_Teeth
   double Gator_Lips=iAlligator(_Symbol,PERIOD_CURRENT,13,8,8,5,5,3,MODE_SMMA,PRICE_MEDIAN,MODE_GATORLIPS,1);           // Gator_Lips 
//---
   for(i=3;i<Bars-2;i++) // Active fractal down                
     {
      fractal_down=iFractals(_Symbol,PERIOD_CURRENT,MODE_LOWER,i);
      if(fractal_down>0 && fractal_down<=Gator_Teeth) break;
     }
//---
   for(j=3;j<Bars-2;j++) // Active fractal up
     {
      fractal_up=iFractals(_Symbol,PERIOD_CURRENT,MODE_UPPER,j);
      if(fractal_up>=Gator_Teeth) break;
     }
//--- Classic signal
   if(MathAbs(Gator_Jaw-Gator_Teeth)>=Gator_Div_slow*_Point && MathAbs(Gator_Lips-Gator_Teeth)>=Gator_Div_fast*_Point)
     {
      if(Low[0]+filter*_Point<=fractal_down && Open[0]>=fractal_down && fractal_down<=Gator_Teeth && Close[0]<Open[0]) // Classic sell signal
         Sign=1;
      if(High[0]-filter*_Point>=fractal_up && Open[0]<=fractal_up && fractal_up>=Gator_Teeth && Close[0]>Open[0]) // Classic buy signal
         Sign=2;
     }
//--- Pending signals
   if(MathAbs(Gator_Jaw-Gator_Teeth)<Gator_Div_slow*_Point || MathAbs(Gator_Lips-Gator_Teeth)<Gator_Div_fast*_Point)
     {
      if((Low[1]<=fractal_down && Open[1]>=fractal_down && fractal_down<=Gator_Teeth && Low[0]+filter*_Point<Low[1] && Close[0]<Open[0]) || 
         (Low[2]<=fractal_down && Open[2]>=fractal_down && fractal_down<=Gator_Teeth && Low[0]+filter*_Point<Low[2] && Close[0]<Open[0])) // Pending sell signal
         Sign=1;
      if((High[1]>=fractal_up && Open[1]<=fractal_up && fractal_up>=Gator_Teeth && High[0]-filter*_Point>High[1] && Close[0]>Open[0]) || // Pending buy signal
         (High[2]>=fractal_up && Open[2]<=fractal_up && fractal_up>=Gator_Teeth && High[0]-filter*_Point>High[2] && Close[0]>Open[0]))
         Sign=2;
     }
   return(Sign);
  }
//+------------------------------------------------------------------+
//| Function trading operation                                       |
//+------------------------------------------------------------------+
int OpenOrder(int Sign) // Function OpenOrder
  {
   color clr=clrBlack; int typeOrder=-1; double price=0,Lot=0.01;
   switch(Sign)
     {
      case 1: typeOrder=OP_SELL;price=Bid;clr=clrRed;break; // Order type 
      case 2: typeOrder=OP_BUY;price=Ask;clr=clrGreen;break;
     }
//---
   if(Risk==0)
      Lot=Fix_Lot;
   else
      Lot=Risk*AccountFreeMargin()/100000;
   if(Lot<MarketInfo(_Symbol,MODE_MINLOT)) // Min lot
      Lot=MarketInfo(_Symbol,MODE_MINLOT);
   if(Lot>MarketInfo(_Symbol,MODE_MAXLOT)) // Max lot
      Lot=MarketInfo(_Symbol,MODE_MAXLOT);
   if(AccountFreeMargin()<Lot*MarketInfo(_Symbol,MODE_MARGINREQUIRED))
      Lot=AccountFreeMargin()/MarketInfo(_Symbol,MODE_MARGINREQUIRED);
   while(IsTradeAllowed()==false)
     {
      Comment("Trade context busy");
      Sleep(100);
     }
   RefreshRates();
   int Order=OrderSend(_Symbol,typeOrder,NormalizeDouble(Lot,2),NormalizeDouble(price,_Digits),30,0,0,comment,Magic,0,clr);        // Send order
   return(Order);
  }
//+------------------------------------------------------------------+
//| Function check error                                             |
//+------------------------------------------------------------------+
bool Error()
  {
   int Error=GetLastError(); // Get number error
   switch(Error)
     {
      //---
      case 0:   return (true);
      case 4:   Print("Error ¹ 4. Trade server is busy"); Sleep(180000); return (true);
      case 6:   Print("Error ¹ 6. No connection with trade server"); while(!IsConnected()) Sleep(5000); return (true);
      case 8:   Print("Error ¹ 8. Too frequent requests"); Sleep(10000);return (true);
      case 128: Print("Error ¹ 128. Trade timeout"); Sleep(60000); return (true);
      case 132: Print("Error ¹ 132. Market is closed"); Sleep(180000); return (true);
      case 135: Print("Error ¹ 135. Price changed"); return (true);
      case 136: Print("Error ¹ 136. Off quotes"); Sleep(5000); return (true);
      case 137: Print("Error ¹ 137. Broker is busy"); Sleep(10000); return (true);
      case 138: Print("Error ¹ 138. Requote"); return (true);
      case 139: Print("Error ¹ 139. Order is locked"); Sleep(60000); return (true);
      case 141: Print("Error ¹ 141. Too many requests"); Sleep(10000); return (true);
      case 142: Print("Error ¹ 142. The order is queued"); Sleep(60000); return (true);
      case 143: Print("Error ¹ 142. Order accepted by the dealer for execution"); Sleep(60000); return (true);
      case 145: Print("Error ¹ 145. Modification denied because order is too close to market"); Sleep(15000); return (true);
      case 146: Print("Error ¹ 146. Trade context is busy"); while(IsTradeContextBusy()==true) Sleep(500); return (true);
      //---
      case 2:   Print("Error ¹ 2. Common error"); return (false);
      case 3:   Print("Error ¹ 3. Invalid trade parameters"); return (false);
      case 5:   Print("Error ¹ 5. Old version of the client terminal"); return (false);
      case 7:   Print("Error ¹ 7. Not enough rights"); return (false);
      case 64:  Print("Error ¹ 64. Account disabled"); return (false);
      case 65:  Print("Error ¹ 65. Invalid account"); return (false);
      case 129: Print("Error ¹ 129. Invalid price"); return (false);
      case 130: Print("Error ¹ 130. Invalid stops"); return (false);
      case 131: Print("Error ¹ 131. Invalid trade volume"); return (false);
      case 133: Print("Error ¹ 133. Trade is disabled"); return (false);
      case 134: Print("Error ¹ 134. Not enough money for trading"); return (false);
      case 140: Print("Error ¹ 140. Buy orders only allowed"); return (false);
      case 147: Print("Error ¹ 147. Expirations are denied by broker"); return (false);
      case 148: Print("Error ¹ 148. The amount of open and pending orders has reached the limit set by the broker"); return (false);
      case 149: Print("Error ¹ 149. An attempt to open an order opposite to the existing one when hedging is disabled"); return (false);
      case 150: Print("Error ¹ 150. An attempt to close an order contravening the FIFO rule"); return (false);
      default:  Print("Error ¹ ",Error); return (false);
     }
  }
//+------------------------------------------------------------------+
//| Function TrailingStop                                            |
//+------------------------------------------------------------------+
bool TrailingStop(int OrdBuy,int OrdSell)
  {
   bool  OrderMod=true;
//---
   if(OrdBuy>0)
     {
      Comment("EA is supporting opened ",OrdBuy," Buy order(s)");
      for(int i=0;i<OrdersTotal();i++)
        {
         if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES)==true)
           {
            double BuyStopLoss=BuyStopLoss();
            if(NormalizeDouble(BuyStopLoss,_Digits)!=OrderStopLoss() && Bid-NormalizeDouble(BuyStopLoss,_Digits)>1.5*MarketInfo(_Symbol,MODE_STOPLEVEL))
              {
               if(NormalizeDouble(BuyStopLoss,_Digits)>OrderStopLoss() || OrderStopLoss()==0)
                 {
                  int Ticket=OrderTicket();
                  OrderMod=OrderModify(Ticket,OrderOpenPrice(),NormalizeDouble(BuyStopLoss,_Digits),OrderTakeProfit(),OrderExpiration(),clrNONE);
                 }
              }
           }
         else return(false);
        }
     }
//---
   if(OrdSell>0)
     {
      Comment("EA is supporting opened ",OrdSell," Sell order(s)");
      for(int i=0;i<OrdersTotal();i++)
        {
         if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES)==true)
           {
            double SellStopLoss=SellStopLoss();
            if(NormalizeDouble(SellStopLoss,_Digits)!=OrderStopLoss() && NormalizeDouble(SellStopLoss,_Digits)-Ask>1.5*MarketInfo(_Symbol,MODE_STOPLEVEL))
              {
               if(NormalizeDouble(SellStopLoss,_Digits)<OrderStopLoss() || OrderStopLoss()==0)
                 {
                  int Ticket=OrderTicket();
                  OrderMod=OrderModify(Ticket,OrderOpenPrice(),NormalizeDouble(SellStopLoss,_Digits),OrderTakeProfit(),OrderExpiration(),clrNONE);
                 }
              }
           }
         else return(false);
        }
     }
//---
   return(OrderMod);
  }
//+------------------------------------------------------------------+
//| Function BuyStopLoss                                             |
//+------------------------------------------------------------------+
double BuyStopLoss()
  {
   double fractal_down=0,BuySL=0;
//---
   for(int i=3;i<Bars-2;i++) // Fractal down                 
     {
      fractal_down=iFractals(_Symbol,PERIOD_CURRENT,MODE_LOWER,i);
      if(fractal_down>0) break;
     }
//---
   double Gator_Teeth=iAlligator(_Symbol,PERIOD_CURRENT,13,8,8,5,5,3,MODE_SMMA,PRICE_MEDIAN,MODE_GATORTEETH,1);        // Gator_Teeth
   if(Low[1]<Gator_Teeth && OrderStopLoss()==0)
      BuySL=NormalizeDouble(Low[1]-filter*_Point,_Digits);
   else
     {
      if(fractal_down>=Gator_Teeth)
         BuySL=NormalizeDouble(fractal_down-filter*_Point,_Digits);
      else
         BuySL=NormalizeDouble(Gator_Teeth-filter*_Point,_Digits);
     }
   return(BuySL);
  }
//+------------------------------------------------------------------+
//| Function SellStopLoss                                            |
//+------------------------------------------------------------------+
double SellStopLoss()
  {
   double fractal_up=0,SellSL=0;
//---
   for(int j=3;j<Bars-2;j++) // Fractal up
     {
      fractal_up=iFractals(_Symbol,PERIOD_CURRENT,MODE_UPPER,j);
      if(fractal_up>0) break;
     }
//---
   double Gator_Teeth=iAlligator(_Symbol,PERIOD_CURRENT,13,8,8,5,5,3,MODE_SMMA,PRICE_MEDIAN,MODE_GATORTEETH,1);        // Gator_Teeth
   if(High[1]>Gator_Teeth && OrderStopLoss()==0)
      SellSL=NormalizeDouble(High[1]+filter*_Point,_Digits);
   else
     {
      if(fractal_up<=Gator_Teeth)
         SellSL=NormalizeDouble(fractal_up+filter*_Point,_Digits);
      else
         SellSL=NormalizeDouble(Gator_Teeth+filter*_Point,_Digits);
     }
   return(SellSL);
  }
//+------------------------------------------------------------------+
