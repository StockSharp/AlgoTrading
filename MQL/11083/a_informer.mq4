//+------------------------------------------------------------------+
//|                                                   a_informer.mq4 |
//|                                         Copyright © 2010, Elmare |
//|                                        http://elmare.webnode.ru  |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2012, Elmare"
#property link      "http://elmare.webnode.ru"



color ordcolor[]={Olive,DodgerBlue,Orange,Green,Yellow,Gray,Chocolate,Blue,Lime,White,Red};
double dstop=0; // stop for current order (USD)
string sdstop="0"; // stop for current order (USD)(string)
double sl=0; // Stop loss level
double tp=0; // Take profit level
double psl=0; // Stop loss in points
double ptp=0; // Take profit in points

double op=0; // Open price level
int corner=3; // informer corner
int yoffset=30; // Y axe offset
int yshift=20;  // Y shift between lines
int xoffset=80; // Left column X axe offset
int xoffset2=10; // Right column X axe offset
int yk=0; // Y offset factor




int ot=0; // Order ticket
int i=0; //var for counters
double profUSD=0; //Profit in USD
double profPTS=0; //Profit in points
string sprofUSD="";//String of Profit in USD
string sprofPTS="";//String of Profit in points
color pcol=White;


extern int stop=300;// Stop to exe
extern int take=1000;// Take to exe
extern string symbol = "EURUSD";
int ord=1;
int slip=5; //Slippage
string silots="";
string siTP="";
string sstop="";
string stake="";
string name1="";
string name2="";
string st="";
int ordtot=0;
string tmp="";
extern int lotdecimals=2;
extern int ppr = 0;
int dk = 0;





//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
int init()
  {


if (Digits == 5) dk = 10000;
else if(Digits == 3) dk = 100;
start(); 

//----
   return(0);
  }
