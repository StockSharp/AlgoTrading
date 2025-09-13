//+------------------------------------------------------------------+
//|                                            auto_optimization.mqh |
//|                                           Copyright � 2006, XEON |
//|                                                       xeon@nm.ru |
//+------------------------------------------------------------------+
//-------------------------------------------
#import  "shell32.dll"                                                       //��������� dll (������ � ������ windows)       
  int ShellExecuteA(int hwnd,string Operation,string File,string Parameters,string Directory,int ShowCmd); 
#import
//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
int Tester(int TestDay,string NameMTS,string NameFileSet,string PuthTester,int TimeOut,int Gross_Profit,int Profit_Factor,int Expected_Payoff,string Per1,string Per2,string Per3,string Per4)
  {
   string PuthTerminal    = TerminalPath()+"\experts\files";                  //���� � ��������� 
   string FileOptim       = "optimise.ini";                                   //��� ini ����� ��� �������
   string FileOptim1      = "\optimise.ini";  
      string FileTemp        = "\temp.htm";                         
   datetime DayStart      = TimeLocal()-86400*TestDay;                        //������ ���� ������
   string DateStart       = TimeToStr(DayStart,TIME_DATE);                    //���� ������� �����������
   string DateStop        = TimeToStr(TimeLocal(),TIME_DATE);                 //���� ��������� �����������
   string FileReport      = "FileReport_"+Symbol()+"_"+DateStop+".htm";       //��� ����� ������ �������
   string FileReport1     = "\FileReport_"+Symbol()+"_"+DateStop+".htm";
   double MinTr           = TestDay-2;                                        //����������� �� ����������� ���������� ������ � ����
   double MaxTr           = (60/Period()*TestDay)+2;                          //����������� �� ������������ ���������� ������ � ����
   int    KvoPptk         = 10;                                               //���������� ������� ����������� ���� ������
   int    StepRes         = 50;                                               //���������� ����� ��� ����������
//-------------------------------������ ������������� ����������------------------------
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
//----------------------------------- ���������� ������� -------------------------
   string ArrayOpttim[15]; 
   string ArrayStrg[10]; 
   double ArrayData[10][9]; 
   double ArrayTrans[10][9];
//------------------------------���������� ini ���� ��� �����������----------------
   ArrayOpttim[0] = ";optimise strategy tester";             
   ArrayOpttim[1] = "ExpertsEnable=false";                        //���/���� ��������
   ArrayOpttim[2] = "TestExpert="+NameMTS;                        //������������ ����� ��������
   ArrayOpttim[3] = "TestExpertParameters="+NameFileSet;          //������������ ����� � �����������
   ArrayOpttim[4] = "TestSymbol="+Symbol();                       //����������
   ArrayOpttim[5] = "TestPeriod="+Period();                       //������
   ArrayOpttim[6] = "TestModel="+0;                               //����� �������������
   ArrayOpttim[7] = "TestRecalculate=false";                      //�����������
   ArrayOpttim[8] = "TestOptimization=true";                      //�����������
   ArrayOpttim[9] = "TestDateEnable=true";                        //������������ ����
   ArrayOpttim[10]= "TestFromDate="+DateStart;                    //���� ������ ������������
   ArrayOpttim[11]= "TestToDate="+DateStop;                       //���� ��������� ������������
   ArrayOpttim[12]= "TestReport="+FileReport;                     //��� ����� ������
   ArrayOpttim[13]= "TestReplaceReport=true";                     //���������� ����� ������
   ArrayOpttim[14]= "TestShutdownTerminal=true";                  //������� �������� �� ����������
//------------------------------- ������� ������ � ini ���� --------------------------                 
   OptimArraySize=ArraySize(ArrayOpttim);                         //������� ������ �������
   opttim=FileOpen(FileOptim,FILE_CSV|FILE_WRITE,0x7F);           //������� ��� ��� ������
   if(opttim>0){
      for(int i=0; i<OptimArraySize; i++){
          ini=ArrayOpttim[i];                                     //�� ������� � ����������
          FileWrite(opttim, ini);                                 //�� ���������� � ����
      } 
      FileClose(opttim);                                          //������� ����
   }
   else{Print("��������� �������� ������ � ini ����. ������ � ",GetLastError());return(0);}
//-------------------------- ��������� ini ���� � ��������� ������� ----------
   copyini = ShellExecuteA(0,"Open","xcopy","\""+PuthTerminal+FileOptim1+"\" \""+PuthTester+"\" /y","",3);
   Sleep(1200);                                                    //�������� ���� ����������� ����
   if(copyini<0){Print("��������� ����������� ini ����");return(0);}
//---------------------------------- �������� ������ -------------------------
   start   = ShellExecuteA(0,"Open","terminal.exe",FileOptim,PuthTester,3);
   if( start<0){Print("��������� ��������� ������");return(0);}
//------------------------ ��������� ���� ������ � ��������� ��������� -------
    Comment("������� ��������� ����������� � "+TimeToStr(TimeLocal()+60*TimeOut,TIME_MINUTES));
    Sleep(60000*TimeOut);                                           //�������� ��������� �����������
    for(Pptk=0;Pptk<KvoPptk;Pptk++){                                //�������� ���� ������� ����������� ����� ������
        Comment("������� � "+Pptk+" ����������� ���� ������");
        copyini = ShellExecuteA(0,"Open","xcopy","\""+PuthTester+FileReport1+"\" \""+PuthTerminal+"\" /y","",3);
        Sleep(1200);                                                //�������� ���� ����������� ����
        file=FileOpen(FileReport,FILE_READ,0x7F);                   //���������� ������� ���� ������
        if(file<0){Sleep(60000);}                                   //���� �� �������, ��� �������� � ��������� �����
        else break;             
    }
    if(file<0){Print("��������� ����������� ���� ������");return(0);}
//---------------- ������ �� ����� � ������ ----------------------------------
    while(FileIsEnding(file)==false){                               //���� �� �������� ����� ����� - ����
          FileLine=FileReadString(file);                            //��������� ������ �� ����� ������
          index=StringFind(FileLine, "title=", 10);                  //������ ������ ������ � ��������� ����� �������
          if(index>0){
             ArrayResize(ArrayStrg,NumStr+1);                       //����������� ������ �������
             ArrayStrg[NumStr]=FileLine;                            //���������� � ������ ������ �� �����
             NumStr++;
    }}
    FileClose(file);                                                //������� ����
    FileDelete(FileReport);                                         //������ ���� ���� �� ������� �����
    ArrayResize(ArrayData,NumStr);                                  //��������� ������ ������� �� ���������� ��������� �� ����� �����
    for(text=0;text<NumStr;text++){
        select=ArrayStrg[text]; 
//-------------------------------------------------------------------------
//                    ��������� ������ ������  (�������� ��� �� ������)       
        //---------------------������� ������-----------------------------
        ClStep=StringFind(select, "; \">",20)+4;                                       //������ ������ �������
        ClStepRazm=StringFind(select, "</td>",ClStep);                                 //������ ����� �������
        CycleStep = StringSubstr(select, ClStep, ClStepRazm-ClStep);                   //������� ��������
        //---------------- ������� ������� -------------------------------
        GrProf=StringFind(select, "<td class=mspt>",ClStepRazm-1);                                  //������ ������ �������
        GrProfRazm=StringFind(select, "</td>",GrProf);                                 //������ ����� �������
        GrossProfit = StringSubstr(select, GrProf+15,GrProfRazm-(GrProf+15));            //������� ��������
         //-------------������� ����� ������ -----------------------------
        TotTrad=StringFind(select, "<td>",GrProfRazm);                                 //������ ������ �������
        TotTradRazm=StringFind(select, "</td>",TotTrad);                               //������ ����� �������
        TotalTrades = StringSubstr(select, TotTrad+4,TotTradRazm-(TotTrad+4));         //������� ��������
        //-------------������� ������������--------------------------------
        ProfFact=StringFind(select, "<td class=mspt>",TotTradRazm-1);                               //������ ������ �������
        ProfFactRazm=StringFind(select, "</td>",ProfFact);                             //������ ����� �������
        ProfitFactor = StringSubstr(select, ProfFact+15,ProfFactRazm-(ProfFact+15));     //������� ��������
       //-------------������� ��� ��������---------------------------------
        ExpPay=StringFind(select, "<td class=mspt>",ProfFactRazm-1);                                //������ ������ �������
        ExpPayRazm=StringFind(select, "</td>",ExpPay);                                 //������ ����� �������
        ExpectedPayoff = StringSubstr(select, ExpPay+15,ExpPayRazm-(ExpPay+15));         //������� ��������
        //------------------------------------------------------------------
        //-------------������� ����������-������� �� ������---------------------
        P1=StringFind(select, Per1,20);                                                //������ ������ �������
        P1k=StringFind(select, ";",P1);                                                //������ ����� �������
        Perem1 = StringSubstr(select,P1+StringLen(Per1)+1,P1k-(P1+1+StringLen(Per1))); //������� ����������
        P2=StringFind(select, Per2,20);                                                //������ ������ �������
        P2k=StringFind(select, ";",P2);                                                //������ ����� ������� 
        Perem2 = StringSubstr(select,P2+StringLen(Per2)+1,P2k-(P2+1+StringLen(Per2))); //������� ����������
        P3=StringFind(select, Per3,20);                                                //������ ������ �������
        P3k=StringFind(select, ";",P3);                                                //������ ����� �������
        Perem3 = StringSubstr(select,P3+StringLen(Per3)+1,P3k-(P3+1+StringLen(Per3))); //������� ����������
        P4=StringFind(select, Per4,20);                                                //������ ������ �������
        P4k=StringFind(select, ";",P4);                                                //������ ����� �������
        Perem4 = StringSubstr(select,P4+StringLen(Per4)+1,P4k-(P4+1+StringLen(Per4))); //������� ����������
        Comment("���� ������ ���������� �����������"); 
//-----------------------��������� � �������� ������----------------------------
       TotalTradesTransit=NormalizeDouble(StrToDouble(TotalTrades),0);
       GrossProfitTransit=NormalizeDouble(StrToDouble(GrossProfit),0);
       ExpectedPayoffTran=NormalizeDouble(StrToDouble(ExpectedPayoff),2);
       nodubl=true;
       if(MinTr < TotalTradesTransit && MaxTr > TotalTradesTransit){                    //����������� �� ���������� ������
          PrFactDouble = NormalizeDouble(StrToDouble(ProfitFactor),2);
          if(PrFactDouble==0){PrFactDouble=1000;}                                       //������� 0 � ������������ ��� ����������� �������
//-------------- ����������� ������ � ����������� ���������� -------------------       
           for(Dubl=0;Dubl<=text;Dubl++){                                               //�������� ���� ������ ���������� ��������
               if(GrossProfitTransit == ArrayData[Dubl][1]){                            //�������� ���������� ���������� �� ������������ �������
                  if(TotalTradesTransit == ArrayData[Dubl][2]){                         //�������� ���������� ���������� �� ���������� ������
                     if(PrFactDouble == ArrayData[Dubl][3]){                            //�������� ���������� ���������� �� ������������
                        if(ExpectedPayoffTran == ArrayData[Dubl][4]){                   //�������� ���������� ���������� �� �����������
                           nodubl=false;                                                //���� ��� �������, �� �������� ���� ����������
           }}}}}
            
//---------------- ������� ��������������� ������ � ������ ----------------------
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
    //||||||||||||||||||||||||||||||| ������� ������ � ������������� ���� ||||||||||||||||||||||||||||||||||||                 
   int FileTst;
   int trs1,trs2,trs3,trs6,trs7,trs8,trs9;
   double trs4,trs5;
   FileTst=FileOpen("FileTest1.csv",FILE_CSV|FILE_WRITE,0x7F);           //������� ��� ��� ������
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
          FileWrite(FileTst,trs2+";"+trs3+";"+trs4+";"+trs5+";"+trs6+";"+trs7+";"+trs8+";"+trs9);                                   //�� ���������� � ����
      } 
      FileClose(FileTst);                                          //������� ���� ������������� ������
   }
   else{
        Print("��������� �������� ������ � �������� ����. ������ � ",GetLastError());
        Comment("������ !!! �������� ���� FileTest1 ������ ������ ����������");
        return(0);
   }
//|||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
FileTst=FileOpen("FileTest2.csv",FILE_CSV|FILE_WRITE,0x7F);                      
 
//------------------------------����������---------------------------------------- 
// ������� ������� - ���������������� �������� ������������ �������� �������� ��������� ���������� ����������   
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
              //������������ � 1 ����������
              if(Gross_Profit   ==1){SortTrans=Prior1;}
              if(Profit_Factor  ==1){SortTrans=Prior2;}
              if(Expected_Payoff==1){SortTrans=Prior3;}
           }
           if(PrioStep==2){
              //�������������
              if(Gross_Profit   ==1){Prior1=Sort;}
              if(Profit_Factor  ==1){Prior2=Sort;}
              if(Expected_Payoff==1){Prior3=Sort;} 
              //������������ �� 2 ����������
              if(Gross_Profit   ==2){SortTrans=Prior1;}
              if(Profit_Factor  ==2){SortTrans=Prior2;}
              if(Expected_Payoff==2){SortTrans=Prior3;}
           }
           if(PrioStep==3){
              //�������������
              if(Gross_Profit   ==2){Prior1=Sort;}
              if(Profit_Factor  ==2){Prior2=Sort;}
              if(Expected_Payoff==2){Prior3=Sort;} 
              //������������ � 3 ����������
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
       ArraySort(ArrayTrans,StepRes,0,MODE_DESCEND);               //����������� ������
       ArrayResize(ArrayTrans,StepRes);                            //������� ������
       for(CopyAr=0;CopyAr<StepRes;CopyAr++){
           ArrayData[CopyAr][0]=ArrayTrans[CopyAr][0];
           ArrayData[CopyAr][1]=ArrayTrans[CopyAr][1];
           ArrayData[CopyAr][2]=ArrayTrans[CopyAr][2];
           ArrayData[CopyAr][3]=ArrayTrans[CopyAr][3];
           ArrayData[CopyAr][4]=ArrayTrans[CopyAr][4];             
           ArrayData[CopyAr][5]=ArrayTrans[CopyAr][5];             //Per1    ���������� 1 
           ArrayData[CopyAr][6]=ArrayTrans[CopyAr][6];             //Per2    ���������� 2
           ArrayData[CopyAr][7]=ArrayTrans[CopyAr][7];             //Per3    ���������� 3
           ArrayData[CopyAr][8]=ArrayTrans[CopyAr][8];             //Per4    ���������� 4
      }
//||||||||||||||||||||||||||||||| ������� ������ � ������������� ���� ||||||||||||||||||||||||||||||||||||                 
   
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
          FileWrite(FileTst,trs2+";"+trs3+";"+trs4+";"+trs5+";"+trs6+";"+trs7+";"+trs8+";"+trs9);                           //�� ���������� � ����
   }} 
   else{
        Print("��������� �������� ������ � �������� ����. ������ � ",GetLastError());
        Comment("������ !!! �������� ���� FileTest2 ������ ������ ����������");
        return(0);
   }
//|||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||

       StepRes=StepRes/2;
   } 
    FileClose(FileTst);                                          //������� ���� ������������� ������
     
       //������� ���������� ������ � ����������
       double Peremen1 = ArrayTrans[0][5];                         
       double Peremen2 = ArrayTrans[0][6];
       double Peremen3 = ArrayTrans[0][7];
       double Peremen4 = ArrayTrans[0][8];
       //���� ��� ���������� ������� �� ������� ��������� � ���������� ����������
       if(Per1!=""){GlobalVariableSet(Per1,Peremen1);}             
       if(Per2!=""){GlobalVariableSet(Per2,Peremen2);}
       if(Per3!=""){GlobalVariableSet(Per3,Peremen3);}
       if(Per4!=""){GlobalVariableSet(Per4,Peremen4);}
       Comment(Per1," ",Peremen1,"  | ",Per2," ",Peremen2,"  | ",Per3," ",Peremen3,"  | ",Per4," ",Peremen4);
       Print(Per1," ",Peremen1,"  | ",Per2," ",Peremen2,"  | ",Per3," ",Peremen3,"  | ",Per4," ",Peremen4);
 }  //����� ������� 

//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~