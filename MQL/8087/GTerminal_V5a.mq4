//+----------------------------------------------------------------------------+ 
//|                                   Aleksandr Pak, Alma-Ata, 03.2008         |
//|                                                     ekr-ap@mail.ru        |
//|�������� GTerminal ����������� ���������� ���������,                        |
//|����������� �� ������/�������� �� �����, �� ���������� � ����� �����        |
//+----------------------------------------------------------------------------+

/* 19_04_2008�.
GTerminal V5 


��������� ������: articles.mql4.com/ru/597

...............................................................................................
� ���� ������:
����������� ����� �������, ����� �����, �������� ������� �� ������. 
��������� � ��������� ������ �� ����������� ����� � ����� �� ������� ������� ��� � ���� ����������.
�������� ������������� ����������, �������� �� ����� ����� ��� �� ���������� ����� ���������.
�� �� � �����������))
...............................................................................................
����������� ������: ��� ����� TrendLine  � ������� � ���� "���" �������� �������� ��������, 
����������� �� ���� ������������ ���� �����.

����������� �����, �.�. ����� ��� ����������� ������� ���������� �������� ��������.
         �������� ������

         BuyStop     tp=x sl=x     //���� (������) ���� �����.
         BuyLimit    tp=x sl=x     //���� (������) ���� �����
         SellStop    tp=x sl=x     //tp/sl ����� �������������, ������� � ����� �������
         SellLimit   tp=x sl=x

         �������� ������ 
         SLBUY
         TPBUY
         SLSELL
         TPSELL

         �������� ���� ������� ���������� ����
         SLALLBUY                
         TPALLBUY
         ALALLSELL
         TPALLSELL

����� �������-��������� �����������/��������� � ����� ����������� ������.

         SLINITBUY               //
         TPINITBUY
         SLINITSELL
         TPINITSELL

�������
         ������� � ��������/���. ������� /����� ����� �� �����������. ������ ��������. 
         ����� �������� ���������� � ���������� ������ ���� �� ���� �������� (� ��������� ���������).
         �� ��� ����� �������� ������������ � �������� �����. 
         ����� ���� ��������� ��������, ���� O.k., ���� ��� ����������.
         �������� ���������� ���������� ����.
         ����� �� ��������� ����������� ������� ����/���� CVLOSE 0-������.
         
         �� ������ ����� ������� �������� �� �����������. ����� ������� ������� ���������� �� �����.
         
         ������ ����� ����������� ����������. ����� � ����� ����� "=" ������ ���, ������ ������.
         ����� ����������� �� �������� ������� � � ����� ����������, ��� ����������� � �����������.
         
         ����� ����� ���������� ������������ �������, ������. 
         �������� � ���� ���������� ������ TPBUY 2, � �� �������� ������� TPBUY. ������ ��������� ��, �� ������� �����������.
         
                 
         ���������� ������ ��, ������� ����� ������� ��������:

         RSI
         CCI
         WPR
         Momentum
         Force Index
         DeMarker
         ATR
         OBV
         MFI

������ ���������� ������ ��������� � Period_indicator � ��������� ���������. 
��� ����������� ����� �������� � ����� �����������.


         ����/����
���� ���� �������� �����, �� �������� ������� ��������� ����� ������ ������� - �� ���������� ���� ����.
������� ������������ ����� � ����/���� ��� �����. ��� ������ ��� ��������� ����� �������� ����, 
��� ����� ��� �������� ��������� ���������. ���������� �� �������������.

����� �������� ������ �������� �������. ��� ��� ����������� ����� ������� � ��������� ������.
���� ������� ���� �������, ��������� �����. ���� ������� ����� - ��������� �������.

�������� ����� �������� ������ � ��� ������ - ��� tp/sl, � ����� ����������� tp/sl. 

�������� ��������� ������ ���� ����� Buy � ���� Sell. ������ Buy �� �������.
������ ����� ������� �������. 

����� �������� �������, �� �������� �� �� �����. ������������� ��� ��� ���������:
������ �������� ������� ����� ������� �� ������ TP/SLALLBUY  TP/SLALLSELL

��� �������� �������� ������� ����. 
��� �������� ��������� ����� ������� TP/SLINITBUY  TP/SLINITSELL. 


         ������
����� �������� �� ���������. ����� ����� ���. ����������� ������� "||/>>"


����� ����������� ������� ����������� �� ������ ���� ������, ������� ����� ����������, ���������������, ������ ���������.


������: 
1. ����� ������� �� ���� ������ ����� �����/������� => ������ ����/��������������
2. ��� ������ �� ��������� ���������� ��������� ������� ������� ������.
3. ����������� ������-����� ����������� ���������� �������. ���� �������� - ����� �����.
4. ������������ ����� �������� ������ ����������� ���������������� ��� ��������� ����� ��������, �.�. ����� ����� ����� ���������.

�������!!!
*/

