//+------------------------------------------------------------------+
//| RAND002 CLOSE POSITION WITH TRAILING STOP FUNTION                |
//+------------------------------------------------------------------+
#define MAGICMA  19730321

extern double SLEEP=5;
extern double SL = 0.00080;
extern double TP = 0.00025;

double NS = 0.00036; //Minimun SL
double TS = 0.00001; //Trailing stop distance
double BID_SL = 0.0;
double ASK_SL = 0.0;
double ma;
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void init()
  {
   NS=MarketInfo("EURUSD",MODE_STOPLEVEL)*Point;    //Fija el nivel de SL actual
   Print("INIT - STOPLEVEL: ",DoubleToStr(NS,5));

   BID_SL = 0.0;
   ASK_SL = 0.0;

   SetLevelStyle(STYLE_SOLID,1,Yellow);

  }
//+------------------------------------------------------------------+
//| Calculate open positions                                         |
//+------------------------------------------------------------------+
int CalculateCurrentOrders(string symbol)
  {
   int buys=0,sells=0;
//----
   for(int i=0;i<OrdersTotal();i++)
     {
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES)==false) break;
      if(OrderSymbol()==Symbol() && OrderMagicNumber()==MAGICMA)
        {
         if(OrderType()==OP_BUY)  buys++;
         if(OrderType()==OP_SELL) sells++;
        }
     }
//---- return orders volume
   if(buys>0) return(buys);
   else       return(-sells);
  }
//+------------------------------------------------------------------+
//| Check for open order conditions                                  |
//+------------------------------------------------------------------+
void CheckForOpen()
  {
   int res;
   double rnd;
   bool uno=true;

   if(!RefreshRates( )) return;

//---- CALCULATE SELL or BUY POSITION
   rnd=CalcSellBuy();

//---- sell conditions
   if(rnd==0)
     {
      res=OrderSend(Symbol(),OP_SELL,0.1,Bid,0,0,0,"Robot OP_SELL",MAGICMA,0,Red);
      if(res==-1)
        {
         Comment("ERROR on OP_SELL: ",GetLastError()," - Bid: ",Bid);
         Print("ERROR on OP_SELL: ",GetLastError()," - Bid: ",Bid);
           }else{
         ASK_SL=0.0;
         Comment("OP_SELL: ",res);
         //SetLevelValue(1,OrderOpenPrice() - NS - TS);          
         //ObjectDelete("1-SL on OP_SELL");         
         //if(!ObjectCreate("1-SL on OP_SELL", OBJ_HLINE, 0,0, OrderOpenPrice() - TS))
         //  {
         //   Print("error: can't create text_object! code #",GetLastError());
         //   return;
         //  }
        }
      return;
     }
//---- buy conditions
   if(rnd==1)
     {
      res=OrderSend(Symbol(),OP_BUY,0.1,Ask,0,0,0,"Robot OP_BUY",MAGICMA,0,Blue);
      if(res==-1)
        {
         Comment("ERROR on OP_BUY: ",GetLastError()," - Ask: ",Ask);
         Print("ERROR on OP_BUY: ",GetLastError()," - Ask: ",Ask);
           }else{
         BID_SL=0.0;
         Comment("OP_BUY: ",res);
         //SetLevelValue(1,OrderOpenPrice() + NS + TS); 
         //ObjectDelete("1-SL on OP_BUY");         
         //if(!ObjectCreate("1-SL on OP_BUY", OBJ_HLINE, 0,0 , OrderOpenPrice() + TS))
         //  {
         //   Print("error: can't create text_object! code #",GetLastError());
         //   return;
         //  }
        }
      return;
     }
  }
