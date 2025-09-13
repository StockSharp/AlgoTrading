//+------------------------------------------------------------------+
//|                                        Template_M5_Envelopes.mq4 |
//|                                                   JorgeDeveloper |
//|                     https://www.mql5.com/en/users/jorgedeveloper |
//+------------------------------------------------------------------+
#property copyright "JorgeDeveloper"
#property link      "https://www.mql5.com/en/users/jorgedeveloper"
#property version   "1.00"
#property strict


extern int Max_Spread               = 10;         // Max Spread / points
extern int Take_Profit              = 50;         // Take profit / points
extern int Stop_Loss                = 100;        // Stop loss / points
extern int Enter_Point              = 30;         // Points for Order entry
extern bool	Trail_My_Orders         = true;       // Trailing stop
extern int	Trailing_Stop           = 30;         // Trailing distance
extern double FixedLot              = 0.01;       // Lot
extern int Envelopes_Period         = 3;          // Envelopes Period
extern double Envelopes_Desviation  = 0.07;       // Envelopes Desviation
extern int Distance                 = 140;
extern string OtherSettings = "=============================== Other Settings ===============================";
extern int Magic                    = 924578;     // Magic number
extern int	Slippage                = 15;         // Slippage

double point;
string Signal = "empty";

//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
      if (Point == 0.00001) 
         point = 0.0001;     // 5 Digits
      else if (Point == 0.001) 
         point = 0.01;       // 3 Digits
      else point = Point;
      
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
      Signal();
      OpenOrders();
      OrderModifyFnct();
  }
//+------------------------------------------------------------------+

void Signal(){

   RefreshRates();

	double envup = iEnvelopes(Symbol(), 0, Envelopes_Period, MODE_LWMA, 0, PRICE_MEDIAN, Envelopes_Desviation, MODE_UPPER, 1);
	double envdn = iEnvelopes(Symbol(), 0, Envelopes_Period, MODE_LWMA, 0, PRICE_MEDIAN, Envelopes_Desviation, MODE_LOWER, 1);
	
   Signal = "empty";

   if (envdn-Low[1] > Distance*Point && envdn-Bid > Distance*Point)            	
   	Signal = "buy";
   else if (High[1]-envup > Distance*Point && Bid-envup > Distance*Point)    	
   	Signal = "sell";
}

void OpenOrders(){

   RefreshRates();
   
   string description = "";
   double lot = FixedLot;
   
   long spread = SymbolInfoInteger(Symbol(), SYMBOL_SPREAD);
   
   // Spread Filter
   if(spread <= Max_Spread && CountMyOrders("all") < 1){
   
      // Buy
      if(Signal == "buy"){
      
         double price = NormalizeDouble(Ask+(Enter_Point*Point), Digits);
         double sl = ((Stop_Loss == 0) ? 0: getStopLoss(Stop_Loss, OP_BUYSTOP, price));
         double tp = ((Take_Profit == 0) ? 0: getTakeProfit(Take_Profit, OP_BUYSTOP, price));
         
         if(OrderSend(Symbol(), OP_BUYSTOP, lot, price, Slippage, sl, tp, "Order BUY STOP", Magic, 0, clrBlue) == -1){
            return;
         }
      }

      // Sell
      if(Signal=="sell"){

         double price = NormalizeDouble(Bid-(Enter_Point*Point), Digits);
         double sl = ((Stop_Loss == 0) ? 0 : getStopLoss(Stop_Loss, OP_SELLSTOP, price));
         double tp = ((Take_Profit == 0) ? 0 : getTakeProfit(Take_Profit, OP_SELLSTOP, price));
         
         if(OrderSend(Symbol(),OP_SELLSTOP, lot, price, Slippage, sl, tp, "Order SELL STOP", Magic, 0, clrRed) == -1){
            return;
         }
      }
   }
}

