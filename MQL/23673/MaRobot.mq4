//+------------------------------------------------------------------+
//|                                                     MaRobert.mq4 |
//|                                                           Junluo |
//|                                              http://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "Junluo"
#property link      "http://www.mql5.com"
#property version   "1.04"
#property strict

input int MAGICMA=20181115;

input double Lots  = 0.01;
input int MovingFastPeriod  =10;
input int MovingSlowPeriod  =23;
input double AdxThresHold = 30;
input double RsiThresHold = 38;
input double TakeProfit    =0.038;
input int StopLossPoint = 10;
input double Protect = 0.001;
input int BackClose =  12;
input int HOUR = 2;
input bool  Debug =  true;

int AdxPeriod = 14;
double AdxMain;

int RsiPeriod = 14;
double Rsi;

double MaFastCurrent;
double MaFastPrevious;
double MaSlowCurrent;
double StopLoss;

datetime LastbarTime;
datetime NewbarTime;
int TimeframeSeconds;

//+------------------------------------------------------------------+
//| Calculate open positions                                         |
//+------------------------------------------------------------------+
int CalculateCurrentOrders(string symbol)
{
   int buys=0,sells=0;
//---
   for(int i=0;i<OrdersTotal();i++)
     {
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES)==false) break;
      if(OrderSymbol()==Symbol() && OrderMagicNumber()==MAGICMA)
        {
         if(OrderType()==OP_BUY)  buys++;
         if(OrderType()==OP_SELL) sells++;
        }
     }
//--- return orders volume
   if(buys>0) return(buys);
   else       return(-sells);
}

//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
{
//---
    if(Period()>=PERIOD_D1)
    {
        Print("Period():",Period()," Time frame should less then H1!"); 
    }   
//---
    TimeframeSeconds = PeriodSeconds(PERIOD_CURRENT); 
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
//---
    if(IsTradeAllowed()==false) return;
    
    if(Period()>=PERIOD_D1) return;
  
    NewbarTime = TimeCurrent()/TimeframeSeconds*TimeframeSeconds;

    if(LastbarTime==0)
    {
        LastbarTime=NewbarTime;           
    }      
    
    if(LastbarTime<NewbarTime)       
    {    
        LastbarTime=NewbarTime;  
        
        AdxMain=iADX(NULL,PERIOD_D1,AdxPeriod,PRICE_CLOSE,MODE_MAIN,1); 
        
        Rsi = iRSI(NULL,PERIOD_D1,RsiPeriod,PRICE_CLOSE,1);   
        
        MaFastCurrent=iMA(NULL,0,MovingFastPeriod,0,MODE_SMA,PRICE_CLOSE,1); 
        MaFastPrevious=iMA(NULL,0,MovingFastPeriod,0,MODE_SMA,PRICE_CLOSE,2); 
        MaSlowCurrent=iMA(NULL,0,MovingSlowPeriod,0,MODE_SMA,PRICE_CLOSE,1);  
        
        if(Debug&&TimeHour(NewbarTime)==HOUR) Print("AdxMain:",AdxMain,",Rsi:",Rsi);          
    }   
     
    if(MaSlowCurrent<=0)
    {
        return;
    }
    
    if(CalculateCurrentOrders(Symbol())==0)
    {
        CheckForOpen();
    }
    //else
    {
        CheckForClose();      
    }   
}
//+------------------------------------------------------------------+