#property copyright "Aleksandr Pak, Almaty,2008-ver5"
#property link "articles.mql4.com/ru/597"
#property show_inputs
#include <WinUser32.mqh>

 extern double Lot=0.1;          //������ ����
 extern int Slipp=6;             //Slippage
 extern int Pop=3;               //������� �������� ������. 
 extern int cross_method=1;      //���� ������� ���������� ���e������� ����/����� 0=������ �� ���� �����,1 ����� �� �� ������ �������.
 extern int start=0;             //������ ������� 0=������� ���, 1 = 1 ������ ��� � �.�.
 extern int start_indicator=1;   //��� �� ������� ������������ ���������
 extern int Period_indicator=14; //������ ���� �����������
 extern int Magic=0;              //����������������� ����� ������, ��������� ��� ���������� ������ ����������
 extern bool DoubleOrderSending=False;  //������� ������ ������� � ������� ������
 extern bool Teg_Pause = TRUE;  //���� ���������� ����� �����, � �������  �� ���, ������������� � ��������, � ������� ������ ���������
 extern bool Teg_DeletOpen=TRUE; //���������� ����� �������� ��� �������� ������
 extern bool Teg_DeletOrderOnLine=FALSE; //���������� ������ ���� ��� ����� �������� ��� �������� ������
 extern color color_buy=Aqua;    //����� ����� buy
 extern color color_sell=Orange; //����� ����� sell
 extern color color_init=Red;    //����� ����� ������� tp/sl

double price0, price1;
double Last_time;
int Buy_ticket,Sell_ticket;
int tp, sl;
double tpinitbuy,slinitbuy,tpinitsell,slinitsell;
int Pause=0;
double last_pause;
int t_first=0; 
int glob_s=0, glob_b=0;

color color_tp_buy=Aqua, color_sl_buy=Aqua;
string BUY_global_name,SELL_global_name;
string message; 
string last_line; 
string Pause_name;
string s_tpinitbuy,s_slinitbuy,s_tpinitsell,s_slinitsell;

int init()
{double t;
               
         BUY_global_name=  "GT_BUY_"+  Symbol();
         SELL_global_name= "GT_SELL_"+ Symbol();
              
         if(!IsTesting())
         {  Buy_ticket = GlobalVariableGet(BUY_global_name);
            if(Buy_ticket!=0)
            {if(OrderSelect(Buy_ticket,SELECT_BY_TICKET)==TRUE) 
                       { t=OrderCloseTime();
                           if(t!=0) { Buy_ticket=0; GlobalVariableSet(BUY_global_name,0); }
                       }   else {Buy_ticket=0; GlobalVariableSet(BUY_global_name,0);}
            }
            
            Sell_ticket = GlobalVariableGet(SELL_global_name);
            if(Sell_ticket!=0)
            {if(OrderSelect(Sell_ticket,SELECT_BY_TICKET)==TRUE) 
                  {t=OrderCloseTime();
                           if(t!=0) { Sell_ticket=0; GlobalVariableSet(SELL_global_name,0); }
                  }        else {Sell_ticket=0;GlobalVariableSet(SELL_global_name,0);}
            }
          }
              
     if(!IsTesting())
     {
         if(Teg_Pause)
         {
         if(ObjectFind("PAUSE")<0)
         {ObjectCreate("PAUSE", OBJ_VLINE, 0,iTime(Symbol(),0,0)+12*60*Period(),0);
         ObjectSet("PAUSE",OBJPROP_WIDTH,1);
         ObjectSet("PAUSE",OBJPROP_COLOR,Red);             
         }
         else ObjectSet("PAUSE",OBJPROP_TIME1,iTime(Symbol(),0,0)+12*60*Period());
         }                  
     }    
    Comment("ticket buy="+DoubleToStr(Buy_ticket,0)+"  ticket sell="+DoubleToStr(Sell_ticket,0));        
return (0);
}
//.....................

