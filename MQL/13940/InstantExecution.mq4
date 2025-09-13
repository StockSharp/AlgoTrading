//+------------------------------------------------------------------+
//|                                                    InstantExecution.mq4 |
//|                                 Copyright 2015, @traderconfident |
//|                            https://confident-trader.blogspot.com |
//+------------------------------------------------------------------+
#property copyright "Copyright 2015, @traderconfident"
#property link      "https://confident-trader.blogspot.com"
#property version   "1.0"
#property strict

//+------------------------------------------------------------------+
//| Expert initialization function  
                  
//+------------------------------------------------------------------+

extern string _Orders_ = " --- Set Order ---";
extern double Lots = 0.05; 
extern int StopLoss = 0;
extern int TakeProfit = 70;              
extern int TrailingStart = 5 ;  
extern int TrailingSize = 5 ;    
extern int MaxOrderAtOnceTime = 1;
extern int Slippage = 3;
extern int Magic = 90910;



double _sl, _tp, _pip;


int OnInit()
  {
    _pip = Point;
    if(Digits==3 || Digits==5) _pip = 10*Point;
    
//---
   ObjectCreate(0,"CloseButton",OBJ_BUTTON,0,0,0);
   ObjectSetInteger(0,"CloseButton",OBJPROP_XDISTANCE,10);
   ObjectSetInteger(0,"CloseButton",OBJPROP_YDISTANCE,15);
   ObjectSetInteger(0,"CloseButton",OBJPROP_XSIZE,100);
   ObjectSetInteger(0,"CloseButton",OBJPROP_YSIZE,25);
   
   ObjectSetString(0,"CloseButton",OBJPROP_TEXT,"Close Orders");
   
   ObjectSetInteger(0,"CloseButton",OBJPROP_COLOR, White);
   ObjectSetInteger(0,"CloseButton",OBJPROP_BGCOLOR, Red);
   ObjectSetInteger(0,"CloseButton",OBJPROP_BORDER_COLOR,Red);
   ObjectSetInteger(0,"CloseButton",OBJPROP_BORDER_TYPE,BORDER_FLAT);
   ObjectSetInteger(0,"CloseButton",OBJPROP_HIDDEN,true);
   ObjectSetInteger(0,"CloseButton",OBJPROP_STATE,false);
   ObjectSetInteger(0,"CloseButton",OBJPROP_FONTSIZE,12);
   
   //Exit
   ObjectCreate(0,"Exit",OBJ_BUTTON,0,0,0);
   ObjectSetInteger(0,"Exit",OBJPROP_XDISTANCE,120);
   ObjectSetInteger(0,"Exit",OBJPROP_YDISTANCE,15);
   ObjectSetInteger(0,"Exit",OBJPROP_XSIZE,80);
   ObjectSetInteger(0,"Exit",OBJPROP_YSIZE,25);
   
   ObjectSetString(0,"Exit",OBJPROP_TEXT,"Exit");
   
   ObjectSetInteger(0,"Exit",OBJPROP_COLOR, White);
   ObjectSetInteger(0,"Exit",OBJPROP_BGCOLOR, Green);
   ObjectSetInteger(0,"Exit",OBJPROP_BORDER_COLOR,Green);
   ObjectSetInteger(0,"Exit",OBJPROP_BORDER_TYPE,BORDER_FLAT);
   ObjectSetInteger(0,"Exit",OBJPROP_HIDDEN,true);
   ObjectSetInteger(0,"Exit",OBJPROP_STATE,false);
   ObjectSetInteger(0,"Exit",OBJPROP_FONTSIZE,12);
   
   //Buy
   ObjectCreate(0,"Buy",OBJ_BUTTON,0,0,0);
   ObjectSetInteger(0,"Buy",OBJPROP_XDISTANCE,210);
   ObjectSetInteger(0,"Buy",OBJPROP_YDISTANCE,15);
   ObjectSetInteger(0,"Buy",OBJPROP_XSIZE,50);
   ObjectSetInteger(0,"Buy",OBJPROP_YSIZE,25);
   
   ObjectSetString(0,"Buy",OBJPROP_TEXT,"Buy");
   
   ObjectSetInteger(0,"Buy",OBJPROP_COLOR, White);
   ObjectSetInteger(0,"Buy",OBJPROP_BGCOLOR, Blue);
   ObjectSetInteger(0,"Buy",OBJPROP_BORDER_COLOR,Blue);
   ObjectSetInteger(0,"Buy",OBJPROP_BORDER_TYPE,BORDER_FLAT);
   ObjectSetInteger(0,"Buy",OBJPROP_HIDDEN,true);
   ObjectSetInteger(0,"Buy",OBJPROP_STATE,false);
   ObjectSetInteger(0,"Buy",OBJPROP_FONTSIZE,12);
   
   //Sell
   ObjectCreate(0,"Sell",OBJ_BUTTON,0,0,0);
   ObjectSetInteger(0,"Sell",OBJPROP_XDISTANCE,270);
   ObjectSetInteger(0,"Sell",OBJPROP_YDISTANCE,15);
   ObjectSetInteger(0,"Sell",OBJPROP_XSIZE,50);
   ObjectSetInteger(0,"Sell",OBJPROP_YSIZE,25);
   
   ObjectSetString(0,"Sell",OBJPROP_TEXT,"Sell");
   
   ObjectSetInteger(0,"Sell",OBJPROP_COLOR, White);
   ObjectSetInteger(0,"Sell",OBJPROP_BGCOLOR, Gray);
   ObjectSetInteger(0,"Sell",OBJPROP_BORDER_COLOR,Gray);
   ObjectSetInteger(0,"Sell",OBJPROP_BORDER_TYPE,BORDER_FLAT);
   ObjectSetInteger(0,"Sell",OBJPROP_HIDDEN,true);
   ObjectSetInteger(0,"Sell",OBJPROP_STATE,false);
   ObjectSetInteger(0,"Sell",OBJPROP_FONTSIZE,12);      
      
   //Closed at Profit
   ObjectCreate(0,"CloseAtProfit",OBJ_BUTTON,0,0,0);
   ObjectSetInteger(0,"CloseAtProfit",OBJPROP_XDISTANCE,330);
   ObjectSetInteger(0,"CloseAtProfit",OBJPROP_YDISTANCE,15);
   ObjectSetInteger(0,"CloseAtProfit",OBJPROP_XSIZE,100);
   ObjectSetInteger(0,"CloseAtProfit",OBJPROP_YSIZE,25);
   
   ObjectSetString(0,"CloseAtProfit",OBJPROP_TEXT,"Close Profit");
   
   ObjectSetInteger(0,"CloseAtProfit",OBJPROP_COLOR, White);
   ObjectSetInteger(0,"CloseAtProfit",OBJPROP_BGCOLOR, Green);
   ObjectSetInteger(0,"CloseAtProfit",OBJPROP_BORDER_COLOR,Green);
   ObjectSetInteger(0,"CloseAtProfit",OBJPROP_BORDER_TYPE,BORDER_FLAT);
   ObjectSetInteger(0,"CloseAtProfit",OBJPROP_HIDDEN,true);
   ObjectSetInteger(0,"CloseAtProfit",OBJPROP_STATE,false);
   ObjectSetInteger(0,"CloseAtProfit",OBJPROP_FONTSIZE,12);   
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
//void OnTick(){}
//+------------------------------------------------------------------+

int start(){  
  
   Trailing();
   return(0);
}

//| ChartEvent function                                              |
//+------------------------------------------------------------------+
void OnChartEvent(const int id,
                  const long &lparam,
                  const double &dparam,
                  const string &sparam)
  { 
      int _ticket = 0;
      if(sparam=="CloseButton") // Close button has been pressed
      {
            int total = OrdersTotal();
            int i = 0;
            for (i = total ; i >=0 ; i--)
            {
               if(OrderSelect(i,SELECT_BY_POS) &&  OrderSymbol()==Symbol())
               {
                  //OrderSelect(i,SELECT_BY_POS);
                  if(OrderSymbol()==Symbol() && (OrderType()==OP_BUY || OrderType()==OP_SELL)){
                     _ticket = OrderClose(OrderTicket(),OrderLots(),MarketInfo(Symbol(),MODE_ASK),5);
                     _ticket = OrderClose(OrderTicket(),OrderLots(),MarketInfo(Symbol(),MODE_BID),5);
                  }
               }
            }
            if(_ticket>0){
               ObjectSetInteger(0,"CloseButton",OBJPROP_STATE,false);
               ObjectsDeleteAll();
               ExpertRemove();
            }

      }
      if (sparam=="Exit"){
         ObjectSetInteger(0,"Exit",OBJPROP_STATE,false);
         ObjectsDeleteAll();
         ExpertRemove();
      } 
      if (sparam=="Buy"){
         ObjectSetInteger(0,"Buy",OBJPROP_STATE,false);
         _ticket = Order("BUY");
      } 
      if (sparam=="Sell"){
         ObjectSetInteger(0,"Sell",OBJPROP_STATE,false);
         _ticket = Order("SELL");
      } 
      if (sparam=="CloseAtProfit"){
         ObjectSetInteger(0,"CloseAtProfit",OBJPROP_STATE,false);
         CloseAtProfit();
      }     
  }
//+------------------------------------------------------------------+

void Trailing(){
   int ticket=0;
   for(int cnt=0;cnt<OrdersTotal();cnt++) {
      ticket = OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
      if( OrderSymbol()==Symbol() && OrderMagicNumber()==Magic)     
        {
         if(OrderType()==OP_BUY)
           {
            if((TakeProfit>0 && Bid-OrderOpenPrice()>=TakeProfit*_pip) || (StopLoss>0 && OrderOpenPrice()-Ask>StopLoss*_pip))
              {
               ticket = OrderClose(OrderTicket(),OrderLots(),Bid,0,Violet);
              }
 //Trail  
            if(TrailingStart>0){
               if(Bid-OrderOpenPrice()>TrailingStart*_pip){
                  if(OrderStopLoss()<Bid -(TrailingSize+1)*_pip){
                     ticket = OrderModify(OrderTicket(),OrderOpenPrice(),Bid-TrailingSize*_pip,OrderTakeProfit(),0,Violet);
                  }
               }
            }
           }
         if(OrderType()==OP_SELL)
           {
            if((TakeProfit>0 && OrderOpenPrice()-Ask>=TakeProfit*_pip) || (StopLoss>0 && Bid-OrderOpenPrice()>StopLoss*_pip))
              {
               ticket = OrderClose(OrderTicket(),OrderLots(),Ask,0,Violet); 
              }
            //Trail  
            if(TrailingStart>0){        
               if(OrderOpenPrice()-Ask>TrailingStart*_pip){
                  if(OrderStopLoss()>Ask+(TrailingSize+1)*_pip || OrderStopLoss()==0){
                     ticket = OrderModify(OrderTicket(),OrderOpenPrice(),Ask+TrailingSize*_pip,OrderTakeProfit(),0,Violet);
                  }
               }                   
            }
           }
        }
    }
}


void CloseAtProfit(){
   int ticket=0;
   RefreshRates();
   for(int cnt=0;cnt<OrdersTotal();cnt++) {
      ticket = OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
      if(OrderSymbol()==Symbol() && OrderType()==OP_BUY && Bid>OrderOpenPrice()) {
         ticket = OrderClose(OrderTicket(),OrderLots(),Bid,0,Violet);
      }
      if(OrderSymbol()==Symbol() && OrderType()==OP_SELL && OrderOpenPrice()>Ask) {
         ticket = OrderClose(OrderTicket(),OrderLots(),Ask,0,Violet);       
      }
   }
}

int Order(string _Order) {
   int i, ticket = 0;
   _sl = 0.0;
   _tp = 0.0;
   if(_Order=="BUY"){
      for (i=0; i<MaxOrderAtOnceTime; i++) {
         ticket = OrderSend(Symbol(),OP_BUY,Lots,Ask,Slippage,_sl,_tp,"",Magic,0,Blue);                            
      }
   }else {
      for (i=0; i<MaxOrderAtOnceTime; i++) {
        ticket =  OrderSend(Symbol(),OP_SELL,Lots,Bid,Slippage,_sl,_tp,"",Magic,0,Red);
      }          
   }
   
   return(ticket);
}


