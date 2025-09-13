//+------------------------------------------------------------------+
//|                                                exp_statusbot.mq4 |
//|                               Leonid Salavatov [MUSTADDON]© 2010 |
//+------------------------------------------------------------------+
#property copyright "Leonid Salavatov [MUSTADDON]© 2010"
//---- externs
extern string statusfilename = "status.txt";
extern string spamfilename   = "notify.txt";
extern string reportfilename = "report.txt";
//---- vars
string expname = "statusbot";
int    ord_tickets[];
int    ord_tickets_past[];
int    ord_tickets_changing_open[];
int    ord_tickets_changing_close[];
double curbalance = 0.0;
//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
int init()
  {
//----
   WriteReport();
   WriteStatus();
//----
   string filename = expname+"/"+spamfilename;
   int filehandle=FileOpen(filename,FILE_WRITE);  
   if(filehandle>0)
     {FileWrite(filehandle,"Starting expert "+expname);
      FileClose(filehandle);
     }
   else Print("Не удалось создать файл ",spamfilename,", Error:",GetLastError()); 
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| expert deinitialization function                                 |
//+------------------------------------------------------------------+
int deinit()
  {
//----
   FileDelete(expname+"/"+statusfilename);
   FileDelete(expname+"/"+spamfilename);
   FileDelete(expname+"/"+reportfilename);
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| expert start function                                            |
//+------------------------------------------------------------------+
int start()
  {
//---- собираем статистику и пишем файл report.txt
   WriteReport();
//---- собираем инфу и пишем файл status.txt
   WriteStatus();
//---- собираем инфу и пишем файл notify.txt
   WriteNotify();
//----
   return(0);
  }
//+------------------------------------------------------------------+
void WriteStatus()
  {int profit;
   ArrayResize(ord_tickets, 0);
   string filename = expname+"/"+statusfilename;
   string abzac ="-----------";
   int filehandle=FileOpen(filename,FILE_WRITE,"  "); 
   if(filehandle>0)
     {FileWrite(filehandle,"Balance =",DoubleToStr(AccountBalance(),2),AccountCurrency());
      if(OrdersTotal()>0)
        {FileWrite(filehandle,abzac);
         for(int i=0;i<OrdersTotal();i++)
            {if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES)==false)     break;
             ArrayResize(ord_tickets, i+1);
             ord_tickets[i]=OrderTicket();
             if(OrderType()==OP_BUY)
               {profit=(MarketInfo(OrderSymbol(),MODE_BID)-OrderOpenPrice())/MarketInfo(OrderSymbol(),MODE_POINT);
                FileWrite(filehandle,OrderSymbol(),"buy",DoubleToStr(OrderLots(),2),profit,"pips");
               }
             if(OrderType()==OP_SELL)
               {profit=(OrderOpenPrice()-MarketInfo(OrderSymbol(),MODE_ASK))/MarketInfo(OrderSymbol(),MODE_POINT);
                FileWrite(filehandle,OrderSymbol(),"sell",DoubleToStr(OrderLots(),2),profit,"pips");
               }
            }
         FileWrite(filehandle,"summa =",DoubleToStr(AccountProfit(),2),AccountCurrency());  
         FileWrite(filehandle,abzac);
         FileWrite(filehandle,"Equity =",DoubleToStr(AccountEquity(),2),AccountCurrency());
        }    
      FileClose(filehandle);
     }
   else Print("Не удалось создать файл ",statusfilename,", Error:",GetLastError());
  }
void WriteNotify()
  {//---- проверяем добавление/удаление ордеров
   int size = 0;
   int profit;
   ArrayResize(ord_tickets_changing_open, ArraySize(ord_tickets));
   ArrayResize(ord_tickets_changing_close, ArraySize(ord_tickets_past));
   
   for(int j=0;j<ArraySize(ord_tickets);j++)
     {for(int i=0;i<ArraySize(ord_tickets_past);i++)
         {if(ord_tickets[j]==ord_tickets_past[i]) break;}
      if(i==ArraySize(ord_tickets_past) && ArraySize(ord_tickets_changing_open)>0){ord_tickets_changing_open[size]=ord_tickets[j];size++;}
     }
   ArrayResize(ord_tickets_changing_open, size); 
   size=0;
   for(j=0;j<ArraySize(ord_tickets_past);j++)
     {for(i=0;i<ArraySize(ord_tickets);i++)
        {if(ord_tickets[i]==ord_tickets_past[j]) break;}
      if(i==ArraySize(ord_tickets) && ArraySize(ord_tickets_changing_close)>0){ord_tickets_changing_close[size]=ord_tickets_past[j];size++;}
     }
   ArrayResize(ord_tickets_changing_close, size);
   ArrayResize(ord_tickets_past, ArraySize(ord_tickets));
   if(ArraySize(ord_tickets)>0) ArrayCopy(ord_tickets_past,ord_tickets,0,0,WHOLE_ARRAY);
   if(ArraySize(ord_tickets_changing_open)==0 && ArraySize(ord_tickets_changing_close)==0) return;
   //---- если есть изменения то пишем notify.txy
   string addoninfo;
   string filename = expname+"/"+spamfilename;
   int    filehandle=FileOpen(filename,FILE_WRITE,"  "); 
   if(filehandle>0)
     {for(j=0;j<ArraySize(ord_tickets_changing_open);j++)
        {addoninfo="[order added]";
         if(OrderSelect(ord_tickets_changing_open[j],SELECT_BY_TICKET,MODE_TRADES)==true)
           {if(OrderType()==OP_BUY)
              {profit=(MarketInfo(OrderSymbol(),MODE_BID)-OrderOpenPrice())/MarketInfo(OrderSymbol(),MODE_POINT);
               FileWrite(filehandle,addoninfo,OrderSymbol(),"buy",DoubleToStr(OrderLots(),2),profit,"pips");
              }
            if(OrderType()==OP_SELL)
              {profit=(OrderOpenPrice()-MarketInfo(OrderSymbol(),MODE_ASK))/MarketInfo(OrderSymbol(),MODE_POINT);
               FileWrite(filehandle,addoninfo,OrderSymbol(),"sell",DoubleToStr(OrderLots(),2),profit,"pips");
              }
           }
        }
      for(j=0;j<ArraySize(ord_tickets_changing_close);j++)
        {addoninfo="[order closed]";
         if(OrderSelect(ord_tickets_changing_close[j],SELECT_BY_TICKET,MODE_HISTORY)==true)
            {if(OrderType()==OP_BUY)
               {profit=(OrderClosePrice()-OrderOpenPrice())/MarketInfo(OrderSymbol(),MODE_POINT);
                FileWrite(filehandle,addoninfo,OrderSymbol(),"buy",DoubleToStr(OrderLots(),2),profit,"pips");
               }
             if(OrderType()==OP_SELL)
               {profit=(OrderOpenPrice()-OrderClosePrice())/MarketInfo(OrderSymbol(),MODE_POINT);
                FileWrite(filehandle,addoninfo,OrderSymbol(),"sell",DoubleToStr(OrderLots(),2),profit,"pips");
               }
            }
        }
      FileClose(filehandle);
     }
   else Print("Не удалось создать файл ",spamfilename,", Error:",GetLastError()); 
  }
