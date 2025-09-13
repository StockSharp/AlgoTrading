
//+------------------------------------------------------------------+
//|                                                       eurusd.mq4 |
//|                                    Copyright © 2006-2007, Daniil |
//|                      |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2006 Daniil"
#property link      "npplus@mail.ru"
#property link      "http://www.fxmts.ru"

//-----------------------------------------------------------|
//  EXTERN                                                   |
//-----------------------------------------------------------|

extern string rem0="Выбор лота стопа и профита";
extern double TakeProfit   = 500; // оптимально
extern double Profit_one   = 70; // оптимально
extern double Stop         = 80; // оптимально
extern double BezUbitok    = 0;  // через данное количество пунктов профита стоп передвигается в уровень безубыточности
extern double Lots         = 1;  // количество лотов
extern double chapter_lots = 5;

extern string rem1="Управление капиталом:";
extern bool   MM               = false; 
extern bool   AccountIsMicro   = false;
extern double MaximumRisk  = 0.01;
extern double DecreaseFactor = 1;


extern string rem3="Параметры средних:";
extern double Exp=55;
extern double Simple=69;
extern double HMA=15;
extern double FilterMA=2;


extern string rem4="Параметры MACD:";
extern double a=2300;
extern double b=4000;
extern double c=800;

//2300 4000 800

bool buy = True;
bool sell =True;
bool close_buy_signal=false;  // сигнал на закрытие покупки
bool close_sell_signal=false; // сигнал на закрытие продажи

//int cnt; 

bool close_1=false;   // закрылась ли первая часть    (0.1 лот)
bool close_2=false;   // закрылась ли вторая часть    (0.1 лот)
bool close_3=false;   // закрылась ли третья часть    (0.1 лот)
bool close_4=false;   // закрылась ли четвертая часть (0.1 лот)
bool close_5=false;   // закрылась ли пятая часть     (0.1 лот)

//-----------------------------------------------------------|
//  LOT OPTIMIZATOR                                          |
//-----------------------------------------------------------|

double LotsOptimized()
  {
   double lot=Lots;
   int    orders=HistoryTotal();    
   int    losses=0;                 

   lot=NormalizeDouble(AccountFreeMargin()*MaximumRisk/1000.0,1);

   if(DecreaseFactor>0)
     {
      for(int i=orders-1;i>=0;i--)
        {
         if(OrderSelect(i,SELECT_BY_POS,MODE_HISTORY)==false) { Print("Error in history!"); break; }
         if(OrderSymbol()!=Symbol() || OrderType()>OP_SELL) continue;
         //----
         if(OrderProfit()>0) break;
         if(OrderProfit()<0) losses++;
        }
      if(losses==1) lot=NormalizeDouble(lot*2,1);
        if(losses==2) lot=NormalizeDouble(lot*3,1);
          if(losses==3) lot=NormalizeDouble(lot*4,1);
            if(losses==4) lot=NormalizeDouble(lot*5,1);
              if(losses==5) lot=NormalizeDouble(lot*6,1);
                  if(losses>5) lot=NormalizeDouble(lot*7,1);
        }

   if(lot<0.1) lot=0.1;
   if(MM==false) lot=Lots;
   if(AccountIsMicro==true) lot=lot/10;
//--- for CONTEST only
 //  if(lot > 5) lot=5; 
   return(lot);
  }

//-----------------------------------------------------------|
//  START                                                    |
//-----------------------------------------------------------|