int deinit()
{
   ObjectDelete("PipsWork");
   ObjectDelete("PAUSE");
   return(0);
}
//*************************************
//*************************************
void start()
{  int i,j,k,Slipp,Pop,err,crach;
   double t;
   int ticket;
   bool t_busy=TRUE;
         
         RefreshRates();
         t=iTime(Symbol(),0,0);
         if(t>Last_time){Last_time=t; t_first=1;}
 
  if(Buy_ticket!=0)
            {if(OrderSelect(Buy_ticket,SELECT_BY_TICKET)==TRUE) 
                       { t=OrderCloseTime();
                           if(t!=0) { Buy_ticket=0; glob_b=1;}
                       }   else {Buy_ticket=0; glob_b=1;}
            }
  if(Sell_ticket!=0)
            {if(OrderSelect(Sell_ticket,SELECT_BY_TICKET)==TRUE) 
                  {t=OrderCloseTime();
                           if(t!=0) { Sell_ticket=0; glob_s=1; }
                  }        else {Sell_ticket=0; glob_s=1;}
            }
            
    SearchWorkLine();
    
    if(!search_name_pause())
      {
    
     if(IsTradeContextBusy()) 
     {
     while(t_busy){Comment("�������� ��������� ����� ��������"); Sleep(1000); RefreshRates(); t_busy=IsTradeContextBusy(); }
     } 
     else
     {
            
 //........................................................................................................
//.................����������� �������� ��������     
   
                  if(cross_up    ("buystop",color_buy))     OpenBuy();                          
                  if(cross_down  ("buylimit",color_buy))    OpenBuy();
                  if( cross_down ("slbuy",color_buy))       CloseBuy();
                  if(cross_up    ("tpbuy",color_buy))       CloseBuy();
                  if(cross_down  ("slallbuy",color_buy ))   { close_all(OP_BUY); Buy_ticket=0; glob_b=1;  }
                  if(cross_up    ("tpallbuy",color_buy ))   { close_all(OP_BUY); Buy_ticket=0; glob_b=1;  }   
  //************ 
       
       if   (cross_down ("sellstop",color_sell))    OpenSell();   
       if   (cross_up   ("selllimit",color_sell))     OpenSell();
       if   (cross_up   ("slsell",color_sell))           CloseSell();                 
       if   (cross_down ("tpsell",color_sell))         CloseSell();
       if   (cross_up   ("slallsell",color_sell))    {  close_all(OP_SELL); Sell_ticket=0; glob_s=1; }
       if(  cross_down  ("tpallsell",color_sell))  {  close_all(OP_SELL); Sell_ticket=0; glob_s=1; }                                                                                        
//...................................................          
   
   }//IsTradeContextBusy
   } //pause
   
     else{/*Print("�����");*/}
      
   if(!IsTesting())
      {if(glob_b==1){glob_b=0; GlobalVariableSet(BUY_global_name,  Buy_ticket);}
         if(glob_s==1){glob_s=0; GlobalVariableSet(SELL_global_name, Sell_ticket);}  
      }
   t_first=0; 
   Comment(StringConcatenate("ticket buy=",DoubleToStr(Buy_ticket,0),"  ticket sell="+DoubleToStr(Sell_ticket,0)));
   ObjectDelete("PipsWork");
}

//******************************************************************************************
int CloseSell()
{
         param(last_line);
         if(Sell_ticket!=0) 
         { 
         if(close(Sell_ticket)==0) 
            {
            Sell_ticket=0; glob_s=1; 
            } 
         }
}

