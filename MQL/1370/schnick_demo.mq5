//+------------------------------------------------------------------+
//|                                                 Schnick_Demo.mq5 |
//|                        Copyright 2011, MetaQuotes Software Corp. |
//|                                              http://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "Copyright 2011, MetaQuotes Software Corp."
#property link      "http://www.mql5.com"
#property version   "1.00"

//+------------------------------------------------------------------+
//| This script demonstrates the capabilities of the Support Vector
//|                     Machine Learning Tool
//+------------------------------------------------------------------+

//+------------------------------------------------------------------+
//| The following statement imports all of the functions included in
//| the Support Vector Machine Tool 'svMachineTool.ex5'
//+------------------------------------------------------------------+
#import "svMachineTool_demo.ex5"
enum ENUM_TRADE {BUY,SELL};
enum ENUM_OPTION {OP_MEMORY,OP_MAXCYCLES,OP_TOLERANCE};
int  initSVMachine(void);
void setIndicatorHandles(int handle,int &indicatorHandles[],int offset,int N);
void setParameter(int handle,ENUM_OPTION option,double value);
bool genOutputs(int handle,ENUM_TRADE trade,int StopLoss,int TakeProfit,double duration);
bool genInputs(int handle);
bool setInputs(int handle,double &Inputs[],int nInputs);
bool setOutputs(int handle,bool &Outputs[]);
bool training(int handle);
bool classify(int handle);
bool classify(int handle,int offset);
bool classify(int handle,double &iput[]);
void deinitSVMachine(void);
#import
//--- The number of inputs we will be using for the svm
int N_Inputs=7;
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
   double inputs[];           //empty double array to be used for creating training inputs
   bool   outputs[];          //empty bool array to be used for creating training inputs
   int N_TrainingPoints=5000; //defines the number of training samples to be generated
   int N_TestPoints=5000;     //defines the number of samples to used when testing

   genTrainingData(inputs,outputs,N_TrainingPoints); //Generates the inputs and outputs to be used for training the svm

   int handle1=initSVMachine();             //initializes a new support vector machine and returns a handle
   setInputs(handle1,inputs,7);             //passes the inputs (without errors) to the support vector machine
   setOutputs(handle1,outputs);             //passes the outputs (without errors) to the support vector machine
   setParameter(handle1,OP_TOLERANCE,0.01); //sets the error tolerance parameter to <5%
   training(handle1);                       //trains the support vector machine using the inputs/outputs passed

   insertRandomErrors(inputs,outputs,500);  //takes the original inputs/outputs generated and adds random errors to the data

   int handle2=initSVMachine();             //initializes a new support vector machine and returns a handle
   setInputs(handle2,inputs,7);             //passes the inputs (with errors) to the support vector machine
   setOutputs(handle2,outputs);             //passes the outputs (with errors) to the support vector machine
   setParameter(handle2,OP_TOLERANCE,0.01); //sets the error tolerance parameter to <5%
   training(handle2);                       //trains the support vector machine using the inputs/outputs passed

   double t1=testSVM(handle1,N_TestPoints); //tests the accuracy of the trained support vector machine and saves it to t1
   double t2=testSVM(handle2,N_TestPoints); //tests the accuracy of the trained support vector machine and saves it to t2

   Print("The SVM accuracy is ",NormalizeDouble(t1,2),"% (using training inputs/outputs without errors)");
   Print("The SVM accuracy is ",NormalizeDouble(t2,2),"% (using training inputs/outputs with errors)");
   deinitSVMachine(); //Cleans up all of the memory used in generating the SVM to avoid memory leakage
   return(0);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
//--- No functions executed in OnDeinit()
  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
//--- No functions executed in OnTick()   
  }
//+------------------------------------------------------------------+
//| This function takes the observation properties of the observed 
//| animal and based on the critera we have chosen, returns
//| true/false whether it is a schnick
//+------------------------------------------------------------------+
bool isItASchnick(double height,double weight,double N_legs,double N_eyes,double L_arm,double av_speed,double f_call)
  {
   if(height   < 1000  || height   > 1100)  return(false); //If the height is outside the parameters > return(false)
   if(weight   < 40    || weight   > 50)    return(false); //If the weight is outside the parameters > return(false)
   if(N_legs   < 8     || N_legs   > 10)    return(false); //If the N_Legs is outside the parameters > return(false)
   if(N_eyes   < 3     || N_eyes   > 4)     return(false); //If the N_eyes is outside the parameters > return(false)
   if(L_arm    < 400   || L_arm    > 450)   return(false); //If the L_arm  is outside the parameters > return(false)
   if(av_speed < 2     || av_speed > 2.5)   return(false); //If the av_speed is outside the parameters > return(false)
   if(f_call   < 11000 || f_call   > 15000) return(false); //If the f_call is outside the parameters > return(false)
   return(true);                                           //Otherwise > return(true)
  }
