//+------------------------------------------------------------------+
//|      Если у кого появятся предложения прошу засылать на мыло.mq4 |
//|                                                            Евген |
//|                                                  z_e_e_d@mail.ru |
//+------------------------------------------------------------------+
#property copyright "Евген"
#property link      "z_e_e_d@mail.ru"

//---- input parameters
extern double my_profit=500.00;//для фунта
extern double my_stop=150.00;
extern double shag=30.00;
extern double mylot=0.01;
  
  int order1,order2,order3,order4,order5,order6,order7,order8,order9,order10,order11,order12,order13,order14,order15,order16,order17,
  order18,order19,order20,order21,order22,order23,order24,order25,order26,order27,order28,order29,order30,order31,order32,SELL,BUY;
  
  int tiket1,tiket2,tiket3,tiket4,tiket5,tiket6,tiket7,tiket8,tiket9,tiket10,tiket11,tiket12,tiket13,tiket14,tiket15,tiket16,tiket17,
  tiket18,tiket19,tiket20,tiket21,tiket22,tiket23,tiket24,tiket25,tiket26,tiket27,tiket28,tiket29,tiket30;
  double real_prise1,real_prise2,balans;  
  
int init()
  {
order1=0;
order2=0;
order3=0;
order4=0;
order5=0;
order6=0;
order7=0;
order8=0;
order9=0;
order10=0;
order11=0;
order12=0;
order13=0;
order14=0;
order15=0;
order16=0;
order17=0;
order18=0;
order19=0;
order20=0;
order21=0;
order22=0;
order23=0;
order24=0;
order25=0;
order26=0;
order27=0;
order28=0;
order29=0;
order30=0;
  
tiket1=0;
tiket2=0;
tiket3=0;
tiket4=0;
tiket5=0;
tiket6=0;
tiket7=0;
tiket8=0;
tiket9=0;
tiket10=0;
tiket11=0;
tiket12=0;
tiket13=0;
tiket14=0;
tiket15=0;
tiket16=0;
tiket17=0;
tiket18=0;
tiket19=0;
tiket20=0;
tiket21=0;
tiket22=0;
tiket23=0;
tiket24=0;
tiket25=0;
tiket26=0;
tiket27=0;
tiket28=0;
tiket29=0;
tiket30=0;

real_prise1=0;
real_prise2=0;

balans=0;
    return(0);
  }
int deinit()
  {
  
   return(0);
  }
