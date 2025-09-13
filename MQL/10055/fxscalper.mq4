

#define Magic 66671
#define SLIPPAGE 3 // nejaukt ar indikatora parametru Slipage


//---- input parameters - indikatoram
extern int    Speed=5;      // BB periods
  int    Slipage=2;    // BB deviation
extern int    MM=1;         // ?? nobiide grafikam no aktuaalaas cenas (vizuaali)
int    Signal=1;     // 0 raada tikai punktinjus, 1 râda punktinjus un bultinju, 2 raada tikai bultinju
int    Line=1;       // 0 nerâda lînijas , 1 raada   
  int   Candles=100;  // cik sveces skaitiit katru reizi

// -- eksperta parametri
extern int TakeProfit            = 150;
extern int StopLoss              = 200;

extern int TrailingStop          = 0;
//extern int BreakEven             = 0;

//extern int CloseAfterXBars       = 2; // peec shitik ilga laika aizveersim
extern double Lots               = 0.1;

  bool CloseOrderOnOppositeSignal = false;
  bool DisplaySignalArrows = false;
//extern double RiskPercent        = 5;
//extern bool mm                   = false; // money management active?


//---- indicator buffers
double UpTrendBuffer[10000];
double DownTrendBuffer[10000];
double UpTrendSignal[10000];
double DownTrendSignal[10000];
double UpTrendLine[10000];
double DownTrendLine[10000];
  bool AlertON=true;
bool TurnedUp = false;
bool TurnedDown = false;

int barCount = 0;
int currPosition = 0; // 0 nav treidi, 1 buy, -1 sell

//---------------------------------------------------------------------------------------------------------------
int init()
{
    barCount=0; // svechu skaits grafikaa, lai zinaatu, vai ir jauna svece  
    currPosition=0;
//----
   return(0);
}
  

void drawVLine(datetime barTime) 
{
     double cena; color kraasa; string nosaukums; string descr;
 
     nosaukums="fxscalper"+barTime;
     

     cena=0;
     kraasa=Yellow;
     descr="";
     
     
          ObjectCreate(nosaukums,OBJ_VLINE,0,barTime, cena, 0, 0,0,0);
          ObjectSet(nosaukums,OBJPROP_WIDTH,1);  
          ObjectSet(nosaukums,OBJPROP_COLOR,kraasa); // color
          ObjectSet(nosaukums,OBJPROP_STYLE,STYLE_SOLID);  
          ObjectSetText(nosaukums,descr,10);

 
     
}  

void drawArrow(datetime barTime, double cena, color kraasa, int tips) 
{
     string nosaukums;
     nosaukums="fxscalper"+barTime+cena;
     
     if (cena==0) { ObjectDelete(nosaukums); return(0); }
 
     if (ObjectFind(nosaukums)==-1) { // veidojam jaunu objektu
          ObjectCreate(nosaukums,OBJ_ARROW,0,barTime, cena, 0, 0,0,0);
          ObjectSet(nosaukums,OBJPROP_WIDTH,1);  
          ObjectSet(nosaukums,OBJPROP_COLOR,kraasa); // color
          ObjectSet(nosaukums,OBJPROP_ARROWCODE,tips);  
          //ObjectSetText(nosaukums,descr,10);

     }
     else ObjectSet(nosaukums,OBJPROP_PRICE1,cena);
     
}

  
int deleteChartObjects() {
   string nosaukums;
   for (int i = ObjectsTotal() - 1; i >= 0; i--) {
      nosaukums = ObjectName(i);
      if (StringSubstr(nosaukums, 0, 9) == "fcscalper") ObjectDelete(nosaukums);
   }
   return (0);
}  
  