//+------------------------------------------------------------------+
//| Check for close order conditions                                 |
//+------------------------------------------------------------------+
void CheckForClose()
  {
//Comment("AccountStopoutLevel: ",AccountStopoutLevel( ));
//if(!RefreshRates( )) return;

   for(int i=0;i<OrdersTotal();i++)
     {
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES)==false) break;

      //---- check order type 
      if(OrderType()==OP_BUY)
        {
         if(OrderOpenPrice()>=(Ask+SL))
           {
            if(OrderClose(OrderTicket(),OrderLots(),Bid,0,White)) //PIERDO EN LA COMPRA 
              {
               //res = OrderSend(Symbol(),OP_SELL,0.1,Bid,0,0,0,"Robot OP_SELL",MAGICMA,0,Red);                                
               //Comment("OrderClose: BUY-LOSS so SELL after: ", res); 
               break;
              }
           }
         if((Bid>=(OrderOpenPrice()+NS+TS)) && (BID_SL==0.0))
           {
            //if(OrderModify(OrderTicket(),0,Bid - (NS + TS),0,0,Red))      //Set firts SL on TP conditions      
            if(OrderModify(OrderTicket(),0,Bid-NS,0,0,Red)) //Set firts SL on TP conditions      
              {
               //BID_SL = Bid - (NS + TS);          
               BID_SL=Bid-NS;
               Comment("OrderModify: 1-SET STOP LOSS for BUY Option to: ",BID_SL);
               break;
                 }else {Print("ERROR: In 1-BID_SL: ",GetLastError(),"BID_SL: ",Bid-NS);break;
              }
           }
         if(Bid>=(BID_SL+NS+TS) && (BID_SL!=0.0))
           {
            //if(OrderModify(OrderTicket(),0,Bid - (NS + TS),0,0,Red))      //Set next SL on TP conditions          
            if(OrderModify(OrderTicket(),0,Bid-NS,0,0,Red)) //Set next SL on TP conditions          
              {
               BID_SL=Bid-NS;
               Comment("OrderModify: n-SET STOP LOSS for BUY Option to: ",BID_SL);
               break;
                 }else {Print("ERROR: In 1-BID_SL: ",GetLastError(),"BID_SL: ",Bid-NS);break;
              }
           }
        }

      if(OrderType()==OP_SELL)
        {
         if(Bid>=(OrderOpenPrice()+SL))
           {
            if(OrderClose(OrderTicket(),OrderLots(),Ask,0,White)) //PIERDO EN LA VENTA 
              {
               //res = OrderSend(Symbol(),OP_BUY,0.1,Ask,0,0,0,"Robot OP_BUY",MAGICMA,0,Blue);
               //Comment("OrderClose: SELL-LOSS so BUY after: ", res); 
               break;
              }
           }

         if((OrderOpenPrice()>=(Ask+NS+TS)) && (ASK_SL==0.0))
           {
            if(OrderModify(OrderTicket(),0,Ask+NS,0,0,Red)) //Set firts SL on TS conditions               
              {
               Comment("OrderModify: 1-SET STOP LOSS for SELL Option to: ",Ask+NS);
               ASK_SL=Ask+NS;
               break;
                 }else {Print("ERROR: In 1-BID_SL: ",GetLastError(),"BID_SL: ",Ask+NS);break;
              }
           }

         if(ASK_SL>=(Ask+NS+TS) && (ASK_SL!=0.0))
           {
            if(OrderModify(OrderTicket(),0,Ask+NS,0,0,Red)) //Set next SL on TS conditions         
              {
               Comment("OrderModify: n-SET STOP LOSS for SELL Option to: ",ASK_SL);
               ASK_SL=Ask+NS;
               break;
                 }else {Print("ERROR: In 1-BID_SL: ",GetLastError(),"BID_SL: ",Ask+NS);break;
              }
           }
        }
     }
  }
//+------------------------------------------------------------------+
//| Start function                                                   |
//+------------------------------------------------------------------+
void start()
  {
//---- check for history and trading
   if(Bars<100 || IsTradeAllowed()==false) return;

//---- calculate open orders by current symbol
   if(CalculateCurrentOrders(Symbol())==0)
     {

      Comment("GOING TO SLEEP... at ",TimeHour(TimeCurrent()),":",TimeMinute(TimeCurrent()),":",TimeSeconds(TimeCurrent()));
      Sleep(SLEEP*60*1000);
      Comment("NOW! WAKE UP....");

      CheckForOpen();
     }
   else
     {
      CheckForClose();
     }
//----
  }
//+------------------------------------------------------------------+

int CalcSellBuy()
  {
   int rnd;

   rnd=MathMod(MathRand(),5);

   ma=iMA(NULL,1,100,0,MODE_SMA,PRICE_MEDIAN,0);

   Print("MA(100): ",ma);

//---- get Random position
   if(ma<Bid) //Compra MAS
     {
      if(rnd==1) //OP_SELL
        {
         rnd=0;
        }

      if(rnd==0 || rnd==2 || rnd==3 || rnd==4) //OP_BUY
        {
         rnd=1;
        }
     }

   if(ma>Bid) //Vende MAS
     {
      if(rnd==1) //OP_BUY
        {
         rnd=1;
        }

      if(rnd==0 || rnd==2 || rnd==3 || rnd==4) //OP_SELL
        {
         rnd=0;
        }
     }

   return (rnd);
  }
//+------------------------------------------------------------------+
