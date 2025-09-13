//+---------------------------------------------------------+
//|                                   price_vs_alpha_v0.6wf |
//|                          Copyright © 2007, Daniil Gudz' |
//+---------------------------------------------------------+

#property copyright "Copyright © 2007, Daniil Gudz', fxmts@mail.ru"
#property link      "fxmts@mail.ru"


extern string m1="Выбор лота стопа и профита";
extern double StepProfit   = 40; 
extern double Step         = 22; 
extern double TakeProfit   = 40; 
extern double Stop         = 900; 
 
extern double Lot1         = 0.2; 
extern double Lot2         = 0.1; 
extern double Lot3         = 0.3; 
extern double Lot_1        = 0.1; 
extern double Lot_2        = 0.3; 

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
extern double a2=12;
extern double d2=26;
extern double f2=9;
extern string m9="MACD #3:";
extern double a3=12;
extern double d3=26;
extern double f3=9;

double OpenPrice_buy1, OpenPrice_sell1;
double OpenPrice_buy2, OpenPrice_sell2;
double OpenPrice_buy3, OpenPrice_sell3;
double OpenPrice_buy4, OpenPrice_sell4;

double OpenPrice_buy1_1, OpenPrice_sell1_1;
double OpenPrice_buy1_2, OpenPrice_sell1_2;
double OpenPrice_buy1_3, OpenPrice_sell1_3;
double OpenPrice_buy1_4, OpenPrice_sell1_4;

double OpenPrice_buy1_1_1, OpenPrice_sell1_1_1;
double OpenPrice_buy1_1_2, OpenPrice_sell1_1_2;
double OpenPrice_buy1_2_1, OpenPrice_sell1_2_1;
double OpenPrice_buy1_2_2, OpenPrice_sell1_2_2;
double OpenPrice_buy1_3_1, OpenPrice_sell1_3_1;
double OpenPrice_buy1_3_2, OpenPrice_sell1_3_2;
double OpenPrice_buy1_4_1, OpenPrice_sell1_4_1;
double OpenPrice_buy1_4_2, OpenPrice_sell1_4_2;



int cnt = 0;
int i = 0;
int i2 = 0;
int b1,b2,b3,b4;
int s1,s2,s3,s4;
int c0,c1,c2,c3,c4;
int e1,e2,e3,e4;

int c1_1,c1_1_1, c1_1_2 ;
int c1_2,c1_2_1, c1_2_2 ;

int b1_1, b1_2 ,b1_3 ,b1_4;
int s1_1, s1_2, s1_3, s1_4;

int b1_1_1, b1_1_2;
int b1_2_1, b1_2_2;
int b1_3_1, b1_3_2;
int b1_4_1, b1_4_2;

int s1_1_1, s1_1_2;
int s1_2_1, s1_2_2;
int s1_3_1, s1_3_2;
int s1_4_1, s1_4_2;

//+---------------------------------------------------------+
//|                                                   start |
//+---------------------------------------------------------+
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
 
//+---------------------------------------------------------+
//|                                            work history |
//+---------------------------------------------------------+
        

 int accTotal=OrdersHistoryTotal();
   int n=0;
   if ( accTotal>30){n =accTotal-30;}
 
  for(i=accTotal-1; i>=n; i--)
    {
     //---- check selection result
     if(OrderSelect(i,SELECT_BY_POS,MODE_HISTORY)==true)
       {
 
//+-----------+
//|    WH buy |
//+-----------+
         if (OrderCloseTime()>0 && OrderType()==OP_BUY && OrderTicket()==b1_4 && OrderProfit()>0) 
                  {
                 OpenPrice_buy1_4=0;
                 
                for(cnt=0; cnt<=OrdersTotal()-1; cnt++) 
                       { 
                        OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES);
                             if (OrderTicket()==b1 && c1==1)
                                {
                               OrderClose(OrderTicket(),OrderLots(),Bid,5,White);
                               OpenPrice_buy1=0;
                               c1=0;
                                }
                          }      
                          
                for(cnt=0; cnt<=OrdersTotal()-1; cnt++) 
                       { 
                        OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES);                          
                            if (OrderTicket()==b1_1 && c2==1)
                                {
                               OrderClose(OrderTicket(),OrderLots(),Bid,5,White);
                               OpenPrice_buy1_1=0;
                               c2=0;
                                } 
                        }    
                        
                for(cnt=0; cnt<=OrdersTotal()-1; cnt++) 
                       { 
                        OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES);         
                             if (OrderTicket()==b1_2 && c3==1 )
                                {
                               OrderClose(OrderTicket(),OrderLots(),Bid,5,White);
                               OpenPrice_buy1_2=0;
                               c3=0;
                                }
                       }
                       
                for(cnt=0; cnt<=OrdersTotal()-1; cnt++) 
                       { 
                        OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES);                       
                            if (OrderTicket()==b1_3 && c4==1 )
                                {
                               OrderClose(OrderTicket(),OrderLots(),Bid,5,White);
                               OpenPrice_buy1_3=0;
                               c4=0;
                                }    
                        }             
                  }

           if (OrderCloseTime()>0 && OrderType()==OP_BUY && OrderTicket()==b1_1_1 && OrderProfit()>0)  //это новая функция v0.5 
                  {                                                                                  //для обнуления после профита 
              OpenPrice_buy1_1_1=0;
                  }

           if (OrderCloseTime()>0 && OrderType()==OP_BUY && OrderTicket()==b1_2_1 && OrderProfit()>0)  //это новая функция v0.5 
                  {                                                                                  //для обнуления после профита 
              OpenPrice_buy1_2_1=0;
                  }
                  
          if (OrderCloseTime()>0 && OrderType()==OP_BUY && OrderTicket()==b1_3_1 && OrderProfit()>0)  //это новая функция v0.5 
                  {                                                                                  //для обнуления после профита 
              OpenPrice_buy1_3_1=0;
                  }       
                  

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
              
         
              
                for(cnt=0; cnt<=OrdersTotal()-1; cnt++) 
                       { 
                        OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES);  
                            if (OrderTicket()==b1 && c1==1)
                                {
                               OrderClose(OrderTicket(),OrderLots(),Bid,5,White);
                               OpenPrice_buy1=0;
                               c1=0;
                                }
                         }
                         
                for(cnt=0; cnt<=OrdersTotal()-1; cnt++) 
                       { 
                        OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES);  
                            if (OrderTicket()==b2 && c2==1)
                                {
                               OrderClose(OrderTicket(),OrderLots(),Bid,5,White);
                               OpenPrice_buy2=0;
                               c2=0;
                                }    
                       }   
                  }
                  
        if (OrderCloseTime()>0 && OrderType()==OP_BUY && OrderTicket()==b3 && OrderProfit()<0) 
                  {
              OpenPrice_buy3=0;
             
                for(cnt=0; cnt<=OrdersTotal()-1; cnt++) 
                       { 
                        OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES); 
                            if (OrderTicket()==b1 && c1==1)
                                {
                               OrderClose(OrderTicket(),OrderLots(),Bid,5,White);
                               OpenPrice_buy1=0;
                               c1=0;
                                }
                         }
                         
                for(cnt=0; cnt<=OrdersTotal()-1; cnt++) 
                       { 
                        OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES); 
                            if (OrderTicket()==b2 && c2==1)
                                {
                               OrderClose(OrderTicket(),OrderLots(),Bid,5,White);
                               OpenPrice_buy2=0;
                               c2=0;
                                }    
                       }   
                  }
                  
            if (OrderCloseTime()>0 && OrderType()==OP_BUY && OrderTicket()==b1_4_2 && OrderProfit()<0) 
                  {
              OpenPrice_buy1_4_2=0;
              
                for(cnt=0; cnt<=OrdersTotal()-1; cnt++) 
                       { 
                        OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES); 
                            if (OrderTicket()==b1_4_1 && c1==1)
                                {
                               OrderClose(OrderTicket(),OrderLots(),Bid,5,White);
                               OpenPrice_buy1_4_1=0;
                               c1=0;
                       }
                                }
                 for(cnt=0; cnt<=OrdersTotal()-1; cnt++) 
                       { 
                        OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES); 
                            if (OrderTicket()==b1_4 && c2==1)
                                {
                               OrderClose(OrderTicket(),OrderLots(),Bid,5,White);
                               OpenPrice_buy1_4=0;
                               c2=0;
                                }    
                       }
                       
                for(cnt=0; cnt<=OrdersTotal()-1; cnt++) 
                       { 
                        OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES);     
                           if (OrderTicket()==b1_3 && c3==1)
                                {
                               OrderClose(OrderTicket(),OrderLots(),Bid,5,White);
                               OpenPrice_buy1_3=0;
                               c3=0;
                                }                
                        }   
                  }        
         
              if (OrderCloseTime()>0 && OrderType()==OP_BUY && OrderTicket()==b1_3_2 && OrderProfit()<0) 
                  {
              OpenPrice_buy1_3_2=0;
             
                for(cnt=0; cnt<=OrdersTotal()-1; cnt++) 
                       { 
                        OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES); 
                            if (OrderTicket()==b1_3_1 && c1==1)
                                {
                               OrderClose(OrderTicket(),OrderLots(),Bid,5,White);
                               OpenPrice_buy1_3_1=0;
                               c1=0;
                                }
                        }
                        
                for(cnt=0; cnt<=OrdersTotal()-1; cnt++) 
                       { 
                        OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES);  
                            if (OrderTicket()==b1_3 && c2==1)
                                {
                               OrderClose(OrderTicket(),OrderLots(),Bid,5,White);
                               OpenPrice_buy1_3=0;
                               c2=0;
                                } 
                       }       
                        
                for(cnt=0; cnt<=OrdersTotal()-1; cnt++) 
                       { 
                        OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES);                              
                           if (OrderTicket()==b1_2 && c3==1)
                                {
                               OrderClose(OrderTicket(),OrderLots(),Bid,5,White);
                               OpenPrice_buy1_2=0;
                               c3=0;
                                continue;
                                }                 
                        }   
                  }            
                  
                 
                    if (OrderCloseTime()>0 && OrderType()==OP_BUY && OrderTicket()==b1_2_2 && OrderProfit()<0) 
                  {
              OpenPrice_buy1_2_2=0;
             
                 for(cnt=0; cnt<=OrdersTotal()-1; cnt++) 
                       { 
                        OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES); 
                            if (OrderTicket()==b1_2_1 && c1==1)
                                {
                               OrderClose(OrderTicket(),OrderLots(),Bid,5,White);
                             OpenPrice_buy1_2_1=0;
                               c1=0;
                                }
                       }
                       
                for(cnt=0; cnt<=OrdersTotal()-1; cnt++) 
                       { 
                        OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES); 
                            if (OrderTicket()==b1_2 && c2==1)
                                {
                               OrderClose(OrderTicket(),OrderLots(),Bid,5,White);
                               OpenPrice_buy1_2=0;
                               c2=0;
                                }    
                        }
                        
                for(cnt=0; cnt<=OrdersTotal()-1; cnt++) 
                       { 
                        OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES);  
                           if (OrderTicket()==b1_1 && c3==1)
                                {
                               OrderClose(OrderTicket(),OrderLots(),Bid,5,White);
                               OpenPrice_buy1_1=0;
                               c3=0;
                                }            
                        }   
                  }             
                
                if (OrderCloseTime()>0 && OrderType()==OP_BUY && OrderTicket()==b1_1_2 && OrderProfit()<0) 
                  {
              OpenPrice_buy1_1_2=0;
             
                for(cnt=0; cnt<=OrdersTotal()-1; cnt++) 
                       { 
                        OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES); 
                            if (OrderTicket()==b1_1_1 )//&& c1==1)
                                {
                               OrderClose(OrderTicket(),OrderLots(),Bid,5,White);
                               OpenPrice_buy1_1_1=0;
                               c1=0;
                                }
                       }
                       
                for(cnt=0; cnt<=OrdersTotal()-1; cnt++) 
                       { 
                        OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES); 
                            if (OrderTicket()==b1_1 && c2==1)
                                {
                               OrderClose(OrderTicket(),OrderLots(),Bid,5,White);
                               OpenPrice_buy1_1=0;
                               c2=0;
                                }    
                        }
                        
                for(cnt=0; cnt<=OrdersTotal()-1; cnt++) 
                       { 
                        OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES);        
                           if (OrderTicket()==b1 && c3==1)
                                {
                               OrderClose(OrderTicket(),OrderLots(),Bid,5,White);
                               OpenPrice_buy1=0;
                               c3=0;
                                }          
                        }   
                  }                  
                  