void WriteReport()
  {if(AccountBalance()==curbalance) return;
   if(OrdersHistoryTotal()<=0) return;
   string report_buffer[];
   string report_buffer_sorted[];
   ArrayResize(report_buffer,OrdersHistoryTotal());
   int report_size = 0;
   string rts = "|";
   string opentime,type,size,item,openprice,loss_lim,profit_lim,closetime,closeprice,commision,swap,profit;
   //---- собираем историю
   for(int i=0;i<OrdersHistoryTotal();i++)
     {if(OrderSelect(i,SELECT_BY_POS,MODE_HISTORY)==false)  break;
      if(OrderType()!=OP_BUY && OrderType()!=OP_SELL)       continue;
      if(OrderType()==OP_BUY) type="buy";
      if(OrderType()==OP_SELL)type="sell";
      opentime   = TimeToStr(OrderOpenTime(),TIME_DATE|TIME_MINUTES);
      size       = DoubleToStr(OrderLots(),2);
      item       = OrderSymbol();
      openprice  = DoubleToStr(OrderOpenPrice(),MarketInfo(OrderSymbol(),MODE_DIGITS));
      loss_lim   = DoubleToStr(OrderStopLoss(),MarketInfo(OrderSymbol(),MODE_DIGITS));
      profit_lim = DoubleToStr(OrderTakeProfit(),MarketInfo(OrderSymbol(),MODE_DIGITS));
      closetime  = TimeToStr(OrderCloseTime(),TIME_DATE|TIME_MINUTES);
      closeprice = DoubleToStr(OrderOpenPrice(),MarketInfo(OrderSymbol(),MODE_DIGITS));
      commision  = DoubleToStr(OrderCommission(),2);
      swap       = DoubleToStr(OrderSwap(),2);
      profit     = DoubleToStr(OrderProfit(),2);
      report_buffer[report_size] = opentime+rts+type+rts+size+rts+item+rts+openprice+rts+loss_lim+rts+profit_lim+rts+closetime+rts+closeprice+rts+commision+rts+swap+rts+profit;
      report_size++;
     }
   //---- сортируем по дате закрытия
   ArrayResize(report_buffer_sorted,report_size);
   datetime mindate;
   datetime curdate;
   int      minid;
   int      firstid=-1;
   int      startpos,endpos;
   for(i=0;i<report_size;i++)
     {for(int j=0;j<report_size;j++)
         {if(report_buffer[j]=="") continue;
          startpos=0;
          for(int k=1;k<8;k++)
             {startpos=StringFind(report_buffer[j],rts,startpos)+1;
              endpos=StringFind(report_buffer[j],rts,startpos);
             }
          curdate=StrToTime(StringSubstr(report_buffer[j],startpos,endpos-startpos));
          if(firstid<0) {mindate=curdate;minid=j;firstid=j;continue;}
          if(curdate<mindate) {mindate=curdate;minid=j;}
         }
      report_buffer_sorted[i]=report_buffer[minid];
      report_buffer[minid]="";
      firstid=-1;
     }
   //---- пишем report.txt
   string filename = expname+"/"+reportfilename;
   int filehandle=FileOpen(filename,FILE_CSV|FILE_WRITE,rts);  
   if(filehandle>0)
     {for(i=0;i<report_size;i++)
        {
         FileWrite(filehandle,"@",report_buffer_sorted[i]);
        }
      FileWrite(filehandle,"#",DoubleToStr(AccountBalance(),2),AccountCurrency());
      FileClose(filehandle);
      curbalance=AccountBalance();
     }
   else Print("Не удалось создать файл ",reportfilename,", Error:",GetLastError()); 
  }
//+------------------------------------------------------------------+