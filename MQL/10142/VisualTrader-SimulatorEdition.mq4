//+------------------------------------------------------------------+
//|                                                 OrderManager.mq4 |
//|                                                                  |
//|                                       when-money-makes-money.com |
//+------------------------------------------------------------------+
#property copyright "when-money-makes-money.com"
#property link      "when-money-makes-money.com"
extern color StoplossColor=Red;
extern color TakeprofitColor=Green;
extern color OpenPriceColor=Magenta;

#define MODE_INIT 3
#define MODE_DEINIT 2
#define MODE_NORMAL 1


double pmod;

void DeleteGFX(int tickets[]){
   for(int i=ArraySize(tickets)-1;i>=0;i--){
      ObjectDelete(tickets[i]+"-sl");
      ObjectDelete(tickets[i]+"-open");
      ObjectDelete(tickets[i]+"-tp");      
      ObjectDelete(tickets[i]+"-sl_l");
      ObjectDelete(tickets[i]+"-tp_l"); 
   }
}

void UpdateGFX(int tickets[]){
   for(int i=ArraySize(tickets)-1;i>=0;i--){
      OrderSelect(tickets[i],SELECT_BY_TICKET);
      ObjectCreate(tickets[i]+"-sl",OBJ_HLINE,0,0,OrderStopLoss());
      ObjectSet(tickets[i]+"-sl",OBJPROP_PRICE1,OrderStopLoss());
      ObjectSet(tickets[i]+"-sl",OBJPROP_COLOR,StoplossColor);    
      ObjectCreate(tickets[i]+"-sl_l",OBJ_TEXT,0,OrderOpenTime(),OrderStopLoss());
      ObjectSet(tickets[i]+"-sl_l",OBJPROP_TIME1,OrderOpenTime());
      ObjectSet(tickets[i]+"-sl_l",OBJPROP_PRICE1,OrderStopLoss());
      ObjectSet(tickets[i]+"-sl_l",OBJPROP_COLOR,StoplossColor);  
      ObjectSetText(tickets[i]+"-sl_l","[SL]"+OrderTicket()+"  for "+DoubleToStr(MathAbs(OrderOpenPrice()-OrderStopLoss())/pmod,1)+" pips");  

      ObjectCreate(tickets[i]+"-tp",OBJ_HLINE,0,0,OrderTakeProfit());
      ObjectSet(tickets[i]+"-tp",OBJPROP_COLOR,TakeprofitColor);   
      ObjectSet(tickets[i]+"-tp",OBJPROP_PRICE1,OrderTakeProfit());
      ObjectCreate(tickets[i]+"-tp_l",OBJ_TEXT,0,OrderOpenTime(),OrderTakeProfit());
      ObjectSet(tickets[i]+"-tp_l",OBJPROP_TIME1,OrderOpenTime());
      ObjectSet(tickets[i]+"-tp_l",OBJPROP_PRICE1,OrderTakeProfit());
      ObjectSet(tickets[i]+"-tp_l",OBJPROP_COLOR,TakeprofitColor);  
      ObjectSetText(tickets[i]+"-tp_l","[TP]"+OrderTicket()+"  for "+DoubleToStr(MathAbs(OrderOpenPrice()-OrderTakeProfit())/pmod,1)+" pips");  


      
      ObjectCreate(tickets[i]+"-open",OBJ_HLINE,0,0,OrderOpenPrice());
      ObjectSet(tickets[i]+"-open",OBJPROP_COLOR,OpenPriceColor);      
      ObjectSet(tickets[i]+"-open",OBJPROP_PRICE1,OrderOpenPrice());   
   }   
}

