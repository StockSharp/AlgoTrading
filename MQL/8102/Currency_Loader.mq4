//+------------------------------------------------------------------+
//|                           Currency_Loader.mq4                    |
//|                      Copyright © 2006, Larionov P.V.             |
//|                                        lolion@mail.ru            |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2006, Larionov P.V."
#property link      "lolion@mail.ru"
//---- input parameters
extern int  BarsMin=100;         // Minimal number bars in history which might be loaded into files.
extern int MaxBarsInFile = 20000; // Max Bars for loading into file.
extern int FrequencyUpdate = 60; // this value identify frequency update for files in sec.
extern bool LoadM1  = false;       //Timeframes of securities which data will be loaded onto File if True       
extern bool LoadM5  = false;
extern bool LoadM15 = false;
extern bool LoadM30 = false;
extern bool LoadH1  = false;
extern bool LoadH4  = false;
extern bool LoadD1  = false;
extern bool LoadW1  = false;
extern bool LoadMN  = false;
extern bool AllowInfo = True;
extern bool AllowLogFile = True;

string ExpertName = "Currency_Loader";
int deinit(){return(0);}
double ArrayM1[][6], ArrayM5[][6], ArrayM15[][6], ArrayM30[][6], ArrayH1[][6],ArrayH4[][6], ArrayD1[][6], ArrayW1[][6], ArrayMN[][6]; 
int ct, iDigits, Tryes=5, Pause=500, ArrSizeM1, ArrSizeM5, ArrSizeM15, ArrSizeM30, ArrSizeH1, ArrSizeH4, ArrSizeD1, ArrSizeW1, ArrSizeMN, i, i2, i3, h1, h2, h3, h4, h5, h6, h7, h8, h9, LCM1, LCM5, LCM15, LCM30, LCH1, LCH4, LCD1, LCW1, LCMN, LastError;
string CString, x, x2, FileNameM1, FileNameM5, FileNameM15, FileNameM30, FileNameH1, FileNameH4, FileNameD1, FileNameW1, FileNameMN, FilePatch, FirstLine;

int init(){//1
x="\"";
x2="\\";
iDigits=MarketInfo(Symbol(),MODE_DIGITS);
FilePatch = "Export_History"+x2+Symbol()+x2; 
FileNameM1 = FilePatch+Symbol()+"_"+"M1"+".csv"; 
FileNameM5 = FilePatch+Symbol()+"_"+"M5"+".csv"; 
FileNameM15 = FilePatch+Symbol()+"_"+"M15"+".csv"; 
FileNameM30 = FilePatch+Symbol()+"_"+"M30"+".csv"; 
FileNameH1 = FilePatch+Symbol()+"_"+"H1"+".csv"; 
FileNameH4 = FilePatch+Symbol()+"_"+"H4"+".csv"; 
FileNameD1 = FilePatch+Symbol()+"_"+"D1"+".csv"; 
FileNameW1 = FilePatch+Symbol()+"_"+"W1"+".csv"; 
FileNameMN = FilePatch+Symbol()+"_"+"MN"+".csv"; 
FirstLine =  ""+x+"Date"+x+" "+x+"Time"+x+" "+x+"Open"+x+" "+x+"High"+x+" "+x+"Low"+x+" "+x+"Close"+x+" "+x+"Volume"+x;
ct=CurTime()+61;
return(0);}



bool ROpen=false;

int start(){//1
if(ct == 0||ct < StrToTime(TimeToStr(CurTime(),TIME_MINUTES))){ct=StrToTime(TimeToStr(CurTime()+FrequencyUpdate,TIME_MINUTES)); ROpen=true;}
if(ROpen){//2
////////////////////////////////////////////////////////////////////////////// 
if(LoadM1){LoadingM1();}
if(LoadM5){LoadingM5();}
if(LoadM15){LoadingM15();}
if(LoadM30){LoadingM30();}
if(LoadH1){LoadingH1();}
if(LoadH4){LoadingH4();}
if(LoadD1){LoadingD1();}
if(LoadW1){LoadingW1();}
if(LoadMN){LoadingMN();}
ROpen=false;
////////////////////////////////////////////////////////////////////////////////
}//2   
return(0);
}//1

