//+------------------------------------------------------------------+
//|                                            auto_optimization.mqh |
//|                                           Copyright © 2006, XEON |
//|                                                       xeon@nm.ru |
//+------------------------------------------------------------------+
//-------------------------------------------
#import  "shell32.dll"                                                       //Подключим dll (входит в состав windows)       
  int ShellExecuteA(int hwnd,string Operation,string File,string Parameters,string Directory,int ShowCmd); 
#import
//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
int Tester(int TestDay,string NameMTS,string NameFileSet,string PuthTester,int TimeOut,int Gross_Profit,int Profit_Factor,int Expected_Payoff,string Per1,string Per2,string Per3,string Per4)
  {
   string PuthTerminal    = TerminalPath()+"\experts\files";                  //Путь к терминалу 
   string FileOptim       = "optimise.ini";                                   //Имя ini файла для тестера
   string FileOptim1      = "\optimise.ini";  
      string FileTemp        = "\temp.htm";                         
   datetime DayStart      = TimeLocal()-86400*TestDay;                        //Расчет даты старта
   string DateStart       = TimeToStr(DayStart,TIME_DATE);                    //Дата напчала оптимизации
   string DateStop        = TimeToStr(TimeLocal(),TIME_DATE);                 //Дата окончания оптимизации
   string FileReport      = "FileReport_"+Symbol()+"_"+DateStop+".htm";       //Имя файла отчета тестера
   string FileReport1     = "\FileReport_"+Symbol()+"_"+DateStop+".htm";
   double MinTr           = TestDay-2;                                        //Ограничение на минимальное количество сделок в день
   double MaxTr           = (60/Period()*TestDay)+2;                          //Ограничение на максимальное количество сделок в день
   int    KvoPptk         = 10;                                               //Количество попыток скопировать файл отчета
   int    StepRes         = 50;                                               //Количество строк для сортировки
//-------------------------------Прочие промежуточные переменные------------------------
   int    copyini,start,opttim,file,Pptk,OptimArraySize,tempf;
   int    P1,P2,P3,P4,P1k,P2k,P3k,P4k;
   int    ClStep,ClStepRazm,GrProf,GrProfRazm,TotTrad,TotTradRazm,ProfFact,ProfFactRazm,ExpPay,ExpPayRazm;
   int    text,index,kol,NumberStr,NumStr,test,CopyAr;
   int    GrosPr,PrCycle,Dubl;
   int    ResizeArayNew;
   double kol1,kol2,kol3,kol4,kol5,kol6,kol7,kol8,kol9;
   double TotalTradesTransit,GrossProfitTransit,ExpectedPayoffTran;
   double PrFactDouble;
   double Prior1, Prior2, Prior3, transit, transit1, transit2, transit3, transit4;
   double NewPrior, NewPrior1, NewPrior2, NewPrior3, Sort, SortTrans;
   string FileLine; 
   string ini;
   string CycleStep,GrossProfit,TotalTrades,ProfitFactor,ExpectedPayoff;
   string Perem1,Perem2,Perem3,Perem4; 
   string select;
   bool   nodubl;
//----------------------------------- Подготовим массивы -------------------------
   string ArrayOpttim[15]; 
   string ArrayStrg[10]; 
   double ArrayData[10][9]; 
   double ArrayTrans[10][9];
//------------------------------Подготовим ini файл для оптимизации----------------
   ArrayOpttim[0] = ";optimise strategy tester";             
   ArrayOpttim[1] = "ExpertsEnable=false";                        //Вкл/Выкл эксперты
   ArrayOpttim[2] = "TestExpert="+NameMTS;                        //Наименование файла эксперта
   ArrayOpttim[3] = "TestExpertParameters="+NameFileSet;          //Наименование файла с параметрами
   ArrayOpttim[4] = "TestSymbol="+Symbol();                       //Инструмент
   ArrayOpttim[5] = "TestPeriod="+Period();                       //Период
   ArrayOpttim[6] = "TestModel="+0;                               //Режим моделирования
   ArrayOpttim[7] = "TestRecalculate=false";                      //Пересчитать
   ArrayOpttim[8] = "TestOptimization=true";                      //Оптимизация
   ArrayOpttim[9] = "TestDateEnable=true";                        //Использовать дату
   ArrayOpttim[10]= "TestFromDate="+DateStart;                    //Дата начала тестирования
   ArrayOpttim[11]= "TestToDate="+DateStop;                       //Дата окончания тестирования
   ArrayOpttim[12]= "TestReport="+FileReport;                     //Имя файла отчета
   ArrayOpttim[13]= "TestReplaceReport=true";                     //Перезапись файла отчета
   ArrayOpttim[14]= "TestShutdownTerminal=true";                  //Закрыть терминал по завершению
//------------------------------- Запишем данные в ini файл --------------------------                 
   OptimArraySize=ArraySize(ArrayOpttim);                         //Выясним размер массива
   opttim=FileOpen(FileOptim,FILE_CSV|FILE_WRITE,0x7F);           //Откроем фал для записи
   if(opttim>0){
      for(int i=0; i<OptimArraySize; i++){
          ini=ArrayOpttim[i];                                     //из массива в переменную
          FileWrite(opttim, ini);                                 //из переменной в файл
      } 
      FileClose(opttim);                                          //закроем файл
   }
   else{Print("Неудалось записать данные в ini файл. Ошибка № ",GetLastError());return(0);}
//-------------------------- скопируем ini файл в песочницу тестера ----------
   copyini = ShellExecuteA(0,"Open","xcopy","\""+PuthTerminal+FileOptim1+"\" \""+PuthTester+"\" /y","",3);
   Sleep(1200);                                                    //подождем пока скопируется файл
   if(copyini<0){Print("Неудалось скопировать ini файл");return(0);}
//---------------------------------- Запустим Тестер -------------------------
   start   = ShellExecuteA(0,"Open","terminal.exe",FileOptim,PuthTester,3);
   if( start<0){Print("Неудалось запустить тестер");return(0);}
//------------------------ скопируем файл отчета в песочницу терминала -------
    Comment("Ожидаем окончания оптимизации в "+TimeToStr(TimeLocal()+60*TimeOut,TIME_MINUTES));
    Sleep(60000*TimeOut);                                           //подождем окончания оптимизации
    for(Pptk=0;Pptk<KvoPptk;Pptk++){                                //Запустим цикл попыток копирования файла отчета
        Comment("Попытка № "+Pptk+" скопировать файл отчета");
        copyini = ShellExecuteA(0,"Open","xcopy","\""+PuthTester+FileReport1+"\" \""+PuthTerminal+"\" /y","",3);
        Sleep(1200);                                                //подождем пока скопируется файл
        file=FileOpen(FileReport,FILE_READ,0x7F);                   //Попытаемся открыть файл отчета
        if(file<0){Sleep(60000);}                                   //если не удалось, ещё подождем и попробуем снова
        else break;             
    }
    if(file<0){Print("Неудалось скопировать файл отчета");return(0);}
//---------------- Чтение из файла в массив ----------------------------------
    while(FileIsEnding(file)==false){                               //Пока не наступил конец файла - цикл
          FileLine=FileReadString(file);                            //Прочитаем строку из файла отчета
          index=StringFind(FileLine, "title=", 10);                  //Найдем нужную строку и установим точку отсчета
          if(index>0){
             ArrayResize(ArrayStrg,NumStr+1);                       //Увеличиваем размер массива
             ArrayStrg[NumStr]=FileLine;                            //Записываем в массив строки из файла
             NumStr++;
    }}
    FileClose(file);                                                //Закроем файл
    FileDelete(FileReport);                                         //Удалим файл чтоб не плодить копии
    ArrayResize(ArrayData,NumStr);                                  //Установим размер массива по количеству считанных из файла строк
    for(text=0;text<NumStr;text++){
        select=ArrayStrg[text]; 
//-------------------------------------------------------------------------
//                    Обработка текста отчета  (Отделяем мух от котлет)       
        //---------------------Позиция Проход-----------------------------
        ClStep=StringFind(select, "; \">",20)+4;                                       //Найдем начало позиции
        ClStepRazm=StringFind(select, "</td>",ClStep);                                 //Найдем конец позиции
        CycleStep = StringSubstr(select, ClStep, ClStepRazm-ClStep);                   //Считаем значение
        //---------------- Позиция Прибыль -------------------------------
        GrProf=StringFind(select, "<td class=mspt>",ClStepRazm-1);                                  //Найдем начало позиции
        GrProfRazm=StringFind(select, "</td>",GrProf);                                 //Найдем конец позиции
        GrossProfit = StringSubstr(select, GrProf+15,GrProfRazm-(GrProf+15));            //Считаем значение
         //-------------Позиция Всего Сделок -----------------------------
        TotTrad=StringFind(select, "<td>",GrProfRazm);                                 //Найдем начало позиции
        TotTradRazm=StringFind(select, "</td>",TotTrad);                               //Найдем конец позиции
        TotalTrades = StringSubstr(select, TotTrad+4,TotTradRazm-(TotTrad+4));         //Считаем значение
        //-------------Позиция Прибыльность--------------------------------
        ProfFact=StringFind(select, "<td class=mspt>",TotTradRazm-1);                               //Найдем начало позиции
        ProfFactRazm=StringFind(select, "</td>",ProfFact);                             //Найдем конец позиции
        ProfitFactor = StringSubstr(select, ProfFact+15,ProfFactRazm-(ProfFact+15));     //Считаем значение
       //-------------Позиция Мат Ожидание---------------------------------
        ExpPay=StringFind(select, "<td class=mspt>",ProfFactRazm-1);                                //Найдем начало позиции
        ExpPayRazm=StringFind(select, "</td>",ExpPay);                                 //Найдем конец позиции
        ExpectedPayoff = StringSubstr(select, ExpPay+15,ExpPayRazm-(ExpPay+15));         //Считаем значение
        //------------------------------------------------------------------
        //-------------Позиции переменных-начиная со второй---------------------
        P1=StringFind(select, Per1,20);                                                //Найдем начало позиции
        P1k=StringFind(select, ";",P1);                                                //Найдем конец позиции
        Perem1 = StringSubstr(select,P1+StringLen(Per1)+1,P1k-(P1+1+StringLen(Per1))); //Считаем Переменную
        P2=StringFind(select, Per2,20);                                                //Найдем начало позиции
        P2k=StringFind(select, ";",P2);                                                //Найдем конец позиции 
        Perem2 = StringSubstr(select,P2+StringLen(Per2)+1,P2k-(P2+1+StringLen(Per2))); //Считаем Переменную
        P3=StringFind(select, Per3,20);                                                //Найдем начало позиции
        P3k=StringFind(select, ";",P3);                                                //Найдем конец позиции
        Perem3 = StringSubstr(select,P3+StringLen(Per3)+1,P3k-(P3+1+StringLen(Per3))); //Считаем Переменную
        P4=StringFind(select, Per4,20);                                                //Найдем начало позиции
        P4k=StringFind(select, ";",P4);                                                //Найдем конец позиции
        Perem4 = StringSubstr(select,P4+StringLen(Per4)+1,P4k-(P4+1+StringLen(Per4))); //Считаем Переменную
        Comment("Идет анализ полученных результатов"); 
//-----------------------Переведем в числовой формат----------------------------
       TotalTradesTransit=NormalizeDouble(StrToDouble(TotalTrades),0);
       GrossProfitTransit=NormalizeDouble(StrToDouble(GrossProfit),0);
       ExpectedPayoffTran=NormalizeDouble(StrToDouble(ExpectedPayoff),2);
       nodubl=true;
       if(MinTr < TotalTradesTransit && MaxTr > TotalTradesTransit){                    //Отфильтруем по количеству сделок
          PrFactDouble = NormalizeDouble(StrToDouble(ProfitFactor),2);
          if(PrFactDouble==0){PrFactDouble=1000;}                                       //Убираем 0 в прибыльности для правильного анализа
//-------------- Отфильтруем данные с одинаковыми значениями -------------------       
           for(Dubl=0;Dubl<=text;Dubl++){                                               //Запустим цикл поиска одинаковых значений
               if(GrossProfitTransit == ArrayData[Dubl][1]){                            //проверим совпадения резултатов по максимальной прибыли
                  if(TotalTradesTransit == ArrayData[Dubl][2]){                         //проверим совпадения резултатов по количеству сделок
                     if(PrFactDouble == ArrayData[Dubl][3]){                            //проверим совпадения резултатов по прибыльности
                        if(ExpectedPayoffTran == ArrayData[Dubl][4]){                   //проверим совпадения резултатов по матожиданию
                           nodubl=false;                                                //Если все совпало, то поставим флаг совпадения
           }}}}}
            
//---------------- Запишем отфильтрованные данные в массив ----------------------
           if(nodubl){
              ArrayData[ResizeArayNew][1]=GrossProfitTransit;                                
              ArrayData[ResizeArayNew][2]=TotalTradesTransit;
              ArrayData[ResizeArayNew][3]=PrFactDouble;
              ArrayData[ResizeArayNew][4]=ExpectedPayoffTran;
              ArrayData[ResizeArayNew][5]=StrToDouble(Perem1);
              ArrayData[ResizeArayNew][6]=StrToDouble(Perem2);
              ArrayData[ResizeArayNew][7]=StrToDouble(Perem3);
              ArrayData[ResizeArayNew][8]=StrToDouble(Perem4);
              ResizeArayNew++;
    }}}   
    //||||||||||||||||||||||||||||||| Запишем данные в Промежуточный файл ||||||||||||||||||||||||||||||||||||                 
   int FileTst;
   int trs1,trs2,trs3,trs6,trs7,trs8,trs9;
   double trs4,trs5;
   FileTst=FileOpen("FileTest1.csv",FILE_CSV|FILE_WRITE,0x7F);           //Откроем фал для записи
   FileWrite(FileTst,"GrossProfit ; TotalTrades ; ProfitFactor ; ExpectedPayoff ;"+Per1+";"+Per2+";"+Per3+";"+Per4); 
   if(FileTst>0){
      for(i=0; i<ResizeArayNew; i++){
           trs2 = ArrayData[i][1];             
           trs3 = ArrayData[i][2];
           trs4 = ArrayData[i][3];             
           trs5 = ArrayData[i][4];             
           trs6 = ArrayData[i][5];
           trs7 = ArrayData[i][6];
           trs8 = ArrayData[i][7];
           trs9 = ArrayData[i][8];
          FileWrite(FileTst,trs2+";"+trs3+";"+trs4+";"+trs5+";"+trs6+";"+trs7+";"+trs8+";"+trs9);                                   //из переменной в файл
      } 
      FileClose(FileTst);                                          //закроем файл промежуточных данных
   }
   else{
        Print("Неудалось записать данные в тестовый файл. Ошибка № ",GetLastError());
        Comment("Ошибка !!! Возможно файл FileTest1 открыт другой программой");
        return(0);
   }
//|||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
FileTst=FileOpen("FileTest2.csv",FILE_CSV|FILE_WRITE,0x7F);                      
 
//------------------------------Анализатор---------------------------------------- 
// Принцип анализа - последовательная проверка максимальных значений согласно заданному приоритету фильтрации   
   ArrayResize(ArrayTrans, ResizeArayNew-1);
   for(int PrioStep=1;PrioStep<4;PrioStep++){
   FileWrite(FileTst,"GrossProfit ; TotalTrades ; ProfitFactor ; ExpectedPayoff ;"+Per1+";"+Per2+";"+Per3+";"+Per4); 
       for(PrCycle=0;PrCycle<ResizeArayNew;PrCycle++){
           Sort     = ArrayData[PrCycle][0];
           Prior1   = ArrayData[PrCycle][1];             
           transit  = ArrayData[PrCycle][2];
           Prior2   = ArrayData[PrCycle][3];             
           Prior3   = ArrayData[PrCycle][4];             
           transit1 = ArrayData[PrCycle][5];
           transit2 = ArrayData[PrCycle][6];
           transit3 = ArrayData[PrCycle][7];
           transit4 = ArrayData[PrCycle][8]; 
           
           if(PrioStep==1){
              //Подготовимся к 1 сортировке
              if(Gross_Profit   ==1){SortTrans=Prior1;}
              if(Profit_Factor  ==1){SortTrans=Prior2;}
              if(Expected_Payoff==1){SortTrans=Prior3;}
           }
           if(PrioStep==2){
              //Восстановимся
              if(Gross_Profit   ==1){Prior1=Sort;}
              if(Profit_Factor  ==1){Prior2=Sort;}
              if(Expected_Payoff==1){Prior3=Sort;} 
              //Подготовимся ко 2 сортировке
              if(Gross_Profit   ==2){SortTrans=Prior1;}
              if(Profit_Factor  ==2){SortTrans=Prior2;}
              if(Expected_Payoff==2){SortTrans=Prior3;}
           }
           if(PrioStep==3){
              //Восстановимся
              if(Gross_Profit   ==2){Prior1=Sort;}
              if(Profit_Factor  ==2){Prior2=Sort;}
              if(Expected_Payoff==2){Prior3=Sort;} 
              //Подготовимся к 3 сортировке
              if(Gross_Profit   ==3){SortTrans=Prior1;}
              if(Profit_Factor  ==3){SortTrans=Prior2;}
              if(Expected_Payoff==3){SortTrans=Prior3;}
           }          
           ArrayTrans[PrCycle][0] = SortTrans;
           ArrayTrans[PrCycle][1] = Prior1;
           ArrayTrans[PrCycle][2] = transit;
           ArrayTrans[PrCycle][3] = Prior2;
           ArrayTrans[PrCycle][4] = Prior3;
           ArrayTrans[PrCycle][5] = transit1;
           ArrayTrans[PrCycle][6] = transit2;
           ArrayTrans[PrCycle][7] = transit3;
           ArrayTrans[PrCycle][8] = transit4;
       }
       ArraySort(ArrayTrans,StepRes,0,MODE_DESCEND);               //Отсортируем массив
       ArrayResize(ArrayTrans,StepRes);                            //Обрежем лишнее
       for(CopyAr=0;CopyAr<StepRes;CopyAr++){
           ArrayData[CopyAr][0]=ArrayTrans[CopyAr][0];
           ArrayData[CopyAr][1]=ArrayTrans[CopyAr][1];
           ArrayData[CopyAr][2]=ArrayTrans[CopyAr][2];
           ArrayData[CopyAr][3]=ArrayTrans[CopyAr][3];
           ArrayData[CopyAr][4]=ArrayTrans[CopyAr][4];             
           ArrayData[CopyAr][5]=ArrayTrans[CopyAr][5];             //Per1    Переменная 1 
           ArrayData[CopyAr][6]=ArrayTrans[CopyAr][6];             //Per2    Переменная 2
           ArrayData[CopyAr][7]=ArrayTrans[CopyAr][7];             //Per3    Переменная 3
           ArrayData[CopyAr][8]=ArrayTrans[CopyAr][8];             //Per4    Переменная 4
      }
//||||||||||||||||||||||||||||||| Запишем данные в Промежуточный файл ||||||||||||||||||||||||||||||||||||                 
   
   if(FileTst>0){
      for(i=0; i<StepRes; i++){
          trs2 = ArrayData[i][1];             
          trs3 = ArrayData[i][2];
          trs4 = ArrayData[i][3];             
          trs5 = ArrayData[i][4];             
          trs6 = ArrayData[i][5];
          trs7 = ArrayData[i][6];
          trs8 = ArrayData[i][7];
          trs9 = ArrayData[i][8];
          FileWrite(FileTst,trs2+";"+trs3+";"+trs4+";"+trs5+";"+trs6+";"+trs7+";"+trs8+";"+trs9);                           //из переменной в файл
   }} 
   else{
        Print("Неудалось записать данные в тестовый файл. Ошибка № ",GetLastError());
        Comment("Ошибка !!! Возможно файл FileTest2 открыт другой программой");
        return(0);
   }
//|||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||

       StepRes=StepRes/2;
   } 
    FileClose(FileTst);                                          //закроем файл промежуточных данных
     
       //Запишем полученные данные в переменные
       double Peremen1 = ArrayTrans[0][5];                         
       double Peremen2 = ArrayTrans[0][6];
       double Peremen3 = ArrayTrans[0][7];
       double Peremen4 = ArrayTrans[0][8];
       //Если имя переменной указано то запишем результат в глобальные переменные
       if(Per1!=""){GlobalVariableSet(Per1,Peremen1);}             
       if(Per2!=""){GlobalVariableSet(Per2,Peremen2);}
       if(Per3!=""){GlobalVariableSet(Per3,Peremen3);}
       if(Per4!=""){GlobalVariableSet(Per4,Peremen4);}
       Comment(Per1," ",Peremen1,"  | ",Per2," ",Peremen2,"  | ",Per3," ",Peremen3,"  | ",Per4," ",Peremen4);
       Print(Per1," ",Peremen1,"  | ",Per2," ",Peremen2,"  | ",Per3," ",Peremen3,"  | ",Per4," ",Peremen4);
 }  //Конец функции 

//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~