//***************
int CloseBuy()
{
                        param(last_line);
                        if(close(Buy_ticket)==0)
                        {
                           Buy_ticket=0;
                           glob_b=1;
                        }
}
//****************
int OpenBuy()
{int ticket;
                    param(last_line); 
                     if(Buy_ticket<=0)               
                        { 
                           if(!DoubleOrderSending)
                           {
                           //Print("tp/sl   ",tpinitbuy,"  ",slinitbuy);
                           Buy_ticket=send_order(0,0,ticket,Lot,tp,sl,tpinitbuy,slinitbuy); 
                           }
                           else
                           {
                           Buy_ticket=send_order(0,0,ticket,Lot,0,0,0,0);
                           if(Buy_ticket>0)
                                 {
                                 Sleep(1000);
                                 ticket=send_order(0,1,Buy_ticket,Lot,tp,sl,tpinitbuy,slinitbuy);
                                 }
                           }
                              if(Buy_ticket>0) 
                                 {  
                                    fixline(last_line,Buy_ticket,color_buy);
                                    glob_b=1;
                                 } 
                         }
}
 //**********************
 int OpenSell()
 {int ticket;
                   param(last_line);
                   if(Sell_ticket<=0)
                        {
                              if(!DoubleOrderSending)
                              Sell_ticket=send_order(1,0,ticket,Lot,tp,sl,tpinitsell,slinitsell);
                              else
                              {
                              Sell_ticket=send_order(1,0,ticket,Lot,0,0,0,0);
                              if(Sell_ticket>0)
                                       {
                                       Sleep(1000); ticket=send_order(1,1,Sell_ticket,Lot,tp,sl,tpinitsell,slinitsell);
                                       }
                              }
                              if(Sell_ticket>0) { glob_s=1; fixline(last_line,Sell_ticket,color_sell); }
                        }
}
//**************************
bool search_name_pause()
{double p,p2,t,t2,y; int error,i; string n;
         if(IsTesting()) return(FALSE);
         if(!Teg_Pause) return (FALSE);
         i=search_name_obj("paus");
         if (i>=0)
         {
            p=   ObjectGet(Pause_name,OBJPROP_TIME1); 
            p2=iTime(NULL,0,0); 
     
            if(p-p2<0) 
               { 
               
               if(last_pause==p&&Pause==0) 
               {
                y=p2+12*60*Period();
                        for(i=0;i<10;i++)
                        {
                        ObjectSet(Pause_name,OBJPROP_TIME1,y);
                        WindowRedraw();
                        if(GetLastError()==0) break;
                        Sleep(100);
                        }
                       
                        last_pause=   y;
                        return(FALSE);
               }
                else
                {
                  Pause=1;
                  ObjectSet(Pause_name,OBJPROP_WIDTH,4);
                  WindowRedraw();
                  last_pause=p;
               return(TRUE); 
               }
               } else  
                        {                        
                        ObjectSet(Pause_name,OBJPROP_WIDTH,1);
                        if(t_first==1)
                              {
                              ObjectSet(Pause_name,OBJPROP_TIME1,ObjectGet(Pause_name,OBJPROP_TIME1)+Period()*60);
                              }
                        Pause=0;
                        WindowRedraw();
                        last_pause=ObjectGet(Pause_name,OBJPROP_TIME1); 
                        return(FALSE);
                        }
         }
}

//.........................
int search_name_obj(string c) 
{int i,k; string s;
   k=ObjectsTotal(); 
   for(i=k-1;i>=0;i--)  
   {  
      s=lowercaps(ObjectName(i));
    if (StringFind(s,c,0)>=0){ Pause_name=ObjectName(i); return(i);} 
   }
return (-1);
}
//....................................................
int fixline(string _name, int _B, color _color)
{int error;

string             txn=StringConcatenate("TICKET=",DoubleToStr(_B,0)," ",_name," DATE=",TimeToStr(TimeLocal(),
                   
                   TIME_DATE)," TIME=",TimeToStr(TimeLocal(),TIME_SECONDS));
                   
                   ObjectCreate(txn,OBJ_TREND,ObjectFind(_name),
                   ObjectGet( _name,OBJPROP_TIME1),ObjectGet( _name,OBJPROP_PRICE1),
                   ObjectGet( _name,OBJPROP_TIME2),ObjectGet( _name,OBJPROP_PRICE2));
                   
                   while(TRUE)
                   {
                     ObjectDelete(_name); 
                     WindowRedraw();
                     if(ObjectFind(_name)==-1) break;
                   }
                   
                   ObjectSet(txn,OBJPROP_STYLE,STYLE_DOT);
                   ObjectSet(txn,OBJPROP_COLOR,_color);
                  
}
//..........................................

int close_all(int typs)
  {
   bool   result;
   double price;
   int    cmd,error,i=0,k;
  
   double t;
   int ticket;
   string tr;
//----

  if(typs==0) tr="CLOSE ALL BUY"; else tr="CLOSE ALL SELL";
   k=OrdersTotal();
   for(i=k-1;i>=0;i--)
   {   
   if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES)) 
      {
         cmd=OrderType();
         t=OrderCloseTime();
         if((cmd==typs)&&Symbol()==OrderSymbol()&&t==0) 
         { 
               t=OrderOpenTime();
               if(t!=0)
                  { 
                     ObjectCreate("PipsWork",OBJ_TEXT,0,iTime(NULL,0,10),High[10]);
                     ObjectSetText("PipsWork", tr, 14,"",Red);
                     WindowRedraw();
                     close_(cmd); /* Sleep(500);*/
                   }              
               } 
      }  
        
  }     
  
ObjectDelete("PipsWork");
   return(0);
  }
//.............................