//******************************************************************************************
// loading history data from M1
void LoadingM1(){//1
int MaxBars=MaxBarsInFile;
if(LoadM1 && iBars(Symbol(),PERIOD_M1)>BarsMin){ArrayCopyRates(ArrayM1,Symbol(),PERIOD_M1); ArrSizeM1=ArrayRange(ArrayM1,0);}
 if(ArrSizeM1>1){//2
 if(MaxBars>ArrSizeM1){MaxBars=ArrSizeM1;}
  for(i2=1; i2<=Tryes; i2++){//3
  h1 = FileOpen(FileNameM1,FILE_CSV|FILE_WRITE);
  if(h1==-1){LastError=GetLastError();Info("1.2",1,""," There is an error while opening file: "+FileNameM1+" at "+i2+" Try  "+ErrorDescription(LastError));Pause=Pause+Pause; Sleep(Pause); continue; }else{Info("1.2",2,"","File "+FileNameM1+" successfully opened ");} 
  FileWrite(h1,FirstLine);
   for (i=MaxBars-1; i>=0; i-- ){//4
   CString=CString+TimeToStr(ArrayM1[i][0],TIME_DATE)+",";    //  date of bar
   CString=CString+TimeToStr(ArrayM1[i][0],TIME_MINUTES)+","; //  time of bar
   CString=CString+DoubleToStr(ArrayM1[i][1],iDigits)+","; //  Open price
   CString=CString+DoubleToStr(ArrayM1[i][3],iDigits)+","; //  High price
   CString=CString+DoubleToStr(ArrayM1[i][2],iDigits)+","; //  Low price
   CString=CString+DoubleToStr(ArrayM1[i][4],iDigits)+","; //  Close price
   CString=CString+DoubleToStr(ArrayM1[i][5],0); //  Volume
   FileWrite(h1,CString);
   CString="";
   }//4
   FileClose(h1);
   LCM1 = iTime(Symbol(),PERIOD_M1,0);
   return;
  }//3
 }//2
}//1

void LoadingM5(){//1
int MaxBars=MaxBarsInFile;
if(LoadM5 && iBars(Symbol(),PERIOD_M5)>BarsMin){ArrayCopyRates(ArrayM5,Symbol(),PERIOD_M5); ArrSizeM5=ArrayRange(ArrayM5,0);}
 if(ArrSizeM5>1){//2
 if(MaxBars>ArrSizeM5){MaxBars=ArrSizeM5;}
  for(i2=1; i2<=Tryes; i2++){//3
  h2 = FileOpen(FileNameM5,FILE_CSV|FILE_WRITE);
  if(h2==-1){LastError=GetLastError();Info("1.2",1,""," There is an error while opening file: "+FileNameM5+" at "+i2+" Try  "+ErrorDescription(LastError));Pause=Pause+Pause; Sleep(Pause); continue; }else{Info("1.2",2,"","File "+FileNameM5+" successfully opened ");} 
  FileWrite(h2,FirstLine);
   for (i=MaxBars-1; i>=0; i-- ){//4
   CString=CString+TimeToStr(ArrayM5[i][0],TIME_DATE)+",";    //  date of bar
   CString=CString+TimeToStr(ArrayM5[i][0],TIME_MINUTES)+","; //  time of bar
   CString=CString+DoubleToStr(ArrayM5[i][1],iDigits)+","; //  Open price
   CString=CString+DoubleToStr(ArrayM5[i][3],iDigits)+","; //  High price
   CString=CString+DoubleToStr(ArrayM5[i][2],iDigits)+","; //  Low price
   CString=CString+DoubleToStr(ArrayM5[i][4],iDigits)+","; //  Close price
   CString=CString+DoubleToStr(ArrayM5[i][5],0); //  Volume
   FileWrite(h2,CString);
   CString="";
   }//4
   FileClose(h2);
   LCM5 = iTime(Symbol(),PERIOD_M5,0);
   return;
  }//3
 }//2
}//1