int startIndicator()
  {
   int    i,shift,trend;
   double smax[25000],smin[25000],bsmax[25000],bsmin[25000];
   
   deleteChartObjects();
   
   for (shift=Candles;shift>0;shift--)
   {
   UpTrendBuffer[shift]=0;
   DownTrendBuffer[shift]=0;
   UpTrendSignal[shift]=0;
   DownTrendSignal[shift]=0;
   UpTrendLine[shift]=EMPTY_VALUE;
   DownTrendLine[shift]=EMPTY_VALUE;
   }
//   Write("start indicator");
   for (shift=Candles-Speed-1;shift>0;shift--)
   {	
     smax[shift]=iBands(NULL,0,Speed,Slipage,0,PRICE_CLOSE,MODE_UPPER,shift);
	  smin[shift]=iBands(NULL,0,Speed,Slipage,0,PRICE_CLOSE,MODE_LOWER,shift);
	
	  if (Close[shift]>smax[shift+1]) trend=1; 
	  if (Close[shift]<smin[shift+1]) trend=-1;
		 	
	  if(trend>0 && smin[shift]<smin[shift+1]) smin[shift]=smin[shift+1];
	  if(trend<0 && smax[shift]>smax[shift+1]) smax[shift]=smax[shift+1];
	  	  
	  bsmax[shift]=smax[shift]+0.5*(MM-1)*(smax[shift]-smin[shift]);
	  bsmin[shift]=smin[shift]-0.5*(MM-1)*(smax[shift]-smin[shift]);
		
	  if(trend>0 && bsmin[shift]<bsmin[shift+1]) bsmin[shift]=bsmin[shift+1];
	  if(trend<0 && bsmax[shift]>bsmax[shift+1]) bsmax[shift]=bsmax[shift+1];
	  
	  if (trend>0) 
	  {
	     if (Signal>0 && UpTrendBuffer[shift+1]==-1.0)
	     {
	     UpTrendSignal[shift]=bsmin[shift];
	     UpTrendBuffer[shift]=bsmin[shift];
	     if(Line>0) UpTrendLine[shift]=bsmin[shift];
	     

	     if (DisplaySignalArrows) drawArrow(Time[shift], bsmin[shift],LawnGreen,233);
	     
     if (AlertON==true && shift==1 && !TurnedUp)
         {
     Alert("  Alert Buy      ",Symbol(),"TF",Period());
                       TurnedUp = true;
            TurnedDown = false;
     }
	     }
	     else
	     {
	     UpTrendBuffer[shift]=bsmin[shift];
	     if(Line>0) UpTrendLine[shift]=bsmin[shift];
	     UpTrendSignal[shift]=-1;
	     }
	  if (Signal==2) UpTrendBuffer[shift]=0;   
	  DownTrendSignal[shift]=-1;
	  DownTrendBuffer[shift]=-1.0;
	  DownTrendLine[shift]=EMPTY_VALUE;
	  }
	  if (trend<0) 
	  {
	  if (Signal>0 && DownTrendBuffer[shift+1]==-1.0)
	     {
	     DownTrendSignal[shift]=bsmax[shift];
	     DownTrendBuffer[shift]=bsmax[shift];
	     if(Line>0) DownTrendLine[shift]=bsmax[shift];
	     
 
	     if (DisplaySignalArrows) drawArrow(Time[shift], bsmax[shift],OrangeRed,234);
	     
     if (AlertON==true && shift==1 && !TurnedDown)
         {
     Alert("  Alert Sell      ",Symbol(),"TF",Period());
            TurnedDown = true;
            TurnedUp = false;
     }
	     }
	     else
	     {
	     DownTrendBuffer[shift]=bsmax[shift];
	     if(Line>0)DownTrendLine[shift]=bsmax[shift];
	     DownTrendSignal[shift]=-1;
	     }
	  if (Signal==2) DownTrendBuffer[shift]=0;    
	  UpTrendSignal[shift]=-1;
	  UpTrendBuffer[shift]=-1.0;
	  UpTrendLine[shift]=EMPTY_VALUE;
	  }
	  
	 }
	return(0);	
 }
  

void OpenBuyOrder()
 {  
 
    //  Write("Entering OpenBuyOrder");

    int ticket,err,tries;
        tries = 0;
           
    double price, sl, tp;
		    price=Ask;				
		    if (TakeProfit==0) tp=0; 
		    else tp=price+TakeProfit*Point; 	
		    if (StopLoss==0) sl=0; 
		    else sl=price-StopLoss*Point;  
        
        
          while (tries < 3)
            {
               ticket = OrderSend(Symbol(),OP_BUY,Lots,price,SLIPPAGE,sl,tp,"fx EA Order",Magic,0,Blue);
           //    Write("in function OpenBuyOrder OrderSend Executed , ticket ="+ticket);
               if(ticket<=0) {
                  Write("Error Accured : "+ErrorDescription(GetLastError())+" Open Buy Order, price="+price+", TP="+tp+", SL="+sl);
                  tries++;
               } else tries = 3;
            }
            
          if (ticket>0) currPosition=1;
 }