int start()
  {
   

   int cnt, ticket, total;
  
//--- 
   double FMA, FMAprev;
   double EMA,SMA;
   double MacdCurrent, MacdPrevious, SignalCurrent;
   double SignalPrevious;
  
  
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
     
     
//-----------------------------------------------------------|
// ФУНКЦИИ:                                                  |
//-----------------------------------------------------------|

//--- фильтр

   FMA=iMA(NULL,0,FilterMA,0,MODE_LWMA,PRICE_CLOSE,0);
   FMAprev=iMA(NULL,0,FilterMA,0,MODE_LWMA,PRICE_CLOSE,2);

//--- средние
   EMA=iMA(NULL,0,Exp,0,MODE_EMA,PRICE_CLOSE,0);
   SMA=iMA(NULL,0,Simple,0,MODE_SMA,PRICE_CLOSE,0);
   
   
   
//--- MACD
   MacdCurrent=iMACD(NULL,0,a,b,c,PRICE_CLOSE,MODE_MAIN,2);
   MacdPrevious=iMACD(NULL,0,a,b,c,PRICE_CLOSE,MODE_MAIN,4);
   SignalCurrent=iMACD(NULL,0,a,b,c,PRICE_CLOSE,MODE_SIGNAL,2);
   SignalPrevious=iMACD(NULL,0,a,b,c,PRICE_CLOSE,MODE_SIGNAL,4);   
   

 
           
 //---------------------\\
   total=OrdersTotal();
   if(total<1) 
     {
      // no opened orders identified
      if(AccountFreeMargin()<(1000*LotsOptimized()))
        {
         Print("We have no money. Free Margin = ", AccountFreeMargin());
         return(0);  
        }
        

        
//-----------------------------------------------------------|
//  OPEN BUY                                                 |  
//-----------------------------------------------------------|

if(1==1) 
 {
 //   if( MacdCurrent>MacdPrevious && MacdPrevious<0 && MacdCurrent>0 )
 
     
        if (MacdCurrent>SignalCurrent && MacdPrevious<SignalPrevious)
        {                                     
         ticket=OrderSend(Symbol(),OP_BUY,LotsOptimized(),Ask,5,Ask-Stop*Point,Ask+TakeProfit*Point,"priceEX",16384,0,Green);
         sell=true;
         buy=false;
          
         if(ticket>0)
           {
            if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES)) Print("BUY order opened : ",OrderOpenPrice()); //MODE_TRADES
           }
         else Print("Error opening BUY order : ",GetLastError()); 
         return(0); 
         }
        }
        
//-----------------------------------------------------------|
//  OPEN SELL                                                |
//-----------------------------------------------------------|



if (1==1) 
    {  
   
 //  if( MacdCurrent<MacdPrevious && MacdPrevious>0 && MacdCurrent<0 )
       if (MacdCurrent<SignalCurrent && MacdPrevious>SignalPrevious) 
       
        
        {                                      
         ticket=OrderSend(Symbol(),OP_SELL,LotsOptimized(),Bid,5,Bid+Stop*Point,Bid-TakeProfit*Point,"priceEX",16384,0,Red);
         buy=true;
         sell=false;
        
         if(ticket>0)
           {                                    
            if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES)) Print("SELL order opened : ",OrderOpenPrice()); //,MODE_TRADES
           }
         else Print("Error opening SELL order : ",GetLastError()); 
         return(0); 
        }
        }
      return(0);
    }

//-----------------------------------------------------------|
//  CLOSE ORDER                                              |
//-----------------------------------------------------------|



 if (OrdersTotal()>0 && 1==1)
      {
         OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES);
    
//--- закрытие полного ордера по облакам (сильнейшему сигналу)

         if (OrderType()==OP_BUY)
           {
        
  //     if( MacdCurrent<MacdPrevious && MacdPrevious>0 && MacdCurrent<0  )
    if (MacdCurrent<SignalCurrent && MacdPrevious>SignalPrevious) 
         
                {
         
                  OrderClose(OrderTicket(),OrderLots(),Bid,3,White);
                  
                  close_1=false;
                  close_2=false;
                                    
                  Print("Закрыли ордер на покупку полностью по противоположному сигналу");
                  //buy=false; 
           //       return(0);
                }
           }
         
         if (OrderType()==OP_SELL ) // закрываем ордер на покупку так как есть сигнал 
           {
      
     //               if(MacdCurrent>MacdPrevious && MacdPrevious<0 && MacdCurrent>0 )
                if (MacdCurrent>SignalCurrent && MacdPrevious<SignalPrevious)
                
         
               {
                  OrderClose(OrderTicket(),OrderLots(),Ask,3,White);    
                  close_1=false;
                  close_2=false;
         
                  Print("Закрыли ордер на продажу полностью по противоположному сигналу"); 
                  // sell=false;
         //         return(0);
               }
            }
  