//******************************************************************************************
//loading history data from M15
void LoadingM15(){//1
int MaxBars=MaxBarsInFile;
if(LoadM15 && iBars(Symbol(),PERIOD_M15)>BarsMin){ArrayCopyRates(ArrayM15,Symbol(),PERIOD_M15); ArrSizeM15=ArrayRange(ArrayM15,0);}
if(ArrSizeM15>1){//2
 if(MaxBars>ArrSizeM15){MaxBars=ArrSizeM15;}
  for(i2=1; i2<=Tryes; i2++){//3
  h3 = FileOpen(FileNameM15,FILE_CSV|FILE_WRITE);
  if(h3==-1){LastError=GetLastError();Info("1.2",1,""," There is an error while opening file: "+FileNameM15+" at "+i2+" Try  "+ErrorDescription(LastError));Pause=Pause+Pause; Sleep(Pause); continue; }else{Info("1.2",2,"","File "+FileNameM15+" successfully opened ");} 
  FileWrite(h3,FirstLine);
   for (i=MaxBars-1; i>=0; i-- ){//4
   CString=CString+TimeToStr(ArrayM15[i][0],TIME_DATE)+",";    //  date of bar
   CString=CString+TimeToStr(ArrayM15[i][0],TIME_MINUTES)+","; //  time of bar
   CString=CString+DoubleToStr(ArrayM15[i][1],iDigits)+","; //  Open price
   CString=CString+DoubleToStr(ArrayM15[i][3],iDigits)+","; //  High price
   CString=CString+DoubleToStr(ArrayM15[i][2],iDigits)+","; //  Low price
   CString=CString+DoubleToStr(ArrayM15[i][4],iDigits)+","; //  Close price
   CString=CString+DoubleToStr(ArrayM15[i][5],0); //  Volume
   FileWrite(h3,CString);
   CString="";
   }//4
   FileClose(h3);
   LCM15 = iTime(Symbol(),PERIOD_M15,0);
   return;
  }//3
 }//2
}//1

//******************************************************************************************
//loading history data from M30
void LoadingM30(){//1
int MaxBars=MaxBarsInFile;
if(LoadM30 && iBars(Symbol(),PERIOD_M30)>BarsMin){ArrayCopyRates(ArrayM30,Symbol(),PERIOD_M30); ArrSizeM30=ArrayRange(ArrayM30,0);}
if(ArrSizeM30>1){//2
 if(MaxBars>ArrSizeM30){MaxBars=ArrSizeM30;}
  for(i2=1; i2<=Tryes; i2++){//3
  h4 = FileOpen(FileNameM30,FILE_CSV|FILE_WRITE);
  if(h4==-1){LastError=GetLastError();Info("1.2",1,""," There is an error while opening file: "+FileNameM30+" at "+i2+" Try  "+ErrorDescription(LastError));Pause=Pause+Pause; Sleep(Pause); continue; }else{Info("1.2",2,"","File "+FileNameM30+" successfully opened ");} 
  FileWrite(h4,FirstLine);
   for (i=MaxBars-1; i>=0; i-- ){//4
   CString=CString+TimeToStr(ArrayM30[i][0],TIME_DATE)+",";    //  date of bar
   CString=CString+TimeToStr(ArrayM30[i][0],TIME_MINUTES)+","; //  time of bar
   CString=CString+DoubleToStr(ArrayM30[i][1],iDigits)+","; //  Open price
   CString=CString+DoubleToStr(ArrayM30[i][3],iDigits)+","; //  High price
   CString=CString+DoubleToStr(ArrayM30[i][2],iDigits)+","; //  Low price
   CString=CString+DoubleToStr(ArrayM30[i][4],iDigits)+","; //  Close price
   CString=CString+DoubleToStr(ArrayM30[i][5],0); //  Volume
   FileWrite(h4,CString);
   CString="";
   }//4
   FileClose(h4);
   LCM30 = iTime(Symbol(),PERIOD_M30,0);
   return;
  }//3
 }//2
}//1