//+-----------+
//|   WH sell |
//+-----------+                
 
   if (OrderCloseTime()>0 && OrderType()==OP_SELL && OrderTicket()==s1_4 && OrderProfit()>0) 
                  {
                OpenPrice_sell1_4=0;                  
                  
                for(cnt=0; cnt<=OrdersTotal()-1; cnt++) 
                       { 
                        OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES); 
                            if (OrderTicket()==s1 && e1==1)
                                {
                               OrderClose(OrderTicket(),OrderLots(),Bid,5,White);
                               OpenPrice_sell1=0;
                               Print("Close_s1 in WH sell"); 
                               e1=0;
                                }
                       }
                       
                for(cnt=0; cnt<=OrdersTotal()-1; cnt++) 
                       { 
                        OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES); 
                            if (OrderTicket()==s1_1 && e2==1)
                                {
                               OrderClose(OrderTicket(),OrderLots(),Bid,5,White);
                               OpenPrice_sell1_1=0;
                               e2=0;
                                } 
                        }
                        
                for(cnt=0; cnt<=OrdersTotal()-1; cnt++) 
                       { 
                        OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES);        
                             if (OrderTicket()==s1_2 && e3==1 )
                                {
                               OrderClose(OrderTicket(),OrderLots(),Bid,5,White);
                               OpenPrice_sell1_2=0;
                               e3=0;
                                }
                        }
                        
                for(cnt=0; cnt<=OrdersTotal()-1; cnt++) 
                       { 
                        OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES); 
                            if (OrderTicket()==s1_3 && e4==1 )
                                {
                               OrderClose(OrderTicket(),OrderLots(),Bid,5,White);
                               OpenPrice_sell1_3=0;
                               e4=0;
                                }    
                        }
                }
 
        if (OrderCloseTime()>0 && OrderType()==OP_SELL && OrderTicket()==s1_1_1 && OrderProfit()>0)  //-это новая функция v0.5
                  {                                                                                 //для обнуления после профита 
              OpenPrice_sell1_1_1=0;
                  }
 
        if (OrderCloseTime()>0 && OrderType()==OP_SELL && OrderTicket()==s1_2_1 && OrderProfit()>0)  //-это новая функция v0.5
                  {                                                                                 //для обнуления после профита 
              OpenPrice_sell1_2_1=0;
                  }
        if (OrderCloseTime()>0 && OrderType()==OP_SELL && OrderTicket()==s1_2_2 && OrderProfit()>0)  //-это новая функция v0.5
                  {                                                                                 //для обнуления после профита 
              OpenPrice_sell1_2_2=0;
                  }          
                  
                  
        if (OrderCloseTime()>0 && OrderType()==OP_SELL && OrderTicket()==s1_3_1 && OrderProfit()>0)  //-это новая функция v0.5
                  {                                                                                 //для обнуления после профита 
              OpenPrice_sell1_3_1=0;
                  }
 
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
             
               for(cnt=0; cnt<=OrdersTotal()-1; cnt++) 
                       { 
                        OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES); 
                            if (OrderTicket()==s1 && e1==1)
                                {
                               OrderClose(OrderTicket(),OrderLots(),Bid,5,White);
                               OpenPrice_sell1=0;
                               e1=0;
                                }
                        }
                        
                for(cnt=0; cnt<=OrdersTotal()-1; cnt++) 
                       { 
                        OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES); 
                            if (OrderTicket()==s2 && e2==1)
                                {
                               OrderClose(OrderTicket(),OrderLots(),Bid,5,White);
                               OpenPrice_sell2=0;
                               e2=0; 
                                }    
                        }        
                   }   
           
    if (OrderCloseTime()>0 && OrderType()==OP_SELL && OrderTicket()==s3 && OrderProfit()<0) 
                  {
             OpenPrice_sell3=0;
             
                for(cnt=0; cnt<=OrdersTotal()-1; cnt++) 
                       { 
                        OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES); 
                            if (OrderTicket()==s1 && e1==1)
                                {
                               OrderClose(OrderTicket(),OrderLots(),Bid,5,White);
                               OpenPrice_sell1=0;
                               e1=0;
                                }
                        }
                        
                for(cnt=0; cnt<=OrdersTotal()-1; cnt++) 
                       { 
                        OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES); 
                            if (OrderTicket()==s2 && e2==1)
                                {
                               OrderClose(OrderTicket(),OrderLots(),Bid,5,White);
                               OpenPrice_sell2=0;
                               e2=0; 
                                }    
                       }        
                 }  
         
     if (OrderCloseTime()>0 && OrderType()==OP_SELL && OrderTicket()==s1_4_2 && OrderProfit()<0) 
                  {
              OpenPrice_sell1_4_2=0;
             
                for(cnt=0; cnt<=OrdersTotal()-1; cnt++) 
                       { 
                        OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES); 
                            if (OrderTicket()==s1_4_1 && e1==1)
                                {
                               OrderClose(OrderTicket(),OrderLots(),Bid,5,White);
                               OpenPrice_sell1_4_1=0;
                               e1=0;
                                }
                        }
                        
                for(cnt=0; cnt<=OrdersTotal()-1; cnt++) 
                       { 
                        OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES); 
                            if (OrderTicket()==s1_4 && e2==1)
                                {
                               OrderClose(OrderTicket(),OrderLots(),Bid,5,White);
                               OpenPrice_sell1_4=0;
                               e2=0;
                                }    
                       }
                       
                for(cnt=0; cnt<=OrdersTotal()-1; cnt++) 
                       { 
                        OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES); 
                           if (OrderTicket()==s1_3 && e3==1)
                                {
                               OrderClose(OrderTicket(),OrderLots(),Bid,5,White);
                               OpenPrice_sell1_3=0;
                               e3=0;
                                }           
                        }   
                  }        
                 
               if (OrderCloseTime()>0 && OrderType()==OP_SELL && OrderTicket()==s1_3_2 && OrderProfit()<0) 
                  {
              OpenPrice_sell1_3_2=0;
             
                for(cnt=0; cnt<=OrdersTotal()-1; cnt++) 
                       { 
                        OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES); 
                            if (OrderTicket()==s1_3_1 && e1==1)
                                {
                               OrderClose(OrderTicket(),OrderLots(),Bid,5,White);
                               OpenPrice_sell1_3_1=0;
                               e1=0;
                                }
                        }
                        
                for(cnt=0; cnt<=OrdersTotal()-1; cnt++) 
                       { 
                        OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES); 
                            if (OrderTicket()==s1_3 && e2==1)
                                {
                               OrderClose(OrderTicket(),OrderLots(),Bid,5,White);
                               OpenPrice_sell1_3=0;
                               e2=0;
                                }    
                       } 
                       
                for(cnt=0; cnt<=OrdersTotal()-1; cnt++) 
                       { 
                        OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES); 
                           if (OrderTicket()==s1_2 && e3==1)
                                {
                               OrderClose(OrderTicket(),OrderLots(),Bid,5,White);
                               OpenPrice_sell1_2=0;
                               e3=0;
                                }             
                       }   
                  }            
   
                    if (OrderCloseTime()>0 && OrderType()==OP_SELL && OrderTicket()==s1_2_2 && OrderProfit()<0) 
                  {
              OpenPrice_sell1_2_2=0;
             
                for(cnt=0; cnt<=OrdersTotal()-1; cnt++) 
                       { 
                        OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES); 
                            if (OrderTicket()==s1_2_1 && e1==1)
                                {
                               OrderClose(OrderTicket(),OrderLots(),Bid,5,White);
                               OpenPrice_sell1_2_1=0;
                               e1=0;
                                }
                        }
 
                 for(cnt=0; cnt<=OrdersTotal()-1; cnt++) 
                       { 
                        OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES); 
                            if (OrderTicket()==s1_2 && e2==1)
                                {
                               OrderClose(OrderTicket(),OrderLots(),Bid,5,White);
                               OpenPrice_sell1_2=0;
                               e2=0;
                                }  
                        }
                          
                for(cnt=0; cnt<=OrdersTotal()-1; cnt++) 
                       { 
                        OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES);     
                           if (OrderTicket()==s1_1 && e3==1)
                                {
                               OrderClose(OrderTicket(),OrderLots(),Bid,5,White);
                               OpenPrice_sell1_1=0;
                               e3=0;
                                }           
                       }   
                  }             
 
                if (OrderCloseTime()>0 && OrderType()==OP_SELL && OrderTicket()==s1_1_2 && OrderProfit()<0) 
                  {
              OpenPrice_sell1_1_2=0;
             
                for(cnt=0; cnt<=OrdersTotal()-1; cnt++) 
                       { 
                        OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES); 
                            if (OrderTicket()==s1_1_1 && e1==1)
                                {
                               OrderClose(OrderTicket(),OrderLots(),Bid,5,White);
                               OpenPrice_sell1_1_1=0;
                               e1=0;
                                }
                       }
                for(cnt=0; cnt<=OrdersTotal()-1; cnt++) 
                       { 
                        OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES); 
                            if (OrderTicket()==s1_1 && e2==1)
                                {
                               OrderClose(OrderTicket(),OrderLots(),Bid,5,White);
                               OpenPrice_sell1_1=0;
                               e2=0;
                                }   
                        }
                            
                for(cnt=0; cnt<=OrdersTotal()-1; cnt++) 
                       { 
                        OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES); 
                           if (OrderTicket()==s1 && e3==1)
                                {
                               OrderClose(OrderTicket(),OrderLots(),Bid,5,White);
                               OpenPrice_sell1=0;
                               e3=0;
  
                                }                     
                       }   
                }                      
        }
    }


