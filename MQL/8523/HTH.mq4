//+------------------------------------------------------------------+
//|                                                          HTH.mq4 |
//|                                                             c0d3 |
//|                                                                  |
//+------------------------------------------------------------------+
#property copyright "c0d3"
#property link      ""

extern bool trade=true;
extern string C1="EURUSD";
extern string C2="USDCHF";
extern string C3="GBPUSD";
extern string C4="AUDUSD";
extern bool show_profit=true;
extern bool enable_profit=false;
extern bool enable_loss=false;
extern bool enable_emergency_trading=true;
extern int emergency_loss=60;
extern int profit=80;
extern int loss=40;
extern int MagicNumber1=243;
extern int MagicNumber2=244;
extern int MagicNumber3=245;
extern int MagicNumber4=256;
extern int E_MagicNumber=257;
extern double lot=0.01;


void verifyorder(string symbol, int MN, string direction)//function which is used to verify that the orders have been placed
{
Sleep(1000);
int ord_cnt=OrdersTotal();
for (int start=0;start<ord_cnt;start++)   
   {
      OrderSelect(start, SELECT_BY_POS, MODE_TRADES);
      if(OrderMagicNumber()==MN)
         {return;}
   }
if(direction=="LONG"){OrderSend(symbol,OP_BUY,lot,MarketInfo(symbol,MODE_ASK),3,0,0,"Hedge"+symbol,MN);}
if(direction=="SHORT"){OrderSend(symbol,OP_SELL,lot,MarketInfo(symbol,MODE_BID),3,0,0,"Hedge"+symbol,MN);}   
return;
}


void closeorders()
   {
      int close_ord1=OrdersTotal();
         for (int qe1=0;qe1<close_ord1;qe1++)
            {
               OrderSelect(qe1, SELECT_BY_POS, MODE_TRADES);
                 if( OrderMagicNumber()==MagicNumber1  || 
                     OrderMagicNumber()==MagicNumber2  || 
                     OrderMagicNumber()==MagicNumber3  || 
                     OrderMagicNumber()==E_MagicNumber || 
                     OrderMagicNumber()==MagicNumber4)
                     {
                     if(OrderType()==OP_BUY) {RefreshRates();OrderClose(OrderTicket(),OrderLots(),MarketInfo(OrderSymbol(),MODE_BID),3);Sleep(2000);}
                     if(OrderType()==OP_SELL){RefreshRates();OrderClose(OrderTicket(),OrderLots(),MarketInfo(OrderSymbol(),MODE_ASK),3);Sleep(2000);}
                     }
            }
   closeordersverify();         
   return;         
   }
   
   
void closeordersverify()
   {
      int close_ord11=OrdersTotal();
         for (int qe11=0;qe11<close_ord11;qe11++)
            {
               OrderSelect(qe11, SELECT_BY_POS, MODE_TRADES);
                 if( OrderMagicNumber()==MagicNumber1  || 
                     OrderMagicNumber()==MagicNumber2  || 
                     OrderMagicNumber()==MagicNumber3  || 
                     OrderMagicNumber()==E_MagicNumber || 
                     OrderMagicNumber()==MagicNumber4)
                     {closeorders();}
                     //if(OrderType()==OP_BUY) {OrderClose(OrderTicket(),OrderLots(),MarketInfo(OrderSymbol(),MODE_BID),3);Sleep(2000);}
                     //if(OrderType()==OP_SELL){OrderClose(OrderTicket(),OrderLots(),MarketInfo(OrderSymbol(),MODE_ASK),3);Sleep(2000);}
            }
   return;         
   }   
   


void doubleorders()
   {
      int ord_t=OrdersTotal();
         for (int y=0;y<ord_t;y++)
            {
            OrderSelect(y, SELECT_BY_POS, MODE_TRADES);
                              if(OrderMagicNumber()==MagicNumber1 || 
                                 OrderMagicNumber()==MagicNumber2 || 
                                 OrderMagicNumber()==MagicNumber3 || 
                                 OrderMagicNumber()==MagicNumber4)
                                    {
                                       if(OrderProfit()>0)
                                          {
                                             if(OrderType()==OP_BUY) {OrderSend(OrderSymbol(),OP_BUY,OrderLots(),MarketInfo(OrderSymbol(),MODE_ASK),3,0,0,"Emergency Double",E_MagicNumber);}
                                             if(OrderType()==OP_SELL){OrderSend(OrderSymbol(),OP_SELL,OrderLots(),MarketInfo(OrderSymbol(),MODE_BID),3,0,0,"Emergency Double",E_MagicNumber);}
                                          }
                                    }
            }
   enable_emergency_trading=false;         
   }