//******************************************************************************************
//loading history data from H1
void LoadingH1(){//1
int MaxBars=MaxBarsInFile;
if(LoadH1 && iBars(Symbol(),PERIOD_H1)>BarsMin){ArrayCopyRates(ArrayH1,Symbol(),PERIOD_H1); ArrSizeH1=ArrayRange(ArrayH1,0);}
if(ArrSizeH1>1){//2
 if(MaxBars>ArrSizeH1){MaxBars=ArrSizeH1;}
  for(i2=1; i2<=Tryes; i2++){//3
  h5 = FileOpen(FileNameH1,FILE_CSV|FILE_WRITE);
  if(h5==-1){LastError=GetLastError();Info("1.2",1,""," There is an error while opening file: "+FileNameH1+" at "+i2+" Try  "+ErrorDescription(LastError));Pause=Pause+Pause; Sleep(Pause); continue; }else{Info("1.2",2,"","File "+FileNameH1+" successfully opened ");} 
  FileWrite(h5,FirstLine);
   for (i=MaxBars-1; i>=0; i-- ){//4
   CString=CString+TimeToStr(ArrayH1[i][0],TIME_DATE)+",";    //  date of bar
   CString=CString+TimeToStr(ArrayH1[i][0],TIME_MINUTES)+","; //  time of bar
   CString=CString+DoubleToStr(ArrayH1[i][1],iDigits)+","; //  Open price
   CString=CString+DoubleToStr(ArrayH1[i][3],iDigits)+","; //  High price
   CString=CString+DoubleToStr(ArrayH1[i][2],iDigits)+","; //  Low price
   CString=CString+DoubleToStr(ArrayH1[i][4],iDigits)+","; //  Close price
   CString=CString+DoubleToStr(ArrayH1[i][5],0); //  Volume
   FileWrite(h5,CString);
   CString="";
   }//4
   FileClose(h5);
   LCH1 = iTime(Symbol(),PERIOD_H1,0);
   return;
  }//3
 }//2
}//1

//******************************************************************************************
//loading history data from H4
void LoadingH4(){//1
int MaxBars=MaxBarsInFile;
if(LoadH4 && iBars(Symbol(),PERIOD_H4)>BarsMin){ArrayCopyRates(ArrayH4,Symbol(),PERIOD_H4); ArrSizeH4=ArrayRange(ArrayH4,0);}
if(ArrSizeH4>1){//2
 if(MaxBars>ArrSizeH4){MaxBars=ArrSizeH4;}
  for(i2=1; i2<=Tryes; i2++){//3
  h6 = FileOpen(FileNameH4,FILE_CSV|FILE_WRITE);
  if(h6==-1){LastError=GetLastError();Info("1.2",1,""," There is an error while opening file: "+FileNameH4+" at "+i2+" Try  "+ErrorDescription(LastError));Pause=Pause+Pause; Sleep(Pause); continue; }else{Info("1.2",2,"","File "+FileNameH4+" successfully opened ");} 
  FileWrite(h6,FirstLine);
   for (i=MaxBars-1; i>=0; i-- ){//4 
   CString=CString+TimeToStr(ArrayH4[i][0],TIME_DATE)+",";    //  date of bar
   CString=CString+TimeToStr(ArrayH4[i][0],TIME_MINUTES)+","; //  time of bar
   CString=CString+DoubleToStr(ArrayH4[i][1],iDigits)+","; //  Open price
   CString=CString+DoubleToStr(ArrayH4[i][3],iDigits)+","; //  High price
   CString=CString+DoubleToStr(ArrayH4[i][2],iDigits)+","; //  Low price
   CString=CString+DoubleToStr(ArrayH4[i][4],iDigits)+","; //  Close price
   CString=CString+DoubleToStr(ArrayH4[i][5],0); //  Volume
   FileWrite(h6,CString);
   CString="";
   }//4
   FileClose(h6);
   LCH4 = iTime(Symbol(),PERIOD_H4,0);
   return;
  }//3
 }//2
}//1

