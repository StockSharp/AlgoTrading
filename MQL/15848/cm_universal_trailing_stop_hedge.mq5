//+------------------------------------------------------------------+
#property copyright "Copyright � 2016, cmillion@narod.ru"
#property link      "http://cmillion.ru"
#property strict
#property description "�������� ���������� �������� � ������� �������� ���� ���������� ��������"
#property description "�� ������, �� ���������, �� ���������� ATR MA Parabolic, �� �������� ������� � ������ �� �������"
//--------------------------------------------------------------------
enum t
  {
   b=1,     // �� ����������� ������
   c=2,     // �� ���������
   d=3,     // �� ���������� ATR
   e=4,     // �� ���������� Parabolic
   f=5,     // �� ���������� ��
   g=6,     // % �� �������
   i=7,     // �� �������
  };
extern bool    VirtualTrailingStop=false;//����������� ������������
input t        parameters_trailing=1;      //����� �����

extern int     delta=0;     // ������ �� ���������� ������ ���������
extern ENUM_TIMEFRAMES TF_Tralling=0;      // ��������� ����������� (0-�������)

extern int     StepTrall=1;      // ��� ����������� ��������
extern int     StartTrall=1;      // ����������� ������� ����� � �������

color   text_color=clrGreen;     //���� ������ ����������

sinput string Advanced_Options="";

input int     period_ATR=14;//������ ATR (����� 3)

input double Step=0.02; //Parabolic Step (����� 4)
input double Maximum=0.2; //Parabolic Maximum (����� 4)
sinput  int     Magic= -1;//� ����� ������� ������� (-1 ���)
extern bool    GeneralNoLoss=true;   // ���� �� ����� ���������
extern bool    DrawArrow=false;   // ���������� ����� ��������� � ������

input int ma_period=34;//������ �� (����� 5)
input ENUM_MA_METHOD ma_method=MODE_SMA; // ����� ����������  (����� 5)
input ENUM_APPLIED_PRICE applied_price=PRICE_CLOSE;    // ��� ����  (����� 5)

input double PercetnProfit=50;//������� �� ������� (����� 6)

MqlTradeRequest request;  // ��������� ��������� �������
MqlTradeResult result;    // ��������� ��������� �������
MqlTradeCheckResult check;
//--------------------------------------------------------------------
int STOPLEVEL;
double Bid,Ask,SLB=0,SLS=0;
int slippage=100;
int maHandle;    // ����� ���������� Moving Average
double maVal[];  // ������������ ������ ��� �������� �������� ���������� Moving Average ��� ������� ����
int atrHandle;    // ����� ���������� Moving Average
double atrVal[];  // ������������ ������ ��� �������� �������� ���������� Moving Average ��� ������� ����
int sarHandle;    // ����� ���������� Moving Average
double sarVal[];  // ������������ ������ ��� �������� �������� ���������� Moving Average ��� ������� ����
//--------------------------------------------------------------------
int OnInit()
  {
   if(VirtualTrailingStop) GeneralNoLoss=true;
   string txt;
   switch(parameters_trailing)
     {
      case 1: // �� ����������� ������
         StringConcatenate(txt,"�� ������ ",StrPer(TF_Tralling)," +- ",delta);
         break;
      case 2: // �� ���������
         StringConcatenate(txt,"�� ��������� ",StrPer(TF_Tralling)," +- ",delta);
         break;
      case 3: // �� ���������� ATR
         StringConcatenate(txt,"�� ATR (",IntegerToString(period_ATR),") ",StrPer(TF_Tralling),"+- ",delta);
         atrHandle=iATR(_Symbol,TF_Tralling,period_ATR);
         break;
      case 4: // �� ���������� Parabolic
         StringConcatenate(txt,"�� ���������� (",DoubleToString(Step,2)," ",DoubleToString(Maximum,2),") ",StrPer(TF_Tralling)," +- ",delta);
         sarHandle=iSAR(_Symbol,TF_Tralling,Step,Maximum);
         break;
      case 5: // �� ���������� ��
         StringConcatenate(txt,"�� MA (",ma_period," ",ma_method," ",applied_price,") ",StrPer(TF_Tralling)," +- ",delta);
         maHandle=iMA(_Symbol,TF_Tralling,ma_period,0,ma_method,applied_price);
         break;
      case 6: // % �� �������
         StringConcatenate(txt," ",DoubleToString(PercetnProfit,2),"% �� �������)");
         break;
      default: // �� �������
         StringConcatenate(txt,"�� ������� ",delta," �");
         break;
     }
   if(VirtualTrailingStop)
     {
      StringConcatenate(txt,"����������� ���� ",txt);
     }
   else
     {
      StringConcatenate(txt,"T��� ",txt);
     }
   DrawLABEL(3,"cm 3",txt,5,30,text_color,ANCHOR_RIGHT);

   return(INIT_SUCCEEDED);
  }
