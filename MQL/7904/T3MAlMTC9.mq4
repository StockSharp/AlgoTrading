//+------------------------------------------------------------------+
//|                                                    T3MA(MTC).mq4 |
//|                                                Bezborodov Alexey |
//|                                                   AlexeiBv@ya.ru |
//+------------------------------------------------------------------+
#property copyright "Bezborodov Alexey"
#property link      "AlexeiBv@ya.ru"

extern double  lotSize=1.0;
extern bool    useStopLoss = true;
extern int     StopLoss = 40;
extern bool    usetakeProfit = true;
extern int     TakeProfit = 11;
extern bool    MultiPositions = true;
extern int     CalculationBarIndex = 1;


extern int     ExpertMagicNumber=1234;
extern bool    useSoundAlerts=true;


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
double LastOrder=0;
int start()
  {
//----
   int     i=0;
   double   m0,m1,m2;
   int      Ордер=0;
   double   lots=lotSize;
   double   stop=0;
   double   take=0;
   int      ExpertMagicNumber1;
   datetime expiration=0;
   string   comm = "Ордер по Т3МА";
   int      openPosition=0;
   int      cmd;
   double   priceOpen;
   int      Slippage=3;
   color    arrowOpen = 0x008000;

   m0=iCustom(NULL,0,"T3MA-ALARM",true,0,CalculationBarIndex);
   m1=iCustom(NULL,0,"T3MA-ALARM",true,1,CalculationBarIndex);

  Comment("m0="+m0+" m1="+m1+" LastOrder"+LastOrder); 

  if(m0!=0.0 && m0!=LastOrder && (MultiPositions==true || !ExistPositions()) )
  { 
    LastOrder=m0;
    stop=0.0;
    if(useStopLoss==1)
    {
     stop=Bid-StopLoss*Point;
    } 

    cmd=OP_BUY; priceOpen=Ask;
    openPosition=1;
    ExpertMagicNumber1=ExpertMagicNumber;
   
    take=0;
    if(usetakeProfit==1)
     take=(Bid + TakeProfit*Point);
  } 

 
  if(m1!=0.0 && m1!=LastOrder && (MultiPositions==true || !ExistPositions()) )
  { 
    LastOrder=m1;
    stop=0.0;
    if(useStopLoss==1)
    {
     stop=Bid+StopLoss*Point;
    } 

    cmd=OP_SELL; priceOpen=Bid;
    openPosition=1;
    ExpertMagicNumber1=ExpertMagicNumber;
   
    take=0;
    if(usetakeProfit==1)
     take=(Bid - TakeProfit*Point);
  } 
 
  
 if(openPosition>0)
 {
   Ордер=OrderSend(
       Symbol(),
       cmd,
       lots,
       priceOpen,
       Slippage,
       stop,
       take,
       comm,
       ExpertMagicNumber1,
       expiration,
       arrowOpen      );
  if(Ордер<1) 
  {
   Print("OrderSend failed, error #",GetLastError());
  }     
  else
  {

    if(useSoundAlerts==true){PlaySound("alert.wav");}

  }
 }  
//----
   return(0);
  }
//+------------------------------------------------------------------+


bool ExistPositions() 
   {
   for (int i=0; i<OrdersTotal(); i++) 
      {
      if (OrderSelect(i, SELECT_BY_POS, MODE_TRADES)) 
         {
         if (OrderSymbol()==Symbol()) 
            {
            return(True);
            }
         } 
      } 
   return(false);
   }

