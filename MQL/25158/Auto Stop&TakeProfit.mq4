//+------------------------------------------------------------------+
//|                                         Auto Stop&TakeProfit.mq4 |
//|                                   Copyright 2019, Catalin Zachiu |
//|                      https://www.mql5.com/en/users/catalinzachiu |
//+------------------------------------------------------------------+
#property copyright "Copyright 2019, Catalin Zachiu"
#property link      "https://www.mql5.com/en/users/catalinzachiu"
#property version   "1.00"
#property strict

input double StopLoss =500;
input double TakeProfit =500;
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
void OnTick()
  {
//---
   for(int j=0;j<OrdersTotal();j++)
     {
      if(OrderSelect(j,SELECT_BY_POS,MODE_TRADES)==false) break;
      if(OrderSymbol()!=Symbol() || OrderCloseTime()!=0) continue;
        
        {
         //--- long position is opened
        
            if(OrderType()==OP_BUY && OrderStopLoss()==0)
            //--- check for trailing stop
           { if(OrderStopLoss()==OrderOpenPrice()-StopLoss*Point) break;
              else
             
                    {
                     //--- modify order and exit
                     if(!OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice()-StopLoss*Point,OrderTakeProfit(),0,Green))
                        Print("OrderModify error ",GetLastError());
                    // return;
                    }
            }     
            
             if(OrderType()==OP_BUY && OrderTakeProfit()==0)
            //--- check for trailing stop
           { if(OrderTakeProfit()==OrderOpenPrice()+TakeProfit*Point) break;
              else
             
                    {
                     //--- modify order and exit
                     if(!OrderModify(OrderTicket(),OrderOpenPrice(),OrderStopLoss(),OrderOpenPrice()+TakeProfit*Point,0,Green))
                        Print("OrderModify error ",GetLastError());
                    // return;
                    }
            }     
              
              
       
         if(OrderType()==OP_SELL && OrderStopLoss()==0)   
            //--- check for trailing stop
          {  if(OrderStopLoss()==OrderOpenPrice()+StopLoss*Point) break;
            else 
             
                    {
                     //--- modify order and exit
                     if(!OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice()+StopLoss*Point,OrderTakeProfit(),0,Red))
                        Print("OrderModify error ",GetLastError());
                     //return;
                    }
           }         
           
            if(OrderType()==OP_SELL && OrderTakeProfit()==0)   
            //--- check for trailing stop
          {  if(OrderTakeProfit()==OrderOpenPrice()-TakeProfit*Point) break;
            else 
             
                    {
                     //--- modify order and exit
                     if(!OrderModify(OrderTicket(),OrderOpenPrice(),OrderStopLoss(),OrderOpenPrice()-TakeProfit*Point,0,Red))
                        Print("OrderModify error ",GetLastError());
                    // return;
                    }
           }       
                    
        }
        
     } 
     Sleep(1000);
  }

//+------------------------------------------------------------------+