//--------------------------------------------------------------------
void OnTick()
  {
   long OT;
   int n=0;
   double OOP=0;
   string txt;
   StringConcatenate(txt,"Balance ",DoubleToString(AccountInfoDouble(ACCOUNT_BALANCE),2));
   DrawLABEL(2,"cm Balance",txt,5,20,Lime,ANCHOR_RIGHT);
   StringConcatenate(txt,"Equity ",DoubleToString(AccountInfoDouble(ACCOUNT_EQUITY),2));
   DrawLABEL(2,"cm Equity",txt,5,35,Lime,ANCHOR_RIGHT);
//----
   if(!VirtualTrailingStop) STOPLEVEL=(int)SymbolInfoInteger(_Symbol,SYMBOL_TRADE_STOPS_LEVEL);
   double sl,SL;
   Bid=SymbolInfoDouble(Symbol(),SYMBOL_BID);
   Ask=SymbolInfoDouble(Symbol(),SYMBOL_ASK);
   int i,b=0,s=0;
   double PB=0,PS=0,OL=0,NLb=0,NLs=0,LS=0,LB=0;
//----
   for(i=0; i<PositionsTotal(); i++)
     {
      if(_Symbol==PositionGetSymbol(i))
        {
         if(Magic==OrderGetInteger(ORDER_MAGIC) || Magic==-1)
           {
            OL  = PositionGetDouble(POSITION_VOLUME);
            OOP = PositionGetDouble(POSITION_PRICE_OPEN);
            OT  = PositionGetInteger(POSITION_TYPE);
            if(OT==POSITION_TYPE_BUY ) {PB += OOP*OL; LB+=OL; b++;}
            if(OT==POSITION_TYPE_SELL) {PS += OOP*OL; LS+=OL; s++;}
           }
        }
     }
//----
   if(LB!=0)
     {
      NLb=PB/LB;
      ARROW("cm_NL_Buy",NLb,clrAqua);
     }
   if(LS!=0)
     {
      NLs=PS/LS;
      ARROW("cm_NL_Sell",NLs,clrRed);
     }
//----
   request.symbol=_Symbol;
   for(i=0; i<PositionsTotal(); i++)
     {
      if(_Symbol==PositionGetSymbol(i))
        {
         if(Magic==OrderGetInteger(ORDER_MAGIC) || Magic==-1)
           {
            OL  = PositionGetDouble(POSITION_VOLUME);
            OOP = PositionGetDouble(POSITION_PRICE_OPEN);
            OT  = PositionGetInteger(POSITION_TYPE);
            sl=PositionGetDouble(POSITION_SL);
            if(OT==POSITION_TYPE_BUY)
              {
               if(VirtualTrailingStop)
                 {
                  SL=SlLastBar(POSITION_TYPE_BUY,Bid,NLb);
                  if(SL!=-1 && NLb+StartTrall*_Point<SL && SLB<SL) SLB=SL;
                  if(SLB!=0)
                    {
                     HLINE("cm_slb",SLB,clrAqua);
                     if(Bid<=SLB)
                       {
                        request.deviation=slippage;
                        request.volume=PositionGetDouble(POSITION_VOLUME);
                        request.position=PositionGetInteger(POSITION_TICKET);
                        request.action=TRADE_ACTION_DEAL;
                        request.type_filling=ORDER_FILLING_FOK;
                        request.type=ORDER_TYPE_SELL;
                        request.price=Bid;
                        request.comment="";
                        if(!OrderSend(request,result)) Print("error ",GetLastError());
                       }
                    }
                 }
               else
                 {
                  SL=SlLastBar(POSITION_TYPE_BUY,Bid,OOP);
                  if(SL!=-1 && sl+StepTrall*_Point<SL && SL>=OOP+StartTrall*_Point)
                    {
                     request.action    = TRADE_ACTION_SLTP;
                     request.position  = PositionGetInteger(POSITION_TICKET);
                     request.sl        = SL;
                     request.tp        = PositionGetDouble(POSITION_TP);
                     if(!OrderSend(request,result)) Print("error ",GetLastError());
                    }
                 }
              }
            if(OT==POSITION_TYPE_SELL)
              {
               if(VirtualTrailingStop)
                 {
                  SL=SlLastBar(POSITION_TYPE_SELL,Ask,NLs);
                  if(SL!=-1 && (SLS==0 || SLS>SL) && SL<=NLs-StartTrall*_Point) SLS=SL;
                  if(SLS!=0)
                    {
                     HLINE("cm_sls",SLS,clrRed);
                     if(Ask>=SLS)
                       {
                        request.volume=PositionGetDouble(POSITION_VOLUME);
                        request.position=PositionGetInteger(POSITION_TICKET);
                        request.action=TRADE_ACTION_DEAL;
                        request.type_filling=ORDER_FILLING_FOK;
                        request.type=ORDER_TYPE_BUY;
                        request.price=Ask;
                        request.comment="";
                        if(!OrderSend(request,result)) Print("error ",GetLastError());
                       }
                    }
                 }
               else
                 {
                  SL=SlLastBar(POSITION_TYPE_SELL,Ask,OOP);
                  if(SL!=-1 && (sl==0 || sl-StepTrall*_Point>SL) && SL<=OOP-StartTrall*_Point)
                    {
                     request.action    = TRADE_ACTION_SLTP;
                     request.position  = PositionGetInteger(POSITION_TICKET);
                     request.sl        = SL;
                     request.tp        = PositionGetDouble(POSITION_TP);
                     if(OrderCheck(request,check)) if(!OrderSend(request,result)) Print("error ",GetLastError());
                     else Print("error ",GetLastError());
                    }
                 }
              }
           }
        }
     }

   if(b==0)
     {
      SLB=0;
      ObjectDelete(0,"cm SLb");
      ObjectDelete(0,"cm_SLb");
      ObjectDelete(0,"cm_slb");
     }
   if(s==0)
     {
      SLS=0;
      ObjectDelete(0,"cm SLs");
      ObjectDelete(0,"cm_SLs");
      ObjectDelete(0,"cm_sls");
     }
   return;
  }