//+---------------------------------------------------------+
//|                                                 signals |
//+---------------------------------------------------------+

//+--------------+
//|  buy signals |
//+--------------+

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

//+--------------+
//| sell signals |
//+--------------+

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
  
//+---------------------------------------------------------+
//|                                                open buy |
//+---------------------------------------------------------+
    
//#1 buy
if(

OpenPrice_buy1==0 && 
OpenPrice_buy2==0 && 
OpenPrice_buy3==0 && 
OpenPrice_buy1_1==0 &&
OpenPrice_buy1_1_1==0 &&
OpenPrice_buy1_2==0 && 
OpenPrice_buy1_3==0 && 
OpenPrice_buy1_4==0 &&

buy_signal_1==true )//&& buy_signal_2==true && buy_signal_3==true && buy_signal_4==true  )
    {
                             
       ticket=OrderSend(Symbol(),OP_BUY,Lot1,Bid,5,0,0,"b1",b1,0,Green);

         if(ticket>0)
           {
           if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES)) 
           OpenPrice_buy1_2_1=0;
           OpenPrice_buy1=OrderOpenPrice();
 //          OpenPrice_buy1_2_1=1;
           b1=OrderTicket();
           
          Print("Open_b1"); 
           } 
     }
        
   //#1_1 buy
      if(OpenPrice_buy1_4_1==0 && OpenPrice_buy1_1==0 && OpenPrice_buy1!=0 && (Bid-OpenPrice_buy1)>=Step*Point 
     && buy_signal_1==true) //&& buy_signal_2==true && buy_signal_3==true && buy_signal_4==true)
       {
                             
         ticket=OrderSend(Symbol(),OP_BUY,Lot1,Bid,5,0,0,"b1_1",b1_1,0,Green);

           if(ticket>0)
             {
             if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES)) 
             OpenPrice_buy1_1=OrderOpenPrice();
             b1_1=OrderTicket();
               Print("Open_b1_1");
             }
       }
     
        //#1_1_1 buy
             if(OpenPrice_buy1_1_1==0 && OpenPrice_buy1_1!=0 && OpenPrice_sell1_2_1==0 && (OpenPrice_buy1_1-Bid)>=Step*Point)

         //       && buy_signal_1==true && buy_signal_2==true && buy_signal_3==true && buy_signal_4==true)
                  {
                             
                      ticket=OrderSend(Symbol(),OP_BUY,Lot_1,Bid,5,0,Bid+StepProfit*Point,"b1_1_1",b1_1_1,0,Green);

                           if(ticket>0)
                            {
                           if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES)) 
                            OpenPrice_buy1_1_1=OrderOpenPrice();
                            b1_1_1=OrderTicket();
                               Print("Open1_b1_1_1"); 
                            }
                   }
                   
        //#1_1_2 buy
             if(OpenPrice_buy1_1_2==0 && OpenPrice_buy1_1_1!=0 && (OpenPrice_buy1_1_1-Bid)>=Step*Point)

         //       && buy_signal_1==true && buy_signal_2==true && buy_signal_3==true && buy_signal_4==true)
                  {
                             
                      ticket=OrderSend(Symbol(),OP_BUY,Lot_2,Bid,5,Bid-Stop*Point,Bid+StepProfit*Point,"b1_1_2",b1_1_2,0,Green);

                           if(ticket>0)
                            {
                           if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES)) 
                            OpenPrice_buy1_1_2=OrderOpenPrice();
                            c1=1;
                            c2=1;
                            c3=1;
                            b1_1_2=OrderTicket();
                          Print("Open_b1_1_2");
                            }
                   }                   
    
