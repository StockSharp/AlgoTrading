//+------------------------------------------------------------------+
//|                                                        close.mq4 |
//|                                                  ThinkTrustTrade |
//|                                        www.think-trust-trade.com |
//+------------------------------------------------------------------+
#property copyright "ThinkTrustTrade"
#property link      "www.think-trust-trade.com"

extern string  Visit="www.think-trust-trade.com";
extern string  Like="www.facebook.com/ThinkTrustTrade";
extern string side="-----Select-Side-------";
extern bool long=true;
extern bool short=true;
extern string limits="----Set-Profit-and/or-Loss-Limits----";
extern bool profit_limit=true;
extern int pip_limit_profit=100;
extern bool loss_limit=true;
extern int pip_limit_loss=-200;
extern string time_limit="----Set-Time-Limit-----";
extern bool close_after_time_limit=true;
extern int minutes_limit=1;
extern int hours_limit=1;
extern string time_close="--Close-at/after-specific-time-GMT-";
extern bool close_at_specific_time=true;
extern int gmt_offset_hours=3;
extern int gmt_offset_minutes=0;
extern int close_hour=15;
extern int close_minute=0;
extern string magic_filter="---Set-Positions-Magic-Number-to-close/skip---";
extern int only_magic=0;
extern int skip_magic=0;
extern string symbol_filter="---Set-Symbol-to-close---";
extern bool only_below_symbol=false;
extern string symbol="EURUSD";

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
//----
//=====================================================================================================

open_result(pip_limit_profit, pip_limit_loss, minutes_limit, hours_limit);

//----------------------------------------------------------------------------------------------------   
//----
   return(0);
  }
//+------------------------------------------------------------------+

void open_result (int pip_limit_profit, int pip_limit_loss, int minutes_limit, int hours_limit)
{
int time_limit=minutes_limit+hours_limit*60;
//Print (time_limit);
int ticket;
int opened_minutes_ago;
bool above=false;
double limit_profit;
double limit_loss;
double p;

if (OrdersTotal()==0) return;
for (int i=0; i<=OrdersTotal(); i++)
         {
               if (OrderSelect(i, SELECT_BY_POS, MODE_TRADES)==true)
                  {
                  if (only_magic>0 && OrderMagicNumber()!=only_magic) continue;
                  if (skip_magic>0 && OrderMagicNumber()==skip_magic) continue;
                  if (only_below_symbol==true && OrderSymbol()!=symbol)continue;
                  opened_minutes_ago=(TimeCurrent()-OrderOpenTime())/60;
                  if (close_after_time_limit==true && opened_minutes_ago<time_limit) continue;
                  if (close_at_specific_time==true && specitfic_time()!=1) continue;
                     if (OrderType()==0 && long==true)//for long positions
                     {
                     p=MarketInfo(OrderSymbol(), MODE_POINT);
                     limit_profit=pip_limit_profit*p;
                     limit_loss=pip_limit_loss*p;
                     //Print ("Limit Loss: ",limit_loss);
                     
                        if(profit_limit)
                        { 
                        if(MarketInfo(OrderSymbol(),MODE_BID)-OrderOpenPrice()>=limit_profit) 
                              {
                        
                                    //Print ("Position ", OrderTicket(), " should be closed!");
                                    ticket=OrderClose(OrderTicket(),OrderLots(), MarketInfo(OrderSymbol(),MODE_BID), 3,Red);
                                    if (ticket==-1) Print ("Error: ",  GetLastError());
                              }
                        }      
                        if(!loss_limit) continue;       
                        //Print("Position ",OrderTicket(),"result: ",MarketInfo(OrderSymbol(),MODE_BID)-OrderOpenPrice());
                        if((MarketInfo(OrderSymbol(),MODE_BID)-OrderOpenPrice()<=limit_loss) && opened_minutes_ago>time_limit) 
                              {
                        
                                    //Print ("Position ", OrderTicket(), " should be closed!");
                                    ticket=OrderClose(OrderTicket(),OrderLots(), MarketInfo(OrderSymbol(),MODE_BID), 3,Red);
                                    if (ticket==-1) Print ("Error: ",  GetLastError());
                              }

                     }
                  //-------------------------------------------------------------------------------   
                  if (OrderType()==1 && short==true)//for short positions
                     {
                     p=MarketInfo(OrderSymbol(), MODE_POINT);
                     limit_profit=pip_limit_profit*p;
                     limit_loss=pip_limit_loss*p;
                     //Print ("Limit Loss: ",limit_loss);
                     
                        if(profit_limit)
                        {
                        if(OrderOpenPrice()-MarketInfo(OrderSymbol(),MODE_ASK)>=limit_profit)
                              { 
                        
                                    //Print ("Position ", OrderTicket(), " should be closed!");
                                    ticket=OrderClose(OrderTicket(),OrderLots(), MarketInfo(OrderSymbol(),MODE_ASK), 3,Red);
                                    if (ticket==-1) Print ("Error: ",  GetLastError());
                              }
                        }      
                        if(!loss_limit) continue;      
                        //Print("Position ",OrderTicket(),"result: ",(OrderOpenPrice()-MarketInfo(OrderSymbol(),MODE_ASK)));      
                        if(OrderOpenPrice()-MarketInfo(OrderSymbol(),MODE_ASK)<=limit_loss)
                              { 
                        
                                    //Print ("Position ", OrderTicket(), " should be closed!");
                                    ticket=OrderClose(OrderTicket(),OrderLots(), MarketInfo(OrderSymbol(),MODE_ASK), 3,Red);
                                    if (ticket==-1) Print ("Error: ",  GetLastError());
                              }     

                     }
                  //------------------------------------------------------------------------------      
                  }
         }
         return (above);
} 

int specitfic_time()
         {
               int close_gmt_h=close_hour+gmt_offset_hours;
               int close_gmt_m=close_minute+gmt_offset_minutes;
               int ch,chf,cm;
               if(close_gmt_h==24) ch=0;
               if(close_gmt_h<0) ch=24+close_hour+gmt_offset_hours;
               if(close_gmt_h>24) ch=close_hour+gmt_offset_hours-24;
               if(close_gmt_h>=0 && close_gmt_h<24) ch=close_hour+gmt_offset_hours;  
               //Print ("close hour: ", ch);
               if(close_gmt_m==60) cm=0;
               if(close_gmt_m<0) { cm=60+close_minute+gmt_offset_minutes; chf=ch-1;}
               if(close_gmt_m>60) { cm=close_minute+gmt_offset_minutes-60; chf=ch+1;}
               if(close_gmt_m<60 && close_gmt_m>=0) { cm=close_minute+gmt_offset_minutes; chf=ch;}
               //Print ("close hour: ", chf, " close minute: ", cm);/**/
               if (Hour()>=chf && Minute()>=cm) return(1);
         }

