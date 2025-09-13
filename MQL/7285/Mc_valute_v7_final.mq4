//+---------------------------------------------------------+
//|                                  Mc_valute_v7_final.mq4 |
//|                                Copyright © 2007, Daniil |
//+---------------------------------------------------------+

#property copyright "Copyright © 2007 Daniil"
#property link      "www.fxmts.ru"


extern string m1="Выбор лота стопа и профита";
extern double TakeProfit1   = 300; 
extern double TakeProfit   = 30; 
extern double Stop         = 350; 
extern double Step         = 35;  
extern double Lot          = 0.1;  

extern string m2="Параметры средних:";
extern double FilterMA=3;
extern string m3="Параметры blue:";
extern double period_blue=13;
extern double shift_blue=8;
extern string m4="Параметры red:";
extern double period_red=8;
extern double shift_red=5;
extern string m5="Параметры lime:";
extern double period_lime=5;
extern double shift_lime=3;

extern string m6="Параметры MACD:";
extern string m7="MACD #1:";
extern double a1=12;
extern double d1=26;
extern double f1=9;
extern string m8="MACD #2:";
extern double a2=33;
extern double d2=68;
extern double f2=15;
extern string m9="MACD #3:";
extern double a3=66;
extern double d3=156;
extern double f3=25;

double OpenPrice_buy1, OpenPrice_sell1;
double OpenPrice_buy2, OpenPrice_sell2;
double OpenPrice_buy3, OpenPrice_sell3;
double OpenPrice_buy4, OpenPrice_sell4;


int cnt = 0;
int i = 0;
int i2 = 0;
int b1,b2,b3,b4;
int s1,s2,s3,s4;
int c1,c2,c3,c4;
int e1,e2,e3,e4;


//------------------------------=========================<<<<<  Start  >>>>>=======================------------------------\\   