//#1_2 buy
if(OpenPrice_buy1_3==0 && OpenPrice_buy1_4==0 && OpenPrice_buy1_2==0 && OpenPrice_buy1_1!=0 && (Bid-OpenPrice_buy1_1)>=Step*Point 
&& buy_signal_1==true )//&& buy_signal_2==true && buy_signal_3==true && buy_signal_4==true)
    {
                             
       ticket=OrderSend(Symbol(),OP_BUY,Lot1,Bid,5,0,0,"b1_2",b1_2,0,Green);

         if(ticket>0)
           {
           if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES)) 
           OpenPrice_buy1_2=OrderOpenPrice();
           b1_2=OrderTicket();
          Print("Open_b1_2");
           }
     }    
     
        //#1_2_1 buy
             if(OpenPrice_buy1_2_1==0 && OpenPrice_buy1_2!=0 && (OpenPrice_buy1_2-Bid)>=Step*Point)

         //       && buy_signal_1==true && buy_signal_2==true && buy_signal_3==true && buy_signal_4==true)
                  {
                          
                      ticket=OrderSend(Symbol(),OP_BUY,Lot_1,Bid,5,0,Bid+StepProfit*Point,"b1_2_1",16384,0,Green);

                           if(ticket>0)
                            {
                           if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES)) 
                            OpenPrice_buy1_2_1=OrderOpenPrice();
                            b1_2_1=OrderTicket();
                            
                            Print("Open1_b1_2_1dfdfdfdfdfdfdfdfd"); 
                            }
                    }     
     
         //#1_2_2 buy
             if(OpenPrice_buy1_2_2==0 && OpenPrice_buy1_2_1!=0 && (OpenPrice_buy1_2_1-Bid)>=Step*Point)

         //       && buy_signal_1==true && buy_signal_2==true && buy_signal_3==true && buy_signal_4==true)
                  {
                             
                      ticket=OrderSend(Symbol(),OP_BUY,Lot_2,Bid,5,Bid-Stop*Point,Bid+StepProfit*Point,"b1_2_2",16384,0,Green);

                           if(ticket>0)
                            {
                           if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES)) 
                            OpenPrice_buy1_2_2=OrderOpenPrice();
                            b1_2_2=OrderTicket();
                            c1=1;
                            c2=1;
                            c3=1;
                             Print("Open1_b1_2_2");
                            }
                   }          
 
//#1_3 buy
if(
OpenPrice_buy1_4==0 &&  // ???????????????????????????????????????? del if do not work right
OpenPrice_buy1_3_1==0 &&  // ???????????????????????????????????????? del if do not work right
OpenPrice_buy1_3==0 && OpenPrice_buy1_2!=0 && (Bid-OpenPrice_buy1_2)>=Step*Point 
&& buy_signal_1==true && buy_signal_2==true && buy_signal_3==true && buy_signal_4==true)
    {
                             
       ticket=OrderSend(Symbol(),OP_BUY,Lot1,Bid,5,0,0,"b1_3",16384,0,Green);

         if(ticket>0)
           {
           if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES)) 
           OpenPrice_buy1_3=OrderOpenPrice();
           b1_3=OrderTicket();
            Print("Open1_b1_3"); 
           }
     }     

     //#1_3_1 buy
             if(OpenPrice_buy1_3_1==0 && OpenPrice_buy1_3!=0 && (OpenPrice_buy1_3-Bid)>=Step*Point && OpenPrice_buy1_4_1==0)

         //       && buy_signal_1==true && buy_signal_2==true && buy_signal_3==true && buy_signal_4==true)
                  {
                             
                      ticket=OrderSend(Symbol(),OP_BUY,Lot_1,Bid,5,0,Bid+StepProfit*Point,"b1_3_1",16384,0,Green);

                           if(ticket>0)
                            {
                           if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES)) 
                            OpenPrice_buy1_3_1=OrderOpenPrice();
                            b1_3_1=OrderTicket();
                            Print("Open1_b1_3_1"); 
                            }
                     }     
     
        //#1_3_2 buy
             if(OpenPrice_buy1_3_2==0 && OpenPrice_buy1_3_1!=0 && (OpenPrice_buy1_3_1-Bid)>=Step*Point && OpenPrice_buy1==0)

         //       && buy_signal_1==true && buy_signal_2==true && buy_signal_3==true && buy_signal_4==true)
                  {
                             
                      ticket=OrderSend(Symbol(),OP_BUY,Lot_2,Bid,5,Bid-Stop*Point,Bid+StepProfit*Point,"b1_3_2",16384,0,Green);

                           if(ticket>0)
                            {
                           if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES)) 
                            OpenPrice_buy1_3_2=OrderOpenPrice();
                            b1_3_2=OrderTicket();
                             c1=1;
                            c2=1;
                            c3=1;
                            Print("Open1_b1_3_2"); 
                            }
                   }                     
     
//#1_4 buy
if(OpenPrice_buy1_4==0 && OpenPrice_buy1_3!=0 && (Bid-OpenPrice_buy1_3)>=Step*Point 
&& buy_signal_1==true && buy_signal_2==true && buy_signal_3==true && buy_signal_4==true)
    {
                             
       ticket=OrderSend(Symbol(),OP_BUY,Lot1,Bid,5,0,Bid+TakeProfit*Point,"b1_4",16384,0,Green);

         if(ticket>0)
           {
           if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES)) 
           OpenPrice_buy1_4=OrderOpenPrice();
           b1_4=OrderTicket();
                            c1=1;
                            c2=1;
                            c3=1;
                            c4=1;           

           Print("Open1_b1_4"); 
           }
     }                

     //#1_4_1 buy
             if(OpenPrice_buy1_4_1==0 && OpenPrice_buy1_4!=0 && Bid<=OpenPrice_buy1_3)

         //       && buy_signal_1==true && buy_signal_2==true && buy_signal_3==true && buy_signal_4==true)
                  {
                             
                      ticket=OrderSend(Symbol(),OP_BUY,Lot_1,Bid,5,0,Bid+StepProfit*Point,"b1_4_1",16384,0,Green);

                           if(ticket>0)
                            {
                           if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES)) 
                            OpenPrice_buy1_4_1=OrderOpenPrice();
                            b1_4_1=OrderTicket();
                            Print("Open1_b1_4_1"); 
                            }
                           //   return(0); 
                   }     
     
        //#1_4_2 buy
             if(OpenPrice_buy1_4_2==0 && OpenPrice_buy1_4_1!=0 && (OpenPrice_buy1_4_1-Bid)>=Step*Point)

         //       && buy_signal_1==true && buy_signal_2==true && buy_signal_3==true && buy_signal_4==true)
                  {
                             
                      ticket=OrderSend(Symbol(),OP_BUY,Lot_2,Bid,5,Bid-Stop*Point,Bid+StepProfit*Point,"b1_4_2",16384,0,Green);

                           if(ticket>0)
                            {
                           if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES)) 
                            OpenPrice_buy1_4_2=OrderOpenPrice();
                            b1_4_2=OrderTicket();
                            c1=1;
                            c2=1;
                            c3=1;
                            c4=1;
      
                            Print("Open1_b1_4_2"); 
                            }
                   }                     
//#2 buy 
if(
/*
OpenPrice_buy1_2==0 && 
OpenPrice_buy1_3==0 && 
OpenPrice_buy1_4==0 &&
OpenPrice_buy1_2_1==0 && 
OpenPrice_buy1_1_2==0 && 
*/
OpenPrice_buy1_1_1==0 &&//для того чтобы не открывался бай 2 когда открыт 1_1_1 нужно!!!
(OpenPrice_buy1-Bid)>=Step*Point && OpenPrice_buy2==0 && OpenPrice_buy1!=0)// && OpenPrice_buy1_1_1==0)
  {
  //  Print("Span",OpenPrice_buy1);                    
       ticket=OrderSend(Symbol(),OP_BUY,Lot2,Bid,5,0,Bid+StepProfit*Point,"b2",16384,0,Green);
   
                  
         if(ticket>0)
           {
           if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES)) 
           OpenPrice_buy2=OrderOpenPrice();
           b2=OrderTicket();
           Print("Open1_b2");    
           }
   }
        
//#3 buy
if((OpenPrice_buy2-Bid)>=Step*Point && OpenPrice_buy3==0 && OpenPrice_buy2!=0)
   {
//    Print("Span",OpenPrice_buy1);                    
       ticket=OrderSend(Symbol(),OP_BUY,Lot3,Bid,5,Bid-Stop*Point,Bid+StepProfit*Point,"b3",16384,0,Green);
   
                  
         if(ticket>0)
           {
           if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES)) 
           OpenPrice_buy3=OrderOpenPrice();
           b3=OrderTicket();
           c1=1;
           c2=1;
           Print("Open1_b3");    
           } 
    }

//+---------------------------------------------------------+
//|                                               open sell |
//+---------------------------------------------------------+