void OrderModifyFnct(){

   RefreshRates();
   
   long spread = SymbolInfoInteger(Symbol(), SYMBOL_SPREAD);

   // Delete orders
   if(spread > Max_Spread)
      DeleteOrders();
      
   // Modify orders
   for (int i = OrdersTotal() - 1; i >= 0; i--){
      RefreshRates();
  
      if(OrderSelect(i, SELECT_BY_POS)){
      
         int ticket = OrderTicket();
         int type = OrderType();
         int aux = 0;
         double price = 0.0, sl = 0.0, tp = 0.0;
         
         double openPrice = OrderOpenPrice();
   
         if(OrderMagicNumber() == Magic){
         
            // Si el precio baja 3 points cambia el precio de la operacion
            if (type == OP_BUYSTOP){
            
               price = NormalizeDouble(Ask+(Enter_Point*Point), Digits);
               aux = int((openPrice-Ask)/Point);
               
               if(openPrice != price){
               
                  // Si el precio baja mas de 30 puntos modifica la orden
                  if(aux > Enter_Point){
                  
                     sl = ((Stop_Loss == 0) ? 0: getStopLoss(Stop_Loss, OP_BUYSTOP, price));
                     tp = ((Take_Profit == 0) ? 0: getTakeProfit(Take_Profit, OP_BUYSTOP, price));
                  
                     if(OrderModifyCheck(ticket, openPrice, sl, tp)){
                        if(!OrderModify(ticket, price, sl, tp, 0, clrBlue))
                           Print("OrderModify - BuyStop has been ended with an error #",GetLastError());
                     }
                  }
               }
            }
           
            if (type == OP_SELLSTOP){
           
               price = NormalizeDouble(Bid-(Enter_Point*Point), Digits);
               aux = int((Bid-openPrice)/Point);

               if(openPrice != price){
               
                  // Si el precio sube mas de 30 puntos modifica la orden
                  if(aux > Enter_Point){
                  
                     sl = ((Stop_Loss == 0) ? 0 : getStopLoss(Stop_Loss, OP_SELLSTOP, price));
                     tp = ((Take_Profit == 0) ? 0 : getTakeProfit(Take_Profit, OP_SELLSTOP, price));
                  
                     if(OrderModifyCheck(ticket, openPrice, sl, tp)){
                        if(!OrderModify(ticket, price, sl, tp, 0, clrRed))
                           Print("OrderModify - SellStop has been ended with an error #",GetLastError());
                     }
                  }
               }  
            }
         }
      }
   }

   // Trailing stop
   if(Trail_My_Orders){
      TrailingStop();
   }
}

bool OrderModifyCheck(int ticket,double price,double sl,double tp){
   
   if(OrderSelect(ticket,SELECT_BY_TICKET)){
   
      string symbol=OrderSymbol();
      double _point=SymbolInfoDouble(symbol,SYMBOL_POINT);

      bool PriceOpenChanged=true;
      int type=OrderType();
      
      if(!(type==OP_BUY || type==OP_SELL)){
         PriceOpenChanged=(MathAbs(OrderOpenPrice()-price)>_point);
      }
      
      bool StopLossChanged=(MathAbs(OrderStopLoss()-sl)>_point);

      bool TakeProfitChanged=(MathAbs(OrderTakeProfit()-sl)>tp);

      if(PriceOpenChanged || StopLossChanged || TakeProfitChanged)
         return(true);  // order can be modified      
      else
         PrintFormat("Order #%d already has levels of Open=%.5f SL=.5f TP=%.5f",
                     ticket,OrderOpenPrice(),OrderStopLoss(),OrderTakeProfit());
     }
   
   return(false);
  }
  