double LotsOptimized()
{
    double lot=0;
    string description;
    
    if(AccountBalance() <= 0)
    {
        return 0;
    }
  
    lot = AccountEquity()/1000;
    lot = lot*Lots;  
  
    if(MarketInfo(Symbol(),MODE_LOTSTEP)==0.1)
    {
        lot = NormalizeDouble(lot,1);  
        if(lot<=0.1) return 0;
    }
    else
    {
        lot = NormalizeDouble(lot,2); 
        if(lot<=0.01) return 0;
    }
    
    double min_volume=SymbolInfoDouble(Symbol(),SYMBOL_VOLUME_MIN);
    if(lot<min_volume)
    {
      description=StringFormat("Volume is less than the minimal allowed SYMBOL_VOLUME_MIN=%.2f",min_volume);
      Print(description);
      lot = 0;
    }   
    
    double max_volume=SymbolInfoDouble(Symbol(),SYMBOL_VOLUME_MAX);
    if(lot>max_volume)
    {
        description=StringFormat("Volume is greater than the maximal allowed SYMBOL_VOLUME_MAX=%.2f",max_volume);
        Print(description);
        lot = max_volume;
    }     
  
    if(Debug&&TimeHour(NewbarTime)==HOUR) Print("lot:",lot);    
  
    return (lot);
}
//+------------------------------------------------------------------+
//| Check for open order conditions                                  |
//+------------------------------------------------------------------+
void CheckForOpen()
{
    double ticket=0;
    int index = 0;  
    double lot = 0;
    datetime optime;
    int i = 0;
    
    if(AdxMain> AdxThresHold)
        return;
   
    for(i=OrdersHistoryTotal()-1;i>=0;i--)
    {
        if(OrderSelect(i,SELECT_BY_POS,MODE_HISTORY)==false) break;
        if(OrderSymbol()==Symbol() && OrderMagicNumber()==MAGICMA)
        {
            optime = OrderOpenTime();  
           
            index = iBarShift(NULL,0,optime,true);
  
            if(index == 0) 
            {         
                return;
            }
            
            break;
        }
    }    
   
    for(i=OrdersTotal()-1;i>=0;i--)
    {
        if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES)==false) break;
        if(OrderSymbol()==Symbol() && OrderMagicNumber()==MAGICMA)
        {
            optime = OrderOpenTime();  
           
            index = iBarShift(NULL,0,optime,true);
            
            if(index == 0) 
            {         
                return;
            }
            
            break;            
        }
    }    

    {          
        if(MaFastCurrent > MaSlowCurrent && MaFastPrevious < MaSlowCurrent
            &&Rsi < RsiThresHold)
        {
            lot = LotsOptimized();      
            if(lot > 0) ticket=OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"",MAGICMA,0,Green);
            if(ticket<0)
            {
                StopLoss = 0;
                Print("Error opening BUY order : ",GetLastError()); 
            }  
            else
            {
                index=iLowest(NULL,0,MODE_LOW,BackClose, 0); 
                if(index!=-1) 
                {
                    StopLoss = iLow(NULL,0,index); 
                    Print("Buy Enter. StopLoss : ",StopLoss);
                }
                else
                {
                    Print("Error iLowest : ",GetLastError());            
                }
            }
        } 
         
        if(MaFastPrevious > MaSlowCurrent && MaFastCurrent < MaSlowCurrent
            &&Rsi>100-RsiThresHold)
        {  
            lot = LotsOptimized();     
            if(lot > 0) ticket=OrderSend(Symbol(),OP_SELL,lot,Bid,3,0,0,"",MAGICMA,0,Red);  
            if(ticket<0)
            {
                Print("Error opening SELL order : ",GetLastError()); 
            }  
            else
            {
                index=iHighest(NULL,0,MODE_HIGH,BackClose, 0); 
                if(index!=-1) 
                {
                    StopLoss = iHigh(NULL,0,index); ; 
                    Print("Sell Enter. StopLoss : ",StopLoss);
                }
                else
                {
                    Print("Error iHighest : ",GetLastError());            
                }        
            }           
        } 
    }
}
//+------------------------------------------------------------------+
//| Check for close order conditions                                 |
//+------------------------------------------------------------------+
void CheckForClose()
{ 
//--- go trading only for first tiks of new bar
    int i;
    double op;

    for(i=0;i<OrdersTotal();i++)
    {
        if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES)==false) continue;   
        if(OrderMagicNumber()!=MAGICMA || OrderSymbol()!=Symbol()) continue; 
        op = OrderOpenPrice();
       
        if(OrderType() == OP_BUY)
        {
            if((Close[0]-op)/op>=TakeProfit)
            {
                Print("Take Profit, Close[0]:",Close[0]);         
                if(!OrderClose(OrderTicket(),OrderLots(),Bid,3,clrViolet))
                    Print("OrderClose error ",GetLastError());
                break;
            } 
            
            if(Close[0]<StopLoss)
            {
                Print("Stop Loss, Close[0]:",Close[0]);         
                if(!OrderClose(OrderTicket(),OrderLots(),Bid,3,clrViolet))
                    Print("OrderClose error ",GetLastError());
                break;
            }   
            
            if(MaFastPrevious > MaSlowCurrent && MaFastCurrent < MaSlowCurrent)
            {
                Print("Buy Exit");         
                if(!OrderClose(OrderTicket(),OrderLots(),Bid,3,clrViolet))
                    Print("OrderClose error ",GetLastError());
                break;            
            }     
            
            if(OrderStopLoss()==0)
            {
                if((Close[0]-op)/op>Protect)
                {
                    SetProtectStopLoss(OrderTicket(),StopLossPoint);
                }
            }                                      
        } 
       
        if(OrderType() == OP_SELL)
        {
            if((op-Close[0])/op>=TakeProfit)
            {    
                Print("Take Profit, Close[0]:",Close[0]);        
                if(!OrderClose(OrderTicket(),OrderLots(),Ask,3,clrViolet))
                    Print("OrderClose error ",GetLastError());
                break; 
            }    
            
            if(Close[0]>StopLoss)
            {
                Print("Stop Loss, Close[0]:",Close[0]);        
                if(!OrderClose(OrderTicket(),OrderLots(),Ask,3,clrViolet))
                    Print("OrderClose error ",GetLastError());
                break; 
            }               
            
            if(MaFastCurrent > MaSlowCurrent && MaFastPrevious < MaSlowCurrent)
            {
                Print("Sell Exit");        
                if(!OrderClose(OrderTicket(),OrderLots(),Ask,3,clrViolet))
                    Print("OrderClose error ",GetLastError());
                break;             
            }
            
            if(OrderStopLoss()==0)
            {
                if((op-Close[0])/op>Protect)
                {
                    SetProtectStopLoss(OrderTicket(),StopLossPoint);
                }
            }              
        }    
    }    
   
    return;