//#1 sell
if(

OpenPrice_sell1==0 && 
OpenPrice_sell2==0 && 
OpenPrice_sell3==0 && 
OpenPrice_sell1_1==0 &&
OpenPrice_sell1_1_1==0 &&
OpenPrice_sell1_2==0 && 
OpenPrice_sell1_3==0 && 
OpenPrice_sell1_4==0 &&

sell_signal_1==true )//&& sell_signal_2==true && sell_signal_3==true && sell_signal_4==true  )
    {
                             
       ticket=OrderSend(Symbol(),OP_SELL,Lot1,Bid,5,0,0,"s1",16384,0,Green);

         if(ticket>0)
           {
           if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES)) 
           OpenPrice_sell1_2_1=0;
           OpenPrice_sell1=OrderOpenPrice();
           s1=OrderTicket();

          Print("Open_s1"); 
           }  
     }
        
  
   //#1_1 sell
      if(OpenPrice_sell1_1==0 && OpenPrice_sell1_4_1==0 && OpenPrice_sell1!=0 && (OpenPrice_sell1-Bid)>=Step*Point 
      && sell_signal_1==true )//&& sell_signal_2==true && sell_signal_3==true && sell_signal_4==true)
       {
                             
         ticket=OrderSend(Symbol(),OP_SELL,Lot1,Bid,5,0,0,"s1_1",16384,0,Green);

           if(ticket>0)
             {
             if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES)) 
             OpenPrice_sell1_1=OrderOpenPrice();
             s1_1=OrderTicket();
               Print("Open_s1_1");
             } 
       }
     
        //#1_1_1 sell
          
             if(OpenPrice_sell1_1_1==0 && OpenPrice_sell1_2_1==0 && OpenPrice_sell1_1!=0 && (Bid-OpenPrice_sell1_1)>=Step*Point)

         //       && sell_signal_1==true && sell_signal_2==true && sell_signal_3==true && sell_signal_4==true)
                  {
                             
                      ticket=OrderSend(Symbol(),OP_SELL,Lot_1,Bid,5,0,Bid-StepProfit*Point,"s1_1_1",16384,0,Green);

                           if(ticket>0)
                            {
                           if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES)) 
                            OpenPrice_sell1_1_1=OrderOpenPrice();
                            s1_1_1=OrderTicket();
                               Print("Open_s1_1_1"); 
                            }
                   }
                   
        //#1_1_2 sell
             if(OpenPrice_sell1_1_2==0 && OpenPrice_sell1_1_1!=0 && (Bid-OpenPrice_sell1_1_1)>=Step*Point)

         //       && sell_signal_1==true && sell_signal_2==true && sell_signal_3==true && sell_signal_4==true)
                  {
                             
                      ticket=OrderSend(Symbol(),OP_SELL,Lot_2,Bid,5,Bid+Stop*Point,Bid-StepProfit*Point,"s1_1_2",16384,0,Green);

                           if(ticket>0)
                            {
                           if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES)) 
                            OpenPrice_sell1_1_2=OrderOpenPrice();
                            e1=1;
                             e2=1;
                             e3=1;
                            s1_1_2=OrderTicket();
                          Print("Open_s1_1_2");
                            }
                   }                   
    
//#1_2 sell
if(OpenPrice_sell1_2==0 && OpenPrice_sell1_1!=0 && (OpenPrice_sell1_1-Bid)>=Step*Point 
&& sell_signal_1==true && sell_signal_2==true && sell_signal_3==true && sell_signal_4==true)
    {
                             
       ticket=OrderSend(Symbol(),OP_SELL,Lot1,Bid,5,0,0,"s1_2",16384,0,Green);

         if(ticket>0)
           {
           if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES)) 
           OpenPrice_sell1_2=OrderOpenPrice();
           s1_2=OrderTicket();
           Print("Open_s1_2");
           }
     }    
     
        //#1_2_1 sell
             if(OpenPrice_sell1_2_1==0 && OpenPrice_sell1_2!=0 && (Bid-OpenPrice_sell1_2)>=Step*Point)

         //       && sell_signal_1==true && sell_signal_2==true && sell_signal_3==true && sell_signal_4==true)
                  {
                             
                      ticket=OrderSend(Symbol(),OP_SELL,Lot_1,Bid,5,0,Bid-StepProfit*Point,"s1_2_1",16384,0,Green);

                           if(ticket>0)
                            {
                            if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES)) 
                            OpenPrice_sell1_2_1=OrderOpenPrice();
                            s1_2_1=OrderTicket();
                            Print("Open_s1_2_1"); 
                            }
                           //   return(0); 
         
                   }     
     
         //#1_2_2 sell
             if(OpenPrice_sell1_2_2==0 && OpenPrice_sell1_2_1!=0 && (Bid-OpenPrice_sell1_2_1)>=Step*Point)

         //       && sell_signal_1==true && sell_signal_2==true && sell_signal_3==true && sell_signal_4==true)
                  {
                             
                      ticket=OrderSend(Symbol(),OP_SELL,Lot_2,Bid,5,Bid+Stop*Point,Bid-StepProfit*Point,"b1_2_2",16384,0,Green);

                           if(ticket>0)
                            {
                           if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES)) 
                            OpenPrice_sell1_2_2=OrderOpenPrice();
                            e1=1;
                             e2=1;
                             e3=1;
                            s1_2_2=OrderTicket();
                             Print("Open_s1_2_2");
                            }
                   }          
     
     
                
//#1_3 sell
if(
OpenPrice_sell1_4==0 &&
OpenPrice_sell1_3_1==0 && 
OpenPrice_sell1_3==0 && OpenPrice_sell1_2!=0 && (OpenPrice_sell1_2-Bid)>=Step*Point 
&& sell_signal_1==true && sell_signal_2==true && sell_signal_3==true && sell_signal_4==true)
    {
                             
       ticket=OrderSend(Symbol(),OP_SELL,Lot1,Bid,5,0,0,"s1_3",16384,0,Green);

         if(ticket>0)
           {
           if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES)) 
           OpenPrice_sell1_3=OrderOpenPrice();
           s1_3=OrderTicket();
            Print("Open_s1_3"); 
           }
      }     
     
     //#1_3_1 sell
             if(OpenPrice_sell1_3_1==0 && OpenPrice_sell1_3!=0 && (Bid-OpenPrice_sell1_3)>=Step*Point && OpenPrice_sell1_4_1==0)

         //       && sell_signal_1==true && sell_signal_2==true && sell_signal_3==true && sell_signal_4==true)
                  {
                             
                      ticket=OrderSend(Symbol(),OP_SELL,Lot_1,Bid,5,0,Bid-StepProfit*Point,"s1_3_1",16384,0,Green);

                           if(ticket>0)
                            {
                           if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES)) 
                            OpenPrice_sell1_3_1=OrderOpenPrice();
                            s1_3_1=OrderTicket();
                            Print("Open_s1_3_1"); 
                            }
                  }     
     
        //#1_3_2 sell
             if(OpenPrice_sell1_3_2==0 && OpenPrice_sell1_3_1!=0 && (Bid-OpenPrice_sell1_3_1)>=Step*Point && OpenPrice_sell1==0)

         //       && sell_signal_1==true && sell_signal_2==true && sell_signal_3==true && sell_signal_4==true)
                  {
                             
                      ticket=OrderSend(Symbol(),OP_SELL,Lot_2,Bid,5,Bid+Stop*Point,Bid-StepProfit*Point,"s1_3_2",16384,0,Green);

                           if(ticket>0)
                            {
                           if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES)) 
                            OpenPrice_sell1_3_2=OrderOpenPrice();
                            e1=1;
                             e2=1;
                             e3=1;
                            s1_3_2=OrderTicket();
                            Print("Open_s1_3_2"); 
                            }
                     }                     
     
//#1_4 sell
if(OpenPrice_sell1_4==0 && OpenPrice_sell1_3!=0 && (OpenPrice_sell1_3-Bid)>=Step*Point 
&& sell_signal_1==true && sell_signal_2==true && sell_signal_3==true && sell_signal_4==true)
    {
                             
       ticket=OrderSend(Symbol(),OP_SELL,Lot1,Bid,5,0,Bid-TakeProfit*Point,"s1_4",16384,0,Green);

         if(ticket>0)
           {
           if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES)) 
           OpenPrice_sell1_4=OrderOpenPrice();
           s1_4=OrderTicket();
            e1=1;
           e2=1;
            e3=1;
           e4=1;
           
           Print("Open_s1_4"); 
           }
      }                

     
//#1_4_1 sell усредняемся на убыток первым ордером селл
// условия: 1.открыт ордер селл1_4 2.первый селл не открыт 3. цена >= цена открытия селл 1_3      
     
             if(OpenPrice_sell1_4_1==0 && OpenPrice_sell1_4!=0 && (Bid-OpenPrice_sell1_4)>=Step*Point)

         //       && sell_signal_1==true && sell_signal_2==true && sell_signal_3==true && sell_signal_4==true)
                  {
                             
                      ticket=OrderSend(Symbol(),OP_SELL,Lot_1,Bid,5,0,Bid-StepProfit*Point,"s1_4_1",16384,0,Green);

                           if(ticket>0)
                            {
                           if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES)) 
                            OpenPrice_sell1_4_1=OrderOpenPrice();
                            s1_4_1=OrderTicket();
                            Print("Open_s1_4_1"); 
                            }
                      }     
     
