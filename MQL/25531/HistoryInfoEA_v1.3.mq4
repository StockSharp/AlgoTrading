//=========================================================================================================================================================================================================================//
#property copyright   "Copyright 2016-2020, Nikolaos Pantzos"
#property link        "https://www.mql5.com/en/users/pannik"
#property version     "1.3"
#property description "\nIt's a history orders' information tool."
#property description "\nPlace Magic Number Or Comment or Symbol, in the corresponding position, Of Orders You Want To Count."
//#property icon        "\\Images\\HistoryInfo-Logo.ico";
#property strict
//=========================================================================================================================================================================================================================//
enum By {Count_By_MagicNumber, Count_By_Comment, Count_By_Symbol};
//=========================================================================================================================================================================================================================//
extern By     Lineament   = Count_By_Symbol;//Select Lineament Orders
extern int    MagicNumber = 0;//Add Magic Number To Count Orders
extern string OrdersComm  = "OrdersComment";//Add Comment To Count Orders
extern string OrdersPair  = "OrdersSymbol";//Add Pair To Count Orders
//=========================================================================================================================================================================================================================//
int MultiplierPoint;
string BackgroundName;
int CommentLen;
//=========================================================================================================================================================================================================================//
int OnInit()
  {
//------------------------------------------------------
//Background
   BackgroundName="Background-"+WindowExpertName();
//---
   if(ObjectFind(BackgroundName)==-1)
     {
      ObjectCreate(BackgroundName,OBJ_LABEL,0,0,0);
      ObjectSet(BackgroundName,OBJPROP_CORNER,0);
      ObjectSet(BackgroundName,OBJPROP_BACK,FALSE);
      ObjectSet(BackgroundName,OBJPROP_YDISTANCE,14);
      ObjectSet(BackgroundName,OBJPROP_XDISTANCE,0);
      ObjectSetText(BackgroundName,"g",120,"Webdings",clrDarkBlue);
     }
//------------------------------------------------------
//Get len of order's comment
   CommentLen=StringLen(OrdersComm);
//------------------------------------------------------
//Calculate for 4 or 5 digits broker
   MultiplierPoint=1;
//---
   if((MarketInfo(Symbol(),MODE_DIGITS)==3) || (MarketInfo(Symbol(),MODE_DIGITS)==5))
      MultiplierPoint=10;
//------------------------------------------------------
   OnTick();
   return(INIT_SUCCEEDED);
  }
//=========================================================================================================================================================================================================================//
void OnDeinit(const int reason)
  {
//----
   ObjectDelete(BackgroundName);
   Comment("");
//----
  }
//=========================================================================================================================================================================================================================//
void OnTick()
  {
   string Comments;
   string MagicNo;
   string Pairs;
   string FirstOrderStr;
   string LastOrderStr;
   double TotalProfit=0;
   double TotalPips=0;
   double TotalLots=0;
   int TotalOrders=0;
   datetime FirstOrder=TimeCurrent();
   datetime LastOrder=0;
//---
   for(int i=0; i<OrdersHistoryTotal(); i++)
     {
      if(OrderSelect(i,SELECT_BY_POS,MODE_HISTORY))
        {
         if(((OrderMagicNumber()==MagicNumber)&&(Lineament==0))||((StringSubstr(OrderComment(),0,CommentLen)==OrdersComm)&&(Lineament==1))||((OrderSymbol()==OrdersPair)&&(Lineament==2)))
           {
            if(OrderOpenPrice()!=0.0)
              {
               FirstOrder=MathMin(FirstOrder,OrderOpenTime());
               LastOrder=MathMax(LastOrder,OrderOpenTime());
               TotalOrders++;
               TotalProfit+=OrderProfit()+OrderCommission()+OrderSwap();
               TotalLots+=OrderLots();
               if(MarketInfo(OrderSymbol(),MODE_POINT)!=0)
                 {
                  if(OrderType()==OP_BUY)
                     TotalPips+=(OrderClosePrice()-OrderOpenPrice())/(MarketInfo(OrderSymbol(),MODE_POINT)*MultiplierPoint);
                  if(OrderType()==OP_SELL)
                     TotalPips+=(OrderOpenPrice()-OrderClosePrice())/(MarketInfo(OrderSymbol(),MODE_POINT)*MultiplierPoint);
                 }
               else
                 {
                  if(OrderType()==OP_BUY)
                     TotalPips+=(OrderClosePrice()-OrderOpenPrice())/MultiplierPoint;
                  if(OrderType()==OP_SELL)
                     TotalPips+=(OrderOpenPrice()-OrderClosePrice())/MultiplierPoint;
                 }
              }
           }
        }
     }
//----
   if(FirstOrder==TimeCurrent())
      FirstOrderStr="EMPTY VALUE";
   if(LastOrder==0)
      LastOrderStr="EMPTY VALUE";
   if(FirstOrder!=TimeCurrent())
      FirstOrderStr=TimeToStr(FirstOrder,TIME_DATE)+" || "+TimeToStr(FirstOrder,TIME_MINUTES);
   if(LastOrder!=0)
      LastOrderStr=TimeToStr(LastOrder,TIME_DATE)+" || "+TimeToStr(LastOrder,TIME_MINUTES);
//----
   if(MagicNumber==0)
      MagicNo="Magic No: EMPTY VALUE";
   else
      MagicNo="Magic No  Calculate: "+IntegerToString(MagicNumber);
   if(OrdersComm=="OrdersComment")
      Comments="Comment: EMPTY VALUE";
   else
      Comments="Comment  Calculate: "+OrdersComm;
   if(OrdersPair=="OrdersSymbol")
      Pairs="Pair: EMPTY VALUE";
   else
      Pairs="Pair Calculate: "+OrdersPair;
//----
   Comment("======================"+
           "\n First Order: "+FirstOrderStr+
           "\n Last Order : "+LastOrderStr+
           "\n======================"+
           "\n "+MagicNo+
           "\n "+Comments+
           "\n "+Pairs+
           "\n======================"+
           "\n Total Lots    : "+DoubleToStr(TotalLots,2)+
           "\n Total Pips    : "+DoubleToStr(TotalPips,2)+
           "\n Total Profit  : "+DoubleToStr(TotalProfit,2)+
           "\n Total Orders: "+DoubleToStr(TotalOrders,0)+
           "\n======================");
//----
  }
//=========================================================================================================================================================================================================================//