//+------------------------------------------------------------------+
//| This function takes an empty double array and empty boolean array
//| and generates the inputs/outputs to be used for training the SVM
//+------------------------------------------------------------------+ 
void genTrainingData(double &inputs[],bool &outputs[],int N)
  {
   double in[];                    //creates an empty double array to be used
                                   //for temporarily storing the inputs generated
   ArrayResize(in,N_Inputs);       //resize the in[] array to N_Inputs
   ArrayResize(inputs,N*N_Inputs); //resize the inputs[] array to have a size of N*N_Inputs 
   ArrayResize(outputs,N);         //resize the outputs[] array to have a size of N 
   for(int i=0;i<N;i++)
     {
      in[0]=    randBetween(980,1120);    //Random input generated for height
      in[1]=    randBetween(38,52);       //Random input generated for weight
      in[2]=    randBetween(7,11);        //Random input generated for N_legs
      in[3]=    randBetween(3,4.2);       //Random input generated for N_eyes
      in[4]=    randBetween(380,450);     //Random input generated for L_arms
      in[5]=    randBetween(2,2.6);       //Random input generated for av_speed
      in[6]=    randBetween(10500,15500); //Random input generated for f_call

      //--- copy the new random inputs generated into the training input array
      ArrayCopy(inputs,in,i*N_Inputs,0,N_Inputs);
      //--- assess the random inputs and determine if it is a schnick
      outputs[i]=isItASchnick(in[0],in[1],in[2],in[3],in[4],in[5],in[6]);
     }
  }
//+------------------------------------------------------------------+
//| This function takes the handle for the trained SVM and tests how
//| successful it is at classifying new random inputs
//+------------------------------------------------------------------+ 
double testSVM(int handle,int N)
  {
   double in[];
   int atrue=0;
   int afalse=0;
   int N_correct=0;
   bool Predicted_Output;
   bool Actual_Output;
   ArrayResize(in,N_Inputs);
   for(int i=0;i<N;i++)
     {
      in[0]=    randBetween(980,1120);    //Random input generated for height
      in[1]=    randBetween(38,52);       //Random input generated for weight
      in[2]=    randBetween(7,11);        //Random input generated for N_legs
      in[3]=    randBetween(3,4.2);       //Random input generated for N_eyes
      in[4]=    randBetween(380,450);     //Random input generated for L_arms
      in[5]=    randBetween(2,2.6);       //Random input generated for av_speed
      in[6]=    randBetween(10500,15500); //Random input generated for f_call

      //--- uses the isItASchnick fcn to determine the actual desired output
      Actual_Output=isItASchnick(in[0],in[1],in[2],in[3],in[4],in[5],in[6]);
      //--- uses the trained SVM to return the prediced output.
      Predicted_Output=classify(handle,in);
      if(Actual_Output==Predicted_Output)
        {
         N_correct++; //This statement keeps count of the number of times the predicted output is correct.
        }
     }
//--- returns the accuracy of the trained SVM as a percentage
   return(100*((double)N_correct/(double)N));
  }
//+------------------------------------------------------------------+
//| This function takes the correct training inputs and outputs 
//| generated and inserts N random errors into the data
//+------------------------------------------------------------------+ 
void insertRandomErrors(double &inputs[],bool &outputs[],int N)
  {
   int nTrainingPoints=ArraySize(outputs); //calculates the number of training points
   int index;                              //creates new integer 'index'
   bool randomOutput;                      //creates new bool 'randomOutput'
   double in[];                            //creates an empty double array to be used
                                           //for temporarily storing the inputs generated
   ArrayResize(in,N_Inputs);               //resize the in[] array to N_Inputs
   for(int i=0;i<N;i++)
     {
      in[0]=    randBetween(980,1120);    //Random input generated for height
      in[1]=    randBetween(38,52);       //Random input generated for weight
      in[2]=    randBetween(7,11);        //Random input generated for N_legs
      in[3]=    randBetween(3,4.2);       //Random input generated for N_eyes
      in[4]=    randBetween(380,450);     //Random input generated for L_arms
      in[5]=    randBetween(2,2.6);       //Random input generated for av_speed
      in[6]=    randBetween(10500,15500); //Random input generated for f_call

      //--- randomly chooses one of the training inputs to insert an error
      index=(int)MathRound(randBetween(0,nTrainingPoints-1));
      //--- generates a random boolean output to be used to create error
      if(randBetween(0,1)>0.5) randomOutput=true;
      else                     randomOutput=false;

      //--- copy the new random inputs generated into the training input array
      ArrayCopy(inputs,in,index*N_Inputs,0,N_Inputs);
      //--- copy the new random output generated into the training output array
      outputs[index]=randomOutput;
     }
  }
//+------------------------------------------------------------------+
//| This function is used to create a random value between t1 and t2
//+------------------------------------------------------------------+ 
double randBetween(double t1,double t2)
  {
   return((t2-t1)*((double)MathRand()/(double)32767)+t1);
  }
//+------------------------------------------------------------------+