//--------------------------------------------------------------------
void OnDeinit(const int reason)
  {
   ObjectsDeleteAll(0,"cm");
   Comment("");
   if(parameters_trailing==3) IndicatorRelease(atrHandle);
   if(parameters_trailing==4) IndicatorRelease(sarHandle);
   if(parameters_trailing==5) IndicatorRelease(maHandle);
  }
//--------------------------------------------------------------------
double SlLastBar(int tip,double price,double OOP)
  {
   double prc=0;
   int i;
   string txt;
   switch(parameters_trailing)
     {
      case 1: // �� ����������� ������
         if(tip==POSITION_TYPE_BUY)
           {
            for(i=1; i<500; i++)
              {
               prc=NormalizeDouble(iLow(Symbol(),TF_Tralling,i)-delta*_Point,_Digits);
               if(prc!=0) if(price-STOPLEVEL*_Point>prc) break;
               else prc=0;
              }
            StringConcatenate(txt,"SL Buy candle ",DoubleToString(prc,_Digits));
           }
         if(tip==POSITION_TYPE_SELL)
           {
            for(i=1; i<500; i++)
              {
               prc=NormalizeDouble(iHigh(Symbol(),TF_Tralling,i)+delta*_Point,_Digits);
               if(prc!=0) if(price+STOPLEVEL*_Point<prc) break;
               else prc=0;
              }
            StringConcatenate(txt,"SL Sell candle ",DoubleToString(prc,_Digits));
           }
         break;

      case 2: // �� ���������
         if(tip==POSITION_TYPE_BUY)
           {
            for(i=2; i<100; i++)
              {
               if(iLow(Symbol(),TF_Tralling,i)<iLow(Symbol(),TF_Tralling,i+1) && 
                  iLow(Symbol(),TF_Tralling,i)<iLow(Symbol(),TF_Tralling,i-1) && 
                  iLow(Symbol(),TF_Tralling,i)<iLow(Symbol(),TF_Tralling,i+2))
                 {
                  prc=iLow(Symbol(),TF_Tralling,i);
                  if(prc!=0)
                    {
                     prc=NormalizeDouble(prc-delta*_Point,_Digits);
                     if(price-STOPLEVEL*_Point>prc) break;
                    }
                  else prc=0;
                 }
              }
            StringConcatenate(txt,"SL Buy Fractals ",DoubleToString(prc,_Digits));
           }
         if(tip==POSITION_TYPE_SELL)
           {
            for(i=2; i<100; i++)
              {
               if(iHigh(Symbol(),TF_Tralling,i)>iHigh(Symbol(),TF_Tralling,i+1) && 
                  iHigh(Symbol(),TF_Tralling,i)>iHigh(Symbol(),TF_Tralling,i-1) && 
                  iHigh(Symbol(),TF_Tralling,i)>iHigh(Symbol(),TF_Tralling,i+2))
                 {
                  prc=iHigh(Symbol(),TF_Tralling,i);
                  if(prc!=0)
                    {
                     prc=NormalizeDouble(prc+delta*_Point,_Digits);
                     if(price+STOPLEVEL*_Point<prc) break;
                    }
                  else prc=0;
                 }
              }
            StringConcatenate(txt,"SL Sell Fractals ",DoubleToString(prc,_Digits));
           }
         break;
      case 3: // �� ���������� ATR
         ArraySetAsSeries(atrVal,true);
         if(CopyBuffer(atrHandle,0,0,3,atrVal)<0)
           {
            StringConcatenate(txt,"������ ATR :",GetLastError());
            prc=-1;
            break;
           }
         prc=atrVal[1];
         if(tip==POSITION_TYPE_BUY)
           {
            prc=NormalizeDouble(Bid-prc-delta*_Point,_Digits);
            StringConcatenate(txt,"SL Buy ATR ",DoubleToString(prc,_Digits));
           }
         if(tip==POSITION_TYPE_SELL)
           {
            prc=NormalizeDouble(Ask+prc+delta*_Point,_Digits);
            StringConcatenate(txt,"SL Buy ATR ",DoubleToString(prc,_Digits));
           }
         break;

      case 4: // �� ���������� Parabolic
         ArraySetAsSeries(sarVal,true);
         if(CopyBuffer(sarHandle,0,0,3,sarVal)<0)
           {
            StringConcatenate(txt,"������ Parabolic SAR :",GetLastError());
            prc=-1;
            break;
           }
         prc=sarVal[1];
         if(tip==POSITION_TYPE_BUY)
           {
            prc=NormalizeDouble(prc-delta*_Point,_Digits);
            if(price-STOPLEVEL*_Point<prc) prc=0;
            StringConcatenate(txt,"SL Buy Parabolic ",DoubleToString(prc,_Digits));
           }
         if(tip==POSITION_TYPE_SELL)
           {
            prc=NormalizeDouble(prc+delta*_Point,_Digits);
            if(price+STOPLEVEL*_Point>prc) prc=0;
            StringConcatenate(txt,"SL Buy Parabolic ",DoubleToString(prc,_Digits));
           }
         break;

      case 5: // �� ���������� ��
         ArraySetAsSeries(maVal,true);
         if(CopyBuffer(maHandle,0,0,3,maVal)<0)
           {
            StringConcatenate(txt,"������ Moving Average :",GetLastError());
            prc=-1;
            break;
           }
         prc=maVal[1];
         if(tip==POSITION_TYPE_BUY)
           {
            prc=NormalizeDouble(prc-delta*_Point,_Digits);
            if(price-STOPLEVEL*_Point<prc) prc=0;
            StringConcatenate(txt,"SL Buy MA ",DoubleToString(prc,_Digits));
           }
         if(tip==POSITION_TYPE_SELL)
           {
            prc=NormalizeDouble(prc+delta*_Point,_Digits);
            if(price+STOPLEVEL*_Point>prc) prc=0;
            StringConcatenate(txt,"SL Sell MA ",DoubleToString(prc,_Digits));
           }
         break;
      case 6: // % �� �������
         if(tip==POSITION_TYPE_BUY)
           {
            prc=NormalizeDouble(OOP+(price-OOP)/100*PercetnProfit,_Digits);
            StringConcatenate(txt,"SL Buy % ",DoubleToString(prc,_Digits));
           }
         if(tip==POSITION_TYPE_SELL)
           {
            prc=NormalizeDouble(OOP-(OOP-price)/100*PercetnProfit,_Digits);
            StringConcatenate(txt,"SL Sell % ",DoubleToString(prc,_Digits));
           }
         break;
      default: // �� �������
         if(tip==POSITION_TYPE_BUY)
           {
            prc=NormalizeDouble(price-delta*_Point,_Digits);
            StringConcatenate(txt,"SL Buy pips ",DoubleToString(prc,_Digits));
           }
         if(tip==POSITION_TYPE_SELL)
           {
            prc=NormalizeDouble(price+delta*_Point,_Digits);
            StringConcatenate(txt,"SL Sell pips ",DoubleToString(prc,_Digits));
           }
         break;
     }
   if(tip==POSITION_TYPE_BUY)
     {
      ARROW("cm_SLb",prc,clrGray);
      DrawLABEL(3,"cm SLb",txt,5,50,Color(prc>OOP,clrGreen,clrGray),ANCHOR_RIGHT);
     }
   if(tip==POSITION_TYPE_SELL)
     {
      ARROW("cm_SLs",prc,clrGray);
      DrawLABEL(3,"cm SLs",txt,5,70,Color(prc<OOP,clrRed,clrGray),ANCHOR_RIGHT);
     }
   return(prc);
  }