//+------------------------------------------------------------------+
//| expert deinitialization function                                 |
//+------------------------------------------------------------------+
int deinit()
  {
//----
   ObjectDelete("Buy"); 
   ObjectDelete("Sell");
   ObjectDelete("Order1");
   ObjectDelete("Lots");
   ObjectDelete("Profit");
   ObjectDelete("Profit2");
   ObjectDelete("Stop");
   ObjectDelete("Stopv"); 
   ObjectDelete("Take");
   ObjectDelete("Takev");
   ObjectDelete("Exe");
   ObjectDelete("Close");
   ObjectDelete("Sep");
   
   ObjectDelete("iStopv");
   ObjectDelete("iStop");
   ObjectDelete("iTPcount"); 
   ObjectDelete("iTP");
   ObjectDelete("iProfit");
   ObjectDelete("iLots");
   
   
   
   JunkObjects("OI");
   JunkObjects("SL");
   JunkObjects("OL");
   JunkObjects("OT");
   JunkObjects("OI");
   JunkObjects("OI");
   JunkObjects("SL");
   JunkObjects("OL");
   JunkObjects("OT");
   JunkObjects("OI");
   JunkObjects("OI");
   JunkObjects("SL");
   JunkObjects("OL");
   JunkObjects("OT");
   JunkObjects("OI");
   JunkObjects("OI");
   JunkObjects("SL");
   JunkObjects("OL");
   JunkObjects("OT");
   JunkObjects("OI");
   JunkObjects("TL");
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| expert start function                                            |
//+------------------------------------------------------------------+
int start()
  {
 



while(true)
{

RefreshRates();
WindowRedraw();

JunkObjects("#");


ModStopIfSL();//Stop loss modifuying if Stop line dragging


   /* if orders */
ordtot=OrdersTotal(); 




if(ordtot>0)
{
 
   for(i=0;i<OrdersTotal();i++)
   {
   OrderSelect(i,SELECT_BY_POS,MODE_TRADES);

if(OrderSymbol()== symbol)
{
   
   sl=OrderStopLoss();
   tp=OrderTakeProfit();
   op=OrderOpenPrice();
   ot=OrderTicket();
   if(OrderStopLoss()==0) CreateStopTake(ot,stop,take);
   if(OrderType()==OP_BUY) profPTS=(Bid-OrderOpenPrice())*dk;
   else profPTS=(OrderOpenPrice()-Ask)*dk;
 
   
   profUSD=OrderProfit();
   if(profPTS>0) pcol=Green; else pcol=Red;
   sprofPTS=DoubleToStr(profPTS,1);
   sprofUSD=DoubleToStr(profUSD,2);
  
   CreateSL(sl,ot,i);
   CreateTL(tp,ot,i);
   CreateOL(op,ot,i);
   CreateOI(ot);
   CreateOI2(ot);
   
     ord=OrdersTotal()+1;
 
   name1="OI_"+DoubleToStr(ot,0);
   if(OrderType()==OP_BUY)
   {
   //tmp=OrderSymbol();
    tmp="Bought";
    psl=(op-sl)*dk;
    ptp=(tp-op)*dk;

   }
   if(OrderType()==OP_SELL)
   {

    tmp="Sold";
    psl=(sl-op)*dk;
    ptp=(op-tp)*dk;

   }

   string f="";
   if (ptp<100) f="  "; else f="";
   st = tmp+" "+DoubleToStr(OrderLots(),lotdecimals)+": "+"          "+DoubleToStr(psl,0)+" / "+f+DoubleToStr(ptp,0);
   ObjectSet(name1,OBJPROP_CORNER,corner);
   ObjectSet(name1,OBJPROP_XDISTANCE,xoffset2);
   ObjectSet(name1,OBJPROP_YDISTANCE,yoffset*1+yshift*i);
   ObjectSetText(name1,st,10,"Microsoft Sans Serif",ordcolor[i]);
   

   name2="OI2_"+DoubleToStr(ot,0);
   st=" "+sprofPTS+"  "; 
   ObjectSet(name2,OBJPROP_CORNER,corner);
   ObjectSet(name2,OBJPROP_XDISTANCE,xoffset2+65+ppr);
   ObjectSet(name2,OBJPROP_YDISTANCE,yoffset*1+yshift*i);
   ObjectSetText(name2,st,10,"Microsoft Sans Serif",pcol);
 

   }
   }


}
  
   /* end if orders */
 /* begin of no orders */  
if(OrdersTotal()==0)
{

   JunkObjects("SL");
   JunkObjects("OL");
   JunkObjects("OT");
   JunkObjects("OI");
   JunkObjects("TL");
 }

     /* End of no orders */


        Sleep(500);            // Небольшая пауза

     }
//----


   return(0);
  }
//FUNCTION DEFINITION


  
void CreateSL(double sl,int ot,int i)
{
      color oc=ordcolor[i];
      string name="SL_"+DoubleToStr(ot,0);

      ObjectCreate(name,OBJ_HLINE,0,0,0);
      ObjectSet(name,OBJPROP_PRICE1,sl);
      ObjectSet(name,OBJPROP_COLOR,oc);
      ObjectSet(name,OBJPROP_STYLE,4);
      ObjectSet(name,OBJPROP_WIDTH,1);

}
void CreateTL(double tp,int ot,int i)
{
      color oc=ordcolor[i];
      string name="TL_"+DoubleToStr(ot,0);

      ObjectCreate(name,OBJ_HLINE,0,0,0);
      ObjectSet(name,OBJPROP_PRICE1,tp);
      ObjectSet(name,OBJPROP_COLOR,oc);
      ObjectSet(name,OBJPROP_STYLE,3);
      ObjectSet(name,OBJPROP_WIDTH,1);

}

void CreateOL(double op,int ot,int i)
{
      color oc=ordcolor[i];
      string name="OL_"+DoubleToStr(ot,0);
      ObjectCreate(name,OBJ_HLINE,0,0,0);
      ObjectSet(name,OBJPROP_PRICE1,op);
      ObjectSet(name,OBJPROP_COLOR,oc);
      ObjectSet(name,OBJPROP_STYLE,0);
      ObjectSet(name,OBJPROP_WIDTH,1);

}


void CreateOI(int ot)
{
      string name="OI_"+DoubleToStr(ot,0);
      ObjectCreate(name,OBJ_LABEL,0,0,0);
     
}
void CreateOI2(int ot)
{
      string name="OI2_"+DoubleToStr(ot,0);
      ObjectCreate(name,OBJ_LABEL,0,0,0);
     
}



void CreateStopTake(int ot,double stop,double take)
{
     double sl=0;
     double tp=0;
     if(OrderType()==OP_BUY)
     {

        sl=OrderOpenPrice()-stop*Point;
        tp=OrderOpenPrice()+take*Point;
     
        
        OrderModify(ot,OrderOpenPrice(),sl,tp,0,0);
     }
     if(OrderType()==OP_SELL)
     {
        sl=OrderOpenPrice()+stop*Point;
        tp=OrderOpenPrice()-take*Point;

        OrderModify(ot,OrderOpenPrice(),sl,tp,0,0);
     }

}

void ModStopIfSL()
{
   string name = "";
   int objtot=ObjectsTotal();
   string sorder;
   int order;
   double stopl;
   double takep;
   double takepx;
   double takepy;
   for(i=0;i<objtot;i++)
   {
      name = ObjectName(i);
      int td=StringLen(name);
      if(StringFind(name,"SL",0)!=-1)
      {
         sorder=StringSubstr(name,3,0);
         order=StrToDouble(sorder);
         OrderSelect(order,SELECT_BY_TICKET,MODE_TRADES);
         ObjectSet(name,OBJPROP_PRICE1,OrderStopLoss());
         stopl=ObjectGet(name,OBJPROP_PRICE1);
         OrderModify(order,OrderOpenPrice(),stopl,OrderTakeProfit(),0,0);
      }
      if(StringFind(name,"TL",0)!=-1)
      {
         sorder=StringSubstr(name,3,0);
         order=StrToDouble(sorder);
         OrderSelect(order,SELECT_BY_TICKET,MODE_TRADES);
         ObjectSet(name,OBJPROP_PRICE1,OrderTakeProfit());
         takep=ObjectGet(name,OBJPROP_PRICE1);
         OrderModify(order,OrderOpenPrice(),OrderStopLoss(),takep,0,0);
      }
      if(StringFind(name,"OI",0)!=-1)
      {
         sorder=StringSubstr(name,3,0);
         order=StrToDouble(sorder);
         OrderSelect(order,SELECT_BY_TICKET,MODE_TRADES);
         ObjectSet(name,OBJPROP_PRICE1,OrderTakeProfit());
         takepx=ObjectGet(name,OBJPROP_XDISTANCE);
         takepy=ObjectGet(name,OBJPROP_YDISTANCE);
         //if((takepy<yexe+25)&&(takepy>yexe)&&(takepx>xexe)&&(xsell<xexe+150))
         if(takepx>200)
         {
            double price=0;
            if(OrderType()==OP_BUY) price=Bid;
            else            price=Ask;
            OrderClose(order,OrderLots(),price,3,0);
            JunkObjects(sorder);
         }

      }
      
      
   }

}

void JunkObjects(string substr)
{
  int    obj_total=ObjectsTotal();
  string name;
  for(int i=0;i<obj_total;i++)
    {
     name=ObjectName(i);
     if (StringFind(name,substr,0)!=-1) ObjectDelete(name);
    }
}









    