//#1_4_2 sell усредняемся на убыток вторым ордером селл
// условия: 1.открыт первый ордер селл 2.второй селл не открыт 3. Шаг > цена - цена открытия первого 4. селл 1_4_1 не открыт
        
             if(OpenPrice_sell1_4_2==0 && OpenPrice_sell1_4_1!=0 && (Bid-OpenPrice_sell1_4_1)>=Step*Point)
             // && sell_signal_1==true && sell_signal_2==true && sell_signal_3==true && sell_signal_4==true)
                  {
                      ticket=OrderSend(Symbol(),OP_SELL, Lot_2 ,Bid,5,Bid+Stop*Point,Bid-StepProfit*Point,"s1_4_2",s1_4_2,0,Red);

                           if(ticket>0)
                            {
                           if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES)) 
                            OpenPrice_sell1_4_2=OrderOpenPrice();
                            s1_4_2=OrderTicket();
                             e1=1;
                             e2=1;
                             e3=1;
                            
                            Print("Open_s1_4_2"); 
                            }
                   }                     

        
//#2 sell усредняемся на убыток вторым ордером селл
// условия: 1.открыт первый ордер селл 2.второй селл не открыт 3. Шаг > цена - цена открытия первого 4. селл 1_1_1 не открыт

if(
/*
OpenPrice_sell1_1==0 &&
OpenPrice_sell1_2==0 && 
OpenPrice_sell1_3==0 && 
OpenPrice_sell1_4==0 &&
OpenPrice_sell1_1_1==0 && 
OpenPrice_sell1_1_2==0 && 
*/
OpenPrice_sell1_1_1==0 && //для того чтобы не открывался селл 2 когда открыт 1_1_1 нужно!!!
((Bid-OpenPrice_sell1)>=Step*Point && OpenPrice_sell2==0 && OpenPrice_sell1!=0 ))//&& OpenPrice_sell1_1_1==0)
   {        
       ticket=OrderSend(Symbol(),OP_SELL, Lot2 ,Bid,5,0,Bid-StepProfit*Point,"s2",s2,0,Red);
                   
         if(ticket>0)
           {
           if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES)) 
           OpenPrice_sell2=OrderOpenPrice();
           s2=OrderTicket();
           Print("Open_s2");
               
           }
   }
        
//#3 sell усредняемся на убыток третим ордером селл
// условия: 1.открыт второй ордер селл 2.третий селл не открыт 3. Шаг > цена - цена открытия второго  

if((Bid-OpenPrice_sell2)>=Step*Point && OpenPrice_sell3==0 && OpenPrice_sell2!=0)
   {
      ticket=OrderSend(Symbol(),OP_SELL, Lot3 ,Bid,5,Bid+Stop*Point,Bid-StepProfit*Point,"s3",s3,0,Red);
 
         if(ticket>0)
           {
           if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES)) 
           OpenPrice_sell3=OrderOpenPrice();
           s3=OrderTicket();
           Print("Open_s3");
           e1=1;
           e2=1;
               
           }        
    }
    
//+---------------------------------------------------------+
//|                                             close order |
//+---------------------------------------------------------+
 int ototal=OrdersTotal();
   
   for(i2=ototal; i2>=0; i2--) 
   { 

   if   (OrderSelect(i2,SELECT_BY_POS,MODE_TRADES)==true)
          
          {
   
//+------------+
//|  close buy |
//+------------+
//1_2 => 1_2_1 
      // эта функция дублирует тэйк профит b1_2_1 поэтому ее можно исключить впринципе
           if (OrderType()==OP_BUY && OrderTicket()==b1_2_1 && StepProfit*Point<=(Bid-OpenPrice_buy1_2_1) && OrderProfit()>0)// && c1_2_1==0)
                         { 
                         OrderClose(OrderTicket(),OrderLots(),Bid,5,White);
                         OpenPrice_buy1_2_1=0;
                       //  c1_2_1=1;
                         Print("close: b1_2_1" );
                        
                          }   
      
          if (OrderType()==OP_BUY && OrderTicket()==b1 && OrderProfit()>0 && OpenPrice_buy1_2_1!=0)
      
                         {
                         OrderClose(OrderTicket(),OrderLots(),Bid,5,White);
                   //     OpenPrice_buy1=0;
                         Print("close: b1" );
    
                         }
                         
  
//1_3 => 1_3_1 
      
           if (OrderType()==OP_BUY && OrderTicket()==b1_3_1 && Bid>=OpenPrice_buy1_3 && OrderProfit()>0)// && c1_2_1==0)
                   
                         {
                         OrderClose(OrderTicket(),OrderLots(),Bid,5,White);
                         OpenPrice_buy1_3_1=0;
                 //        c1_2_1=1;
                         Print("close: b1_3_1" );
                           }                             
                    
          if (OrderType()==OP_BUY && OrderTicket()==b1 && OrderProfit()>0 && OpenPrice_buy1_3_1!=0) //&& c1==0)
      
                         {
                         OrderClose(OrderTicket(),OrderLots(),Bid,5,White);
                  //       OpenPrice_buy1=0;
                         Print("close: b1" );
                       //  c1=1;
                         
                         }                         
 
          if (OrderType()==OP_BUY && OrderTicket()==b1_1  && OpenPrice_buy1_3_1!=0) //&& c1==0)
      
                         {
                         OrderClose(OrderTicket(),OrderLots(),Bid,5,White);
                         OpenPrice_buy1_1=0;
                         Print("close: b1_1" );
                       //  c1=1;
                         
                         }         
         if (OrderType()==OP_BUY && OrderTicket()==b1_2  && OpenPrice_buy1_3_1!=0) //&& c1==0)
      
                         {
                         OrderClose(OrderTicket(),OrderLots(),Bid,5,White);
                         OpenPrice_buy1_2=0;
                         Print("close: b1_2" );
                       //  c1=1;
                         
                         }                        
                         
                                           

//1_4 => 1_4_1 
      
           if (OrderType()==OP_BUY && OrderTicket()==b1_4_1 && Bid>=OpenPrice_buy1_4 && OrderProfit()>0)// && c1_2_1==0)
                   
                         {
                         OrderClose(OrderTicket(),OrderLots(),Bid,5,White);
                         OpenPrice_buy1_4_1=0;
                 //        c1_2_1=1;
                         Print("close: b1_4_1" );
                           }                             
                    
     /*     if (OrderType()==OP_BUY && OrderTicket()==b1  && OpenPrice_buy1_4_1!=0 && OrderProfit()>0) //&& c1==0)
      
                         {
                         OrderClose(OrderTicket(),OrderLots(),Bid,5,White);
                  //       OpenPrice_buy1=0;
                         Print("close: b1" );
                       //  c1=1;
                         
                         }       */                  
 
          if (OrderType()==OP_BUY && OrderTicket()==b1_1  && OpenPrice_buy1_4_1!=0) //&& c1==0)
      
                         {
                         OrderClose(OrderTicket(),OrderLots(),Bid,5,White);
                         OpenPrice_buy1_1=0;
                         Print("close: b1_1" );
                       //  c1=1;
                         
                         }   

          if (OrderType()==OP_BUY && OrderTicket()==b1_2  && OpenPrice_buy1_4_1!=0) //&& c1==0)
      
                         {
                         OrderClose(OrderTicket(),OrderLots(),Bid,5,White);
                         OpenPrice_buy1_2=0;
                         Print("close: b1_2" );
                       //  c1=1;
                    //     return(0);
                         }   
               
                                        
                         
//1_1_1            
          if (OrderType()==OP_BUY && OpenPrice_buy1_1_1<=Bid && OpenPrice_buy1_1_1!=0 && OpenPrice_buy1_1_2!=0) //&& OrderProfit()>=TakeProfit*3)
               {
              ototal=OrdersTotal();
                for(cnt=ototal; cnt>=0; cnt--)                         //!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                       {                                               //!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                          OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES);  //!!!!!!!!!!!!!!!!!!!!!!!!!!!!
              
                        if (OrderTicket()==b1_1_1  )
                         {
                         OrderClose(OrderTicket(),OrderLots(),Bid,5,White);
                         OpenPrice_buy1_1_1=0;
                         
                         Print("close: b1_1_1" );
                     //    continue;
                          }
                            
                        if (OrderTicket()==b1_1)//&&OpenPrice_buy1_1!=0 )
                         {
                         OrderClose(OrderTicket(),OrderLots(),Bid,5,White);
                         OpenPrice_buy1_1=0;
                        
                         Print("close: b1_1");
                    //     continue;
                          }
                          
                        if (OrderTicket()==b1 && OrderProfit()>0)
                         {
                         OrderClose(OrderTicket(),OrderLots(),Bid,5,White);
                      //   OpenPrice_buy1=0;
                         Print("close: b1" );
                        
                    //     continue;
                          } 
                          
                        if (OrderTicket()==b1_1_2 )
                         {
                         OrderClose(OrderTicket(),OrderLots(),Bid,5,White);
                         OpenPrice_buy1_1_2=0;
                         
                         Print("close: b1_1_2" );
                         
                  //       continue;
                         }
                       }
                      
               }  
        
             
//1_2_1                      
          if (OrderType()==OP_BUY && OpenPrice_buy1_2_1<=Bid && OpenPrice_buy1_2_1!=0 && OpenPrice_buy1_2_2!=0) //&& OrderProfit()>=TakeProfit*3)
               {
             ototal=OrdersTotal();  
                for(cnt=ototal; cnt>=0; cnt--)                         //!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                       {                                               //!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                          OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES);  //!!!!!!!!!!!!!!!!!!!!!!!!!!!!
              
                        if (OrderTicket()==b1_2_1 )
                         {
                         OrderClose(OrderTicket(),OrderLots(),Bid,5,White);
                         OpenPrice_buy1_2_1=0;
                        
                         Print("close: b1_2_1" );
                         continue;
                          }
                            
                        if (OrderTicket()==b1_2 )//&&OpenPrice_buy1_1!=0 )
                         {
                         OrderClose(OrderTicket(),OrderLots(),Bid,5,White);
                         OpenPrice_buy1_2=0;
                        
                         Print("close: b1_2");
                         continue;
                          }
                          
                        if (OrderTicket()==b1_1  )
                         {
                         OrderClose(OrderTicket(),OrderLots(),Bid,5,White);
                         OpenPrice_buy1_1=0;
                         Print("close: b1" );
                         
                         continue;
                          } 
                          
                        if (OrderTicket()==b1_2_2 )
                         {
                         OrderClose(OrderTicket(),OrderLots(),Bid,5,White);
                         OpenPrice_buy1_2_2=0;
                         
                         Print("close: b1_2_2" );
           
                  //       continue;
                         }                     
                  } 
                 
              }
       