int start()
  {
//-----------  
//  SELL=0;
//  BUY=0;
//----------- 
 if(balans==0)
  balans=AccountBalance();
//-------------------------------------------------------------------
  int total=OrdersTotal();
  int pos;  
  double profit1=AccountEquity()- balans;
   
  bool work=true;
  if((profit1<my_profit)&&(profit1>-my_stop))
    work=true;
  else
    work=false;   
//+---------------------------------------------------------------------------------------------------------------------+
//|WORK==TRUE                                                                                                           |
//+---------------------------------------------------------------------------------------------------------------------+      
    
   double prise1=Bid+shag*1*Point;            //+1
   double prise3=real_prise1+shag*2*Point;    //+2
   double prise5=real_prise1+shag*3*Point;    //+3
   double prise7=real_prise1+shag*4*Point;    //+4
   double prise9=real_prise1+shag*5*Point;    //+5
   double prise11=real_prise1+shag*6*Point;   //+6
   double prise13=real_prise1+shag*7*Point;   //+7
   double prise15=real_prise1+shag*8*Point;   //+8
   double prise17=real_prise1+shag*9*Point;   //+9
   double prise19=real_prise1+shag*10*Point;   //+10
   double prise21=real_prise1+shag*11*Point;   //+11
   double prise23=real_prise1+shag*12*Point;   //+12
   double prise25=real_prise1+shag*13*Point;   //+13
   double prise27=real_prise1+shag*14*Point;   //+14
   double prise29=real_prise1+shag*15*Point;   //+15

               
   double prise2=Ask-shag*1*Point;            //-1
   double prise4=real_prise2-shag*2*Point;    //-2
   double prise6=real_prise2-shag*3*Point;    //-3
   double prise8=real_prise2-shag*4*Point;    //-4
   double prise10=real_prise2-shag*5*Point;   //-5
   double prise12=real_prise2-shag*6*Point;   //-6
   double prise14=real_prise2-shag*7*Point;   //-7
   double prise16=real_prise2-shag*8*Point;   //-8
   double prise18=real_prise2-shag*9*Point;   //-9
   double prise20=real_prise2-shag*10*Point;   //-10
   double prise22=real_prise2-shag*11*Point;   //-11
   double prise24=real_prise2-shag*12*Point;   //-12
   double prise26=real_prise2-shag*13*Point;   //-13
   double prise28=real_prise2-shag*14*Point;   //-14
   double prise30=real_prise2-shag*15*Point;   //-15
   
   double profit=0;//             prise1+300*Point;
   double stop=0;//               prise1-300*Point;
//-----------------------------------------------------------------------------------

        
//-----------------------------------------------------------------------------------
   
//--
  if(work==true && order1==0)
   {
   tiket1 = OrderSend(Symbol(),OP_SELLLIMIT,mylot,prise1,3,0,0,NULL,0,0,CLR_NONE );//   &&=И ||=ИЛИ
   if(tiket1>0)
   {
   Print("tiket1=",tiket1);
     if(OrderSelect(tiket1,SELECT_BY_TICKET)==true)
       {
       real_prise1=OrderOpenPrice();
       Print("real_prise1=",real_prise1);
       order1=1;
       Sleep(5000);
       }
   }
   else
   {
   Print("tiket1<0-ПРОИЗОШЛА ОШИБКА-",GetLastError());
   return(0);
   }
   }
//------------------------------------------------------------------------------------   
   RefreshRates();
//------------------------------------------------------------------------------------   
   if(work==true && order2==0)
   {
   tiket2 = OrderSend(Symbol(),OP_BUYLIMIT,mylot,prise2,3,0,0,NULL,0,0,CLR_NONE );
   if(tiket2>0)
   {
   Print("tiket2=",tiket2);
     if(OrderSelect(tiket2,SELECT_BY_TICKET)==true)
       {
       real_prise2=OrderOpenPrice();
       Print("real_prise2=",real_prise2);
       order2=1; 
       Sleep(5000);
       }
   }
   else
   {
   Print("tiket2<0-ПРОИЗОШЛА ОШИБКА-",GetLastError());
   return(0);
   }
   }
//------------------------------------------------------------------------------------   
   RefreshRates();
//------------------------------------------------------------------------------------   
   if(work==true && order3==0)
   {
   tiket3 = OrderSend(Symbol(),OP_SELLLIMIT,mylot,prise3,3,0,0,NULL,0,0,CLR_NONE );
   if(tiket3>0)
   {
   Print("tiket3=",tiket3);
     if(OrderSelect(tiket3,SELECT_BY_TICKET)==true)
       {
       order3=1; 
       Sleep(5000);
       }
   }
   else
   {
   Print("tiket3<0-ПРОИЗОШЛА ОШИБКА-",GetLastError());
   return(0);
   }
   }
//------------------------------------------------------------------------------------   
   RefreshRates();
//------------------------------------------------------------------------------------   
   if(work==true && order4==0)
   {
   tiket4 = OrderSend(Symbol(),OP_BUYLIMIT,mylot,prise4,3,0,0,NULL,0,0,CLR_NONE );
   if(tiket4>0)
   {
   Print("tiket4=",tiket4);
     if(OrderSelect(tiket4,SELECT_BY_TICKET)==true)
       {
       order4=1; 
       Sleep(10000);
       Print("четвёртый установлен, выдержана пауза в 10 сек и вернут return()для запуска слежения и дальнейшей расстановки ордеров");
       return(0);
              }
   }
   else
   {
   Print("tiket4<0-ПРОИЗОШЛА ОШИБКА-",GetLastError());
   return(0);
   }
   }   
//-----------------------------------------------------------------------------------   
//БЛОК ПРОВЕРКИ НА СРАБАТЫВАНИЕ ОРДЕРОВ №1 И №2
//-----------------------------------------------------------------------------------   
  if(work==true && order5==0)
   {
   tiket5 = OrderSend(Symbol(),OP_SELLLIMIT,mylot,prise5,3,0,0,NULL,0,0,CLR_NONE );
   if(tiket5>0)
   {
   Print("tiket5=",tiket5);
     if(OrderSelect(tiket5,SELECT_BY_TICKET)==true)
       {
       order5=1;
       Sleep(5000);
       }
   }
   else
   {
   Print("tiket5<0-ПРОИЗОШЛА ОШИБКА-",GetLastError());
   return(0);
   }
   }
//------------------------------------------------------------------------------------   
   RefreshRates();
//------------------------------------------------------------------------------------   
   if(work==true && order6==0)
   {
   tiket6 = OrderSend(Symbol(),OP_BUYLIMIT,mylot,prise6,3,0,0,NULL,0,0,CLR_NONE );
   if(tiket6>0)
   {
   Print("tiket6=",tiket6);
     if(OrderSelect(tiket6,SELECT_BY_TICKET)==true)
       {
       order6=1; 
       Sleep(5000);
       }
   }
   else
   {
   Print("tiket6<0-ПРОИЗОШЛА ОШИБКА-",GetLastError());
   return(0);
   }
   }
//------------------------------------------------------------------------------------   
   RefreshRates();
//------------------------------------------------------------------------------------   
   if(work==true && order7==0)
   {
   tiket7 = OrderSend(Symbol(),OP_SELLLIMIT,mylot,prise7,3,0,0,NULL,0,0,CLR_NONE );
   if(tiket7>0)
   {
   Print("tiket7=",tiket7);
     if(OrderSelect(tiket7,SELECT_BY_TICKET)==true)
       {
       order7=1; 
       Sleep(5000);
       }
   }
   else
   {
   Print("tiket7<0-ПРОИЗОШЛА ОШИБКА-",GetLastError());
   return(0);
   }
   }
//------------------------------------------------------------------------------------   
   RefreshRates();
//------------------------------------------------------------------------------------   
   if(work==true && order8==0)
   {
   tiket8 = OrderSend(Symbol(),OP_BUYLIMIT,mylot,prise8,3,0,0,NULL,0,0,CLR_NONE );
   if(tiket8>0)
   {
   Print("tiket8=",tiket8);
     if(OrderSelect(tiket8,SELECT_BY_TICKET)==true)
       {
       order8=1; 
       Sleep(10000);
       Print("восьмой установлен, выдержана пауза в 10 сек и вернут return()для запуска слежения и дальнейшей расстановки ордеров");
       return(0);
              }
   }
   else
   {
   Print("tiket8<0-ПРОИЗОШЛА ОШИБКА-",GetLastError());
   return(0);
   }
   }
//-------------------------------------------------------------------------------------
  if(work==true && order9==0)
   {
   tiket9 = OrderSend(Symbol(),OP_SELLLIMIT,mylot,prise9,3,0,0,NULL,0,0,CLR_NONE );
   if(tiket9>0)
   {
   Print("tiket9=",tiket9);
     if(OrderSelect(tiket9,SELECT_BY_TICKET)==true)
       {
       order9=1;
       Sleep(5000);
       }
   }
   else
   {
   Print("tiket9<0-ПРОИЗОШЛА ОШИБКА-",GetLastError());
   return(0);
   }
   }
//------------------------------------------------------------------------------------   
   RefreshRates();
//------------------------------------------------------------------------------------   
   if(work==true && order10==0)
   {
   tiket10 = OrderSend(Symbol(),OP_BUYLIMIT,mylot,prise10,3,0,0,NULL,0,0,CLR_NONE );
   if(tiket10>0)
   {
   Print("tiket10=",tiket10);
     if(OrderSelect(tiket10,SELECT_BY_TICKET)==true)
       {
       order10=1; 
       Sleep(5000);
       }
   }
   else
   {
   Print("tiket10<0-ПРОИЗОШЛА ОШИБКА-",GetLastError());
   return(0);
   }
   }
//------------------------------------------------------------------------------------   
   RefreshRates();
//------------------------------------------------------------------------------------   
   if(work==true && order11==0)
   {
   tiket11 = OrderSend(Symbol(),OP_SELLLIMIT,mylot,prise11,3,0,0,NULL,0,0,CLR_NONE );
   if(tiket11>0)
   {
   Print("tiket11=",tiket11);
     if(OrderSelect(tiket11,SELECT_BY_TICKET)==true)
       {
       order11=1; 
       Sleep(5000);
       }
   }
   else
   {
   Print("tiket11<0-ПРОИЗОШЛА ОШИБКА-",GetLastError());
   return(0);
   }
   }
//------------------------------------------------------------------------------------   
   RefreshRates();
//------------------------------------------------------------------------------------   
   if(work==true && order12==0)
   {
   tiket12 = OrderSend(Symbol(),OP_BUYLIMIT,mylot,prise12,3,0,0,NULL,0,0,CLR_NONE );
   if(tiket12>0)
   {
   Print("tiket12=",tiket12);
     if(OrderSelect(tiket12,SELECT_BY_TICKET)==true)
       {
       order12=1; 
       Sleep(10000);
       Print("двенадцатый установлен, выдержана пауза в 10 сек и вернут return()для запуска слежения и дальнейшей расстановки ордеров");
       return(0);
              }
   }
   else
   {
   Print("tiket12<0-ПРОИЗОШЛА ОШИБКА-",GetLastError());
   return(0);
   }
   }
//---------------------------------------------------------------------------------------------------------------------------
//дошли до 12 ордера
//--------------------------------------------------------------------------------------------------------------------------   
  if(work==true && order13==0)
   {
   tiket13 = OrderSend(Symbol(),OP_SELLLIMIT,mylot,prise13,3,0,0,NULL,0,0,CLR_NONE );
   if(tiket13>0)
   {
   Print("tiket13=",tiket13);
     if(OrderSelect(tiket13,SELECT_BY_TICKET)==true)
       {
       order13=1;
       Sleep(5000);
       }
   }
   else
   {
   Print("tiket13<0-ПРОИЗОШЛА ОШИБКА-",GetLastError());
   return(0);
   }
   }
//------------------------------------------------------------------------------------   
   RefreshRates();
//------------------------------------------------------------------------------------   
   if(work==true && order14==0)
   {
   tiket14 = OrderSend(Symbol(),OP_BUYLIMIT,mylot,prise14,3,0,0,NULL,0,0,CLR_NONE );
   if(tiket14>0)
   {
   Print("tiket14=",tiket14);
     if(OrderSelect(tiket14,SELECT_BY_TICKET)==true)
       {
       order14=1; 
       Sleep(5000);
       }
   }
   else
   {
   Print("tiket14<0-ПРОИЗОШЛА ОШИБКА-",GetLastError());
   return(0);
   }
   }
//------------------------------------------------------------------------------------   
   RefreshRates();
//------------------------------------------------------------------------------------   
   if(work==true && order15==0)
   {
   tiket15 = OrderSend(Symbol(),OP_SELLLIMIT,mylot,prise15,3,0,0,NULL,0,0,CLR_NONE );
   if(tiket15>0)
   {
   Print("tiket15=",tiket15);
     if(OrderSelect(tiket15,SELECT_BY_TICKET)==true)
       {
       order15=1; 
       Sleep(5000);
       }
   }
   else
   {
   Print("tiket15<0-ПРОИЗОШЛА ОШИБКА-",GetLastError());
   return(0);
   }
   }
//------------------------------------------------------------------------------------   
   RefreshRates();
//------------------------------------------------------------------------------------   
   if(work==true && order16==0)
   {
   tiket16 = OrderSend(Symbol(),OP_BUYLIMIT,mylot,prise16,3,0,0,NULL,0,0,CLR_NONE );
   if(tiket16>0)
   {
   Print("tiket16=",tiket16);
     if(OrderSelect(tiket16,SELECT_BY_TICKET)==true)
       {
       order16=1; 
       Sleep(10000);
       Print("шеснадцатый установлен, выдержана пауза в 10 сек и вернут return()для запуска слежения и дальнейшей расстановки ордеров");
       return(0);
              }
   }
   else
   {
   Print("tiket16<0-ПРОИЗОШЛА ОШИБКА-",GetLastError());
   return(0);
   }
   } 
//------------------------------------------------------------------------------------   
   RefreshRates();
//------------------------------------------------------------------------------------        
if(work==true && order17==0)
   {
   tiket17 = OrderSend(Symbol(),OP_SELLLIMIT,mylot,prise17,3,0,0,NULL,0,0,CLR_NONE );//   &&=И ||=ИЛИ
   if(tiket17>0)
   {
   Print("tiket17=",tiket17);
     if(OrderSelect(tiket17,SELECT_BY_TICKET)==true)
       {
       order17=1;
       Sleep(5000);
       }
   }
   else
   {
   Print("tiket17<0-ПРОИЗОШЛА ОШИБКА-",GetLastError());
   return(0);
   }
   }
//------------------------------------------------------------------------------------   
   RefreshRates();
//------------------------------------------------------------------------------------   
   if(work==true && order18==0)
   {
   tiket18 = OrderSend(Symbol(),OP_BUYLIMIT,mylot,prise18,3,0,0,NULL,0,0,CLR_NONE );
   if(tiket18>0)
   {
   Print("tiket18=",tiket18);
     if(OrderSelect(tiket18,SELECT_BY_TICKET)==true)
       {
       order18=1; 
       Sleep(5000);
       }
   }
   else
   {
   Print("tiket18<0-ПРОИЗОШЛА ОШИБКА-",GetLastError());
   return(0);
   }
   }
//------------------------------------------------------------------------------------   
   RefreshRates();
//------------------------------------------------------------------------------------   
   if(work==true && order19==0)
   {
   tiket19 = OrderSend(Symbol(),OP_SELLLIMIT,mylot,prise19,3,0,0,NULL,0,0,CLR_NONE );
   if(tiket19>0)
   {
   Print("tiket19=",tiket19);
     if(OrderSelect(tiket19,SELECT_BY_TICKET)==true)
       {
       order19=1; 
       Sleep(5000);
       }
   }
   else
   {
   Print("tiket19<0-ПРОИЗОШЛА ОШИБКА-",GetLastError());
   return(0);
   }
   }
//------------------------------------------------------------------------------------   
   RefreshRates();
//------------------------------------------------------------------------------------   
   if(work==true && order20==0)
   {
   tiket20 = OrderSend(Symbol(),OP_BUYLIMIT,mylot,prise20,3,0,0,NULL,0,0,CLR_NONE );
   if(tiket20>0)
   {
   Print("tiket20=",tiket20);
     if(OrderSelect(tiket20,SELECT_BY_TICKET)==true)
       {
       order20=1; 
       Sleep(10000);
       Print("четвёртый установлен, выдержана пауза в 10 сек и вернут return()для запуска слежения и дальнейшей расстановки ордеров");
       return(0);
              }
   }
   else
   {
   Print("tiket20<0-ПРОИЗОШЛА ОШИБКА-",GetLastError());
   return(0);
   }
   }   
//-----------------------------------------------------------------------------------   
//БЛОК ПРОВЕРКИ НА СРАБАТЫВАНИЕ ОРДЕРОВ №1 И №2
//-----------------------------------------------------------------------------------   
  if(work==true && order21==0)
   {
   tiket21 = OrderSend(Symbol(),OP_SELLLIMIT,mylot,prise21,3,0,0,NULL,0,0,CLR_NONE );
   if(tiket21>0)
   {
   Print("tiket21=",tiket21);
     if(OrderSelect(tiket21,SELECT_BY_TICKET)==true)
       {
       order21=1;
       Sleep(5000);
       }
   }
   else
   {
   Print("tiket21<0-ПРОИЗОШЛА ОШИБКА-",GetLastError());
   return(0);
   }
   }
//------------------------------------------------------------------------------------   
   RefreshRates();
//------------------------------------------------------------------------------------   
   if(work==true && order22==0)
   {
   tiket22 = OrderSend(Symbol(),OP_BUYLIMIT,mylot,prise22,3,0,0,NULL,0,0,CLR_NONE );
   if(tiket22>0)
   {
   Print("tiket22=",tiket22);
     if(OrderSelect(tiket22,SELECT_BY_TICKET)==true)
       {
       order22=1; 
       Sleep(5000);
       }
   }
   else
   {
   Print("tiket22<0-ПРОИЗОШЛА ОШИБКА-",GetLastError());
   return(0);
   }
   }
//------------------------------------------------------------------------------------   
   RefreshRates();
//------------------------------------------------------------------------------------   
   if(work==true && order23==0)
   {
   tiket23 = OrderSend(Symbol(),OP_SELLLIMIT,mylot,prise23,3,0,0,NULL,0,0,CLR_NONE );
   if(tiket23>0)
   {
   Print("tiket23=",tiket23);
     if(OrderSelect(tiket23,SELECT_BY_TICKET)==true)
       {
       order23=1; 
       Sleep(5000);
       }
   }
   else
   {
   Print("tiket23<0-ПРОИЗОШЛА ОШИБКА-",GetLastError());
   return(0);
   }
   }
//------------------------------------------------------------------------------------   
   RefreshRates();
//------------------------------------------------------------------------------------   
   if(work==true && order24==0)
   {
   tiket24 = OrderSend(Symbol(),OP_BUYLIMIT,mylot,prise24,3,0,0,NULL,0,0,CLR_NONE );
   if(tiket24>0)
   {
   Print("tiket24=",tiket24);
     if(OrderSelect(tiket24,SELECT_BY_TICKET)==true)
       {
       order24=1; 
       Sleep(10000);
       Print("восьмой установлен, выдержана пауза в 10 сек и вернут return()для запуска слежения и дальнейшей расстановки ордеров");
       return(0);
              }
   }
   else
   {
   Print("tiket24<0-ПРОИЗОШЛА ОШИБКА-",GetLastError());
   return(0);
   }
   }
//-------------------------------------------------------------------------------------
  if(work==true && order25==0)
   {
   tiket25 = OrderSend(Symbol(),OP_SELLLIMIT,mylot,prise25,3,0,0,NULL,0,0,CLR_NONE );
   if(tiket25>0)
   {
   Print("tiket25=",tiket25);
     if(OrderSelect(tiket25,SELECT_BY_TICKET)==true)
       {
       order25=1;
       Sleep(5000);
       }
   }
   else
   {
   Print("tiket25<0-ПРОИЗОШЛА ОШИБКА-",GetLastError());
   return(0);
   }
   }
//------------------------------------------------------------------------------------   
   RefreshRates();
//------------------------------------------------------------------------------------   
   if(work==true && order26==0)
   {
   tiket26 = OrderSend(Symbol(),OP_BUYLIMIT,mylot,prise26,3,0,0,NULL,0,0,CLR_NONE );
   if(tiket26>0)
   {
   Print("tiket26=",tiket26);
     if(OrderSelect(tiket26,SELECT_BY_TICKET)==true)
       {
       order26=1; 
       Sleep(5000);
       }
   }
   else
   {
   Print("tiket26<0-ПРОИЗОШЛА ОШИБКА-",GetLastError());
   return(0);
   }
   }
//------------------------------------------------------------------------------------   
   RefreshRates();
//------------------------------------------------------------------------------------   
   if(work==true && order27==0)
   {
   tiket27 = OrderSend(Symbol(),OP_SELLLIMIT,mylot,prise27,3,0,0,NULL,0,0,CLR_NONE );
   if(tiket27>0)
   {
   Print("tiket27=",tiket27);
     if(OrderSelect(tiket27,SELECT_BY_TICKET)==true)
       {
       order27=1; 
       Sleep(5000);
       }
   }
   else
   {
   Print("tiket27<0-ПРОИЗОШЛА ОШИБКА-",GetLastError());
   return(0);
   }
   }
//------------------------------------------------------------------------------------   
   RefreshRates();
//------------------------------------------------------------------------------------   
   if(work==true && order28==0)
   {
   tiket28 = OrderSend(Symbol(),OP_BUYLIMIT,mylot,prise28,3,0,0,NULL,0,0,CLR_NONE );
   if(tiket28>0)
   {
   Print("tiket28=",tiket28);
     if(OrderSelect(tiket28,SELECT_BY_TICKET)==true)
       {
       order28=1; 
       Sleep(10000);
       Print("двенадцатый установлен, выдержана пауза в 10 сек и вернут return()для запуска слежения и дальнейшей расстановки ордеров");
       return(0);
              }
   }
   else
   {
   Print("tiket28<0-ПРОИЗОШЛА ОШИБКА-",GetLastError());
   return(0);
   }
   }
//---------------------------------------------------------------------------------------------------------------------------
//дошли до 12 ордера
//--------------------------------------------------------------------------------------------------------------------------   
  if(work==true && order29==0)
   {
   tiket29 = OrderSend(Symbol(),OP_SELLLIMIT,mylot,prise29,3,0,0,NULL,0,0,CLR_NONE );
   if(tiket29>0)
   {
   Print("tiket29=",tiket29);
     if(OrderSelect(tiket29,SELECT_BY_TICKET)==true)
       {
       order29=1;
       Sleep(5000);
       }
   }
   else
   {
   Print("tiket29<0-ПРОИЗОШЛА ОШИБКА-",GetLastError());
   return(0);
   }
   }
//------------------------------------------------------------------------------------   
   RefreshRates();
//------------------------------------------------------------------------------------   
   if(work==true && order30==0)
   {
   tiket30 = OrderSend(Symbol(),OP_BUYLIMIT,mylot,prise30,3,0,0,NULL,0,0,CLR_NONE );
   if(tiket30>0)
   {
   Print("tiket30=",tiket30);
     if(OrderSelect(tiket30,SELECT_BY_TICKET)==true)
       {
       order30=1; 
       Sleep(5000);
       }
   }
   else
   {
   Print("tiket30<0-ПРОИЗОШЛА ОШИБКА-",GetLastError());
   return(0);
   }
   }
/*/------------------------------------------------------ОРДЕРА ВЫСТАВЛЕНЫ------------------------------------------------------// 
    for ( pos = 0; pos<total; pos++ )
     {
       if (OrderSelect(pos, SELECT_BY_POS, MODE_TRADES) == true)
         {
          //--
          if(OrderType()==OP_SELLSTOP)
          SELL=SELL+1;
          //--
          if(OrderType()==OP_BUYSTOP)
          BUY=BUY+1;
          //--       
          }
       else
           Print("Ошибка ", GetLastError(), " при выборе ордера номер ", pos);
           } 

//+------------------------------------------------------------------+
//|WORK==FALSE                                                       |
//+------------------------------------------------------------------+  */ 
   
   if(work==false && total!=0)// || SELL==0 || BUY==0)
     { 
    for ( pos = 0; pos<total; pos++ )
     {
       if (OrderSelect(pos, SELECT_BY_POS, MODE_TRADES) == true)
         {
         //--
          if((OrderType()==OP_SELLLIMIT )||(OrderType()==OP_BUYLIMIT))
          {
          if (OrderDelete(OrderTicket()))
             Print("Стоп ордер удален");
           else
             Print("Ошибка ", GetLastError(), " при удалении стоп ордера");
          }
          //-- 
         double price;
         if (OrderType()==OP_SELL) 
             price = MarketInfo(OrderSymbol(), MODE_ASK);
         else
             price = MarketInfo(OrderSymbol(), MODE_BID);     
         if (OrderClose(OrderTicket(), OrderLots(), price, 3)==true)
          Print("закрыта позиция", OrderTicket());
          else
          Print("Ошибка ", GetLastError()," при закрытии позиции ", OrderTicket());         
          }
       else
           Print("Ошибка ", GetLastError(), " при выборе ордера номер ", pos);
           } 
   }
   if(work==false && total==0)
       {          
order1=0;
order2=0;
order3=0;
order4=0;
order5=0;
order6=0;
order7=0;
order8=0;
order9=0;
order10=0;
order11=0;
order12=0;
order13=0;
order14=0;
order15=0;
order16=0;
order17=0;
order18=0;
order19=0;
order20=0;
order21=0;
order22=0;
order23=0;
order24=0;
order25=0;
order26=0;
order27=0;
order28=0;
order29=0;
order30=0;
  
tiket1=0;
tiket2=0;
tiket3=0;
tiket4=0;
tiket5=0;
tiket6=0;
tiket7=0;
tiket8=0;
tiket9=0;
tiket10=0;
tiket11=0;
tiket12=0;
tiket13=0;
tiket14=0;
tiket15=0;
tiket16=0;
tiket17=0;
tiket18=0;
tiket19=0;
tiket20=0;
tiket21=0;
tiket22=0;
tiket23=0;
tiket24=0;
tiket25=0;
tiket26=0;
tiket27=0;
tiket28=0;
tiket29=0;
tiket30=0;
real_prise1=0;
real_prise2=0; //реальная цена срабатывания ордера
balans=0;

}
/*if((SELL==0 || BUY==0) && total==0)
       {          
order1=0;
order2=0;
order3=0;
order4=0;
order5=0;
order6=0;
order7=0;
order8=0;
order9=0;
order10=0;
order11=0;
order12=0;
order13=0;
order14=0;
order15=0;
order16=0;
order17=0;
order18=0;
order19=0;
order20=0;
order21=0;
order22=0;
order23=0;
order24=0;
order25=0;
order26=0;
order27=0;
order28=0;
order29=0;
order30=0;
  
tiket1=0;
tiket2=0;
tiket3=0;
tiket4=0;
tiket5=0;
tiket6=0;
tiket7=0;
tiket8=0;
tiket9=0;
tiket10=0;
tiket11=0;
tiket12=0;
tiket13=0;
tiket14=0;
tiket15=0;
tiket16=0;
tiket17=0;
tiket18=0;
tiket19=0;
tiket20=0;
tiket21=0;
tiket22=0;
tiket23=0;
tiket24=0;
tiket25=0;
tiket26=0;
tiket27=0;
tiket28=0;
tiket29=0;
tiket30=0;
real_prise1=0;
real_prise2=0; //реальная цена срабатывания ордера

}*/
  Print("total=",total,", work=",work);
  Print("balans=",balans,", profit1=",profit1);   
//----
   return(0);
  }
   