int start()
  {
  
int cnt,ticket,total;
    
double SMMA_blue,
       SMMA_red,
       SMMA_lime,
       FMA,
       FMAprev;


double MacdCurrent1, 
       MacdPrevious1, 
       SignalCurrent1,
       SignalPrevious1;
double MacdCurrent2, 
       MacdPrevious2, 
       SignalCurrent2,
       SignalPrevious2; 
double MacdCurrent3, 
       MacdPrevious3, 
       SignalCurrent3,
       SignalPrevious3;      
   

   if(Bars<100)
     {
      Print("bars less than 100");
      return(0);  
     }
   if(TakeProfit<2)
     {
      Print("TakeProfit less than 200");
      return(0);  // check TakeProfit
     }

//--- фильтр

   FMA=iMA(NULL,0,FilterMA,0,MODE_EMA,PRICE_CLOSE,0);
   FMAprev=iMA(NULL,0,FilterMA,0,MODE_EMA,PRICE_CLOSE,1);
   
//--- Средние
   SMMA_blue=iMA(NULL,0,period_blue,shift_blue,MODE_SMMA,PRICE_MEDIAN,0);
   SMMA_red=iMA(NULL,0,period_red,shift_red,MODE_SMMA,PRICE_MEDIAN,0);
   SMMA_lime=iMA(NULL,0,period_lime,shift_lime,MODE_SMMA,PRICE_MEDIAN,0);
   
//--- MACD_1   сигнал - линия
   MacdCurrent1=iMACD(NULL,0,a1,d1,f1,PRICE_CLOSE,MODE_MAIN,0);
   MacdPrevious1=iMACD(NULL,0,a1,d1,f1,PRICE_CLOSE,MODE_MAIN,1);
   SignalCurrent1=iMACD(NULL,0,a1,d1,f1,PRICE_CLOSE,MODE_SIGNAL,0);
   SignalPrevious1=iMACD(NULL,0,a1,d1,f1,PRICE_CLOSE,MODE_SIGNAL,1);  
   
//--- MACD_2   
   MacdCurrent2=iMACD(NULL,0,a2,d2,f2,PRICE_CLOSE,MODE_MAIN,0);
   MacdPrevious2=iMACD(NULL,0,a2,d2,f2,PRICE_CLOSE,MODE_MAIN,1);
   SignalCurrent2=iMACD(NULL,0,a2,d2,f2,PRICE_CLOSE,MODE_SIGNAL,0);
   SignalPrevious2=iMACD(NULL,0,a2,d2,f2,PRICE_CLOSE,MODE_SIGNAL,1); 
   
//--- MACD_3   
   MacdCurrent3=iMACD(NULL,0,a3,d3,f3,PRICE_CLOSE,MODE_MAIN,0);
   MacdPrevious3=iMACD(NULL,0,a3,d3,f3,PRICE_CLOSE,MODE_MAIN,1);
   SignalCurrent3=iMACD(NULL,0,a3,d3,f3,PRICE_CLOSE,MODE_SIGNAL,0);
   SignalPrevious3=iMACD(NULL,0,a3,d3,f3,PRICE_CLOSE,MODE_SIGNAL,1);         


 //---------------------------=====================<<<<<< Close orders >>>>>>====================---------------------------\\
 
 int ototal=OrdersTotal();
   
   for(i2=ototal; i2>=0; i2--) 
   { 

   if   (OrderSelect(i2,SELECT_BY_POS,MODE_TRADES)==true)
          
          {
          
          if (FMA<MathMax(SMMA_blue, SMMA_lime) && OrderType()==OP_BUY && OrderProfit()>0)
               {
                 if (OrderTicket()==b1)// && c1==0)
                      {
                     OrderClose(OrderTicket(),OrderLots(),Bid,5,White);
                     OpenPrice_buy1=0;
                //     c1=1;
                     continue;
                      }
   /*              if (OrderTicket()==b2)// && c2==0)
                      {
                     OrderClose(OrderTicket(),OrderLots(),Bid,5,White);
                     OpenPrice_buy2=0;
              //       c2=1;
                     continue;
                      } 
                 if (OrderTicket()==b3)// && c3==0)
                      {
                     OrderClose(OrderTicket(),OrderLots(),Bid,5,White);
                     OpenPrice_buy3=0;
               //      c3=1;
                     continue;
                      }         
       */               
                }   

         if (FMA>MathMin(SMMA_blue, SMMA_lime) && OrderType()==OP_SELL && OrderProfit()>0)
               {
                
                 if (OrderTicket()==s1)// && e1==0)
                      {
                     OrderClose(OrderTicket(),OrderLots(),Bid,5,White);
                     OpenPrice_sell1=0;
                 //    e1=1;
                     continue;
                      }
 /*
                 if (OrderTicket()==s2)// && e2==0)
                      {
                     OrderClose(OrderTicket(),OrderLots(),Bid,5,White);
                     OpenPrice_sell2=0;
                //     e2=1; 
                     continue;
                      }        
                 if (OrderTicket()==s3)// && e3==0)
                      {
                     OrderClose(OrderTicket(),OrderLots(),Bid,5,White);
                     OpenPrice_sell3=0;
                 //    e3=1; 
                     continue;
                      }             
                  */                    
               } 
 
            }

    }
 
   
   
//-----------------------------====================<<<<<  Work history  >>>>>======================----------------------------\\  
        
// retrieving info from trade history
 int accTotal=OrdersHistoryTotal();
   int n=0;
   if ( accTotal>20){n =accTotal-20;}
 
  for(i=accTotal-1; i>=n; i--)
    {
     //---- check selection result
     if(OrderSelect(i,SELECT_BY_POS,MODE_HISTORY)==true)
       {
 
 //-----buy
  
          if (OrderCloseTime()>0 && OrderType()==OP_BUY && OrderTicket()==b1 && OrderProfit()>0) 
                  {
              OpenPrice_buy1=0;
                  }
                  
          if (OrderCloseTime()>0 && OrderType()==OP_BUY && OrderTicket()==b2 && OrderProfit()>0) 
                  {
              OpenPrice_buy2=0;
                  }
            
          if (OrderCloseTime()>0 && OrderType()==OP_BUY && OrderTicket()==b3 && OrderProfit()>0) 
                  {
              OpenPrice_buy3=0;
                  for(cnt=0; cnt<=OrdersTotal(); cnt++) 
                       { 
                          OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES);
                            
                            if (OrderTicket()==b1 && c1==0)
                                {
                               OrderClose(OrderTicket(),OrderLots(),Bid,5,White);
                               OpenPrice_buy1=0;
                               c1=1;
                               continue;
                                }
 
                            if (OrderTicket()==b2 && c2==0)
                                {
                               OrderClose(OrderTicket(),OrderLots(),Bid,5,White);
                               OpenPrice_buy2=0;
                               c2=1;
                                continue;
                                }    
                       }   
                  }
                  
        if (OrderCloseTime()>0 && OrderType()==OP_BUY && OrderTicket()==b3 && OrderProfit()<0) 
                  {
              OpenPrice_buy3=0;
             
                  for(cnt=0; cnt<=OrdersTotal(); cnt++) 
                       { 
                          OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES);
                            
                            if (OrderTicket()==b1 && c1==0)
                                {
                               OrderClose(OrderTicket(),OrderLots(),Bid,5,White);
                               OpenPrice_buy1=0;
                               c1=1;
                                OpenPrice_buy4=1;
                               continue;
                                }
 
                            if (OrderTicket()==b2 && c2==0)
                                {
                               OrderClose(OrderTicket(),OrderLots(),Bid,5,White);
                               OpenPrice_buy2=0;
                               c2=1;
                                continue;
                                }    
                       }   
                  }
                  
//-----sell                  
 
         if (OrderCloseTime()>0 && OrderType()==OP_SELL && OrderTicket()==s1 && OrderProfit()>0) 
                  {
             OpenPrice_sell1=0;
                  }
                  
         if (OrderCloseTime()>0 && OrderType()==OP_SELL && OrderTicket()==s2 && OrderProfit()>0) 
                  {
             OpenPrice_sell2=0;
                  }
                  
         if (OrderCloseTime()>0 && OrderType()==OP_SELL && OrderTicket()==s3 && OrderProfit()>0) 
                  {
             OpenPrice_sell3=0;
                 for(cnt=0; cnt<=OrdersTotal(); cnt++) 
                       { 
                          OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES);
                            
                            if (OrderTicket()==s1 && e1==0)
                                {
                               OrderClose(OrderTicket(),OrderLots(),Bid,5,White);
                               OpenPrice_sell1=0;
                               e1=1;
                                continue;
                                }
 
                            if (OrderTicket()==s2 && e2==0)
                                {
                               OrderClose(OrderTicket(),OrderLots(),Bid,5,White);
                               OpenPrice_sell2=0;
                               e2=1; 
                                continue;
                                }    
                        }        
                   }   
                   
    if (OrderCloseTime()>0 && OrderType()==OP_SELL && OrderTicket()==s3 && OrderProfit()<0) 
                  {
             OpenPrice_sell3=0;
             
                 for(cnt=0; cnt<=OrdersTotal(); cnt++) 
                       { 
                          OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES);
                            
                            if (OrderTicket()==s1 && e1==0)
                                {
                               OrderClose(OrderTicket(),OrderLots(),Bid,5,White);
                               OpenPrice_sell1=0;
                               e1=1;
                               OpenPrice_sell4=1;
                                continue;
                                }
 
                            if (OrderTicket()==s2 && e2==0)
                                {
                               OrderClose(OrderTicket(),OrderLots(),Bid,5,White);
                               OpenPrice_sell2=0;
                               e2=1; 
                                continue;
                                }    
                       }        
                 }   
        }
     // работа с ордером ...
    }