void RecalcByGFX(int &tickets[],double &sl[], double &tp[]){
   for(int i=ArraySize(tickets)-1;i>=0;i--){
      OrderSelect(tickets[i],SELECT_BY_TICKET);
      double gfx.tp,gfx.sl;
      gfx.tp=NormalizeDouble(ObjectGet(tickets[i]+"-tp",OBJPROP_PRICE1),Digits);
      if(gfx.tp!=OrderTakeProfit()){
         if(gfx.tp!=tp[i]){
            if(OrderModify(OrderTicket(),OrderOpenPrice(),OrderStopLoss(),NormalizeDouble(gfx.tp,Digits),0,CLR_NONE)){
               tp[i]=gfx.tp;
               ObjectSet(tickets[i]+"-tp_l",OBJPROP_TIME1,OrderOpenTime());
               ObjectSet(tickets[i]+"-tp_l",OBJPROP_PRICE1,gfx.tp);
               ObjectSet(tickets[i]+"-tp_l",OBJPROP_COLOR,TakeprofitColor);  
               ObjectSetText(tickets[i]+"-tp_l","[TP]"+OrderTicket()+"  for "+DoubleToStr(MathAbs(OrderOpenPrice()-gfx.tp)/pmod,1)+" pips");  

            }
         }else{
            ObjectSet(tickets[i]+"-tp",OBJPROP_PRICE1,OrderTakeProfit());
            ObjectSet(tickets[i]+"-tp_l",OBJPROP_TIME1,OrderOpenTime());
            ObjectSet(tickets[i]+"-tp_l",OBJPROP_PRICE1,OrderTakeProfit());
            ObjectSet(tickets[i]+"-tp_l",OBJPROP_COLOR,TakeprofitColor);  
            ObjectSetText(tickets[i]+"-tp_l","[TP]"+OrderTicket()+"  for "+DoubleToStr(MathAbs(OrderOpenPrice()-OrderTakeProfit())/pmod,1)+" pips");  

            tp[i]=gfx.tp;               
         }
      }

      gfx.sl=NormalizeDouble(ObjectGet(tickets[i]+"-sl",OBJPROP_PRICE1),Digits);
      if(gfx.sl!=OrderStopLoss()){
         if(gfx.sl!=sl[i]){
            if(OrderModify(OrderTicket(),OrderOpenPrice(),NormalizeDouble(gfx.sl,Digits),OrderTakeProfit(),0,CLR_NONE)){
               sl[i]=gfx.sl;
               ObjectSet(tickets[i]+"-sl_l",OBJPROP_TIME1,OrderOpenTime());
               ObjectSet(tickets[i]+"-sl_l",OBJPROP_PRICE1,gfx.sl);
               ObjectSet(tickets[i]+"-sl_l",OBJPROP_COLOR,StoplossColor);  
               ObjectSetText(tickets[i]+"-sl_l","[SL]"+OrderTicket()+"  for "+DoubleToStr(MathAbs(OrderOpenPrice()-gfx.sl)/pmod,1)+" pips");  

            }
         }else{
            ObjectSet(tickets[i]+"-sl",OBJPROP_PRICE1,OrderStopLoss());
            ObjectSet(tickets[i]+"-sl_l",OBJPROP_TIME1,OrderOpenTime());
            ObjectSet(tickets[i]+"-sl_l",OBJPROP_PRICE1,OrderStopLoss());
            ObjectSet(tickets[i]+"-sl_l",OBJPROP_COLOR,StoplossColor);  
            ObjectSetText(tickets[i]+"-sl_l","[SL]"+OrderTicket()+"  for "+DoubleToStr(MathAbs(OrderOpenPrice()-OrderStopLoss())/pmod,1)+" pips");  

            sl[i]=gfx.sl;               
         }
      }

   }   
}