//******************************************************************************************
//loading history data from D1
void LoadingD1(){//1
int MaxBars=MaxBarsInFile;
if(LoadD1 && iBars(Symbol(),PERIOD_D1)>BarsMin){ArrayCopyRates(ArrayD1,Symbol(),PERIOD_D1); ArrSizeD1=ArrayRange(ArrayD1,0);}
if(ArrSizeD1>1){//2
 if(MaxBars>ArrSizeD1){MaxBars=ArrSizeD1;}
  for(i2=1; i2<=Tryes; i2++){//3
  h7 = FileOpen(FileNameD1,FILE_CSV|FILE_WRITE);
  if(h7==-1){LastError=GetLastError();Info("1.2",1,""," There is an error while opening file: "+FileNameD1+" at "+i2+" Try  "+ErrorDescription(LastError));Pause=Pause+Pause; Sleep(Pause); continue; }else{Info("1.2",2,"","File "+FileNameD1+" successfully opened ");} 
  FileWrite(h7,FirstLine);
   for (i=MaxBars-1; i>=0; i-- ){//4 
   CString=CString+TimeToStr(ArrayD1[i][0],TIME_DATE)+",";    //  date of bar
   CString=CString+TimeToStr(ArrayD1[i][0],TIME_MINUTES)+","; //  time of bar
   CString=CString+DoubleToStr(ArrayD1[i][1],iDigits)+","; //  Open price
   CString=CString+DoubleToStr(ArrayD1[i][3],iDigits)+","; //  High price
   CString=CString+DoubleToStr(ArrayD1[i][2],iDigits)+","; //  Low price
   CString=CString+DoubleToStr(ArrayD1[i][4],iDigits)+","; //  Close price
   CString=CString+DoubleToStr(ArrayD1[i][5],0); //  Volume
   FileWrite(h7,CString);
   CString="";
   }//4
   FileClose(h7);
   LCD1 = iTime(Symbol(),PERIOD_D1,0);
   return;
  }//3
 }//2
}//1

//******************************************************************************************
//loading history data from W1
void LoadingW1(){//1
int MaxBars=MaxBarsInFile;
if(LoadW1 && iBars(Symbol(),PERIOD_W1)>BarsMin){ArrayCopyRates(ArrayW1,Symbol(),PERIOD_W1); ArrSizeW1=ArrayRange(ArrayW1,0);}
if(ArrSizeW1>1){//2
 if(MaxBars>ArrSizeW1){MaxBars=ArrSizeW1;}
  for(i2=1; i2<=Tryes; i2++){//3
  h8 = FileOpen(FileNameW1,FILE_CSV|FILE_WRITE);
  if(h8==-1){LastError=GetLastError();Info("1.2",1,""," There is an error while opening file: "+FileNameW1+" at "+i2+" Try  "+ErrorDescription(LastError));Pause=Pause+Pause; Sleep(Pause); continue; }else{Info("1.2",2,"","File "+FileNameW1+" successfully opened ");} 
  FileWrite(h8,FirstLine);
   for (i=MaxBars-1; i>=0; i-- ){//4 
   CString=CString+TimeToStr(ArrayW1[i][0],TIME_DATE)+",";    //  date of bar
   CString=CString+TimeToStr(ArrayW1[i][0],TIME_MINUTES)+","; //  time of bar
   CString=CString+DoubleToStr(ArrayW1[i][1],iDigits)+","; //  Open price
   CString=CString+DoubleToStr(ArrayW1[i][3],iDigits)+","; //  High price
   CString=CString+DoubleToStr(ArrayW1[i][2],iDigits)+","; //  Low price
   CString=CString+DoubleToStr(ArrayW1[i][4],iDigits)+","; //  Close price
   CString=CString+DoubleToStr(ArrayW1[i][5],0); //  Volume
   FileWrite(h8,CString);
   CString="";
   }//4
   FileClose(h8);
   LCW1 = iTime(Symbol(),PERIOD_W1,0);
   return;
  }//3
 }//2
}//1