int close_(int cmd)
{
   bool   result;
   double price;
   int    error,i=0;         
                  while(true)
                  {i+=1;
                   RefreshRates();
                  if(cmd==OP_BUY)      price=Bid; 
                        else           price=Ask;
                        result=OrderClose(OrderTicket(),OrderLots(),price,12,CLR_NONE);
                        error=GetLastError(); 
                        if(result==TRUE) error=0; 
                        if(error!=0) {Sleep(3000); RefreshRates();} //��������� �����, ���� 6 �������
                  else break;
                  if(i>6)break; ///6 ������� ������� �����
                 }
return (error);
}
//......................
int close(int ticket)
  {
   bool   result;
   double price;
   int    cmd,error=-1,i=0;
   double t;
   string tr=StringConcatenate("CLOSE ", DoubleToStr(ticket,0)) ;
  
   if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES)) 
      {
       cmd=OrderType();
       t=OrderCloseTime();
       if(Symbol()==OrderSymbol()&&t==0) { 
       ObjectCreate("PipsWork",OBJ_TEXT,0,iTime(NULL,0,10),High[10]);
       ObjectSetText("PipsWork", tr, 14,"",Red);
       WindowRedraw();

       error=close_(cmd); 
       } 
      }  else {/*Print("������ � ������ �������=",i);*/}       
   ObjectDelete("PipsWork");
   return(error);
}
//..............................................................
int send_order(int teg_b,int sm, int ticket, double sLot, int _tp, int _sl, double ptp, double psl)
{int err=1,k=0,crach;
 double loss,profit,_Lot;
 bool result=TRUE;
 string tr;
 if(teg_b==0) tr="OPEN BUY"; else tr="OPEN SELL";
 ObjectCreate("PipsWork",OBJ_TEXT,0,iTime(NULL,0,10),High[10]);
 ObjectSetText("PipsWork", tr, 14,"",Red);
 WindowRedraw();
       
       while(TRUE)
         { k+=1; RefreshRates(); 
                  double ask=NormalizeDouble(Ask,Digits),bid=NormalizeDouble(Bid,Digits);
              
                  if(sLot==0) _Lot=0.1; else _Lot=sLot;
                  if(teg_b==0)
                  {
                  if (_sl>0) loss  =ask-_sl*Point;  else loss=0;
                  if (_tp>0) profit=ask+_tp*Point;    else profit=0;
                  if(profit==0){if(ptp!=0) profit=ptp; }
                  if(loss==0){if(psl!=0) loss=psl;}
                                  
                  if(sm==0) ticket=OrderSend(Symbol(),OP_BUY,_Lot,ask,Slipp,loss,profit,NULL,0,0,CLR_NONE);                       
                                                else  result=OrderModify(ticket,0,loss,profit,0,CLR_NONE);                
                  } else
                     {
                     if (_sl>0) loss  =bid+_sl*Point;  else loss=0;
                     if (_tp>0) profit=bid-_tp*Point;    else profit=0;
                     
                     if(profit==0){if(ptp!=0) profit=ptp; }
                     if(loss==0){if(psl!=0) loss=psl;}
                     
                     if(sm==0) ticket=OrderSend(Symbol(),OP_SELL,_Lot,bid,Slipp,loss,profit,NULL,0,0,CLR_NONE);                       
                                                else      result=OrderModify(ticket,0,loss,profit,0,CLR_NONE);               
                     }
         err=ShowError(Pop,k);
         if(err<=1&&result==TRUE) break; else  Sleep(3000); 
         if(k>=Pop) break;
         if(err==4||err==6||err==128||err==135||err==137||err==138||err==146) crach=0; else crach=1;
         if(crach==1) break;  //No new repeat is are crazy  
         }

 return (ticket);
}
//....................................................
string search_right(string s, string c)
{ int i,j,k,len; string r="",p;    
         i=StringFind(s,c,i); 
         if(i!=-1) 
         {i+=StringLen(c);
         r=""; len =StringLen(s);
   for(j=0;j<len;j++) { k=StringGetChar(s,i+j); if(k<=57&&k>=48||k==46||k==44) 
                                                {  p=StringSubstr(s,i+j,1); r=r+p;
                                                } else 
                break;}
          }
   return(r);
}
//..............
string search_left(string s,string c) 
{ int i,j,k,len; string r="",p;    
         i=StringFind(s,c,0);  
  if(i!=-1)
  {
  r="";        len =StringLen(s);
  r=StringSubstr(s,0,i);
  }
  else r=s;
  r=lowercaps(r);
  return(r);
}
//.........................
string lowercaps(string s)
{int i,k,c; string r=""; k=StringLen(s); for(i=0;i<k;i++){c=StringGetChar(s,i); if(c<91&&c>64) c+=32;r=r+CharToStr(c);}
 return (r);
}
//..........................