//--------------------------------------------------------------------
string StrPer(int per)
  {
   if(per==PERIOD_CURRENT) per=Period();
   if(per > 0 && per < 31) return("M"+IntegerToString(per));
   if(per == PERIOD_H1) return("H1");
   if(per == PERIOD_H2) return("H2");
   if(per == PERIOD_H3) return("M3");
   if(per == PERIOD_H4) return("M4");
   if(per == PERIOD_H6) return("M6");
   if(per == PERIOD_H8) return("M8");
   if(per == PERIOD_H12) return("M12");
   if(per == PERIOD_D1) return("D1");
   if(per == PERIOD_W1) return("W1");
   if(per == PERIOD_MN1) return("MN1");
   return("������ �������");
  }
//+------------------------------------------------------------------+
void HLINE(string Name,double Price,color c)
  {
   ObjectDelete(0,Name);
   ObjectCreate(0,Name,OBJ_HLINE,0,0,Price,0,0,0,0);
   ObjectSetInteger(0,Name,OBJPROP_SELECTABLE,false);
   ObjectSetInteger(0,Name,OBJPROP_SELECTED,false);
   ObjectSetInteger(0,Name,OBJPROP_COLOR,c);
   ObjectSetInteger(0,Name,OBJPROP_STYLE,STYLE_DOT);
   ObjectSetInteger(0,Name,OBJPROP_WIDTH,1);
  }