//+------------------------------------------------------------------+
  
void OpenSellOrder()
 {
 
    //  Write("Entering OpenSellOrder");

    int ticket,err,tries;
        tries = 0;
        
    double price, sl, tp;
		    price=Bid;				
		    if (TakeProfit==0) tp=0; 
		    else tp=price-TakeProfit*Point; 	
		    if (StopLoss==0) sl=0; 
		    else sl=price+StopLoss*Point;  
                
          while (tries < 3)
            {
               ticket = OrderSend(Symbol(),OP_SELL,Lots,price,SLIPPAGE,sl,tp,"fx EA Order",Magic,0,Red);
          //     Write("in function OpenSellOrder OrderSend Executed , ticket ="+ticket);
               if(ticket<=0) {
                  Write("Error Accured : "+ErrorDescription(GetLastError())+" Open Sell Order, price="+price+", TP="+tp+", SL="+sl);
                  tries++;
               } else tries = 3;
            }
            
          if (ticket>0) currPosition=-1;
 }
 
void closeAllOrders() // aizver orderus
{

    int skaits=OrdersTotal();
    int j=0;
    int closeTickets[];
    
    ArrayResize(closeTickets,skaits);
    
    for (int i=0;i<skaits;i++)
    { //Write("cikls i="+i);
       OrderSelect(i,SELECT_BY_POS,MODE_TRADES);     

       if (
           (OrderSymbol() == Symbol()) && (OrderMagicNumber() == Magic) 
           //&& (StringFind(OrderComment(),seriesPrefix(sIndex))>=0) 
          ) // jaaver ciet
       {
          closeTickets[j]=OrderTicket();
          j++;  
       } 
    }
     
     // pa vidu var sakaartot peec loteem, ja grib aizveert, saakot ar lielaako, - bet pagaidaam paljaujamies uz ticket nr
    for (i=j-1; i>=0;i--) //(i=0; i<j; i++) 
    {
       closeOrderByTicket(closeTickets[i]);
    }
    
    currPosition=0;
     
} 

bool closeOrderByTicket(int ticket)
{
    double closePrice;
    OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES);
    if (OrderType()==OP_BUY) closePrice=Bid; else closePrice=Ask;
    OrderClose(OrderTicket(),OrderLots(),closePrice,SLIPPAGE,White);
}
   
//+------------------------------------------------------------------+
//+------------------------------------------------------------------+
  
//+------------------------------------------------------------------+
//+------------------------------------------------------------------+
/*
void DoBE(int byPips) //breakeven
  {
    for (int i = 0; i < OrdersTotal(); i++) {
     OrderSelect(i, SELECT_BY_POS, MODE_TRADES);
     if ( OrderSymbol()==Symbol() && (OrderMagicNumber() == Magic))  // only look if mygrid and symbol...
        {
            if (OrderType() == OP_BUY) if (Bid - OrderOpenPrice() > byPips * Point) if (OrderStopLoss() < OrderOpenPrice()) {
              Write("Movine StopLoss of Buy Order to BE+1");
              OrderModify(OrderTicket(), OrderOpenPrice(), OrderOpenPrice() +  Point, OrderTakeProfit(), Blue);
            }
            if (OrderType() == OP_SELL) if (OrderOpenPrice() - Ask > byPips * Point) if (OrderStopLoss() > OrderOpenPrice()) { 
               Write("Movine StopLoss of Sell Order to BE-1");
               OrderModify(OrderTicket(), OrderOpenPrice(), OrderOpenPrice() -  Point, OrderTakeProfit(), Red);
            }
        }
    }
  }

//+------------------------------------------------------------------+
 */