//1_3_1                      
          if (OrderType()==OP_BUY && OpenPrice_buy1_3_1<=Bid && OpenPrice_buy1_3_1!=0 && OpenPrice_buy1_3_2!=0) //&& OrderProfit()>=TakeProfit*3)
               {
               
OpenPrice_buy1=0; 
OpenPrice_buy2=0 ;
OpenPrice_buy3=0 ;

OpenPrice_buy1_1=0;
OpenPrice_buy1_1_1=0 ; 
OpenPrice_buy1_1_2=0 ;

OpenPrice_buy1_2=0 ; 
OpenPrice_buy1_2_1=0 ; 
OpenPrice_buy1_2_2=0 ;

OpenPrice_buy1_3=0 ;
OpenPrice_buy1_3_1=0 ; 
OpenPrice_buy1_3_2=0 ; 

OpenPrice_buy1_4=0 ;
OpenPrice_buy1_4_1=0 ; 
OpenPrice_buy1_4_2=0 ; 
        
               ototal=OrdersTotal(); 
                for(cnt=ototal; cnt>=0; cnt--)                         //!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                       {                                               //!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                          OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES);  //!!!!!!!!!!!!!!!!!!!!!!!!!!!!
              
                        if (OrderTicket()==b1_3_1 )// && c1_3_1==0)
                         {
                         OrderClose(OrderTicket(),OrderLots(),Bid,5,White);
                         OpenPrice_buy1_3_1=0;
                        // c1_3_1=1;
                         Print("close: b1_3_1" );
                         continue;
                          }
                            
                        if (OrderTicket()==b1_3)// && c1_2==0)//&&OpenPrice_buy1_1!=0 )
                         {
                         OrderClose(OrderTicket(),OrderLots(),Bid,5,White);
                         OpenPrice_buy1_3=0;
                        // c1_2=1;
                         Print("close: b1_3");
                         continue;
                          }
                          
                        if (OrderTicket()==b1_2 )// && c1_1==0)
                         {
                         OrderClose(OrderTicket(),OrderLots(),Bid,5,White);
                         OpenPrice_buy1_2=0;
                         OpenPrice_buy1_1=0;  //598984654564654654654646564
                         OpenPrice_buy1=0;
                         Print("close: b1_2" );
                       //  c1_1=1;
                         continue;
                          } 
                          
                        if (OrderTicket()==b1_3_2)// && c1_2_2==0)
                         {
                         OrderClose(OrderTicket(),OrderLots(),Bid,5,White);
                         OpenPrice_buy1_3_2=0;
                        // c1_2_2=1;
                         Print("close: b1_3_2" );
           
                  //       continue;
                       }                     
                  } 
               
              }
     
//1_4_1                      
          if (OrderType()==OP_BUY && OpenPrice_buy1_4_1<=Bid && OpenPrice_buy1_4_1!=0 && OpenPrice_buy1_4_2!=0) //&& OrderProfit()>=TakeProfit*3)
               {
               ototal=OrdersTotal(); 
                for(cnt=ototal; cnt>=0; cnt--)                         //!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                       {                                               //!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                          OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES);  //!!!!!!!!!!!!!!!!!!!!!!!!!!!!
              
                        if (OrderTicket()==b1_4_1 )// && c1_3_1==0)
                         {
                         OrderClose(OrderTicket(),OrderLots(),Bid,5,White);
                         OpenPrice_buy1_4_1=0;
                        // c1_3_1=1;
                         Print("close: b1_4_1" );
                         continue;
                          }
                            
                        if (OrderTicket()==b1_4)// && c1_2==0)//&&OpenPrice_buy1_1!=0 )
                         {
                         OrderClose(OrderTicket(),OrderLots(),Bid,5,White);
                         OpenPrice_buy1_4=0;
                        // c1_2=1;
                         Print("close: b1_4");
                         continue;
                          }
                          
                        if (OrderTicket()==b1_3 )// && c1_1==0)
                         {
                         OrderClose(OrderTicket(),OrderLots(),Bid,5,White);
                         OpenPrice_buy1_3=0;
                         Print("close: b1_3" );
                       //  c1_1=1;
                         continue;
                          } 
                          
                        if (OrderTicket()==b1_4_2)// && c1_2_2==0)
                         {
                         OrderClose(OrderTicket(),OrderLots(),Bid,5,White);
                         OpenPrice_buy1_4_2=0;
                        // c1_2_2=1;
                         Print("close: b1_4_2" );
           
                  //       continue;
                       }                     
                    }        
                 }  
                 
                 
                 
//+------------+
//| close sell |
//+------------+
     
    
//1_2 => 1_2_1 
      
           if (OrderType()==OP_SELL && OrderTicket()==s1_2_1 && StepProfit*Point<=(OpenPrice_sell1_2_1-Bid) && OrderProfit()>0)// && c1_2_1==0)
                         {
                         Print("OpenPrice_sell1_2=",OpenPrice_sell1_2 );
                         OrderClose(OrderTicket(),OrderLots(),Bid,5,White);
                         OpenPrice_sell1_2_1=0;
                    //     c1_2_1=1;
                         Print("close: s1_2_1" );
                        
                          }   
      
          if (OrderType()==OP_SELL && OrderTicket()==s1 && OrderProfit()>0 && OpenPrice_sell1_2_1!=0)
      
                         {
                         OrderClose(OrderTicket(),OrderLots(),Bid,5,White);
                   //     OpenPrice_sell1=0;
                         Print("close: s1 in 1_2 => 1_2_1" );
                       
                         
                         }
                         
  
//1_3 => 1_3_1 
      
           if (OrderType()==OP_SELL && OrderTicket()==s1_3_1 && Bid<=OpenPrice_sell1_3 && OrderProfit()>0)// && c1_2_1==0)
                   
                         {
                         OrderClose(OrderTicket(),OrderLots(),Bid,5,White);
                         OpenPrice_sell1_3_1=0;
                 //        c1_2_1=1;
                         Print("close: s1_3_1" );
                           }                             
                    
          if (OrderType()==OP_SELL && OrderTicket()==s1 && OrderProfit()>0 && OpenPrice_sell1_3_1!=0) //&& c1==0)
      
                         {
                         OrderClose(OrderTicket(),OrderLots(),Bid,5,White);
                  //       OpenPrice_sell1=0;
                         Print("close: s1 in 1_3 => 1_3_1" );
                       //  c1=1;
                         
                         }                         
 
          if (OrderType()==OP_SELL && OrderTicket()==s1_1  && OpenPrice_sell1_3_1!=0) //&& c1==0)
      
                         {
                         OrderClose(OrderTicket(),OrderLots(),Bid,5,White);
                         OpenPrice_sell1_1=0;
                         Print("close: s1_1 in 1_3 => 1_3_1" );
                       //  c1=1;
                         
                         }         
                         
         if (OrderType()==OP_SELL && OrderTicket()==s1_2  && OpenPrice_sell1_3_1!=0) //&& c1==0)
      
                         {
                         OrderClose(OrderTicket(),OrderLots(),Bid,5,White);
                         OpenPrice_sell1_2=0;
                         Print("close: s1_2" );
                       //  c1=1;
                         
                         }                       
                                           

