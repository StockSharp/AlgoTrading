//+------------------------------------------------------------------+
//|                               Copyright � 2016, ������� �������� |
//|                                                cmillion@narod.ru |
//+------------------------------------------------------------------+
#property copyright "Copyright � 2016, http://cmillion.ru"
#property link      "cmillion@narod.ru"
#property strict
#property description "�������� ���������� ������ ����� ����������� ����� ��������� ����������. 1��� ����� - �������, 1 ��� ���� - ��������"
#property description "����� ������� ���������� ����, ������� �� ���������� ������ � ������� ������ ��������� ��� ������� ������� �� ���������� ������ ���������."
#property description "�������� ������������������, ������� ��� ������������ ������ ���������� ������ � ������ ������������. ����������� ��� ������� ��������� �� �����"
//--------------------------------------------------------------------
extern bool    buy                  = true;        //��������� buy 
extern bool    sell                 = true;        //��������� sell 
extern int     StepB                = 10;          //��� Buy �������
extern int     StepS                = 10;          //��� Sell �������
extern double  CloseProfitB         = 100;           //��������� buy �� ���������� �������
extern double  CloseProfitS         = 100;           //��������� sell �� ���������� �������
extern double  CloseProfit          = 10;           //��������� ��� �� ���������� �������
extern double  LotB                 = 0.10;        //����� Buy ������� 
extern double  LotS                 = 0.10;        //����� Sell ������� 
extern int     slippage             = 5;          // ���������������
extern int     Magic                = 1;
//--------------------------------------------------------------------
double STOPLEVEL;
double Level;
string val,GV_kn_CB,GV_kn_CS,GV_kn_CA,GV_CPB,GV_CPS,GV_CPA,GV_kn_B,GV_kn_S,GV_kn_A,GV_LB,GV_LS,GV_StB,GV_StS;
bool LANGUAGE;
//-------------------------------------------------------------------- 
int OnInit()
{ 
   LANGUAGE=TerminalInfoString(TERMINAL_LANGUAGE)=="Russian";
   if (IsTesting()) ObjectsDeleteAll(0);
   int AN=AccountNumber();
   string GVn=StringConcatenate("cm fishing ",AN," ",Symbol());
   
   Level=Bid;
   val = " "+AccountCurrency();
   RectLabelCreate(0,"rl BalanceW",0,195,20,195,90);
   DrawLABEL("IsTradeAllowed",Text(LANGUAGE,"��������","Trade"),100,30,clrRed,ANCHOR_CENTER);
   RectLabelCreate(0,"rl Close Profit",0,195,103,195,90);
   DrawLABEL("rl CloseProfit",Text(LANGUAGE,"�������� �� �������","Closing profit"),100,115,clrBlack,ANCHOR_CENTER);
   ButtonCreate(0,"kn close Buy" , 0,130,125,40,20,"X buy");
   ButtonCreate(0,"kn close Sell" ,0,130,147,40,20,"X sell");
   ButtonCreate(0,"kn close All",0,130,169,40,20,Text(LANGUAGE,"����.","X all"));

   ButtonCreate(0,"kn Buy Auto" , 0,40,125,35,20,Text(LANGUAGE,"����","auto"));
   ButtonCreate(0,"kn Sell Auto" ,0,40,147,35,20,Text(LANGUAGE,"����","auto"));
   ButtonCreate(0,"kn All Auto",0,40,169,35,20,Text(LANGUAGE,"����","auto"));
   
   GV_kn_CB=StringConcatenate(GVn," Close Buy Auto");
   if (GlobalVariableCheck(GV_kn_CB)) ObjectSetInteger(0,"kn Buy Auto",OBJPROP_STATE,true);
   GV_kn_CS=StringConcatenate(GVn," Close Sell Auto");
   if (GlobalVariableCheck(GV_kn_CS)) ObjectSetInteger(0,"kn Sell Auto",OBJPROP_STATE,true);
   GV_kn_CA=StringConcatenate(GVn," Close All Auto");
   if (GlobalVariableCheck(GV_kn_CA)) ObjectSetInteger(0,"kn All Auto",OBJPROP_STATE,true);


   GV_CPB=StringConcatenate(GVn," Close Profit Buy");
   if (GlobalVariableCheck(GV_CPB)) CloseProfitB = GlobalVariableGet(GV_CPB);
   
   GV_CPS=StringConcatenate(GVn," Close Profit Sell");
   if (GlobalVariableCheck(GV_CPS)) CloseProfitS = GlobalVariableGet(GV_CPS);
   
   GV_CPA=StringConcatenate(GVn," Close Profit All");
   if (GlobalVariableCheck(GV_CPA)) CloseProfit = GlobalVariableGet(GV_CPA);
   
   EditCreate(0,"rl Buy Auto"  ,0,90,125,50,20,DoubleToString(CloseProfitB,2),"Arial",8,ALIGN_CENTER,false);
   EditCreate(0,"rl Sell Auto" ,0,90,147,50,20,DoubleToString(CloseProfitS,2),"Arial",8,ALIGN_CENTER,false);
   EditCreate(0,"rl All Auto",0,90,169,50,20,DoubleToString(CloseProfit,2) ,"Arial",8,ALIGN_CENTER,false);

   ButtonCreate(0,"kn Clear",0,75,25,70,20,Text(LANGUAGE,"�������","Clear") ,"Times New Roman",8, clrBlack,clrGray,clrLightGray,clrNONE,false,CORNER_RIGHT_LOWER);
   RectLabelCreate(0,"rl Buy",0,190,125,60,20);
   RectLabelCreate(0,"rl Sell",0,190,147,60,20);
   RectLabelCreate(0,"rl All",0,190,169,60,20);

   DrawLABEL("rl Balance",Text(LANGUAGE,"������","Balance"),190,50,clrBlack,ANCHOR_LEFT);
   DrawLABEL("rl Equity",Text(LANGUAGE,"������","Equity"),190,70,clrBlack,ANCHOR_LEFT);
   DrawLABEL("rl FreeMargin",Text(LANGUAGE,"��������","FreeMargin"),190,90,clrBlack,ANCHOR_LEFT);
   
   DrawLABEL("rl val Balance",val,5,50,clrBlack);
   DrawLABEL("rl val Equity",val,5,70,clrBlack);
   DrawLABEL("rl val FreeMargin",val,5,90,clrBlack);
   
   int Y=192;
   RectLabelCreate(0,"rl Step Lot",0,195,Y,195,90);Y+=15;
   DrawLABEL("rl StepLot ",Text(LANGUAGE,"��������� ���� � ����","Settings"),100,Y,clrBlack,ANCHOR_CENTER);Y+=20;
   DrawLABEL("rl Step ",Text(LANGUAGE,"���","Step"),120,Y,clrBlack,ANCHOR_CENTER);
   DrawLABEL("rl ��� ",Text(LANGUAGE,"���","Lot"),170,Y,clrBlack,ANCHOR_CENTER);Y+=10;
   
   GV_LB=StringConcatenate(GVn," Lot Buy");
   if (GlobalVariableCheck(GV_LB)) LotB = GlobalVariableGet(GV_LB);
   GV_LS=StringConcatenate(GVn," Lot Sell");
   if (GlobalVariableCheck(GV_LS)) LotS = GlobalVariableGet(GV_LS);
   GV_StB=StringConcatenate(GVn," Step Buy");
   if (GlobalVariableCheck(GV_StB)) StepB = (int)GlobalVariableGet(GV_StB);
   GV_StS=StringConcatenate(GVn," Step Sell");
   if (GlobalVariableCheck(GV_StS)) StepS = (int)GlobalVariableGet(GV_StS);
   
   EditCreate(0,"rl Buy Step" ,0,139,Y,40,20,IntegerToString(StepB),"Arial",8,ALIGN_CENTER,false);
   EditCreate(0,"rl Buy Lot"  ,0,190,Y,40,20,DoubleToString(LotB,2),"Arial",8,ALIGN_CENTER,false);
   ButtonCreate(0,"kn open Buy" , 0,85,Y,80,20,Text(LANGUAGE,"������","Open Buy"));Y+=20;
   EditCreate(0,"rl Sell Step",0,139,Y,40,20,IntegerToString(StepS),"Arial",8,ALIGN_CENTER,false);
   EditCreate(0,"rl Sell Lot" ,0,190,Y,40,20,DoubleToString(LotS,2),"Arial",8,ALIGN_CENTER,false);
   ButtonCreate(0,"kn open Sell" ,0,85,Y,80,20,Text(LANGUAGE,"�������","Open Sell"));
   GV_kn_B=StringConcatenate(GVn," Buy");
   if (GlobalVariableCheck(GV_kn_B)) buy = GlobalVariableGet(GV_kn_B); else GlobalVariableSet(GV_kn_B,buy);
   
   GV_kn_S=StringConcatenate(GVn," Sell");
   if (GlobalVariableCheck(GV_kn_S)) sell = GlobalVariableGet(GV_kn_S); else GlobalVariableSet(GV_kn_S,sell);
   
   ObjectSetInteger(0,"kn open Buy",OBJPROP_STATE,buy);
   ObjectSetInteger(0,"kn open Sell",OBJPROP_STATE,sell);
   

   return(INIT_SUCCEEDED);
}
//-------------------------------------------------------------------
void OnTick()
{
   if (!IsTradeAllowed()) 
   {
      DrawLABEL("IsTradeAllowed",Text(LANGUAGE,"�������� ���������","Trade is disabled"),100,30,clrRed,ANCHOR_CENTER);
      return;
   }
   else DrawLABEL("IsTradeAllowed",Text(LANGUAGE,"�������� ���������","Trade is enabled"),100,30,clrGreen,ANCHOR_CENTER);
   STOPLEVEL=MarketInfo(Symbol(),MODE_STOPLEVEL);
   LotB=StringToDouble(ObjectGetString(0,"rl Buy Lot",OBJPROP_TEXT));
   LotS=StringToDouble(ObjectGetString(0,"rl Sell Lot",OBJPROP_TEXT));
   StepB=(int)StringToInteger(ObjectGetString(0,"rl Buy Step",OBJPROP_TEXT));
   StepS=(int)StringToInteger(ObjectGetString(0,"rl Sell Step",OBJPROP_TEXT));

   if (LotB!=GlobalVariableGet(GV_LB)) GlobalVariableSet(GV_LB,LotB);
   if (LotS!=GlobalVariableGet(GV_LS)) GlobalVariableSet(GV_LS,LotS);
   if (StepB!=GlobalVariableGet(GV_StB)) GlobalVariableSet(GV_StB,StepB);
   if (StepS!=GlobalVariableGet(GV_StS)) GlobalVariableSet(GV_StS,StepS);

   double OOP,Profit=0,ProfitB=0,ProfitS=0;
   int i,b=0,s=0,tip;
   for (i=0; i<OrdersTotal(); i++)
   {    
      if (OrderSelect(i,SELECT_BY_POS,MODE_TRADES))
      { 
         if (OrderSymbol()==Symbol() && Magic==OrderMagicNumber())
         { 
            tip = OrderType(); 
            OOP = NormalizeDouble(OrderOpenPrice(),Digits);
            Profit=OrderProfit()+OrderSwap()+OrderCommission();
            if (tip==OP_BUY)             
            {  
               ProfitB+=Profit;
               b++; 
            }                                         
            if (tip==OP_SELL)        
            {
               ProfitS+=Profit;
               s++;
            } 
         }
      }
   } 
   Profit = ProfitB + ProfitS;
   DrawLABEL("Balance",DoubleToStr(AccountBalance(),2),40,50,clrBlack);
   DrawLABEL("Equity",DoubleToStr(AccountEquity(),2),40,70,clrBlack);
   DrawLABEL("FreeMargin",DoubleToStr(AccountFreeMargin(),2),40,90,clrBlack);
   DrawLABEL("Profit B",DoubleToStr(ProfitB,2),135,135,Color(ProfitB<0,clrRed,clrGreen),ANCHOR_RIGHT);
   DrawLABEL("Profit S",DoubleToStr(ProfitS,2),135,157,Color(ProfitS<0,clrRed,clrGreen),ANCHOR_RIGHT);
   DrawLABEL("Profit A",DoubleToStr(Profit,2) ,135,179,Color(Profit<0,clrRed,clrGreen),ANCHOR_RIGHT);
   //---
   if (ObjectGetInteger(0,"kn Clear",OBJPROP_STATE))
   {
      ObjectsDeleteAll(0,"#");
      ObjectsDeleteAll(0,OBJ_ARROW);
      ObjectsDeleteAll(0,OBJ_TREND);
      ObjectSetInteger(0,"kn Clear",OBJPROP_STATE,false);
   }
   if (b!=0 && ObjectGetInteger(0,"kn close Buy",OBJPROP_STATE))
   {
      if (!CloseAll(OP_BUY)) Print("Error OrderSend ",GetLastError());
      else ObjectSetInteger(0,"kn close Buy",OBJPROP_STATE,false);
   }
   //---
   if (s!=0 && ObjectGetInteger(0,"kn close Sell",OBJPROP_STATE))
   {
      if (!CloseAll(OP_SELL)) Print("Error OrderSend ",GetLastError());
      else ObjectSetInteger(0,"kn close Sell",OBJPROP_STATE,false);
   }
   //---
   if (s+b!=0 && ObjectGetInteger(0,"kn close All",OBJPROP_STATE))
   {
      if (!CloseAll(-1)) Print("Error OrderSend ",GetLastError());
      else ObjectSetInteger(0,"kn close All",OBJPROP_STATE,false);
   }
   //---
   if (ObjectGetInteger(0,"kn All Auto",OBJPROP_STATE)) 
   {
      if  (GlobalVariableGet(GV_kn_CA)==0) 
           GlobalVariableSet(GV_kn_CA,1);
      
      ObjectSetInteger(0,"rl All Auto",OBJPROP_COLOR,clrRed); 
      CloseProfit=StringToDouble(ObjectGetString(0,"rl All Auto",OBJPROP_TEXT));
      if  (GlobalVariableGet(GV_CPA)!=CloseProfit) GlobalVariableSet(GV_CPA,CloseProfit);
      if (Profit>=CloseProfit) 
      {
         CloseAll(-1);
         return;
      }
   } 
   else {ObjectSetInteger(0,"rl All Auto",OBJPROP_COLOR,clrLightGray); GlobalVariableDel(GV_kn_CA);}
   //---
   if (ObjectGetInteger(0,"kn Sell Auto",OBJPROP_STATE)) 
   {
      if  (GlobalVariableGet(GV_kn_CS)==0) 
           GlobalVariableSet(GV_kn_CS,1);
      
      ObjectSetInteger(0,"rl Sell Auto",OBJPROP_COLOR,clrRed); 
      CloseProfitS=StringToDouble(ObjectGetString(0,"rl Sell Auto",OBJPROP_TEXT));
      if  (GlobalVariableGet(GV_CPS)!=CloseProfitS) GlobalVariableSet(GV_CPS,CloseProfitS);
      if (ProfitS>=CloseProfitS) 
      {
         CloseAll(OP_SELL);
         return;
      }
   } 
   else {ObjectSetInteger(0,"rl Sell Auto",OBJPROP_COLOR,clrLightGray); GlobalVariableDel(GV_kn_CS);}
   //---
   if (ObjectGetInteger(0,"kn Buy Auto",OBJPROP_STATE)) 
   {
      if  (GlobalVariableGet(GV_kn_CB)==1) 
           GlobalVariableSet(GV_kn_CB,1);
      
      ObjectSetInteger(0,"rl Buy Auto",OBJPROP_COLOR,clrRed); 
      CloseProfitB=StringToDouble(ObjectGetString(0,"rl Buy Auto",OBJPROP_TEXT));
      if  (GlobalVariableGet(GV_CPB)!=CloseProfitB) GlobalVariableSet(GV_CPB,CloseProfitB);
      if (ProfitB>=CloseProfitB) 
      {
         CloseAll(OP_BUY);
         return;
      }
   } 
   else {ObjectSetInteger(0,"rl Buy Auto",OBJPROP_COLOR,clrLightGray); GlobalVariableDel(GV_kn_CB);}
   //---
   if (buy!=ObjectGetInteger(0,"kn open Buy",OBJPROP_STATE))
   {
      buy=ObjectGetInteger(0,"kn open Buy",OBJPROP_STATE);
      if  (GlobalVariableGet(GV_kn_B)!=buy) GlobalVariableSet(GV_kn_B,buy);
   }
   if (buy)
   {
      ObjectSetInteger(0,"rl Buy Step",OBJPROP_COLOR,clrRed);
      ObjectSetInteger(0,"rl Buy Lot",OBJPROP_COLOR,clrRed);
   }
   else
   {
      ObjectSetInteger(0,"rl Buy Step",OBJPROP_COLOR,clrLightGray);  
      ObjectSetInteger(0,"rl Buy Lot",OBJPROP_COLOR,clrLightGray);  
   }
   //---
   if (sell!=ObjectGetInteger(0,"kn open Sell",OBJPROP_STATE))
   {
      sell=ObjectGetInteger(0,"kn open Sell",OBJPROP_STATE);
      if  (GlobalVariableGet(GV_kn_S)!=sell) GlobalVariableSet(GV_kn_S,sell);
   }
   if (sell)
   {
      ObjectSetInteger(0,"rl Sell Step",OBJPROP_COLOR,clrRed);
      ObjectSetInteger(0,"rl Sell Lot",OBJPROP_COLOR,clrRed);
   }
   else
   {
      ObjectSetInteger(0,"rl Sell Step",OBJPROP_COLOR,clrLightGray);  
      ObjectSetInteger(0,"rl Sell Lot",OBJPROP_COLOR,clrLightGray);  
   }
   //---
   if (Bid<=Level-StepB*Point)
   {
      if (buy && AccountFreeMarginCheck(Symbol(),OP_BUY,LotB)>0)
      {
         if (OrderSend(Symbol(),OP_BUY, LotB,NormalizeDouble(Ask,Digits),slippage,0,0,NULL,Magic,0,clrNONE)!=-1) Level=Bid;
         else Print("������ �������� ������ <<",Error(GetLastError()),">>  ");
      } 
      else Level=Bid;
   }
   if (Bid>=Level+StepS*Point)
   {
      if (sell && AccountFreeMarginCheck(Symbol(),OP_SELL,LotS)>0)
      {
         if (OrderSend(Symbol(),OP_SELL,LotS,NormalizeDouble(Bid,Digits),slippage,0,0,NULL,Magic,0,clrNONE)!=-1) Level=Bid;
         else Print("������ �������� ������ <<",Error(GetLastError()),">>  ");
      } 
      else Level=Bid;
   }
return;
}
//--------------------------------------------------------------------
color Color(bool P,color a,color b)
{
   if (P) return(a);
   else return(b);
}
//------------------------------------------------------------------
void DrawLABEL(string name, string Name, int X, int Y, color clr,ENUM_ANCHOR_POINT align=ANCHOR_RIGHT)
{
   if (ObjectFind(name)==-1)
   {
      ObjectCreate(name, OBJ_LABEL, 0, 0, 0);
      ObjectSet(name, OBJPROP_CORNER, 1);
      ObjectSet(name, OBJPROP_XDISTANCE, X);
      ObjectSet(name, OBJPROP_YDISTANCE, Y);
      ObjectSetInteger(0,name,OBJPROP_SELECTABLE,false);
      ObjectSetInteger(0,name,OBJPROP_SELECTED,false);
      ObjectSetInteger(0,name,OBJPROP_HIDDEN,true);
      ObjectSetInteger(0,name,OBJPROP_ANCHOR,align); 
   }
   ObjectSetText(name,Name,8,"Arial",clr);
}
//--------------------------------------------------------------------
void OnDeinit(const int reason)
{
   if (!IsTesting())
   {
      ObjectsDeleteAll(0,"Profit");
      ObjectsDeleteAll(0,"kn");
      ObjectsDeleteAll(0,"rl");
      ObjectsDeleteAll(0,"Balance");
      ObjectsDeleteAll(0,"Equity");
      ObjectsDeleteAll(0,"FreeMargin");
   }
   Comment("");
}
//+------------------------------------------------------------------+
bool CloseAll(int tip)
{
   bool error=true;
   int j,err,nn=0,OT;
   while(true)
   {
      for (j = OrdersTotal()-1; j >= 0; j--)
      {
         if (OrderSelect(j, SELECT_BY_POS))
         {
            if (OrderSymbol() == Symbol() && OrderMagicNumber() == Magic )
            {
               OT = OrderType();
               if (tip!=-1 && tip!=OT) continue;
               if (OT==OP_BUY) 
               {
                  error=OrderClose(OrderTicket(),OrderLots(),NormalizeDouble(Bid,Digits),slippage,Blue);
               }
               if (OT==OP_SELL) 
               {
                  error=OrderClose(OrderTicket(),OrderLots(),NormalizeDouble(Ask,Digits),slippage,Red);
               }
               if (!error) 
               {
                  err = GetLastError();
                  if (err<2) continue;
                  if (err==129) 
                  {
                     RefreshRates();
                     continue;
                  }
                  if (err==146) 
                  {
                     if (IsTradeContextBusy()) Sleep(2000);
                     continue;
                  }
                  Print("������ ",err," �������� ������ N ",OrderTicket(),"     ",TimeToStr(TimeCurrent(),TIME_SECONDS));
               }
            }
         }
      }
      int n=0;
      for (j = 0; j < OrdersTotal(); j++)
      {
         if (OrderSelect(j, SELECT_BY_POS))
         {
            if (OrderSymbol() == Symbol() && OrderMagicNumber() == Magic)
            {
               if (tip!=-1 && tip!=OrderType()) continue;
               n++;
            }
         }  
      }
      if (n==0) break;
      nn++;
      if (nn>10) 
      {
         Alert(Symbol()," �� ������� ������� ��� ������, �������� ��� ",n);
         return(0);
      }
      Sleep(1000);
      RefreshRates();
   }
   return(1);
}
//--------------------------------------------------------------------
bool ButtonCreate(const long              chart_ID=0,               // ID �������
                  const string            name="Button",            // ��� ������
                  const int               sub_window=0,             // ����� �������
                  const long               x=0,                      // ���������� �� ��� X
                  const long               y=0,                      // ���������� �� ��� Y
                  const int               width=50,                 // ������ ������
                  const int               height=18,                // ������ ������
                  const string            text="Button",            // �����
                  const string            font="Arial",             // �����
                  const int               font_size=8,             // ������ ������
                  const color             clr=clrBlack,               // ���� ������
                  const color             clrON=clrLightGray,            // ���� ����
                  const color             clrOFF=clrLightGray,          // ���� ����
                  const color             border_clr=clrNONE,       // ���� �������
                  const bool              state=false,       // ���� �������
                  const ENUM_BASE_CORNER  CORNER=CORNER_RIGHT_UPPER)
  {
   if (ObjectFind(chart_ID,name)==-1)
   {
      ObjectCreate(chart_ID,name,OBJ_BUTTON,sub_window,0,0);
      ObjectSetInteger(chart_ID,name,OBJPROP_XSIZE,width);
      ObjectSetInteger(chart_ID,name,OBJPROP_YSIZE,height);
      ObjectSetInteger(chart_ID,name,OBJPROP_CORNER,CORNER);
      ObjectSetString(chart_ID,name,OBJPROP_FONT,font);
      ObjectSetInteger(chart_ID,name,OBJPROP_FONTSIZE,font_size);
      ObjectSetInteger(chart_ID,name,OBJPROP_BACK,0);
      ObjectSetInteger(chart_ID,name,OBJPROP_SELECTABLE,0);
      ObjectSetInteger(chart_ID,name,OBJPROP_SELECTED,0);
      ObjectSetInteger(chart_ID,name,OBJPROP_HIDDEN,1);
      ObjectSetInteger(chart_ID,name,OBJPROP_ZORDER,1);
      ObjectSetInteger(chart_ID,name,OBJPROP_STATE,state);
   }
   ObjectSetInteger(chart_ID,name,OBJPROP_BORDER_COLOR,border_clr);
   color back_clr;
   if (ObjectGetInteger(chart_ID,name,OBJPROP_STATE)) back_clr=clrON; else back_clr=clrOFF;
   ObjectSetInteger(chart_ID,name,OBJPROP_BGCOLOR,back_clr);
   ObjectSetInteger(chart_ID,name,OBJPROP_COLOR,clr);
   ObjectSetString(chart_ID,name,OBJPROP_TEXT,text);
   ObjectSetInteger(chart_ID,name,OBJPROP_XDISTANCE,x);
   ObjectSetInteger(chart_ID,name,OBJPROP_YDISTANCE,y);
   return(true);
}
//--------------------------------------------------------------------
bool RectLabelCreate(const long             chart_ID=0,               // ID �������
                     const string           name="RectLabel",         // ��� �����
                     const int              sub_window=0,             // ����� �������
                     const long              x=0,                     // ���������� �� ��� X
                     const long              y=0,                     // ���������� �� ��� y
                     const int              width=50,                 // ������
                     const int              height=18,                // ������
                     const color            back_clr=clrWhite,        // ���� ����
                     const color            clr=clrBlack,             // ���� ������� ������� (Flat)
                     const ENUM_LINE_STYLE  style=STYLE_SOLID,        // ����� ������� �������
                     const int              line_width=1,             // ������� ������� �������
                     const bool             back=false,               // �� ������ �����
                     const bool             selection=false,          // �������� ��� �����������
                     const bool             hidden=true,              // ����� � ������ ��������
                     const long             z_order=0)                // ��������� �� ������� �����
  {
   ResetLastError();
   if (ObjectFind(chart_ID,name)==-1)
   {
      ObjectCreate(chart_ID,name,OBJ_RECTANGLE_LABEL,sub_window,0,0);
      ObjectSetInteger(chart_ID,name,OBJPROP_BORDER_TYPE,BORDER_FLAT);
      ObjectSetInteger(chart_ID,name,OBJPROP_CORNER,CORNER_RIGHT_UPPER);
      ObjectSetInteger(chart_ID,name,OBJPROP_STYLE,style);
      ObjectSetInteger(chart_ID,name,OBJPROP_WIDTH,line_width);
      ObjectSetInteger(chart_ID,name,OBJPROP_BACK,back);
      ObjectSetInteger(chart_ID,name,OBJPROP_SELECTABLE,selection);
      ObjectSetInteger(chart_ID,name,OBJPROP_SELECTED,selection);
      ObjectSetInteger(chart_ID,name,OBJPROP_HIDDEN,hidden);
      ObjectSetInteger(chart_ID,name,OBJPROP_ZORDER,z_order);
      //ObjectSetInteger(chart_ID,name,OBJPROP_ALIGN,ALIGN_RIGHT); 
   }
   ObjectSetInteger(chart_ID,name,OBJPROP_BGCOLOR,back_clr);
   ObjectSetInteger(chart_ID,name,OBJPROP_COLOR,clr);
   ObjectSetInteger(chart_ID,name,OBJPROP_XSIZE,width);
   ObjectSetInteger(chart_ID,name,OBJPROP_YSIZE,height);
   ObjectSetInteger(chart_ID,name,OBJPROP_XDISTANCE,x);
   ObjectSetInteger(chart_ID,name,OBJPROP_YDISTANCE,y);
   return(true);
}
//--------------------------------------------------------------------
string Error(int code)
{
   switch(code)
   {
      case 0:   return("��� ������");
      case 1:   return("��� ������, �� ��������� ����������");                            
      case 2:   return("����� ������");                                                   
      case 3:   return("������������ ���������");                                         
      case 4:   return("�������� ������ �����");                                          
      case 5:   return("������ ������ ����������� ���������");                            
      case 6:   return("��� ����� � �������� ��������");                                  
      case 7:   return("������������ ����");                                              
      case 8:   return("������� ������ �������");                                         
      case 9:   return("������������ �������� ���������� ���������������� �������");      
      case 64:  return("���� ������������");                                              
      case 65:  return("������������ ����� �����");                                       
      case 128: return("����� ���� �������� ���������� ������");                          
      case 129: return("������������ ����");                                              
      case 130: return("������������ �����");                                             
      case 131: return("������������ �����");                                             
      case 132: return("����� ������");                                                   
      case 133: return("�������� ���������");                                               
      case 134: return("������������ ����� ��� ���������� ��������");                     
      case 135: return("���� ����������");                                                
      case 136: return("��� ���");                                                        
      case 137: return("������ �����");                                                   
      case 138: return("����� ����");                                                     
      case 139: return("����� ������������ � ��� ��������������");                        
      case 140: return("��������� ������ �������");                                       
      case 141: return("������� ����� ��������");                                         
      case 145: return("����������� ���������, ��� ��� ����� ������� ������ � �����");    
      case 146: return("���������� �������� ������");                                     
      case 147: return("������������� ���� ��������� ������ ��������� ��������");         
      case 148: return("���������� �������� � ���������� ������� �������� �������, �������������� ��������.");
      default:   return(StringConcatenate("������ ",code," ���������� "));
   }
}
//--------------------------------------------------------------------
bool EditCreate(const long             chart_ID=0,               // ID ������� 
                const string           name="Edit",              // ��� ������� 
                const int              sub_window=0,             // ����� ������� 
                const int              x=0,                      // ���������� �� ��� X 
                const int              y=0,                      // ���������� �� ��� Y 
                const int              width=50,                 // ������ 
                const int              height=18,                // ������ 
                const string           text="Text",              // ����� 
                const string           font="Arial",             // ����� 
                const int              font_size=8,             // ������ ������ 
                const ENUM_ALIGN_MODE  align=ALIGN_RIGHT,       // ������ ������������ 
                const bool             read_only=true,           // ����������� ������������� 
                const ENUM_BASE_CORNER corner=CORNER_RIGHT_UPPER, // ���� ������� ��� �������� 
                const color            clr=clrBlack,             // ���� ������ 
                const color            back_clr=clrWhite,        // ���� ���� 
                const color            border_clr=clrNONE,       // ���� ������� 
                const bool             back=false,               // �� ������ ����� 
                const bool             selection=false,          // �������� ��� ����������� 
                const bool             hidden=true,              // ����� � ������ �������� 
                const long             z_order=0)                // ��������� �� ������� ����� 
  { 
   ResetLastError(); 
   if(!ObjectCreate(chart_ID,name,OBJ_EDIT,sub_window,0,0)) 
     { 
      Print(__FUNCTION__, 
            ": �� ������� ������� ������ ",name,"! ��� ������ = ",GetLastError()); 
      return(false); 
     } 
   ObjectSetInteger(chart_ID,name,OBJPROP_XDISTANCE,x); 
   ObjectSetInteger(chart_ID,name,OBJPROP_YDISTANCE,y); 
   ObjectSetInteger(chart_ID,name,OBJPROP_XSIZE,width); 
   ObjectSetInteger(chart_ID,name,OBJPROP_YSIZE,height); 
   ObjectSetString(chart_ID,name,OBJPROP_TEXT,text); 
   ObjectSetString(chart_ID,name,OBJPROP_FONT,font); 
   ObjectSetInteger(chart_ID,name,OBJPROP_FONTSIZE,font_size); 
   ObjectSetInteger(chart_ID,name,OBJPROP_ALIGN,align); 
   ObjectSetInteger(chart_ID,name,OBJPROP_READONLY,read_only); 
   ObjectSetInteger(chart_ID,name,OBJPROP_CORNER,corner); 
   ObjectSetInteger(chart_ID,name,OBJPROP_COLOR,clr); 
   ObjectSetInteger(chart_ID,name,OBJPROP_BGCOLOR,back_clr); 
   ObjectSetInteger(chart_ID,name,OBJPROP_BORDER_COLOR,border_clr); 
   ObjectSetInteger(chart_ID,name,OBJPROP_BACK,back); 
   ObjectSetInteger(chart_ID,name,OBJPROP_SELECTABLE,selection); 
   ObjectSetInteger(chart_ID,name,OBJPROP_SELECTED,selection); 
   ObjectSetInteger(chart_ID,name,OBJPROP_HIDDEN,hidden); 
   ObjectSetInteger(chart_ID,name,OBJPROP_ZORDER,z_order); 
   return(true); 
  } 
//+------------------------------------------------------------------+ 
string Text(bool P,string a,string b)
{
   if (P) return(a);
   else return(b);
}
//------------------------------------------------------------------
