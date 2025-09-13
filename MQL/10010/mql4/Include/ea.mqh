//+------------------------------------------------------------------+
//|                                                        Trade.mq4 |
//|                                 Copyright © 2010, Thomas Quester |
//|                                        http://www.olfolders.de   |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2010, Thomas Quester"
#property link      "http://www.olfolders.de"
#include <stdlib.mqh>
extern string     Trade_________________="Trade parameters";
extern double     Lots=0.0;                     // amount of trade
extern double     Slipage=20;    
extern int        StopLoss=30;
extern int        TakeProfit=0;
extern bool       TrailingStopLoss=true;
extern int        BreakEven=20;                  // Set Order to Break Even if more then x pip win
extern double     MinMoney=20;
extern int        Magic=12345;                   // our magic number

double AbsolutStop = 0;
double TotalProfit;
double TotalLoss;
int    TotalSecure;
int    numTickets;
int    tickets[];
int    commands[];
string comment;

#define OP_CLOSE -2

double GetLots()             { return (Lots);         }
void   SetAbsolutStop(double d) { AbsolutStop = d;    }
int    GetNumTickets()       { return (numTickets);   }
int    GetMagic()            { return (Magic);        }
int    GetNumSecureTickets() { return (TotalSecure);  }
int    GetBreakEven()        { return (BreakEven);    }
void   SetMagic(int m)       { Magic = m;             }
int    GetStopLoss()         { return (StopLoss);     }
int    GetTakeProfit()       { return (TakeProfit);   }
bool   GetUseTrailingStopLoss() { return (TrailingStopLoss); }
int    GetTicket(int i)      { return (tickets[i]); }
int    GetCommand(int i)     { return (commands[i]);  }
void   SetComment(string s)  { comment = s;           }
int    GetSlipage()          { return (Slipage);      }
double GetTotalProfit()      { return (TotalProfit);    }
double GetTotalLoss()      { return (TotalLoss);    }

// call this at start, it modifies the Parameters to values from the MarketInfo
// lots = 0 -> Minimal Lots
// TakeProfit, StopLoss, BreakEeven -> If Negativ than it is x * MinStop
void CorrectParameters()
{
   int minstop;
   if (Lots == 0) Lots = MarketInfo(Symbol(),MODE_MINLOT);
   minstop = MarketInfo(Symbol(),MODE_STOPLEVEL);
   if (TakeProfit < 0) TakeProfit = -TakeProfit * minstop;
   if (StopLoss   < 0) StopLoss   = -StopLoss   * minstop;
   if (BreakEven  < 0) BreakEven  = -BreakEven  * minstop;
}   
   
// +----------------------------------------------------------------------------+
// | Eruzeuge ein Textfeld im Chart
// |  Input lblname        Name des Feldes (für SetText)
// |        x,y            Koordinaten
// |        txt            Text
// |        color          Farbe des Textes
// +----------------------------------------------------------------------------+
void makelabel(string lblname,int x,int y,
   string txt,color txtcolor){
   ObjectCreate(lblname, OBJ_LABEL,0, 0, 0);
   ObjectSet(lblname, OBJPROP_CORNER, 0);
   ObjectSetText(lblname,txt,7,"Verdana", txtcolor);
   ObjectSet(lblname, OBJPROP_XDISTANCE, x);
   ObjectSet(lblname, OBJPROP_YDISTANCE, y);
}

// +----------------------------------------------------------------------------+
// | Ändere ein Textfeld
// |  Input name           Name des Feldes 
// |        txt            Neuer Text
// +----------------------------------------------------------------------------+
void SetText(string name, string txt)
{
    ObjectSetText(name,txt,7,"Verdana", White);
}

// +----------------------------------------------------------------------------+
// | Findet alle Orders und zieht Stop Loss nach
//   Input  SetStopLoss   : true: StopLoss setzen
// |
// | Output in globale Variablen
// |        secureProfit  : Gewinn in Pips im Falles eines StopLoss
// |        allSecure     : bool: Alle Orders sind im Plus
// |        totalProfit   : Gewinn in Pips jetzt
// |        allProfit     : bool: Alle Orders sind im Plus
// |        numTickets    : Anzahl der Orders
// |        cTrades       : Anzahl der Orders
// |        cWin          : Anzahl Gewinn-Orders
// |        cLoss         : Anzahl Verlust-Orders
// +----------------------------------------------------------------------------+