//1_4 => 1_4_1 
      
           if (OrderType()==OP_SELL && OrderTicket()==s1_4_1 && Bid<=OpenPrice_sell1_4 && OrderProfit()>0)// && c1_2_1==0)
                   
                         {
                         OrderClose(OrderTicket(),OrderLots(),Bid,5,White);
                         OpenPrice_sell1_4_1=0;
                 //        c1_2_1=1;
                         Print("close: s1_4_1" );
                           }                             
                    
       /*   if (OrderType()==OP_SELL && OrderTicket()==s1  && OpenPrice_sell1_4_1!=0 && OrderProfit()>0) //&& c1==0)
      
                         {
                         OrderClose(OrderTicket(),OrderLots(),Bid,5,White);
                  //       OpenPrice_sell1=0;
                         Print("close: s1 in 1_4 => 1_4_1" );
                       //  c1=1;
                         
                         }     */                    
 
          if (OrderType()==OP_SELL && OrderTicket()==s1_1  && OpenPrice_sell1_4_1!=0) //&& c1==1)
      
                         {
                         OrderClose(OrderTicket(),OrderLots(),Bid,5,White);
                         OpenPrice_sell1_1=0;
                         Print("close: s1_1 in 1_4 => 1_4_1" );
                       // c1=0;
                         
                         }   

          if (OrderType()==OP_SELL && OrderTicket()==s1_2  && OpenPrice_sell1_4_1!=0) //&& c1==0)
      
                         {
                         OrderClose(OrderTicket(),OrderLots(),Bid,5,White);
                         OpenPrice_sell1_2=0;
                         Print("close: s1_2" );
                       //  c1=1;
                    //     return(0);
                         }   
               
                                        
                         
//1_1_1            
          if (OrderType()==OP_SELL && OpenPrice_sell1_1_1>=Bid && OpenPrice_sell1_1_1!=0 && OpenPrice_sell1_1_2!=0) //&& OrderProfit()>=TakeProfit*3)
               {
              ototal=OrdersTotal();
                for(cnt=ototal; cnt>=0; cnt--)                         //!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                       {                                               //!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                          OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES);  //!!!!!!!!!!!!!!!!!!!!!!!!!!!!
              
                        if (OrderTicket()==s1_1_1 )
                         {
                         OrderClose(OrderTicket(),OrderLots(),Bid,5,White);
                         OpenPrice_sell1_1_1=0;
                         
                         Print("close: s1_1_1" );
                     //    continue;
                          }
                            
                        if (OrderTicket()==s1_1)//&&OpenPrice_sell1_1!=0 )
                         {
                         OrderClose(OrderTicket(),OrderLots(),Bid,5,White);
                         OpenPrice_sell1_1=0;
                        
                         Print("close: s1_1");
                    //     continue;
                          }
                          
                        if (OrderTicket()==s1 && OrderProfit()>0)
                         {
                         OrderClose(OrderTicket(),OrderLots(),Bid,5,White);
                      //   OpenPrice_sell1=0;
                         Print("close: s1 in 1_1_1" );
                        
                    //     continue;
                          } 
                          
                        if (OrderTicket()==s1_1_2 )
                         {
                         OrderClose(OrderTicket(),OrderLots(),Bid,5,White);
                         OpenPrice_sell1_1_2=0;
                         
                         Print("close: s1_1_2" );
                         
                  //       continue;
                         }
                       }
                      
               }  
        
             
//1_2_1                      
          if (OrderType()==OP_SELL && OpenPrice_sell1_2_1>=Bid && OpenPrice_sell1_2_1!=0 && OpenPrice_sell1_2_2!=0) //&& OrderProfit()>=TakeProfit*3)
               {
             ototal=OrdersTotal();  
                for(cnt=ototal; cnt>=0; cnt--)                         //!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                       {                                               //!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                          OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES);  //!!!!!!!!!!!!!!!!!!!!!!!!!!!!
              
                        if (OrderTicket()==s1_2_1 )
                         {
                         OrderClose(OrderTicket(),OrderLots(),Bid,5,White);
                         OpenPrice_sell1_2_1=0;
                        
                         Print("close: s1_2_1" );
                         continue;
                          }
                            
                        if (OrderTicket()==s1_2  )//&&OpenPrice_sell1_1!=0 )
                         {
                         OrderClose(OrderTicket(),OrderLots(),Bid,5,White);
                         OpenPrice_sell1_2=0;
                        
                         Print("close: s1_2");
                         continue;
                          }
                          
                        if (OrderTicket()==s1_1 && OpenPrice_sell1_2_2!=0 )
                         {
                         OrderClose(OrderTicket(),OrderLots(),Bid,5,White);
                         OpenPrice_sell1_1=0;
                         Print("OpenPrice_sell1_2_2==",OpenPrice_sell1_2_2 );
                         Print("close: s1_1 _XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX" );
                         
                         continue;
                          } 
                          
                        if (OrderTicket()==s1_2_2 )
                         {
                         OrderClose(OrderTicket(),OrderLots(),Bid,5,White);
                         OpenPrice_sell1_2_2=0;
                         
                         Print("close: s1_2_2" );
           
                  //       continue;
                         }                     
                  } 
                 
              }
       
//1_3_1                      
          if (OrderType()==OP_SELL && OpenPrice_sell1_3_1>=Bid && OpenPrice_sell1_3_1!=0 && OpenPrice_sell1_3_2!=0) //&& OrderProfit()>=TakeProfit*3)
               {
               
OpenPrice_sell1=0;                
OpenPrice_sell2=0  ;
OpenPrice_sell3=0 ;

OpenPrice_sell1_1=0 ;
OpenPrice_sell1_1_1=0 ; 
OpenPrice_sell1_1_2=0 ;
OpenPrice_sell1_2_1=0 ;
OpenPrice_sell1_2_2=0 ;

OpenPrice_sell1_4=0 ;
OpenPrice_sell1_4_1=0 ; 
OpenPrice_sell1_4_2=0  ;           
               

               ototal=OrdersTotal(); 
                for(cnt=ototal; cnt>=0; cnt--)                         //!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                       {                                               //!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                          OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES);  //!!!!!!!!!!!!!!!!!!!!!!!!!!!!
              
                        if (OrderTicket()==s1_3_1 )// && c1_3_1==0)
                         { 
                         OrderClose(OrderTicket(),OrderLots(),Bid,5,White);
                         OpenPrice_sell1_3_1=0;
                        // c1_3_1=1;
                         Print("close: s1_3_1" );
                         continue;
                          }
                            
                        if (OrderTicket()==s1_3)// && c1_2==0)//&&OpenPrice_sell1_1!=0 )
                         {
                         OrderClose(OrderTicket(),OrderLots(),Bid,5,White);
                         OpenPrice_sell1_3=0;
                        // c1_2=1;
                         Print("close: s1_3");
                         continue;
                          }
                          
                        if (OrderTicket()==s1_2 )// && c1_1==0)
                         {
                         OrderClose(OrderTicket(),OrderLots(),Bid,5,White);
                         OpenPrice_sell1_2=0;

                         Print("close: s1_2" );
                       //  c1_1=1;
                         continue;
                          } 
                          
                        if (OrderTicket()==s1_3_2)// && c1_2_2==0)
                         {
                         OrderClose(OrderTicket(),OrderLots(),Bid,5,White);
                         OpenPrice_sell1_3_2=0;
                        // c1_2_2=1;
                         Print("close: s1_3_2" );
           
                  //       continue;
                       }                     
                  } 
               
              }
     
//1_4_1                      
          if (OrderType()==OP_SELL && OpenPrice_sell1_4_1>=Bid && OpenPrice_sell1_4_1!=0 && OpenPrice_sell1_4_2!=0) //&& OrderProfit()>=TakeProfit*3)
               {
               ototal=OrdersTotal(); 
                for(cnt=ototal; cnt>=0; cnt--)                         //!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                       {                                               //!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                          OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES);  //!!!!!!!!!!!!!!!!!!!!!!!!!!!!
              
                        if (OrderTicket()==s1_4_1 )// && c1_3_1==0)
                         {
                         OrderClose(OrderTicket(),OrderLots(),Bid,5,White);
                         OpenPrice_sell1_4_1=0;
                        // c1_3_1=1;
                         Print("close: s1_4_1" );
                         continue;
                          }
                            
                        if (OrderTicket()==s1_4)// && c1_2==0)//&&OpenPrice_sell1_1!=0 )
                         {
                         OrderClose(OrderTicket(),OrderLots(),Bid,5,White);
                         OpenPrice_sell1_4=0;
                        // c1_2=1;
                         Print("close: s1_4");
                         continue;
                          }
                          
                        if (OrderTicket()==s1_3 )// && c1_1==0)
                         {
                         OrderClose(OrderTicket(),OrderLots(),Bid,5,White);
                         OpenPrice_sell1_3=0;
                         Print("close: s1_3" );
                       //  c1_1=1;
                         continue;
                          } 
                          
                        if (OrderTicket()==s1_4_2)// && c1_2_2==0)
                         {
                         OrderClose(OrderTicket(),OrderLots(),Bid,5,White);
                         OpenPrice_sell1_4_2=0;
                        // c1_2_2=1;
                         Print("close: s1_4_2" );
           
                  //       continue;
                       }                     
                    }        
                 }          
             }   
    }
}

  

