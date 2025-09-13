//+------------------------------------------------------------------+
//|                                                    s_wininet.mq4 |
//|                                                                * |
//|                                                                * |
//+------------------------------------------------------------------+
#property copyright "Integer"
#property link      "for-good-letters@yandex.ru"
//----
#import "wininet.dll"
int InternetAttemptConnect (int x);
  int InternetOpenA(string sAgent, int lAccessType, 
                    string sProxyName = "", string sProxyBypass = "", 
                    int lFlags = 0);
  int InternetOpenUrlA(int hInternetSession, string sUrl, 
                       string sHeaders = "", int lHeadersLength = 0,
                       int lFlags = 0, int lContext = 0);
  int InternetReadFile(int hFile, int& sBuffer[], int lNumBytesToRead, 
                       int& lNumberOfBytesRead[]);
  int InternetCloseHandle(int hInet);
#import
//+------------------------------------------------------------------+
//| script program start function                                    |
//+------------------------------------------------------------------+
int start()
  {
   if(!IsDllsAllowed())
     {
      Alert("Error activate DLLs");
       return(0);
     }
   int rv = InternetAttemptConnect(0);
   if(rv != 0)
     {
      Alert("Error with InternetAttemptConnect()");
       return(0);
     }
   int hInternetSession = InternetOpenA("Microsoft Internet Explorer", 
                                        0, "", "", 0);
   if(hInternetSession <= 0)
     {
     Alert("Error with InternetOpenA()");
       return(0);         
     }
   int hURL = InternetOpenUrlA(hInternetSession, 
              "http://twitter.com/FirePips", "", 0, 0, 0);
   if(hURL <= 0)
     {
   Alert("Error with InternetOpenUrlA()");
       InternetCloseHandle(hInternetSession);
       return(0);         
     }      
   int cBuffer[256];
   int dwBytesRead[1]; 
   string TXT = "";
   while(!IsStopped())
     {
       bool bResult = InternetReadFile(hURL, cBuffer, 1024, dwBytesRead);
       if(dwBytesRead[0] == 0)
           break;
       string text = "";   
       for(int i = 0; i < 256; i++)
         {
              text = text + CharToStr(cBuffer[i] & 0x000000FF);
              if(StringLen(text) == dwBytesRead[0])
                  break;
              text = text + CharToStr(cBuffer[i] >> 8 & 0x000000FF);
              if(StringLen(text) == dwBytesRead[0])
                  break;
           text = text + CharToStr(cBuffer[i] >> 16 & 0x000000FF);
           if(StringLen(text) == dwBytesRead[0])
               break;
           text = text + CharToStr(cBuffer[i] >> 24 & 0x000000FF);
         }
       TXT = TXT + text;
       //-------------------------------------------
       // Functions to export Trades from File
 
//-------------------------------------------------
     int j;
        int index=0;
        while(index!=-1)
        {
        index=StringFind(TXT, "Order ID: 7", index+1);
        Print("order 7 found atz=",index);
   }
      //  End of Functions to export Trades from File
    //----------------------------------------------
       Sleep(1);
     }
   if(TXT != "")
     {
       int h = FileOpen("SavedFromInternet.htm", FILE_CSV|FILE_WRITE);
       if(h > 0)
         {
           FileWrite(h,TXT);
           FileClose(h);
          Alert("Page downloaded .../experts/files/SavedFromInternet.htm");
           //-------------------------------------------------------------
    int Read_Open=FileOpen("SavedFromInternet.htm", FILE_CSV|FILE_READ, ';');
    if(Read_Open>0)
      { FileSeek(Read_Open, 0, SEEK_SET);
     int Open_Order_Type;
        Open_Order_Type=StrToInteger(FileReadString(Read_Open));
        string ttt=FileReadString(Read_Open);
        
       
     //  Alert("shiiiiiiiiiijkhijhkjiizt:",Open_Order_Type);
       
      //  Open_Open_Time=StrToTime(FileReadString(Read_Open));
      //  Open_Order_Symbol=FileReadString(Read_Open);
       // Open_Open_Price=StrToDouble(FileReadString(Read_Open));
      //  Stop_Loss=StrToDouble(FileReadString(Read_Open));
       // Take_Profit=StrToDouble(FileReadString(Read_Open));
      }
    else
      { Read_Open=FileOpen("open.csv", FILE_CSV|FILE_WRITE, ';');
        Read_Open=FileOpen("open.csv", FILE_CSV|FILE_READ, ';');
      }
    FileClose(Read_Open);
           //-------------------------------------------------------------
         }
       else
         {
        Alert("Error FileOpen()");
         }
     }
   else
     {
    Alert("Nicht nur ein paar daten");
     }
   InternetCloseHandle(hInternetSession);
   return(0);
}
//+------------------------------------------------------------------+