//+----------------------------------------------------------------------------+ 
//|                                   Aleksandr Pak, Alma-Ata, 03.2008         |
//|                                                     ekr-ap@mail.ru        |
//|Советник GTerminal Графическое управление торговлей,                        |
//|Полуавтомат на пробой/разворот от линий, по параметрам в имени линии        |
//+----------------------------------------------------------------------------+

/* 19_04_2008г.
GTerminal V5 


Стартовая статья: articles.mql4.com/ru/597

...............................................................................................
В двух словах:
Выставляете линии вручную, даете имена, советник торгует по линиям. 
Открывает и закрывает ордера по пересечению линии с ценой на главном графике или в окне индикатора.
Советник устанваливает тэйкпрофит, стоплосс из имени линии или по положениею линий инициации.
На то и полуавтомат))
...............................................................................................
Графические ордера: это линии TrendLine  у которых в поле "имя" записана торговая операция, 
исполняемая по цене относительно этой линии.

ИСПОЛНЯЕМЫЕ ЛИНИИ, т.е. линии при пересечении которых происходит торговая операция.
         Открытие ордера

         BuyStop     tp=x sl=x     //Цена (прошла) выше линии.
         BuyLimit    tp=x sl=x     //Цена (прошла) ниже линии
         SellStop    tp=x sl=x     //tp/sl могут отсутствовать, пишутся в любом порядке
         SellLimit   tp=x sl=x

         Закрытие ордера 
         SLBUY
         TPBUY
         SLSELL
         TPSELL

         Закрытие всех ордеров указанного типа
         SLALLBUY                
         TPALLBUY
         ALALLSELL
         TPALLSELL

ЛИНИИ ЗАДАНИЯ-ИНИЦИАЦИИ ТЭЙКПРОФИТА/СТОПЛОССА в полях отсылаемого ордера.

         SLINITBUY               //
         TPINITBUY
         SLINITSELL
         TPINITSELL

ПРАВИЛА
         Вносить в свойства/имя. Большие /малые буквы не различаются. Удобно большими. 
         Линия принятая советником к исполнению меняет цвет на цвет операции (в свойствах советника).
         То что понял советник возвращается в описание линии. 
         Будет либо считанные парметры, либо O.k., либо нет исполнения.
         Смотреть высвечивая указателем мыши.
         Линия по умоляанию срабатывает условию выше/ниже CVLOSE 0-гобара.
         
         За концом линии никакие операции НЕ исполняются. Линии задания активны независимо от длины.
         
         Пробел между параметрами обязателен. Перед и после знака "=" пробла нет, писать слитно.
         Линии срабатывают на основном графике и в окнах индикатора, при пересечении с индикатором.
         
         Линии могут отличаться произвольным текстом, цифрой. 
         Например в окне индикатора ставим TPBUY 2, а на основном графике TPBUY. Первой сработает та, на которой пересечение.
         
                 
         ИНДИКАТОРЫ только те, которые может увидеть советник:

         RSI
         CCI
         WPR
         Momentum
         Force Index
         DeMarker
         ATR
         OBV
         MFI

Период индикатора должен совпадать с Period_indicator в свойствах советника. 
Все исполняемые линии работают в окнах индикаторов.


         ДЕМО/РЕАЛ
Если тики приходят редко, то ожидание реакции советника будет весьма заметно - до следующего тика цены.
Красная вертикальная линия в демо/реал это Пауза. Как только она находится левее нулевого бара, 
так сразу все операции советника запрещены. Начавшиеся не останавливает.

После открытия ордера остается пунктир. Эта уже сработавшая линия связана с удалением ордера.
Если удалить этот пунктир, закроется ордер. Если удалить ордер - удаляется пунктир.

Советник может отсылать ордера в два приема - без tp/sl, и сразу модификация tp/sl. 

Советник разрешает только один ордер Buy и один Sell. Второй Buy не откроет.
Ордера можно удалять вручную. 

Можно доливать вручную, но советник их не видит. Автоматизация для них следующая:
Ордера открытые вручную можно закрыть по линиям TP/SLALLBUY  TP/SLALLSELL

Нет контроля активных средств депо. 
Нет контроля положения линий задания TP/SLINITBUY  TP/SLINITSELL. 


         ТЕСТЕР
Линии открытия не удаляются. Линии ПАУЗА нет. Пользуйтесь кнопкой "||/>>"


Линии графических ордеров проверяются на каждом тике заново, поэтому можно перемещать, переименовывать, менять параметры.


СОВЕТЫ: 
1. Линии ушедшие за край экрана можно найти/удалить => правая мыши/СписокОбъектов
2. Для работы на фьючерсах применяйте настройку двойная отсылка ордера.
3. Графические ордера-линии эквиваленты отложенным ордерам. Срок действия - конец линии.
4. Устанавливая линию открытия ордера обязательно проконтролируйте уже имеющиеся линии закрытия, т.к. ордер может сразу закрыться.

Успехов!!!
*/