//--- закрытие лота по частям:


// ---1--- первая часть  


         if (OrderType()==OP_BUY && Profit_one>0)
           {
// закрываем часть ордера на покупку если пробивается линия Kijun_sen
              if (Ask>OrderOpenPrice()+Profit_one*Point)          
//            if (Open[1]>Kijun_sen && Close[1]>Kijun_sen && Open[2]>Kijun_sen && Close[2]>Kijun_sen && Ask<Kijun_sen )
                {
                  OrderClose(OrderTicket(),OrderLots()/2,Bid,3,White);
                  close_1=true;
                  Print("Закрыли первой части на покупку");
                  //buy=false; 
//                  return(0);
                }
           }
         
         if (OrderType()==OP_SELL && Profit_one>0) // закрываем ордер на покупку так как есть сигнал 
           {
// закрываем часть ордера на продажу если пробивается линия Kijun_sen 
               if (Ask<OrderOpenPrice()-Profit_one*Point)
//            if (Open[1]<Kijun_sen && Close[1]<Kijun_sen && Open[2]<Kijun_sen && Close[2]<Kijun_sen && Ask>Kijun_sen)
               {
                  OrderClose(OrderTicket(),OrderLots()/2,Ask,3,White);    
                  close_1=true;
                  Print("Закрыли первой части на продажу"); 
                  // sell=false;
//                  return(0);
               }
            }


/*
// ---2--- вторая часть


         if (OrderType()==OP_BUY  && 1==2 && close_2==false && OrderProfit()>0)
           {
// закрываем часть ордера на покупку если пробивается линия Kijun_sen
            if (Open[1]>Hull && Close[1]>Hull && Open[2]>Hull && Close[2]>Hull && Ask<Hull )
                {
                  OrderClose(OrderTicket(),0.4,Bid,3,White);
                  close_2=true;
                  Print("Закрыли первой части на покупку");
                  //buy=false; 
//                  return(0);
                }
           }
         
         if (OrderType()==OP_SELL && 1==2 && close_2==false && OrderProfit()>0) // закрываем ордер на покупку так как есть сигнал 
           {
// закрываем часть ордера на продажу если пробивается линия Kijun_sen 
            if (Open[1]<Hull && Close[1]<Hull && Open[2]<Hull && Close[2]<Hull && Ask>Hull)
               {
                  OrderClose(OrderTicket(),0.4,Ask,3,White);    
                  close_2=true;
                  Print("Закрыли первой части на продажу"); 
                  // sell=false;
//                  return(0);
               }
            }
*/
//-----------------------------------------------------------|
//  BEZUBITOK                                                |
//-----------------------------------------------------------|


if (OrderType()==OP_BUY && BezUbitok>0)  
               {
                if(OrderStopLoss()>0) //|| OrderStopLoss()-OrderOpenPrice()<2*Point)
                  {
                   if (Bid-OrderOpenPrice()>BezUbitok*Point)
                     {
                       OrderModify (OrderTicket(),OrderOpenPrice(),OrderOpenPrice(),OrderTakeProfit(),0,Gold);
                      }
                   }    
               }       
               
               
                if (OrderType()==OP_SELL && BezUbitok>0)
                  {
                      if (OrderStopLoss()>0)// || OrderOpenPrice()-OrderStopLoss()<2*Point)
                        {
                         if (OrderOpenPrice()-Ask>BezUbitok*Point)
                          {
                       OrderModify (OrderTicket(),OrderOpenPrice(),OrderOpenPrice(),OrderTakeProfit(),0,Gold);
                   }
                 }
                 }


    
       }


 

        
   return(0);
  }
  
//-----------------------------------------------------------|
//  THE END                                                  |
//-----------------------------------------------------------|  