void param(string s)
      {string b,r;
      
         r=lowercaps(s);
         b=search_right(r,"tp="); if(StringLen(b)>0) tp =NormalizeDouble(StrToDouble(b),0); else tp=0;
         b=search_right(r,"sl="); if(StringLen(b)>0) sl =NormalizeDouble(StrToDouble(b),0); else sl=0;
         
         ObjectSetText(s,StringConcatenate("!O.k! tp=",DoubleToStr(tp,0),"  ",
         "sl=",DoubleToStr(sl,0)));
      }
//.....................................................................

bool cross_down(string s, color col)
{ return(first_line(s, 0, col));}


bool cross_up(string s, color col)
{  return(first_line(s, 1, col));}
//******************************************
double first_line(string s,int u_d,color col)                                                                         
{     int i,w,wi,ind; 
      bool isfound=FALSE;
      string c,r,b;
      double rline;
      int k=ObjectsTotal(); 

      for(i=k-1;i>=0;i--)
      {  
         c=ObjectName(i);
         r=search_left(c," ");
         if(r==s)
          { 
            w=ObjectFind(c);
            if(w==0)
               {
                  RefreshRates();
                  price0=NormalizeDouble(Close[start],Digits);
                  price1=NormalizeDouble(Close[start+1],Digits);
       
               }else
                     {
                     RefreshRates();
                           isfound=indicator(w);
                     }
            if(w!=0){if(!isfound) {ObjectSetText(s,"����� ����������� �� �����"); return (FALSE);}}
          
            rline = ObjectGetValue_ByCurrent(c, start);
            if(rline!=0)
            {  if(u_d==1)
               {
               if(cross_method==0) {if(rline<price0&&rline>price1) {last_line=c; return (TRUE);  }}
               if(cross_method==1) {if(rline<price0) {last_line=c; return (TRUE);  }}
               } 
               else
                  {
                  if(cross_method==0) {if(rline>price0 && rline<price1){ last_line=c; return (TRUE); } }
                  if(cross_method==1) {if(rline>price0 ){ last_line=c; return (TRUE);} }
                  }
            }
          }
         }//for
   return (FALSE);
}
//...............................................
bool indicator(int w)
{                          int wi; 
                           bool isfound=FALSE;
                           wi=WindowFind(StringConcatenate("RSI(",DoubleToStr(Period_indicator,0),")"));
                           if(w==wi)
                           {
                           price0=iRSI(Symbol(),0,Period_indicator,0,start_indicator);
                           price1=iRSI(Symbol(),0,Period_indicator,0,start_indicator+1);
                           isfound=TRUE;
                           }
                           
                           wi=WindowFind(StringConcatenate("CCI(",DoubleToStr(Period_indicator,0),")"));
                           if(w==wi)
                           {
                           price0=iCCI(Symbol(),0,Period_indicator,0,start_indicator);
                           price1=iCCI(Symbol(),0,Period_indicator,0,start_indicator+1);
                           isfound=TRUE;
                           }
                            wi=WindowFind(StringConcatenate("%R(",DoubleToStr(Period_indicator,0),")"));
                           if(w==wi)
                           {
                           price0=iWPR(Symbol(),0,Period_indicator,start_indicator);
                           price1=iWPR(Symbol(),0,Period_indicator,start_indicator+1);
                           isfound=TRUE;
                           }
                           wi=WindowFind(StringConcatenate("Momentum(",DoubleToStr(Period_indicator,0),")"));
                           if(w==wi)
                           {
                           price0=iMomentum(Symbol(),0,Period_indicator,0,start_indicator);
                           price1=iMomentum(Symbol(),0,Period_indicator,0,start_indicator+1);
                           isfound=TRUE;
                           }
                            wi=WindowFind(StringConcatenate("Force(",DoubleToStr(Period_indicator,0),")"));
                           if(w==wi)
                           {
                           price0=iForce(Symbol(),0,Period_indicator,0,0,start_indicator);
                           price1=iForce(Symbol(),0,Period_indicator,0,0,start_indicator+1);
                           isfound=TRUE;
                           }
                            wi=WindowFind(StringConcatenate("DeM(",DoubleToStr(Period_indicator,0),")"));
                           if(w==wi)
                           {
                           price0=iDeMarker(Symbol(),0,Period_indicator,start_indicator);
                           price1=iDeMarker(Symbol(),0,Period_indicator,start_indicator+1);
                           isfound=TRUE;
                           }
                           wi=WindowFind(StringConcatenate("ATR(",DoubleToStr(Period_indicator,0),")"));
                           if(w==wi)
                           {
                           price0=iATR(Symbol(),0,Period_indicator,start_indicator);
                           price1=iATR(Symbol(),0,Period_indicator,start_indicator+1);
                           isfound=TRUE;
                           }
                           wi=WindowFind("OBV");
                           if(w==wi)
                           {
                           price0=iOBV(Symbol(),0,0,start_indicator);
                           price1=iOBV(Symbol(),0,0,start_indicator+1);
                           isfound=TRUE;
                           }
                           wi=WindowFind(StringConcatenate("MFI(",DoubleToStr(Period_indicator,0),")"));
                           if(w==wi)
                           {
                           price0=iMFI(Symbol(),0,Period_indicator,start_indicator);
                           price1=iMFI(Symbol(),0,Period_indicator,start_indicator+1);
                           isfound=TRUE;
                           }
            return(isfound);
     }