//-------------------------------====================<<<<<  Signals  >>>>>===================-------------------------------\\

//-----buy signals
//#1
bool buy_signal_1=false;
if (FMA>MathMax(SMMA_blue, SMMA_lime))
buy_signal_1=true;

//#2
bool buy_signal_2=false;
if (MacdCurrent1>SignalCurrent1)
buy_signal_2=true;

//#3
bool buy_signal_3=false;
if (MacdCurrent2>SignalCurrent2)
buy_signal_3=true;

//#4
bool buy_signal_4=false;
if (MacdCurrent3>SignalCurrent3)
buy_signal_4=true;



//-----sell signals
//#1
bool sell_signal_1=false;
if (FMA<MathMin(SMMA_blue, SMMA_lime))
sell_signal_1=true; 

//#2
bool sell_signal_2=false;
if (MacdCurrent1<SignalCurrent1)
sell_signal_2=true; 

//#3
bool sell_signal_3=false;
if (MacdCurrent2<SignalCurrent2)
sell_signal_3=true;     

//#4
bool sell_signal_4=false;
if (MacdCurrent3<SignalCurrent3)
sell_signal_4=true;    


//---------------------------=====================<<<<< Open Buy  >>>>>===================---------------------------------\\      
//#1
if(OpenPrice_buy1==0 && buy_signal_1==true)// && buy_signal_2==true && buy_signal_3==true && buy_signal_4==true  )
    {
                             
       ticket=OrderSend(Symbol(),OP_BUY,0.1,Bid,5,0,Bid+TakeProfit1*Point,"priceEX",16384,0,Green);

         if(ticket>0)
           {
           if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES)) 
           OpenPrice_buy1=OrderOpenPrice();
           b1=OrderTicket();
     //     Print("Open1=",OpenPrice_buy1); 
           }
      //   return(0); 
         
     }
        