#property copyright "Aleksandr Pak, Almaty,2008-ver5"
#property link "articles.mql4.com/ru/597"
#property show_inputs
#include <WinUser32.mqh>

 extern double Lot=0.1;          //размер лота
 extern int Slipp=6;             //Slippage
 extern int Pop=3;               //попыток открытия ордера. 
 extern int cross_method=1;      //флаг способа вычисления перeсечения цена/линия 0=жестко по двум барам,1 сразу же по одному касанию.
 extern int start=0;             //начало анализа 0=нулевой бар, 1 = 1 первый бар и т.д.
 extern int start_indicator=1;   //бар на котором сравнивается индикатор
 extern int Period_indicator=14; //период всех индикаторов
 extern int Magic=0;              //Идентификационный номер ордера, применять для различения разных советников
 extern bool DoubleOrderSending=False;  //Отсылка ордера сначала с пустыми полями
 extern bool Teg_Pause = TRUE;  //флаг отключения линии Пауза, и реакции  на нее, действительно в торговле, в тестере всегда отключено
 extern bool Teg_DeletOpen=TRUE; //Ликвидация линий открытия при закрытии ордера
 extern bool Teg_DeletOrderOnLine=FALSE; //Ликвидация ордера если нет линии открытия при закрытии ордера
 extern color color_buy=Aqua;    //цвета линий buy
 extern color color_sell=Orange; //цвета линий sell
 extern color color_init=Red;    //цвета линий задания tp/sl

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
     while(t_busy){Comment("ОЖИДАНИЕ ОКОНЧАНИЯ ЧУЖОЙ ОПЕРАЦИИ"); Sleep(1000); RefreshRates(); t_busy=IsTradeContextBusy(); }
     } 
     else
     {
            
 //........................................................................................................
//.................определение торговых сигналов     
   
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
   
     else{/*Print("Пауза");*/}
      
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
                        if(error!=0) {Sleep(3000); RefreshRates();} //закрывать любим, даем 6 попыток
                  else break;
                  if(i>6)break; ///6 попыток закрыть ордер
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
      }  else {/*Print("Ошибка в выборе позиции=",i);*/}       
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
            if(w!=0){if(!isfound) {ObjectSetText(s,"Линия исполняться НЕ будет"); return (FALSE);}}
          
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
                                else ObjectSetText(c,"Not execute НЕТ исполнения");}
      if(r==   "slinitbuy" )  { if(w==0){slinitbuy=ObjectGetValueByShift(c,0);
                                ObjectSetText(c,"O.k. slinitbuy=",   slinitbuy);s_slinitbuy=c; }
                                else ObjectSetText(c,"Not execute НЕТ исполнения");}
      if(r==   "tpinitsell")  { if(w==0){tpinitsell=ObjectGetValueByShift(c,0);
                                ObjectSetText(c,"O.k. tpinitsell=",  tpinitsell);s_tpinitsell=c;}
                                else ObjectSetText(c,"Not execute НЕТ исполнения");}
      if(r==   "slinitsell")  { if(w==0){slinitsell=ObjectGetValueByShift(c,0);
                                ObjectSetText(c,"O.k. slinitsell=", slinitsell);s_slinitsell=c; }
                                else ObjectSetText(c,"Not execute НЕТ исполнения");}
      if(w!=0)
      {
      if(!indicator(w))ObjectSetText(c,"Not execute НЕТ исполнения");
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

//юююююююююююююююююююююююююююююююююююююююююю


int ShowError(int Pop, int k)
{
   string d_error;
   int err=GetLastError(),crach; //3 129 130 131 134 139 140 
   switch (err)            //повтор только при 4, 6, 135, 136 137 138 146 
   {
      case   0: return;
      case   1: d_error="Результат неизвестен"; break;
      case   2: d_error="Общая ошибка"; break;
      case   3: d_error="Неправильные параметры"; break;
      case   4: d_error="Торговый сервер занят"; break;
      case   5: d_error="Не обслуживаемая версия клиентского терминала"; break;
      case   6: d_error="Нет связи с торговым сервером"; break;
      case   7: d_error="Недостаточно прав"; break;
      case   8: d_error="Слишком частые запросы"; break;
      case   9: d_error="Недопустимая операция вредящая серверу"; break;
      case  64: d_error="Счет заблокирован"; break;
      case  65: d_error="Неправильный номер счета"; break;
      case 128: d_error="Истек срок ожидания выполнения ордера"; break;
      case 129: d_error="Неправильная цена"; break;
      case 130: d_error="Неправильные стопы"; break;
      case 131: d_error="Неправильный объем"; break;
      case 132: d_error="Рынок закрыт"; break;
      case 133: d_error="Торговля запрещена"; break;
      case 134: d_error="Недостаточно денег для совершения операции"; break;
      case 135: d_error="Цена изменилась"; break;
      case 136: d_error="Нет цен"; break;
      case 137: d_error="Брокер занят"; break;
      case 138: d_error="Новые цены"; break;
      case 139: d_error="Ордер заблокирован и уже обрабатывается"; break;
      case 140: d_error="Разрешена только покупка"; break;
      case 141: d_error="Слишком много запросов"; break;
      case 145: d_error="Модификация запрещена, так как ордер слишком близок к рынку"; break;
      case 146: d_error="Подсистема торговли занята"; break;
      case 147: d_error="Использование даты истечения ордера запрещено брокером"; break;
      default : d_error="Неизвестная ошибка"; break;
   }
   
   if(err==4||err==6||err==128||err==135||err==137||err==138||err==146) crach=0; else crach=1; 
   string field="     ";
   string msg="Ошибка #"+err+" "+d_error+field+ "Попыток="+DoubleToStr(k,0)+"  "+DoubleToStr(Pop,0);
   string title="Ошибка"; if (AccountNumber()>0)title=AccountNumber()+": "+title;
  
                  ObjectSetText("PipsWork", msg, 14,"",Red); 
     if(Pop-1==k) ObjectSetText("PipsWork", msg, 14,"",Red); 
   message = msg;
   return (err);
}
//O.k.