//******************************************************************************************
//loading history data from MN1
void LoadingMN(){//1
int MaxBars=MaxBarsInFile;
if(LoadMN && iBars(Symbol(),PERIOD_MN1)>BarsMin){ArrayCopyRates(ArrayMN,Symbol(),PERIOD_MN1); ArrSizeMN=ArrayRange(ArrayMN,0);}
if(ArrSizeMN>1){//2
 if(MaxBars>ArrSizeMN){MaxBars=ArrSizeMN;}
  for(i2=1; i2<=Tryes; i2++){//3
  h9 = FileOpen(FileNameMN,FILE_CSV|FILE_WRITE);
  if(h9==-1){LastError=GetLastError();Info("1.2",1,""," There is an error while opening file: "+FileNameMN+" at "+i2+" Try  "+ErrorDescription(LastError));Pause=Pause+Pause; Sleep(Pause); continue; }else{Info("1.2",2,"","File "+FileNameMN+" successfully opened ");} 
  FileWrite(h9,FirstLine);
   for (i=MaxBars-1; i>=0; i-- ){//4 
   CString=CString+TimeToStr(ArrayMN[i][0],TIME_DATE)+",";    //  date of bar
   CString=CString+TimeToStr(ArrayMN[i][0],TIME_MINUTES)+","; //  time of bar
   CString=CString+DoubleToStr(ArrayMN[i][1],iDigits)+","; //  Open price
   CString=CString+DoubleToStr(ArrayMN[i][3],iDigits)+","; //  High price
   CString=CString+DoubleToStr(ArrayMN[i][2],iDigits)+","; //  Low price
   CString=CString+DoubleToStr(ArrayMN[i][4],iDigits)+","; //  Close price
   CString=CString+DoubleToStr(ArrayMN[i][5],0); //  Volume
   FileWrite(h9,CString);
   CString="";
   }//4
   FileClose(h9);
   LCMN = iTime(Symbol(),PERIOD_MN1,0);
   return;
  }//3
 }//2
}//1

/************************************************************************************************
void Info(string MessageType=1, int Actuality=1, string MessageHead="", string MessageBody="HI")
Описание: формирование информационного канала работы программы, 
ежендевная выгрузка оперативной информации в текстовый файл, 
отправка отчетности по почте, вывод информационных сообщений в окно сообщений.
Параметры:
string MessageType - 1-вывод простого сообщения, 2- запись в файл, 3- отправка электронным письмом. 4- ежедневный вывод отчета о проведенных операциях и событиях.  
int Actuality  - степень важности сообщений, от 1 до 5.
string MessageHead - Заголовок сообщения для электронных писем.
string MessageBody - строка сообщения.  
***********************************************************************************************/
double Info(string MessageType="1,0,0", int Actuality = 1, string MessageHead="", string MessageBody="HI"){//1
int WarnMessLevel=3;
bool AllowMailSending=false, AllowStatement=false;
string _CurTimeDaily;
 string CurRusTimeMin;
 _CurTimeDaily = GetCurRusTime("Days"); 
 if(Actuality<=WarnMessLevel){//2
 int i, fwr;
 int OpString=-1;
 string FileName =StringConcatenate("LOG",ExpertName,_CurTimeDaily); 
 if(MessageHead=="")MessageHead=StringConcatenate("from expert ",ExpertName,TimeToStr(CurTime()));
 OpString = StringFind(MessageType,"1",0);
 if(OpString>-1){if(AllowInfo){Print(MessageBody);}}
 OpString = StringFind(MessageType,"2",0);
  if(OpString>-1){//3
   if(AllowLogFile){//4
    CurRusTimeMin = GetCurRusTime("Seconds")+" ";
    MessageBody= StringConcatenate(CurRusTimeMin,MessageBody); 
    for(i=0; i<5; i++){//5
    int HFile=FileOpen(FileName,FILE_READ|FILE_WRITE," "); 
     if(HFile>0){//6    
     FileSeek(HFile,0,SEEK_END); 
     FileWrite(HFile,MessageBody); 
     FileFlush(HFile); 
     FileClose(HFile);
     break; 
     }else{Sleep(500); continue; }//6
    }//5
   }//4
  }//3
  OpString = StringFind(MessageType,"3",0);
  if(OpString>-1){//3
   if(AllowMailSending && (!IsTesting())){//4
   int RetVal;
   SendMail( MessageHead, MessageBody );
   RetVal = GetLastError();
   if(RetVal>0){Info("1.2",1,"","Some mistakes were happened while mail sending "+ErrorDescription(RetVal));}
   }//4
  }//3
  OpString = StringFind(MessageType,"4",0);
  if(OpString>-1){//3
   if(AllowStatement){//4
   i=i;
   }//4
  }//3
 }else{return(0);}//2
return(0);
}//1