//+------------------------------------------------------------------+
void ARROW(string Name,double Price,color c)
  {
   if(!DrawArrow) return;
   ObjectDelete(0,Name);
   ObjectCreate(0,Name,OBJ_ARROW_RIGHT_PRICE,0,TimeCurrent(),Price,0,0,0,0);
   ObjectSetInteger(0,Name,OBJPROP_SELECTABLE,false);
   ObjectSetInteger(0,Name,OBJPROP_SELECTED,false);
   ObjectSetInteger(0,Name,OBJPROP_COLOR,c);
   ObjectSetInteger(0,Name,OBJPROP_WIDTH,1);
  }
//--------------------------------------------------------------------
void DrawLABEL(int c,string name,string text,int X,int Y,color clr,int ANCHOR=ANCHOR_LEFT,int FONTSIZE=8)
  {
   if(ObjectFind(0,name)==-1)
     {
      ObjectCreate(0,name,OBJ_LABEL,0,0,0);
      ObjectSetInteger(0,name,OBJPROP_CORNER,c);
      ObjectSetInteger(0,name,OBJPROP_XDISTANCE,X);
      ObjectSetInteger(0,name,OBJPROP_YDISTANCE,Y);
      ObjectSetInteger(0,name,OBJPROP_SELECTABLE,false);
      ObjectSetInteger(0,name,OBJPROP_SELECTED,false);
      ObjectSetInteger(0,name,OBJPROP_FONTSIZE,FONTSIZE);
      ObjectSetString(0,name,OBJPROP_FONT,"Arial");
      ObjectSetInteger(0,name,OBJPROP_ANCHOR,ANCHOR);
     }
   ObjectSetString(0,name,OBJPROP_TEXT,text);
   ObjectSetInteger(0,name,OBJPROP_COLOR,clr);
  }
//--------------------------------------------------------------------
color Color(bool P,color c1,color c2)
  {
   if(P) return(c1);
   return(c2);
  }
//--------------------------------------------------------------------
double iLow(string symbol,ENUM_TIMEFRAMES tf,int index)
  {
   if(index < 0) return(-1);
   double Arr[];
   if(CopyLow(symbol,tf, index, 1, Arr)>0) return(Arr[0]);
   else return(-1);
  }
//--------------------------------------------------------------------
double iHigh(string symbol,ENUM_TIMEFRAMES tf,int index)
  {
   if(index < 0) return(-1);
   double Arr[];
   if(CopyHigh(symbol,tf, index, 1, Arr)>0) return(Arr[0]);
   else return(-1);
  }
//+------------------------------------------------------------------+