//
int SearchWorkLine() 
{int i,k,w,ti=0,ct,mt[1000]; string r,c;
   k=ObjectsTotal(); 
   for(i=k-1;i>=0;i--)
   {  
      c=ObjectName(i);
      w=ObjectFind(c);
     
      r=search_left(c," ");
      if(r==   "buylimit"  )  { param(c); ObjectSet(c,OBJPROP_COLOR,color_buy);}
      if(r==   "buystop"   )  { param(c); ObjectSet(c,OBJPROP_COLOR,color_buy);}
      if(r==   "tpbuy"     )  { ObjectSetText(c,"O.k."); ObjectSet(c,OBJPROP_COLOR,color_buy);}
      if(r==   "slbuy"     )  { ObjectSetText(c,"O.k."); ObjectSet(c,OBJPROP_COLOR,color_buy);}
      if(r==   "selllimit" )  { param(c); ObjectSet(c,OBJPROP_COLOR,color_sell);}
      if(r==   "sellstop"  )  { param(c); ObjectSet(c,OBJPROP_COLOR,color_sell);}
      if(r==   "tpsell"    )  { ObjectSetText(c,"O.k."); ObjectSet(c,OBJPROP_COLOR,color_sell);}
      if(r==   "slsell"    )  { ObjectSetText(c,"O.k."); ObjectSet(c,OBJPROP_COLOR,color_sell);}
      if(r==   "slallsell" )  { ObjectSetText(c,"O.k."); ObjectSet(c,OBJPROP_COLOR,color_sell);}
      if(r==   "tpallsell" )  { ObjectSetText(c,"O.k."); ObjectSet(c,OBJPROP_COLOR,color_sell);}
      if(r==   "slallbuy"  )  { ObjectSetText(c,"O.k."); ObjectSet(c,OBJPROP_COLOR,color_buy); }
      if(r==   "tpallbuy"  )  { ObjectSetText(c,"O.k."); ObjectSet(c,OBJPROP_COLOR,color_buy); }
      if(r==   "tpinitbuy" )  { if(w==0){tpinitbuy=ObjectGetValueByShift(c,0);
                                ObjectSetText(c,"O.k. tpinitbuy=",   tpinitbuy);s_tpinitbuy=c; }
                                else ObjectSetText(c,"Not execute ��� ����������");}
      if(r==   "slinitbuy" )  { if(w==0){slinitbuy=ObjectGetValueByShift(c,0);
                                ObjectSetText(c,"O.k. slinitbuy=",   slinitbuy);s_slinitbuy=c; }
                                else ObjectSetText(c,"Not execute ��� ����������");}
      if(r==   "tpinitsell")  { if(w==0){tpinitsell=ObjectGetValueByShift(c,0);
                                ObjectSetText(c,"O.k. tpinitsell=",  tpinitsell);s_tpinitsell=c;}
                                else ObjectSetText(c,"Not execute ��� ����������");}
      if(r==   "slinitsell")  { if(w==0){slinitsell=ObjectGetValueByShift(c,0);
                                ObjectSetText(c,"O.k. slinitsell=", slinitsell);s_slinitsell=c; }
                                else ObjectSetText(c,"Not execute ��� ����������");}
      if(w!=0)
      {
      if(!indicator(w))ObjectSetText(c,"Not execute ��� ����������");
      }
      r=search_left(c,"=");
      if(r==   "ticket"    )  { ti=StrToDouble(search_right(c,"TICKET=")); 
                                  if(qwest_order(ti)>0) 
                                    { 
                                       if(!IsTesting()) {if(Teg_DeletOpen) ObjectDelete(c);}
                                    } 
                                    mt[ct]=ti; ct+=1; 
                           }  
   } 
      int t_w;
      if(Buy_ticket!=0) 
      {t_w=0;
         for(i=0;i<ct;i++)
            {if(Buy_ticket==mt[i]) t_w=1; }
         if(t_w==0){if(close(Buy_ticket)==0){Buy_ticket=0; glob_b=1;}}
      }
      
      if(Sell_ticket!=0) 
      {t_w=0;
         for(i=0;i<ct;i++)
            {if(Sell_ticket==mt[i]) t_w=1; }
         if(t_w==0){if(close(Sell_ticket)==0){Sell_ticket=0; glob_s=1;}}
      }    
return (0);
}
//...............................
int qwest_order(int ticket) 
{ 
 if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES))
         {
            double  t=OrderCloseTime();
            if(t==0) return (0); else return(1);
         }  else return (0);
}
//...............................................
double ObjectGetValue_ByCurrent(string c, int shift) //Value of 
{

double r=ObjectGetValueByShift (c,shift);
      if(r!=0) return(r+ObjectGetDelta_ByCurrent(c)); else return(0);
}
//....................................
double ObjectGetDelta_PerBar(string c) //Increment of Y-ordinate per Bar
{ 
 double p=  ObjectGet(c,OBJPROP_PRICE1);
 double p2= ObjectGet(c,OBJPROP_PRICE2);
 
 int b =    ObjectGetShiftByValue(c,p);
 int b2=    ObjectGetShiftByValue(c,p2);        
 double     z=b-b2;  
            if(z!=0)
               {
               double delta=(p2-p)/z;
               }
 return(delta);
}
//***************************************
double ObjectGetDelta_ByCurrent(string c)
{ 
      double t=TimeCurrent()-iTime(Symbol(),0,0);
      double tf=60*Period();
      double delta=ObjectGetDelta_PerBar(c);
      double r=delta*(t/tf);
 return(r);
}
//****************************************************************************