void FindOrders(bool SetStopLoss,string _Filter="",int _StopLoss=0)
{
    int typ,i,cnt,ticket;
    int _takeProfit;
    int id;
    cnt = OrdersTotal();
    TotalProfit = 0;
    TotalSecure=0;
    TotalLoss = 0;
    numTickets=0;
    id =0;
    ArrayResize(tickets,cnt);
    ArrayResize(commands,cnt);
    if (_StopLoss == 0) _StopLoss = GetStopLoss();   
    
    for (i=0;i<cnt;i++)
    {
       if (OrderSelect(i,SELECT_BY_POS,MODE_TRADES)==true)
       {
          if (OrderSymbol() == Symbol() && OrderMagicNumber() == Magic)
          {
             if (_Filter == "" || _Filter == OrderComment())
             {
               commands[id] = OrderType();
               tickets[id]  = OrderTicket();
               //Print("Ticket ",id," = ",tickets[id]);
               double profit,stop,open;
               typ = OrderType();
               if (typ == OP_BUY || typ == OP_SELL)
               {
                   numTickets ++;
                   profit = OrderProfit();
                   stop   = OrderStopLoss();
                   open   = OrderOpenPrice();
                   if (typ == OP_BUY)
                   {
                       if (stop > open) TotalSecure++;
                   }
                   if (typ == OP_SELL)
                   {
                       if (stop < open) TotalSecure++;
                   }
                   if (OrderProfit() <0)
                      TotalLoss -= OrderProfit();
                   TotalProfit += OrderProfit();
                   if (TrailingStopLoss) SetStopLoss(SetStopLoss, OrderTicket(),_StopLoss, _takeProfit);
               }
            }
          }
       }
    }
}

void CloseAllOrders()
{
    int typ,i,cnt,ticket;
    int _takeProfit;
    double price;
    cnt = OrdersTotal();


    ArrayResize(tickets,cnt);
    ArrayResize(commands,cnt);
    for (i=0;i<cnt;i++)
    {
       if (OrderSelect(i,SELECT_BY_POS,MODE_TRADES)==true)
       {
          if (OrderSymbol() == Symbol() && OrderMagicNumber() == Magic)
          {
             if (OrderType() == OP_BUY || OrderType() == OP_SELL)
             {
                
                if (OrderType() == OP_BUY) price=Ask;
                                 else price = Bid;
                                 
                OrderClose(OrderTicket(),OrderLots(),price,5);
             }
          }
       }
    }
    //Print("Profit = "+totalProfit);
}


// sets the stopp loss/take profit
// stoploss1 wenn noch nicht im profit, sonst stoploss2

// +----------------------------------------------------------------------------+
// | Setze Stop Loss und Take Profit für eine Order
// | Input: SetStopLoss: true : Orders werden modifiziert (bei False nur Statistik)
// |        ticket     : 0: Aktuelle Order
// |                     anderes: Ticket der Order
// |        stopLoss   : StopLoss der Order in Pips oder 0 für keine Änderung
// |        takeProfit : Take Profit der Order in Pips oder 0 für keine Änderung
// | Output in globale Variablen
// |        secureProfit  : Gewinn in Pips im Falles eines StopLoss
// |        allSecure     : bool: Alle Orders sind im Plus
// |        totalProfit   : Gewinn in Pips jetzt
// |        allProfit     : bool: Alle Orders sind im Plus
// +----------------------------------------------------------------------------+
void SetStopLoss(bool SetStopLoss, int ticket, int stopLoss, int takeProfit)
{


   double newStop,newStop2,stop,tp,newtp,profit;
   int typ;
   int stopPips;

   double open;
   double win;
   if (ticket != 0)
       if (!OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES)) return;
       
  
    tp = OrderTakeProfit();
    stop = OrderStopLoss();
    profit = OrderProfit();
    
    stopPips = stopLoss;
    newStop2 = 0;
    newStop= stop;
    newtp = tp;
    typ = OrderType();
    open = OrderOpenPrice();  
    if (typ ==OP_BUY)
    {
       
       if (takeProfit != 0)  newtp = Ask+Point*takeProfit;
       newStop = Ask-Point*stopPips;
       if (newStop < stop) newStop = stop;
       
       if (newStop < open)
       {
           profit = (Ask-open)/Point-MarketInfo(Symbol(),MODE_SPREAD);
           if (profit > BreakEven)
           {
               Print("Order Break even reached");
               newStop = open;
           }
      }
               
    }      
    if (typ == OP_SELL)
    {
       if (takeProfit != 0) newtp = Ask-Point*takeProfit;
       newStop = Ask+Point*stopPips;
       if (newStop > stop) newStop = stop;
       if (newStop < open)
       {
           profit = (open-Bid)/Point-MarketInfo(Symbol(),MODE_SPREAD);
           if (profit > BreakEven)
           {
               Print("Order Break even reached");
               newStop = open;
           }
      }
    }
     
    if (tp != newtp || stop != newStop)
    {
        if (SetStopLoss)
        {
        
            Print("Order Modify ticket=",ticket, " typ=",CmdName(OrderType())," newStop=",newStop, " newTp=",newtp,  " StopLoss=",StopLoss);
            OrderModify(ticket,OrderOpenPrice(),newStop,newtp,OrderExpiration(),White);
        }
    }
       
  
}