void DoTrail() // trailing stop
  {
    for (int i = 0; i < OrdersTotal(); i++) {
     OrderSelect(i, SELECT_BY_POS, MODE_TRADES);
     if ( OrderSymbol()==Symbol() && (OrderMagicNumber() == Magic))  // only look if mygrid and symbol...
        {
          
          if (OrderType() == OP_BUY) {
             if(Bid-OrderOpenPrice()>Point*TrailingStop)
             {
                if(OrderStopLoss()<Bid-Point*TrailingStop)
                  {
                     OrderModify(OrderTicket(),OrderOpenPrice(),Bid-Point*TrailingStop,OrderTakeProfit(),0,Blue);
                     return(0);
                  }
             }
          }

          if (OrderType() == OP_SELL) 
          {
             if((OrderOpenPrice()-Ask)>(Point*TrailingStop))
             {
                if((OrderStopLoss()>(Ask+Point*TrailingStop)) || (OrderStopLoss()==0))
                {
                   OrderModify(OrderTicket(),OrderOpenPrice(),Ask+Point*TrailingStop,OrderTakeProfit(),0,Red);
                   return(0);
                }
             }
          }
       }
    }
 }  
  
 
  
//+------------------------------------------------------------------+

//+------------------------------------------------------------------+
//| Signâla funkcijas                                                |  
//+------------------------------------------------------------------+

bool SignalBuyOpen()
{
     bool rez;

     if (currPosition==1) return(false); //jau esam longaa
     
     if (UpTrendSignal[1]>0) rez=true; else rez=false;
     
   //  Write("Signal buy = "+rez+" UpTrendSignal="+UpTrendSignal[1]);
     
     return(rez);
}        

bool SignalSellOpen()
{
     bool rez;

     if (currPosition==-1) return(false); //jau esam longaa

     if (DownTrendSignal[1]>0) rez=true; else rez=false;     

  //   Write("Signal sell = "+rez+" DnTrendSignal="+DownTrendSignal[1]);

     return(rez);
}


//+------------------------------------------------------------------+
   
  
//+------------------------------------------------------------------+
//| Check for open order conditions                                  |
//+------------------------------------------------------------------+
void CheckForSignal()
  {
  //   Write("Entering CheckForOpen");
     if (SignalBuyOpen()) 
     {
        if (CloseOrderOnOppositeSignal && (currPosition==-1)) closeAllOrders();
        OpenBuyOrder();
     }
        
     if (SignalSellOpen())
     {
        if (CloseOrderOnOppositeSignal && (currPosition==1)) closeAllOrders();
        OpenSellOrder();    
     }
   
  }
  

  
//+------------------------------------------------------------------+
//+------------------------------------------------------------------+

int Write(string str)
{

   Print (">>> "+ str + " Time " + TimeToStr(CurTime(),TIME_DATE|TIME_SECONDS)); // dublçjam uz ekrâna
}
  
  
  
//+------------------------------------------------------------------+
//| Start function                                                   |
//+------------------------------------------------------------------+
void start()
  {
  
     
  //---- go trading only for first tiks of new bar
   if(barCount!=Bars) 
   { // izsaukums perioda laikaa, nevis jauna perioda saakumaa
      
      // Write("start at "+Time[0]+" barcount="+Bars);
       
       startIndicator(); // apreekjina indikatoru
       CheckForSignal();
        
        
       barCount=Bars; 
      
   }

   if (TrailingStop>0) DoTrail(); //trailing stop

  }