string GetCurRusTime(string Detail) 
{//1 
   string StrMonth="",StrDay="",StrHour="",StrMinute="",StrSeconds=""; 
   RefreshRates(); 
 
 if (Detail == "Seconds"){  
   if(Month()<10) { StrMonth="0"+Month(); } else { StrMonth=Month(); } 
   if(Day()<10) { StrDay="0"+Day(); } else { StrDay=Day(); } 
   if(Hour()<10) { StrHour="0"+Hour(); } else { StrHour=Hour(); } 
   if(Minute()<10) { StrMinute="0"+Minute(); } else { StrMinute=Minute(); } 
   if(Seconds()<10) { StrSeconds="0"+Seconds(); } else { StrSeconds=Seconds(); } 
   return(""+StrDay+"."+StrMonth+"."+Year()+" "+StrHour+":"+StrMinute+":"+StrSeconds+" ");  
   }
 if (Detail == "Hours"){  
   if(Month()<10) { StrMonth="0"+Month(); } else { StrMonth=Month(); } 
   if(Day()<10) { StrDay="0"+Day(); } else { StrDay=Day(); } 
   if(Hour()<10) { StrHour="0"+Hour(); } else { StrHour=Hour(); } 
   if(Minute()<10) { StrMinute="0"+Minute(); } else { StrMinute=Minute(); } 
   if(Seconds()<10) { StrSeconds="0"+Seconds(); } else { StrSeconds=Seconds(); } 
   return(""+StrDay+"."+StrMonth+"."+Year()+" "+StrHour+":00:"+"00 ");  
   }
 if (Detail == "Days"){  
   if(Month()<10) { StrMonth="0"+Month(); }else { StrMonth=Month(); } 
   if(Day()<10) { StrDay="0"+Day(); } else { StrDay=Day(); } 
   if(Hour()<10) { StrHour="0"+Hour(); } else { StrHour=Hour(); } 
   if(Minute()<10) { StrMinute="0"+Minute(); } else { StrMinute=Minute(); } 
   if(Seconds()<10) { StrSeconds="0"+Seconds(); } else { StrSeconds=Seconds(); } 
   return(""+StrDay+"."+StrMonth+"."+Year()+" ");  
   }
}//1 