// +----------------------------------------------------------------------------+
// | Buy Lots lots
// +----------------------------------------------------------------------------+
void Order(double lots)
{
   if (lots < 0) Sell(-lots);
         else    Buy(lots);
}

void InitLots()
{
  if (Lots == 0)
      Lots = MarketInfo(Symbol(),MODE_MINLOT);
}
         
void Buy(double lots,int _stopLoss=0,int _takeProfit=0)
{

   InitLots();
   if (_stopLoss == 0)   _stopLoss = GetStopLoss();
   if (_takeProfit == 0) _takeProfit = GetTakeProfit(); 
   if (AccountEquity() < MinMoney)
   {
      Print("You have no money to trade!");
      return;
   }
   double stop,profit;
   int    magic1;
   int    ticket;
   int    i;
   double limit;
   
   stop = Ask-_stopLoss*Point;
   if (AbsolutStop != 0) 
   {
      stop = AbsolutStop; 
      AbsolutStop = 0;
   }
   if (TakeProfit != 0) 
         profit = Ask+_takeProfit*Point;
   else  
         profit = 0;
   Print("Buy lots=",lots," comment=",comment);
   ticket = OrderSend(Symbol(),OP_BUY,lots,Ask,Slipage,stop,profit,comment,Magic,0,Green);
   if (ticket < 0)
       Print("Error ",ErrorDescription(GetLastError()));
   
}

// +----------------------------------------------------------------------------+
// | Sell Lots lots
// +----------------------------------------------------------------------------+
void Sell(double lots,int _stopLoss=0,int _takeProfit=0)
{
   double stop,profit;
   int    magic1;
   int    ticket;
   int    i;
   double limit;
   InitLots();
   if (_stopLoss == 0)   _stopLoss = GetStopLoss();
   if (_takeProfit == 0) _takeProfit = GetTakeProfit(); 
   limit = Bid;
   if (AccountEquity() < MinMoney)
   {
      Print("You have no money to trade!");
      return;
   }
   
   stop = Bid+_stopLoss*Point;
   if (AbsolutStop != 0) 
   {
      stop = AbsolutStop; 
      AbsolutStop = 0;
   }
   if (TakeProfit != 0)
      profit = Bid-_takeProfit*Point;
   else  
         profit = 0;
   Print("sell lots=",lots," comment=",comment);
   ticket = OrderSend(Symbol(),OP_SELL,lots,Bid,Slipage,stop,profit,comment,Magic,0,Green);
   if (ticket < 0)
      Print("Error ",ErrorDescription(GetLastError()));
   
} 

void CloseOrder(int ticket)
{
   double price;
   Print("Closing order ticket=",ticket);
    if (OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES))
    {
       if (OrderType() == OP_BUY) price = Ask; else price=Bid;
       
         if (OrderClose(ticket,OrderLots(),price,Slipage) == false)
            Alert("Problem "+ErrorDescription(GetLastError())+" closing order");
    }
}    

void DeleteOrder(int ticket)
{
   double price;
   Print("Closing order ticket=",ticket);
    if (OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES))
    {
       if (OrderType() == OP_BUY) price = Ask; else price=Bid;
       
         if (OrderDelete(ticket) == false)
            Alert("Problem "+ErrorDescription(GetLastError())+" closing order");
    }
}    

void CloseTrades(int closeCmd)
{
    int typ,i,cnt,ticket;
    cnt = OrdersTotal();
    for (i=0;i<cnt;i++)
    {
       if (OrderSelect(i,SELECT_BY_POS,MODE_TRADES)==true)
       {
          if (OrderSymbol() == Symbol() && OrderMagicNumber() == Magic)
          {
             if (OrderType() == closeCmd)
             {
                 Print ("Closing order ",OrderTicket());
                 CloseOrder(OrderTicket());
                        

             }
          }
       }
    }
}        

// +----------------------------------------------------------------------------+
// | Returns the name of BUY/SELL Constant
// +----------------------------------------------------------------------------+

string CmdName(int cmd)
{
   string r;
   r = cmd;
   switch(cmd)
   {
      case OP_BUY: r = "buy"; break;
      case OP_SELL: r = "sell"; break;
      case OP_BUYSTOP: r = "buy stop"; break;
      case OP_SELLSTOP: r = "sell stop"; break;
      case OP_BUYLIMIT: r = "buy limit"; break;
      case OP_SELLLIMIT: r = "sell limit"; break;
      case OP_CLOSE:     r = "close"; break;
    }
    return (r);
}



void SaveStockParameters(int handle)
{
	FileWrite(handle,"Lots",Lots);
	FileWrite(handle,"StopLoss",StopLoss);
	FileWrite(handle,"TakeProfit",TakeProfit);
	FileWrite(handle,"MinMoney",MinMoney);
	FileWrite(handle,"Magic",Magic);
}