//������������������������������������������


int ShowError(int Pop, int k)
{
   string d_error;
   int err=GetLastError(),crach; //3 129 130 131 134 139 140 
   switch (err)            //������ ������ ��� 4, 6, 135, 136 137 138 146 
   {
      case   0: return;
      case   1: d_error="��������� ����������"; break;
      case   2: d_error="����� ������"; break;
      case   3: d_error="������������ ���������"; break;
      case   4: d_error="�������� ������ �����"; break;
      case   5: d_error="�� ������������� ������ ����������� ���������"; break;
      case   6: d_error="��� ����� � �������� ��������"; break;
      case   7: d_error="������������ ����"; break;
      case   8: d_error="������� ������ �������"; break;
      case   9: d_error="������������ �������� �������� �������"; break;
      case  64: d_error="���� ������������"; break;
      case  65: d_error="������������ ����� �����"; break;
      case 128: d_error="����� ���� �������� ���������� ������"; break;
      case 129: d_error="������������ ����"; break;
      case 130: d_error="������������ �����"; break;
      case 131: d_error="������������ �����"; break;
      case 132: d_error="����� ������"; break;
      case 133: d_error="�������� ���������"; break;
      case 134: d_error="������������ ����� ��� ���������� ��������"; break;
      case 135: d_error="���� ����������"; break;
      case 136: d_error="��� ���"; break;
      case 137: d_error="������ �����"; break;
      case 138: d_error="����� ����"; break;
      case 139: d_error="����� ������������ � ��� ��������������"; break;
      case 140: d_error="��������� ������ �������"; break;
      case 141: d_error="������� ����� ��������"; break;
      case 145: d_error="����������� ���������, ��� ��� ����� ������� ������ � �����"; break;
      case 146: d_error="���������� �������� ������"; break;
      case 147: d_error="������������� ���� ��������� ������ ��������� ��������"; break;
      default : d_error="����������� ������"; break;
   }
   
   if(err==4||err==6||err==128||err==135||err==137||err==138||err==146) crach=0; else crach=1; 
   string field="     ";
   string msg="������ #"+err+" "+d_error+field+ "�������="+DoubleToStr(k,0)+"  "+DoubleToStr(Pop,0);
   string title="������"; if (AccountNumber()>0)title=AccountNumber()+": "+title;
  
                  ObjectSetText("PipsWork", msg, 14,"",Red); 
     if(Pop-1==k) ObjectSetText("PipsWork", msg, 14,"",Red); 
   message = msg;
   return (err);
}
//O.k.