void TrailingStop(){
   for (int i = OrdersTotal() - 1; i >= 0; i--){
      RefreshRates();

      if(OrderSelect(i,SELECT_BY_POS)){
      
         int ticket = OrderTicket(), type = OrderType();
         long spread = SymbolInfoInteger(Symbol(), SYMBOL_SPREAD);
         
         double   
                  sl       = OrderStopLoss(), 
                  tp       = OrderTakeProfit();
                  
         double openPrice = OrderOpenPrice();
         
         if(OrderMagicNumber() == Magic){
         
            if (type == OP_BUY){
               if(sl < NormalizeDouble(Bid - (Trailing_Stop)*Point, Digits) || sl == 0){
                  sl = NormalizeDouble(Bid - (Trailing_Stop)*Point, Digits);
                  
                  if(OrderModifyCheck(ticket, openPrice, sl, tp)){
                     if(! OrderModify(ticket, OrderOpenPrice(), sl, tp, 0))
                        Print("OrderModify - Buy has been ended with an error #",GetLastError());
                  }
               }
            }
        
            if (type == OP_SELL){
               if(sl > NormalizeDouble(Ask + (Trailing_Stop)*Point, Digits) || sl == 0){
                  sl = NormalizeDouble(Ask + (Trailing_Stop)*Point, Digits);
                  
                  if(OrderModifyCheck(ticket, openPrice, sl, tp)){
                     if(! OrderModify(ticket, OrderOpenPrice(), sl, tp, 0))
                        Print("OrderModify - Sell has been ended with an error #",GetLastError());
                  }
               }
            }
         }
      }
   }
}

int CountMyOrders(string type){
   int count = 0;
  
   for (int i = OrdersTotal() - 1; i >= 0; i--){
   
      RefreshRates();

      if(OrderSelect(i,SELECT_BY_POS))
      {
         if(OrderMagicNumber() == Magic)
         {
            if(type=="all")                          // OP_BUY=0, OP_SELL=1, OP_BUYLIMIT=2, OP_SELLLIMIT=3,OP_BUYSTOP=4,OP_SELLSTOP=5
               count ++;
            if(type=="pending" && OrderType() > 1)   // OP_BUYLIMIT=2, OP_SELLLIMIT=3,OP_BUYSTOP=4,OP_SELLSTOP=5
               count ++;
            if(type=="buy" && OrderType() == 0)      // OP_BUY
               count ++;
            if(type=="sell" && OrderType() == 1)     // OP_SELL
               count ++;
         }
      }
   }
  
   return count;
}

void DeleteOrders(){

   if(CountMyOrders("pending") > 0){
      for (int i = OrdersTotal() - 1; i >= 0; i--){
         RefreshRates();
      
         if(OrderSelect(i,SELECT_BY_POS)){
            if(OrderMagicNumber() == Magic && OrderType()>1){
               if(!OrderDelete(OrderTicket()))
                  Print("OrderDelete has been ended with an error #",GetLastError());
            }
         }
      }
   }
}


double getMinStopLevel(){
   return (MarketInfo(Symbol(), MODE_STOPLEVEL));
}
  
long getSpread(){
   return SymbolInfoInteger(Symbol(), SYMBOL_SPREAD);
}
  
double getStopLoss(double sl, int type, double price = 0.00000001){

   long spread = getSpread();
   sl = (sl > getMinStopLevel()) ? sl : getMinStopLevel();
   
   if(type == OP_BUY){
      return NormalizeDouble(Bid - ((sl) * Point), Digits);
   }
   
   if(type == OP_SELL){
      return NormalizeDouble(Ask + ((sl) * Point), Digits);
   }
   
   if(type == OP_BUYSTOP){
      return NormalizeDouble(price - ((sl) * Point), Digits);
   }
   
   if(type == OP_SELLSTOP){
      return NormalizeDouble(price + ((sl) * Point), Digits);
   }
   
   return 0.0;
}
  
double getTakeProfit(double tp, int type, double price = 0.00000001){

   tp = (tp >= getMinStopLevel()) ? tp : getMinStopLevel();
   
   if(type == OP_BUY){
      return NormalizeDouble(Bid + (tp * Point), Digits);
   }
   
   if(type == OP_SELL){
      return NormalizeDouble(Ask - (tp * Point), Digits);
   }
   
   if(type == OP_BUYSTOP){
      return NormalizeDouble(price + (tp * Point), Digits);
   }
   
   if(type == OP_SELLSTOP){
      return NormalizeDouble(price - (tp * Point), Digits);
   }
   
   return 0.0;
}