//---
}

bool SetProtectStopLoss(int ticket, int stopLoss)
{
  double newStop = 0, stop = 0;
  double tp = 0;
  int typ;
  double stopPips = stopLoss*1.0;
  bool err = false;
  double ask = 0;
  double bid = 0;
  ask = MarketInfo(OrderSymbol(), MODE_ASK);
  bid = MarketInfo(OrderSymbol(),MODE_BID);

  double stoplevel = MarketInfo(Symbol(),MODE_STOPLEVEL);
 
  if (ticket != 0)
    if (!OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES)) return err;
     
  tp = OrderTakeProfit();
  stop = OrderStopLoss();
    
  stopPips = (int)MathMax(stopPips,stoplevel);
 
  typ = OrderType();
  
  if (typ ==OP_BUY)
  {     
    if(stopPips != 0)
    {        
      newStop = OrderOpenPrice()+stopPips*Point;  
      
      if(newStop>bid)
      {    
        Print("newStop:",newStop,",bid:",bid);       
        return false;
      }     
    }
  }  
      
  if (typ == OP_SELL)
  {       
    if(stopPips != 0)
    {    
      newStop = OrderOpenPrice() - stopPips*Point;  
      
      if(newStop<ask)
      {     
        Print("newStop:",newStop,",ask:",ask);         
        return false;
      }                                                  
    }
  }
  
  if (stop != newStop)
  {
    err = OrderModify(ticket,OrderOpenPrice(),newStop,tp,OrderExpiration(),White);
    if( err == false) Print(string(OrderTicket())+",OrderSend error:"+string(GetLastError()));    
  }
  
  return err; 
}