//+------------------------------------------------------------------+

  
//+------------------------------------------------------------------+
//| return error description                                         |
//+------------------------------------------------------------------+
string ErrorDescription(int error_code)
  {
   string error_string;
//----
   switch(error_code)
     {
      //---- codes returned from trade server
      case 0:
      case 1:   error_string="no error";                                                  break;
      case 2:   error_string="common error";                                              break;
      case 3:   error_string="invalid trade parameters";                                  break;
      case 4:   error_string="trade server is busy";                                      break;
      case 5:   error_string="old version of the client terminal";                        break;
      case 6:   error_string="no connection with trade server";                           break;
      case 7:   error_string="not enough rights";                                         break;
      case 8:   error_string="too frequent requests";                                     break;
      case 9:   error_string="malfunctional trade operation";                             break;
      case 64:  error_string="account disabled";                                          break;
      case 65:  error_string="invalid account";                                           break;
      case 128: error_string="trade timeout";                                             break;
      case 129: error_string="invalid price";                                             break;
      case 130: error_string="invalid stops";                                             break;
      case 131: error_string="invalid trade volume";                                      break;
      case 132: error_string="market is closed";                                          break;
      case 133: error_string="trade is disabled";                                         break;
      case 134: error_string="not enough money";                                          break;
      case 135: error_string="price changed";                                             break;
      case 136: error_string="off quotes";                                                break;
      case 137: error_string="broker is busy";                                            break;
      case 138: error_string="requote";                                                   break;
      case 139: error_string="order is locked";                                           break;
      case 140: error_string="long positions only allowed";                               break;
      case 141: error_string="too many requests";                                         break;
      case 145: error_string="modification denied because order too close to market";     break;
      case 146: error_string="trade context is busy";                                     break;
      //---- mql4 errors
      case 4000: error_string="no error";                                                 break;
      case 4001: error_string="wrong function pointer";                                   break;
      case 4002: error_string="array index is out of range";                              break;
      case 4003: error_string="no memory for function call stack";                        break;
      case 4004: error_string="recursive stack overflow";                                 break;
      case 4005: error_string="not enough stack for parameter";                           break;
      case 4006: error_string="no memory for parameter string";                           break;
      case 4007: error_string="no memory for temp string";                                break;
      case 4008: error_string="not initialized string";                                   break;
      case 4009: error_string="not initialized string in array";                          break;
      case 4010: error_string="no memory for array\' string";                             break;
      case 4011: error_string="too long string";                                          break;
      case 4012: error_string="remainder from zero divide";                               break;
      case 4013: error_string="zero divide";                                              break;
      case 4014: error_string="unknown command";                                          break;
      case 4015: error_string="wrong jump (never generated error)";                       break;
      case 4016: error_string="not initialized array";                                    break;
      case 4017: error_string="dll calls are not allowed";                                break;
      case 4018: error_string="cannot load library";                                      break;
      case 4019: error_string="cannot call function";                                     break;
      case 4020: error_string="expert function calls are not allowed";                    break;
      case 4021: error_string="not enough memory for temp string returned from function"; break;
      case 4022: error_string="system is busy (never generated error)";                   break;
      case 4050: error_string="invalid function parameters count";                        break;
      case 4051: error_string="invalid function parameter value";                         break;
      case 4052: error_string="string function internal error";                           break;
      case 4053: error_string="some array error";                                         break;
      case 4054: error_string="incorrect series array using";                             break;
      case 4055: error_string="custom indicator error";                                   break;
      case 4056: error_string="arrays are incompatible";                                  break;
      case 4057: error_string="global variables processing error";                        break;
      case 4058: error_string="global variable not found";                                break;
      case 4059: error_string="function is not allowed in testing mode";                  break;
      case 4060: error_string="function is not confirmed";                                break;
      case 4061: error_string="send mail error";                                          break;
      case 4062: error_string="string parameter expected";                                break;
      case 4063: error_string="integer parameter expected";                               break;
      case 4064: error_string="double parameter expected";                                break;
      case 4065: error_string="array as parameter expected";                              break;
      case 4066: error_string="requested history data in update state";                   break;
      case 4099: error_string="end of file";                                              break;
      case 4100: error_string="some file error";                                          break;
      case 4101: error_string="wrong file name";                                          break;
      case 4102: error_string="too many opened files";                                    break;
      case 4103: error_string="cannot open file";                                         break;
      case 4104: error_string="incompatible access to a file";                            break;
      case 4105: error_string="no order selected";                                        break;
      case 4106: error_string="unknown symbol";                                           break;
      case 4107: error_string="invalid price parameter for trade function";               break;
      case 4108: error_string="invalid ticket";                                           break;
      case 4109: error_string="trade is not allowed";                                     break;
      case 4110: error_string="longs are not allowed";                                    break;
      case 4111: error_string="shorts are not allowed";                                   break;
      case 4200: error_string="object is already exist";                                  break;
      case 4201: error_string="unknown object property";                                  break;
      case 4202: error_string="object is not exist";                                      break;
      case 4203: error_string="unknown object type";                                      break;
      case 4204: error_string="no object name";                                           break;
      case 4205: error_string="object coordinates error";                                 break;
      case 4206: error_string="no specified subwindow";                                   break;
      default:   error_string="unknown error";
     }
//----
   return(error_string);
  }  
  
  
//+------------------------------------------------------------------+