//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
int init()
  {
//----

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
//----(100 * Close[0] / iClose(NULL,PERIOD_D1,1)) - 100
double d_c1,d_c2,d_c3,d_c4,d_c11,d_c22,d_c33,d_c44;
d_c1=(100*iClose(C1,NULL,0)/iClose(C1,PERIOD_D1,1))-100;
d_c2=(100*iClose(C2,NULL,0)/iClose(C2,PERIOD_D1,1))-100;
d_c3=(100*iClose(C3,NULL,0)/iClose(C3,PERIOD_D1,1))-100;
d_c4=(100*iClose(C4,NULL,0)/iClose(C4,PERIOD_D1,1))-100;

d_c11=(100*iClose(C1,PERIOD_D1,1)/iClose(C1,PERIOD_D1,2))-100;
d_c22=(100*iClose(C2,PERIOD_D1,1)/iClose(C2,PERIOD_D1,2))-100;
d_c33=(100*iClose(C3,PERIOD_D1,1)/iClose(C3,PERIOD_D1,2))-100;
d_c44=(100*iClose(C4,PERIOD_D1,1)/iClose(C4,PERIOD_D1,2))-100;

//check for profit in PIP, and close if the goal is reached
if(show_profit==true)
   {
   double profit1,profit2,profit3,profit4;
   int totalprofit;
   int profitcheck=OrdersTotal();
      for (int tr=0;tr<profitcheck;tr++)
         {
         OrderSelect(tr, SELECT_BY_POS, MODE_TRADES);
            if(OrderMagicNumber()==MagicNumber1)
               {
               if(OrderType()==OP_BUY) {profit1=(MarketInfo(OrderSymbol(),MODE_BID)-OrderOpenPrice())/MarketInfo(OrderSymbol(),MODE_POINT);}
               if(OrderType()==OP_SELL){profit1=(OrderOpenPrice()-MarketInfo(OrderSymbol(),MODE_ASK))/MarketInfo(OrderSymbol(),MODE_POINT);}
               }
               
            if(OrderMagicNumber()==MagicNumber2)
               {
               if(OrderType()==OP_BUY) {profit2=(MarketInfo(OrderSymbol(),MODE_BID)-OrderOpenPrice())/MarketInfo(OrderSymbol(),MODE_POINT);}
               if(OrderType()==OP_SELL){profit2=(OrderOpenPrice()-MarketInfo(OrderSymbol(),MODE_ASK))/MarketInfo(OrderSymbol(),MODE_POINT);}
               } 
                 
            if(OrderMagicNumber()==MagicNumber3)
               {
               if(OrderType()==OP_BUY) {profit3=(MarketInfo(OrderSymbol(),MODE_BID)-OrderOpenPrice())/MarketInfo(OrderSymbol(),MODE_POINT);}
               if(OrderType()==OP_SELL){profit3=(OrderOpenPrice()-MarketInfo(OrderSymbol(),MODE_ASK))/MarketInfo(OrderSymbol(),MODE_POINT);}
               }
               
            if(OrderMagicNumber()==MagicNumber4)
               {
               if(OrderType()==OP_BUY) {profit4=(MarketInfo(OrderSymbol(),MODE_BID)-OrderOpenPrice())/MarketInfo(OrderSymbol(),MODE_POINT);}
               if(OrderType()==OP_SELL){profit4=(OrderOpenPrice()-MarketInfo(OrderSymbol(),MODE_ASK))/MarketInfo(OrderSymbol(),MODE_POINT);}
               } 
             
             //check profit of emergency trades
             int u_orders=OrdersTotal();
             double e_profit=0;
             for (int h=0;h<=u_orders;h++)  
               {
                 OrderSelect(h, SELECT_BY_POS, MODE_TRADES);
                   if(OrderMagicNumber()==E_MagicNumber)
                     {
                       if(OrderType()==OP_BUY) {e_profit+=(MarketInfo(OrderSymbol(),MODE_BID)-OrderOpenPrice())/MarketInfo(OrderSymbol(),MODE_POINT);}
                       if(OrderType()==OP_SELL){e_profit+=(OrderOpenPrice()-MarketInfo(OrderSymbol(),MODE_ASK))/MarketInfo(OrderSymbol(),MODE_POINT);}
                      }
               } 
                    
         }
         totalprofit=profit1+profit2+profit3+profit4+e_profit;
         if(enable_emergency_trading==true && totalprofit<=-emergency_loss){doubleorders();}
         if(enable_profit==true && totalprofit>=profit){closeorders();}
         if(enable_loss==true && totalprofit<=-loss){closeorders();}
   }
//end check for profit



Comment("\n",
C1+" Deviation: "+d_c1+" | Previous Deviation: "+d_c11,
"\n",C2+" Deviation: "+d_c2+" | Previous Deviation: "+d_c22,
"\n",C3+" Deviation: "+d_c3+" | Previous Deviation: "+d_c33,
"\n",C4+" Deviation: "+d_c4+" | Previous Deviation: "+d_c44,
"\n",
"\n",C1+"   "+C2+" Pair Deviation: "+(d_c1+d_c2),
"\n",C1+"   "+C3+" Pair Deviation: "+(d_c1-d_c3),
"\n",C1+"   "+C4+" Pair Deviation: "+(d_c1-d_c4),
"\n",C2+"   "+C3+" Pair Deviation: "+(d_c2+d_c3),
"\n",C3+"   "+C4+" Pair Deviation: "+(d_c3-d_c4),
"\n",C2+"   "+C4+" Pair Deviation: "+(d_c2+d_c4),
"\n",
"\n",C1+"/"+C2+" vs. "+C3+"/"+C4+" Pair Deviation: "+((d_c1+d_c2)+(d_c3-d_c4)),
"\n","PIP Profit: "+totalprofit ); 


//close orders after one Day
if(Hour()>=23)
   {
      int close_ord=OrdersTotal();
         for (int qe=0;qe<close_ord;qe++)
            {
               OrderSelect(qe, SELECT_BY_POS, MODE_TRADES);
                 if( OrderMagicNumber()==MagicNumber1  || 
                     OrderMagicNumber()==MagicNumber2  || 
                     OrderMagicNumber()==MagicNumber3  ||
                     OrderMagicNumber()==E_MagicNumber ||  
                     OrderMagicNumber()==MagicNumber4)
                     {
                     if(OrderType()==OP_BUY) {OrderClose(OrderTicket(),OrderLots(),MarketInfo(OrderSymbol(),MODE_BID),3);Sleep(2000);}
                     if(OrderType()==OP_SELL){OrderClose(OrderTicket(),OrderLots(),MarketInfo(OrderSymbol(),MODE_ASK),3);Sleep(2000);}
                     }
            }
            closeorders();
   }
//end close orders





//check for opened positions, do not continue if positions are opened
int ord_cnt1=OrdersTotal();
for (int start1=0;start1<ord_cnt1;start1++)   
   {
      OrderSelect(start1, SELECT_BY_POS, MODE_TRADES);
      if(OrderMagicNumber()==MagicNumber1  || 
         OrderMagicNumber()==MagicNumber2  || 
         OrderMagicNumber()==MagicNumber3  || 
         OrderMagicNumber()==E_MagicNumber || 
         OrderMagicNumber()==MagicNumber4)
         {return(0);}
   }

int ticket1,ticket2,ticket3,ticket4;
  //if(trade==true)
  if(Hour()>=0 && Hour()<1 && (Minute()>=5 && Minute()<=12))//start of a new day
   {

      //turn on emergency_exit
      enable_emergency_trading=true;

      if(trade==true && d_c11>0 && IsTradeAllowed()==true) //Previous Day's Deviation is Positive
         {
            //LONG EURUSD
            RefreshRates();
            ticket1=OrderSend(C1,OP_BUY,lot,MarketInfo(C1,MODE_ASK),3,0,0,"Hedge"+C1,MagicNumber1);
            if (ticket1<0){verifyorder(C1,MagicNumber1,"LONG");}Sleep(5000);

            //LONG USDCHF
            RefreshRates();
            ticket2=OrderSend(C2,OP_BUY,lot,MarketInfo(C2,MODE_ASK),3,0,0,"Hedge"+C2,MagicNumber2);
            if (ticket2<0){verifyorder(C2,MagicNumber2,"LONG");}Sleep(5000); 

            //SHORT GBPUSD
            RefreshRates();
            ticket3=OrderSend(C3,OP_SELL,lot,MarketInfo(C3,MODE_BID),3,0,0,"Hedge"+C3,MagicNumber3);
            if (ticket3<0){verifyorder(C3,MagicNumber3,"SHORT");}Sleep(5000); 

            //LONG AUDUSD
            RefreshRates();
            ticket4=OrderSend(C4,OP_BUY,lot,MarketInfo(C4,MODE_ASK),3,0,0,"Hedge"+C4,MagicNumber4);
            if (ticket4<0){verifyorder(C4,MagicNumber4,"LONG");}Sleep(5000); 
         }


      if(trade==true && d_c11<0 && IsTradeAllowed()==true) //Previous Day's Deviation is Negative
         {
            //LONG EURUSD
            RefreshRates();
            ticket1=OrderSend(C1,OP_SELL,lot,MarketInfo(C1,MODE_BID),3,0,0,"Hedge"+C1,MagicNumber1);
            if (ticket1<0){verifyorder(C1,MagicNumber1,"SHORT");}Sleep(5000);

            //LONG USDCHF
            RefreshRates();
            ticket2=OrderSend(C2,OP_SELL,lot,MarketInfo(C2,MODE_BID),3,0,0,"Hedge"+C2,MagicNumber2);
            if (ticket2<0){verifyorder(C2,MagicNumber2,"SHORT");}Sleep(5000); 

            //SHORT GBPUSD
            RefreshRates();
            ticket3=OrderSend(C3,OP_BUY,lot,MarketInfo(C3,MODE_ASK),3,0,0,"Hedge"+C3,MagicNumber3);
            if (ticket3<0){verifyorder(C3,MagicNumber3,"LONG");}Sleep(5000); 

            //LONG AUDUSD
            RefreshRates();
            ticket4=OrderSend(C4,OP_SELL,lot,MarketInfo(C4,MODE_BID),3,0,0,"Hedge"+C4,MagicNumber4);
            if (ticket4<0){verifyorder(C4,MagicNumber4,"SHORT");}Sleep(5000); 
         }
   }

  
//----
   return(0);
  }
//+------------------------------------------------------------------+