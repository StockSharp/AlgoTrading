//+------------------------------------------------------------------+
//|                                                  TPSL-Insert.mq4 |
//|                                                                  |
//|                                                                  |
//+------------------------------------------------------------------+
#property copyright ""
#property link      ""

//---- input parameters
//extern double        TakeProfitPips=35;
//extern double        StopLossPips=100;
extern double  Move_SL_at = 0.9175;


      int         Faktor, Digt, cnt;
   double         TPp, SLp, Total, i;

//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
int init(){}

//+------------------------------------------------------------------+
//| expert deinitialization function                                 |
//+------------------------------------------------------------------+
int deinit(){}

//+------------------------------------------------------------------+
//| expert start function                                            |
//+------------------------------------------------------------------+



int start()
  {
      

double OP = OrderOpenPrice();
double BCP = High[0];
double SCP = Low[0];

Total=OrdersTotal();
  if(Total>0)
  {
     for(i=Total-1; i>=0; i--) 
     {
     if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES))
        {
        if(OrderSymbol() == Symbol()){


            if(OrderStopLoss()==0 )
               {
                  if(OrderType()==OP_BUY && BCP>Move_SL_at)
                     {SLp = OP;
                     OrderModify(OrderTicket(),OrderOpenPrice(),SLp,TPp,0);
                     SendMail("","Stop Loss has just been moved to : "+DoubleToStr(SLp,5)+""  );
                     }
                     
                  if(OrderType()==OP_SELL && SCP<Move_SL_at)                     
                     {SLp = OP;
                     OrderModify(OrderTicket(),OrderOpenPrice(),SLp,TPp,0);
                     SendMail("","Stop Loss has just been moved to : "+DoubleToStr(SLp,5)+""  ); 
                     }
               } else SLp = OrderStopLoss();
            }}} }


Comment(

           "\nStop Loss will move to BE at ", Move_SL_at);
            
/*
//---------------Modify Order--------------------------
            if (OrderType()==OP_BUY || OrderType()==OP_SELL)   
            OrderModify(OrderTicket(),OrderOpenPrice(),SLp,TPp,0);               
//-----------------------------------------------------
*/         
      
Print ("Order Type= ",OrderType());
Print ("Open= ",OrderOpenPrice());
Print ("Ticket= ",OrderTicket());         
Print ("Chart= ",OrderSymbol());
Print ("BCP= ",BCP);
Print ("SCP= ",SCP);


  return(0);
  }// Start()
//+------------------------------------------------------------------+