//#2  
if((OpenPrice_buy1-Bid)>=Step*Point && OpenPrice_buy2==0 && OpenPrice_buy1!=0)
  {
  //  Print("Span",OpenPrice_buy1);                    
       ticket=OrderSend(Symbol(),OP_BUY,0.2,Bid,5,0,Bid+TakeProfit*Point,"priceEX",16384,0,Green);
   
                  
         if(ticket>0)
           {
           if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES)) 
           OpenPrice_buy2=OrderOpenPrice();
           b2=OrderTicket();
              
           }
     
    //     return(0); 
         
   }
        
//#3 
if((OpenPrice_buy2-Bid)>=Step*Point && OpenPrice_buy3==0 && OpenPrice_buy2!=0)
   {
//    Print("Span",OpenPrice_buy1);                    
       ticket=OrderSend(Symbol(),OP_BUY,0.3,Bid,5,Bid-Stop*Point,Bid+TakeProfit*Point,"priceEX",16384,0,Green);
   
                  
         if(ticket>0)
           {
           if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES)) 
           OpenPrice_buy3=OrderOpenPrice();
           b3=OrderTicket();
           c1=0;
           c2=0;
              
           }
     
    //     return(0); 
         
    }

     




//----------------------------====================<<<<< Open Sell  >>>>>=====================------------------------------\\
//#1
if (OpenPrice_sell1==0 && sell_signal_1==true)// && sell_signal_2==true && sell_signal_3==true && sell_signal_4==true) 
    {  
                                  
      ticket=OrderSend(Symbol(),OP_SELL,0.1,Bid,5,0,Bid-TakeProfit1*Point,"priceEX",16384,0,Red);
 
        
         if(ticket>0)
           {                                    
            if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES))
            
            OpenPrice_sell1=OrderOpenPrice();
            s1=OrderTicket();
           
 //    return(0); 
           }
    }
     
//#2    
if ((Bid-OpenPrice_sell1)>=Step*Point && OpenPrice_sell2==0 && OpenPrice_sell1!=0) 
    {  
                                  
      ticket=OrderSend(Symbol(),OP_SELL,0.2,Bid,5,0,Bid-TakeProfit*Point,"priceEX",16384,0,Red);
 
        
         if(ticket>0)
           {                                    
            if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES))
            OpenPrice_sell2=OrderOpenPrice();
            s2=OrderTicket();
           
  //      return(0); 
           }
    }
      
//#3    
if ((Bid-OpenPrice_sell2)>=Step*Point && OpenPrice_sell3==0 && OpenPrice_sell2!=0) 
    {  
                                  
      ticket=OrderSend(Symbol(),OP_SELL,0.3,Bid,5,Bid+Stop*Point,Bid-TakeProfit*Point,"priceEX",16384,0,Red);
 
        
         if(ticket>0)
           {                                    
            if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES))
            Print ("device");
            OpenPrice_sell3=OrderOpenPrice();
            s3=OrderTicket();
            e1=0;
            e2=0;
           
   //   return(0); 
           }
    }
     
 //  Print ("OOOOOOOOOOO3333",OpenPrice_sell3); 

        
     return(0);
    }



           
 //  return(0);
   
  

