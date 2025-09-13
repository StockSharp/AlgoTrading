#define IND 10

#import "c:\\HistTraining\\SharedVarsDLLv2.dll"

 
 
 double   GetFloat@4 (int N);
 int   GetInt@4 (int N);
 //   Init@0 
 void   SetFloat@12 (int N, double Val);
 void    SetInt@8 (int N, int Val);
 


#import

 int handle;
 int magn;
 int k=0;
 int k1=0;
 int f=0;
 int LastNumHL=0;
 string HL_Name[3];
 
 
void init()
{
SetInt@8 (97, 0);
SetInt@8 (98, 0);
SetInt@8 (99, 0);
}

void deinit()
{
}
 
 
int start()
  {
  
  
 


//-----------------------------------------------------------------------------------------


double As1[3];
double As[3];
int obj_total;
int Lines=0;
string name;
int LastHline;
 string LastHlineName;
 bool HlinePres=0;
 int correct_obj=0;
 
obj_total=ObjectsTotal();

if(obj_total>0)
{ 
  for(int i=0;i<obj_total;i++)
    {
    
    
     name=ObjectName(i);
     if(ObjectType(name)==OBJ_HLINE)
     {
     if(ObjectGet(name, OBJPROP_COLOR)==Lime){HL_Name[correct_obj]=ObjectName(i);correct_obj++;}
     Lines=Lines+1;
     LastHline=i;
     LastHlineName=name;
     HlinePres=1;
    
     }
          
   
  }
}  
  

if(GetInt@4 (15)==1 || GetInt@4 (61)==1 || GetInt@4 (62)==1)
{  
  
  int objneeded;
  
  objneeded=GetInt@4 (15)+GetInt@4 (61)+GetInt@4 (62);
  
  if(LastNumHL<Lines && correct_obj<objneeded)
  {
  Print(Lines,": Имя объекта - ",LastHlineName);
  ObjectSet(LastHlineName, OBJPROP_COLOR,Lime);
  
  }
  
//------------------------------------------------------------------------------------------

As1[0]=MathFloor(ObjectGet(HL_Name[0], OBJPROP_PRICE1)*10000);
As1[0]=As1[0]/10000;
As1[1]=MathFloor(ObjectGet(HL_Name[1], OBJPROP_PRICE1)*10000);
As1[1]=As1[1]/10000;
As1[2]=MathFloor(ObjectGet(HL_Name[2], OBJPROP_PRICE1)*10000);
As1[2]=As1[2]/10000;


//Print("Имя объекта1 - ",As1[0]);
//Print("Имя объекта2 - ",As1[1]);
//Print("Имя объекта3 - ",As1[2]);


if(objneeded>1 || GetInt@4 (15)==0)

{
   As[0]=As1[ArrayMaximum(As1)];
   As[2]=As1[ArrayMinimum(As1)];
  
   As1[ArrayMaximum(As1)]=0;
   As1[ArrayMinimum(As1)]=0;
   
   As[1]=As1[ArrayMaximum(As1)];
}
if(objneeded==1 && GetInt@4 (15)==1)
{As[1]=As1[ArrayMaximum(As1)];}






if(GetInt@4 (25)==1 && GetInt@4 (15)!=1) 
{SetInt@8 (25,0);}


if(GetInt@4 (25)==1 && GetInt@4 (15)==1 && correct_obj==objneeded) 
{

   
    if(As[1]>Close[0])
    {
      
     if(correct_obj==2 && GetInt@4 (61)==1 )
     {
      if(OrderSend(Symbol(),OP_BUYSTOP,GetFloat@4 (1), As[0],0,As[1],As[2],NULL,magn,0,CLR_NONE)!=-1)
      {
      SetInt@8(10,magn);SetInt@8(11,3);SetFloat@12 (10, As[0]);magn++;
      }
    
     } 
      
      
      
      if((correct_obj==2 && GetInt@4 (62)==1) || correct_obj==1 || correct_obj==3)
     {
      if(OrderSend(Symbol(),OP_BUYSTOP,GetFloat@4 (1), As[1],0,As[2],As[0],NULL,magn,0,CLR_NONE)!=-1)
      {
      SetInt@8(10,magn);SetInt@8(11,3);SetFloat@12 (10, As[1]);magn++;
      }
     
      
     } 
     
      
     
     
     
     // ObjectDelete(name); 
       
       
              
                ObjectDelete(HL_Name[0]);
                ObjectDelete(HL_Name[1]);
                ObjectDelete(HL_Name[2]);
       
   //  SetInt@8 (25, 3);
     
     }
     
     if(As[1]<Close[0])
    {
     
      if((correct_obj==2 && GetInt@4 (61)==1) || correct_obj==1 || correct_obj==3)
     {
     if(OrderSend(Symbol(),OP_SELLSTOP,GetFloat@4 (1), As[1],0,As[0],As[2],NULL,magn,0,CLR_NONE)!=-1)
     {
     SetInt@8(10,magn);SetInt@8(11,4);SetFloat@12 (10, As[1]);magn++;
     }
     
     
     }
     
      if(correct_obj==2 && GetInt@4 (62)==1)
     {
     if(OrderSend(Symbol(),OP_SELLSTOP,GetFloat@4 (1),As[0],0,As[1],As[2],NULL,magn,0,CLR_NONE)!=-1)
     {
     SetInt@8(10,magn);SetInt@8(11,4);SetFloat@12 (10, As[0]);magn++;
     }
     
      
     
     }
     
     
     
     // ObjectDelete(name); 
      
      
                ObjectDelete(HL_Name[0]);
                ObjectDelete(HL_Name[1]);
                ObjectDelete(HL_Name[2]);
     // SetInt@8 (25, 3);
     }





}  
if(GetInt@4 (25)==1 && GetInt@4 (15)==1 && correct_obj!=objneeded){SetInt@8 (44, 1);}
if(GetInt@4 (25)==1 && GetInt@4 (15)==1){SetInt@8 (25, 3);}



//-----------------------------------------------------------------------------------------
//------------------------------------------------------------------------------------------

if(GetInt@4 (26)==1 && GetInt@4 (15)!=1) 
{SetInt@8 (25,0);}

if(GetInt@4 (26)==1 && GetInt@4 (15)==1  && correct_obj==objneeded) 
{


   
   
    if(As[1]>Close[0])
    {
        if((correct_obj==2 && GetInt@4 (61)==1) || correct_obj==1 || correct_obj==3)
     {
     if(OrderSend(Symbol(),OP_SELLLIMIT,GetFloat@4 (1), As[1],0,As[0],As[2],NULL,magn,0,CLR_NONE)!=-1)
     {
     SetInt@8(10,magn);SetInt@8(11,4);SetFloat@12 (10, As[1]);magn++;
     }
     }
     
      if(correct_obj==2 && GetInt@4 (62)==1)
     {
     if(OrderSend(Symbol(),OP_SELLLIMIT,GetFloat@4 (1),As[0],0,As[1],As[2],NULL,magn,0,CLR_NONE)!=-1)
     {
     SetInt@8(10,magn);SetInt@8(11,4);SetFloat@12 (10, As[0]);magn++;
     }
     }
     // ObjectDelete(name); 
       
     
              
                ObjectDelete(HL_Name[0]);
                ObjectDelete(HL_Name[1]);
                ObjectDelete(HL_Name[2]);
       
    // SetInt@8 (26, 3);
     
     }
     
     if(As[1]<Close[0])
    {
       if(correct_obj==2 && GetInt@4 (61)==1)
     {
      if(OrderSend(Symbol(),OP_BUYLIMIT,GetFloat@4 (1), As[0],0,As[1],As[2],NULL,magn,0,CLR_NONE)!=-1)
      {
      SetInt@8(10,magn);SetInt@8(11,3);SetFloat@12 (10, As[0]);magn++;
      } 
      } 
      
      
      
      if((correct_obj==2 && GetInt@4 (62)==1) || correct_obj==1 || correct_obj==3)
     {
      if(OrderSend(Symbol(),OP_BUYLIMIT,GetFloat@4 (1), As[1],0,As[2],As[0],NULL,magn,0,CLR_NONE)!=-1)
     {
     SetInt@8(10,magn);SetInt@8(11,3);SetFloat@12 (10, As[1]);magn++;
     } 
     } 
     
     
     
     
     // ObjectDelete(name); 
      
      
                ObjectDelete(HL_Name[0]);
                ObjectDelete(HL_Name[1]);
                ObjectDelete(HL_Name[2]);
  
   //   SetInt@8 (26, 3);
  
     }







}  

if(GetInt@4 (26)==1 && GetInt@4 (15)==1 && correct_obj!=objneeded){SetInt@8 (44, 1);}
if(GetInt@4 (26)==1 && GetInt@4 (15)==1){SetInt@8 (26, 3);}


//-----------------------------------------------------------------------------------------


}


//---------------------------------------------------------------------------------------------
//---------------------------------------------------------------------------------------------
if(OrdersTotal()==0){magn=0;} 
  
  

if (GetInt@4 (97)==1  && correct_obj==objneeded)
{

if(OrderSend(Symbol(),OP_BUY,GetFloat@4 (1),Ask,3,As[1],As[0],"",magn,0,Blue)!=-1)
{
SetInt@8(10,magn);SetInt@8(11,1);SetFloat@12 (10, Ask);magn++;
ObjectDelete(HL_Name[0]);
ObjectDelete(HL_Name[1]);
ObjectDelete(HL_Name[2]);
}
if((Ask>As[1] && Ask>As[0]) || (Ask<As[1] && Ask<As[0])){SetInt@8(45,1);}



SetInt@8 (97, 0);


}

if (GetInt@4 (97)==1  && correct_obj!=objneeded){SetInt@8 (44, 1);}
if (GetInt@4 (97)==1){SetInt@8 (97, 0);}




if (GetInt@4 (98)==1  && correct_obj==objneeded)
{

if(OrderSend(Symbol(),OP_SELL,GetFloat@4 (1),Bid,3,As[0],As[1],"",magn,0,Red)!=-1)
{
SetInt@8(10,magn);SetInt@8(11,2);SetFloat@12 (10, Bid);magn++;
ObjectDelete(HL_Name[0]);
ObjectDelete(HL_Name[1]);
ObjectDelete(HL_Name[2]);

}

if((Ask>As[1] && Ask>As[0]) || (Ask<As[1] && Ask<As[0])){SetInt@8(45,1);}

SetInt@8 (98, 0);

}


if (GetInt@4 (98)==1  && correct_obj!=objneeded){SetInt@8 (44, 1);}
if (GetInt@4 (98)==1){SetInt@8 (98, 0);}




if (GetInt@4 (99)==1)
{

for(k=0;k<OrdersTotal();k++)
{
OrderSelect(k,SELECT_BY_POS,MODE_TRADES);
if(OrderMagicNumber()==GetInt@4(20))
{
break;
}
}
}


if (GetInt@4 (99)==1 && OrderType()==OP_SELL)
{
OrderClose(OrderTicket(),OrderLots(),Ask,6,White); SetInt@8 (99, 0);
SetInt@8 (97, 0);
SetInt@8 (98, 0);
}




if (GetInt@4 (99)==1 && OrderType()==OP_BUY)
{
OrderClose(OrderTicket(),OrderLots(),Bid,6,White); SetInt@8 (99, 0);
SetInt@8 (97, 0);
SetInt@8 (98, 0);
}


if (GetInt@4 (99)==1 && OrderType()==OP_BUYSTOP || OrderType()==OP_SELLLIMIT || OrderType()==OP_SELLSTOP || OrderType()==OP_BUYLIMIT)
{
OrderDelete(OrderTicket());SetInt@8 (99, 0);
SetInt@8 (97, 0);
SetInt@8 (98, 0);
}





 
 
 
 
 if (GetInt@4 (30)==1)
{
for (f=OrdersTotal();f>=0;f--)
{
OrderSelect(f,SELECT_BY_POS,MODE_TRADES);
   if (OrderType()==OP_BUY)
   {
   OrderClose(OrderTicket(),OrderLots(),Bid,6,White); 
   SetInt@8 (97, 0);
   SetInt@8 (98, 0);
   }
   if (OrderType()==OP_SELL)
   {
   OrderClose(OrderTicket(),OrderLots(),Ask,6,White); 
   SetInt@8 (97, 0);
   SetInt@8 (98, 0);
   }
   
   if (OrderType()==OP_BUYSTOP || OrderType()==OP_SELLLIMIT || OrderType()==OP_SELLSTOP || OrderType()==OP_BUYLIMIT)
   {
   OrderDelete(OrderTicket());SetInt@8 (99, 0);
   SetInt@8 (97, 0);
   SetInt@8 (98, 0);
   }
   
   
}
SetInt@8 (30, 0);
}

//---------------------------------------------------------------------------------------------
//---------------------------------------------------------------------------------------------












LastNumHL=Lines;
 
 
 
 
 
 
 
 
 
 
 
  
  }