//---- codes returned from trade server
string ErrorDescription(int error_code)
  {
   string error_string;
//----
   switch(error_code)
     {
      case 0:
      case 1:   error_string="no error";                                                  break;
      case 2:   error_string="common error";                                              break;
      case 3:   error_string="invalid trade parameters";                                  break;
      case 4:   error_string="trade server is busy";                                      break;
      case 5:   error_string="old version of the client terminal";                        break;
      case 6:   error_string="no connection with trade server";                           break;
      case 7:   error_string="not enough rights";                                         break;
      case 8:   error_string="too frequent requests";                                     break;
      case 9:   error_string="malfunctional trade operation";                             break;
      case 64:  error_string="account disabled";                                          break;
      case 65:  error_string="invalid account";                                           break;
      case 128: error_string="trade timeout";                                             break;
      case 129: error_string="invalid price";                                             break;
      case 130: error_string="invalid stops";                                             break;
      case 131: error_string="invalid trade volume";                                      break;
      case 132: error_string="market is closed";                                          break;
      case 133: error_string="trade is disabled";                                         break;
      case 134: error_string="not enough money";                                          break;
      case 135: error_string="price changed";                                             break;
      case 136: error_string="off quotes";                                                break;
      case 137: error_string="broker is busy";                                            break;
      case 138: error_string="requote";                                                   break;
      case 139: error_string="order is locked";                                           break;
      case 140: error_string="long positions only allowed";                               break;
      case 141: error_string="too many requests";                                         break;
      case 145: error_string="modification denied because order too close to market";     break;
      case 146: error_string="trade context is busy";                                     break;
      //---- mql4 errors
      case 4000: error_string="no error";                                                 break;
      case 4001: error_string="wrong function pointer";                                   break;
      case 4002: error_string="array index is out of range";                              break;
      case 4003: error_string="no memory for function call stack";                        break;
      case 4004: error_string="recursive stack overflow";                                 break;
      case 4005: error_string="not enough stack for parameter";                           break;
      case 4006: error_string="no memory for parameter string";                           break;
      case 4007: error_string="no memory for temp string";                                break;
      case 4008: error_string="not initialized string";                                   break;
      case 4009: error_string="not initialized string in array";                          break;
      case 4010: error_string="no memory for array\' string";                             break;
      case 4011: error_string="too long string";                                          break;
      case 4012: error_string="remainder from zero divide";                               break;
      case 4013: error_string="zero divide";                                              break;
      case 4014: error_string="unknown command";                                          break;
      case 4015: error_string="wrong jump (never generated error)";                       break;
      case 4016: error_string="not initialized array";                                    break;
      case 4017: error_string="dll calls are not allowed";                                break;
      case 4018: error_string="cannot load library";                                      break;
      case 4019: error_string="cannot call function";                                     break;
      case 4020: error_string="expert function calls are not allowed";                    break;
      case 4021: error_string="not enough memory for temp string returned from function"; break;
      case 4022: error_string="system is busy (never generated error)";                   break;
      case 4050: error_string="invalid function parameters count";                        break;
      case 4051: error_string="invalid function parameter value";                         break;
      case 4052: error_string="string function internal error";                           break;
      case 4053: error_string="some array error";                                         break;
      case 4054: error_string="incorrect series array using";                             break;
      case 4055: error_string="custom indicator error";                                   break;
      case 4056: error_string="arrays are incompatible";                                  break;
      case 4057: error_string="global variables processing error";                        break;
      case 4058: error_string="global variable not found";                                break;
      case 4059: error_string="function is not allowed in testing mode";                  break;
      case 4060: error_string="function is not confirmed";                                break;
      case 4061: error_string="send mail error";                                          break;
      case 4062: error_string="string parameter expected";                                break;
      case 4063: error_string="integer parameter expected";                               break;
      case 4064: error_string="double parameter expected";                                break;
      case 4065: error_string="array as parameter expected";                              break;
      case 4066: error_string="requested history data in update state";                   break;
      case 4099: error_string="end of file";                                              break;
      case 4100: error_string="some file error";                                          break;
      case 4101: error_string="wrong file name";                                          break;
      case 4102: error_string="too many opened files";                                    break;
      case 4103: error_string="cannot open file";                                         break;
      case 4104: error_string="incompatible access to a file";                            break;
      case 4105: error_string="no order selected";                                        break;
      case 4106: error_string="unknown symbol";                                           break;
      case 4107: error_string="invalid price parameter for trade function";               break;
      case 4108: error_string="invalid ticket";                                           break;
      case 4109: error_string="trade is not allowed";                                     break;
      case 4110: error_string="longs are not allowed";                                    break;
      case 4111: error_string="shorts are not allowed";                                   break;
      case 4200: error_string="object is already exist";                                  break;
      case 4201: error_string="unknown object property";                                  break;
      case 4202: error_string="object is not exist";                                      break;
      case 4203: error_string="unknown object type";                                      break;
      case 4204: error_string="no object name";                                           break;
      case 4205: error_string="object coordinates error";                                 break;
      case 4206: error_string="no specified subwindow";                                   break;
      default:   error_string="unknown error";
     }
//----
   return(error_string);
  }