void OrderManager(int mode){
   static int tickets[];
   static double tp[];
   static double sl[];
   string prefix="";
   if(IsTesting()) prefix="";
   if(GlobalVariableCheck(prefix+"NewTrade")){
      int nt_mode=(GlobalVariableGet(prefix+"NewTrade_mode"));
      double nt_lot=(GlobalVariableGet(prefix+"NewTrade_lot"));
      int nt_tp=(GlobalVariableGet(prefix+"NewTrade_tp"));
      int nt_sl=(GlobalVariableGet(prefix+"NewTrade_sl"));   
      double pmod=1;
      if(Digits==5 || Digits==3) pmod=10;
      double nt_slippage=0;
      double nt_stoploss=0;
      double nt_takeprofit=0;
      double nt_op;
      switch(nt_mode){
         case OP_BUY:{
            nt_op=Ask;
            nt_stoploss=nt_op-(nt_sl*Point*pmod);
            nt_takeprofit=nt_op+(nt_tp*Point*pmod);
            break;
         }
         case OP_SELL:{
            nt_op=Bid;
            nt_stoploss=nt_op+(nt_sl*Point*pmod);
            nt_takeprofit=nt_op-(nt_tp*Point*pmod);
            break;
         }
      }
      Print(nt_lot);
      int err=OrderSend(Symbol(),nt_mode,NormalizeDouble(nt_lot,2),nt_op,nt_slippage,NormalizeDouble(nt_stoploss,Digits),NormalizeDouble(nt_takeprofit,Digits),"when-money-makes-money.com",0,0,CLR_NONE);         
      /*if(err>=0)*/ GlobalVariableDel(prefix+"NewTrade");
   }

   
   if(mode==MODE_DEINIT){
      DeleteGFX(tickets);
   }else{
      if(mode==MODE_INIT){
         ArrayResize(tickets,0);
      }
   
      int i=0;
      int OpenOrdersOnSymbol=0;
      for(i=OrdersTotal()-1;i>=0;i--){
         OrderSelect(i,SELECT_BY_POS,MODE_TRADES);
         if(OrderSymbol()==Symbol()){
            OpenOrdersOnSymbol++;
         }
      }
      if(OpenOrdersOnSymbol!=ArraySize(tickets)){
         DeleteGFX(tickets);
         ArrayResize(tickets,OpenOrdersOnSymbol);
         ArrayResize(tp,OpenOrdersOnSymbol);
         ArrayResize(sl,OpenOrdersOnSymbol);
         int j=0;
         for(i=OrdersTotal()-1;i>=0;i--){
            OrderSelect(i,SELECT_BY_POS);
            if(OrderSymbol()==Symbol()){
               tickets[j]=OrderTicket();
               sl[j]=OrderStopLoss();
               tp[j]=OrderStopLoss();
               j++;
            }
         }
         UpdateGFX(tickets);
      }else{
         RecalcByGFX(tickets,tp,sl);
      }
   }
   Comment("NR of orders: "+ArraySize(tickets)+"\nwww.when-money-makes-money.com");
}

int init(){
   ObjectCreate("logo",OBJ_LABEL,0,0,0);
   ObjectSetText("logo","when-money-makes-money.com",20);
   ObjectSet("logo",OBJPROP_XDISTANCE,0);
   ObjectSet("logo",OBJPROP_YDISTANCE,30);
   if(Digits==5||Digits==3){
      pmod=Point*10;
   }else{
      pmod=Point;
   }
   OrderManager(MODE_INIT);
      GlobalVariableDel("NewTrade");
      GlobalVariableDel("NewTrade_mode");
      GlobalVariableDel("NewTrade_lot");
      GlobalVariableDel("NewTrade_tp");
      GlobalVariableDel("NewTrade_sl");  
   return(0);
}

color lc[]={Yellow,Gold,Orange,DarkOrange,OrangeRed,Red,OrangeRed,DarkOrange,Orange,Gold};
int currc=0;
int start()
{
   if(IsTesting()){
   OrderManager(MODE_NORMAL);
   }
   ObjectSet("logo",OBJPROP_COLOR,lc[currc]);
   currc++;
   if(currc>=ArraySize(lc))currc=0;
   return(0);
}

int deinit(){
   OrderManager(MODE_DEINIT);
      GlobalVariableDel("NewTrade");
      GlobalVariableDel("NewTrade_mode");
      GlobalVariableDel("NewTrade_lot");
      GlobalVariableDel("NewTrade_tp");
      GlobalVariableDel("NewTrade_sl");   
   return(0);
}
//+------------------------------------------------------------------+