//+------------------------------------------------------------------+
//|                                               NeuralNetEA.mq5    |
//|           Enhanced Neural Network EA for MetaTrader 5            |
//+------------------------------------------------------------------+
#property copyright "Seyyid Sahin"
#property version   "1.11"
#property strict

//--- Input parameters
input double   MaxRiskPerTrade      = 1.0;        // Maximum risk per trade in percentage
input double   DailyLossLimit       = 5.0;        // Maximum daily loss in percentage
input double   TotalLossLimit       = 10.0;       // Maximum total loss in percentage
input int      MagicNumber          = 12345;      // Unique identifier
input bool     EnableLogging        = true;       // Enable detailed logging
input double   InitialLearningRate  = 0.01;       // Initial learning rate for neural network
input int      HiddenLayerSize      = 5;          // Number of neurons in hidden layer
input int      MaxEpochs            = 1;          // Max epochs per tick for training
input string   ModelFileName        = "neural_network.dat"; // File name for saving/loading model
input int      ATRPeriod            = 14;         // ATR period for dynamic Stop Loss
input double   MaxSpread            = 20.0;       // Maximum allowable spread in points

//--- Global variables
double g_accountEquityAtStart;
double g_dailyEquityAtStart;
datetime g_lastTradeDay = 0;
double g_dailyProfitTarget = 1.0;  // 1% daily profit target

//--- Neural Network Variables
double g_weightsInputHidden[];    // Flattened one-dimensional array
double g_weightsHiddenOutput[];
double g_biasHidden[];
double g_biasOutput;
double g_neuralInputs[];
double g_hiddenLayerOutputs[];
double g_neuralOutput;
double g_learningRate;            // Modifiable learning rate

//--- Additional Variables for Advanced Optimizers
double g_momentumsInputHidden[];
double g_momentumsHiddenOutput[];
double g_momentumBiasHidden[];
double g_momentumBiasOutput;      // Changed from array to scalar
double g_beta1 = 0.9; // Momentum factor

//--- Constants
#define INPUT_SIZE 5  // Number of input neurons
#define RAND_MAX 32767.0
#define EPSILON 1e-8

//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
   // Initialize account equity
   g_accountEquityAtStart = AccountInfoDouble(ACCOUNT_EQUITY);
   g_dailyEquityAtStart = g_accountEquityAtStart;
   g_lastTradeDay = TimeCurrent();
   PrintLog("EA initialized. Starting equity: " + DoubleToString(g_accountEquityAtStart, 2));

   // Initialize learning rate
   g_learningRate = InitialLearningRate;

   // Initialize neural network
   InitNeuralNetwork();

   // Load saved neural network parameters if available
   LoadNeuralNetwork();

   return(INIT_SUCCEEDED);
  }

//+------------------------------------------------------------------+
//| Function to initialize neural network                            |
//+------------------------------------------------------------------+
void InitNeuralNetwork()
  {
   int inputHiddenSize = INPUT_SIZE * HiddenLayerSize;
   ArrayResize(g_weightsInputHidden, inputHiddenSize);
   ArrayResize(g_biasHidden, HiddenLayerSize);
   ArrayResize(g_weightsHiddenOutput, HiddenLayerSize);

   // Initialize weights and biases with small random values
   for(int i = 0; i < INPUT_SIZE; i++)
     {
      for(int j = 0; j < HiddenLayerSize; j++)
        {
         int index = i * HiddenLayerSize + j;
         g_weightsInputHidden[index] = ((double)MathRand() / RAND_MAX) * 0.1 - 0.05;
        }
     }

   for(int j = 0; j < HiddenLayerSize; j++)
     {
      g_biasHidden[j] = ((double)MathRand() / RAND_MAX) * 0.1 - 0.05;
      g_weightsHiddenOutput[j] = ((double)MathRand() / RAND_MAX) * 0.1 - 0.05;
     }

   g_biasOutput = ((double)MathRand() / RAND_MAX) * 0.1 - 0.05;

   // Initialize momentums for advanced optimizer
   ArrayResize(g_momentumsInputHidden, inputHiddenSize);
   ArrayResize(g_momentumsHiddenOutput, HiddenLayerSize);
   ArrayResize(g_momentumBiasHidden, HiddenLayerSize);
   g_momentumBiasOutput = 0.0; // Initialize scalar variable

   ArrayInitialize(g_momentumsInputHidden, 0.0);
   ArrayInitialize(g_momentumsHiddenOutput, 0.0);
   ArrayInitialize(g_momentumBiasHidden, 0.0);
   // No need to initialize g_momentumBiasOutput as an array
  }

//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
   // Save neural network parameters
   SaveNeuralNetwork();

   PrintLog("EA deinitialized. Reason code: " + IntegerToString(reason));
  }

//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
   datetime currentTime = TimeCurrent();

   // Check for new day to reset daily equity
   if(IsNewDay())
     {
      g_dailyEquityAtStart = AccountInfoDouble(ACCOUNT_EQUITY);
      g_lastTradeDay = currentTime;
     }

   // Check for maximum daily and total drawdown
   if(IsMaxDrawdownExceeded())
     {
      PrintLog("Maximum drawdown exceeded. Trading halted.");
      return;
     }

   // Calculate daily profit
   double dailyProfitPercent = ((AccountInfoDouble(ACCOUNT_EQUITY) - g_dailyEquityAtStart) / g_dailyEquityAtStart) * 100.0;

   // Apply penalty if daily profit is less than target
   if(dailyProfitPercent < g_dailyProfitTarget && !IsNewDay())
     {
      // Implement penalty logic
      AdjustParametersForPenalty();
     }

   // Train neural network during backtesting
   if(MQLInfoInteger(MQL_TESTER))
     {
      for(int epoch = 0; epoch < MaxEpochs; epoch++)
        {
         TrainNeuralNetwork();
        }
     }

   // Open trades based on neural network prediction
   OpenTrades();
  }

//+------------------------------------------------------------------+
//| Function to check if it's a new day                              |
//+------------------------------------------------------------------+
bool IsNewDay()
  {
   long currentDay = TimeCurrent() / 86400;
   long lastTradeDay = g_lastTradeDay / 86400;
   if(currentDay != lastTradeDay)
     {
      return(true);
     }
   return(false);
  }

//+------------------------------------------------------------------+
//| Function to check for maximum drawdown                           |
//+------------------------------------------------------------------+
bool IsMaxDrawdownExceeded()
  {
   double currentEquity = AccountInfoDouble(ACCOUNT_EQUITY);
   double totalDrawdownPercent = ((g_accountEquityAtStart - currentEquity) / g_accountEquityAtStart) * 100.0;
   double dailyDrawdownPercent = ((g_dailyEquityAtStart - currentEquity) / g_dailyEquityAtStart) * 100.0;

   if(dailyDrawdownPercent >= DailyLossLimit || totalDrawdownPercent >= TotalLossLimit)
     {
      return(true);
     }
   return(false);
  }

//+------------------------------------------------------------------+
//| Function to adjust parameters as a penalty                       |
//+------------------------------------------------------------------+
void AdjustParametersForPenalty()
  {
   // Decrease learning rate as a penalty
   g_learningRate *= 0.9;

   // Ensure learning rate doesn't go below a minimum threshold
   if(g_learningRate < 0.0001)
      g_learningRate = 0.0001;

   PrintLog("Parameters adjusted due to penalty. LearningRate: " + DoubleToString(g_learningRate, 6));
  }

//+------------------------------------------------------------------+
//| Function to train the neural network                             |
//+------------------------------------------------------------------+
void TrainNeuralNetwork()
  {
   // Use historical data to train
   MqlRates rates[];
   int dataCount = CopyRates(Symbol(), PERIOD_M15, 0, 1000, rates);
   if(dataCount < 101)
      return;

   for(int i = dataCount - 101; i >= 0; i--)
     {
      // Prepare inputs and expected output
      double inputs[INPUT_SIZE];
      double expectedOutput;

      // Example inputs: Open, High, Low, Close, Volume
      inputs[0] = rates[i+1].open;
      inputs[1] = rates[i+1].high;
      inputs[2] = rates[i+1].low;
      inputs[3] = rates[i+1].close;
      inputs[4] = (double)rates[i+1].tick_volume; // Explicit cast to double

      // Normalize inputs
      NormalizeInputs(inputs);

      // Expected output: Next candle's close price movement (up or down)
      expectedOutput = (rates[i].close > rates[i+1].close) ? 1.0 : 0.0;

      // Forward pass
      double output = ForwardPass(inputs);

      // Backward pass (training)
      BackwardPass(inputs, expectedOutput, output);
     }
  }

//+------------------------------------------------------------------+
//| Function to normalize input data                                 |
//+------------------------------------------------------------------+
void NormalizeInputs(double &inputs[])
  {
   // For simplicity, normalize using min-max scaling between 0 and 1
   // In practice, you should compute min and max values over your dataset
   double minValues[INPUT_SIZE] = {1.0e5, 1.0e5, 1.0e5, 1.0e5, 0.0};
   double maxValues[INPUT_SIZE] = {0.0, 0.0, 0.0, 0.0, 0.0};

   // Calculate min and max values dynamically (you may precompute these)
   for(int i = 0; i < INPUT_SIZE - 1; i++)
     {
      if(inputs[i] < minValues[i]) minValues[i] = inputs[i];
      if(inputs[i] > maxValues[i]) maxValues[i] = inputs[i];
     }

   // Normalize inputs
   for(int i = 0; i < INPUT_SIZE - 1; i++)
     {
      inputs[i] = (inputs[i] - minValues[i]) / (maxValues[i] - minValues[i] + EPSILON);
     }

   // Normalize volume separately if needed
   inputs[4] = inputs[4] / (inputs[4] + EPSILON);
  }

//+------------------------------------------------------------------+
//| Function for forward pass of neural network                      |
//+------------------------------------------------------------------+
double ForwardPass(double &inputs[])
  {
   // Calculate hidden layer outputs
   ArrayResize(g_hiddenLayerOutputs, HiddenLayerSize);
   for(int j = 0; j < HiddenLayerSize; j++)
     {
      double activation = 0.0;
      for(int i = 0; i < INPUT_SIZE; i++)
        {
         int index = i * HiddenLayerSize + j;
         activation += inputs[i] * g_weightsInputHidden[index];
        }
      activation += g_biasHidden[j];
      g_hiddenLayerOutputs[j] = ReLU(activation);
     }

   // Calculate output layer
   double activation = 0.0;
   for(int j = 0; j < HiddenLayerSize; j++)
     {
      activation += g_hiddenLayerOutputs[j] * g_weightsHiddenOutput[j];
     }
   activation += g_biasOutput;
   g_neuralOutput = Sigmoid(activation);

   return g_neuralOutput;
  }

//+------------------------------------------------------------------+
//| ReLU activation function                                         |
//+------------------------------------------------------------------+
double ReLU(double x)
  {
   return x > 0.0 ? x : 0.0;
  }

//+------------------------------------------------------------------+
//| Derivative of ReLU function                                      |
//+------------------------------------------------------------------+
double ReLUDerivative(double x)
  {
   return x > 0.0 ? 1.0 : 0.0;
  }

//+------------------------------------------------------------------+
//| Function for backward pass (training)                            |
//+------------------------------------------------------------------+
void BackwardPass(double &inputs[], double expectedOutput, double actualOutput)
  {
   // Calculate gradient for output layer
   double deltaOutput = actualOutput - expectedOutput; // For cross-entropy with sigmoid

   // Update weights and biases for output layer with momentum
   for(int j = 0; j < HiddenLayerSize; j++)
     {
      double gradient = deltaOutput * g_hiddenLayerOutputs[j];
      g_momentumsHiddenOutput[j] = g_beta1 * g_momentumsHiddenOutput[j] + (1.0 - g_beta1) * gradient;
      g_weightsHiddenOutput[j] -= g_learningRate * g_momentumsHiddenOutput[j];
     }

   g_momentumBiasOutput = g_beta1 * g_momentumBiasOutput + (1.0 - g_beta1) * deltaOutput;
   g_biasOutput -= g_learningRate * g_momentumBiasOutput;

   // Calculate gradient for hidden layer
   double deltaHidden[];
   ArrayResize(deltaHidden, HiddenLayerSize);
   for(int j = 0; j < HiddenLayerSize; j++)
     {
      double error = deltaOutput * g_weightsHiddenOutput[j];
      deltaHidden[j] = error * ReLUDerivative(g_hiddenLayerOutputs[j]);
     }

   // Update weights and biases for hidden layer with momentum
   for(int j = 0; j < HiddenLayerSize; j++)
     {
      for(int i = 0; i < INPUT_SIZE; i++)
        {
         int index = i * HiddenLayerSize + j;
         double gradient = deltaHidden[j] * inputs[i];
         g_momentumsInputHidden[index] = g_beta1 * g_momentumsInputHidden[index] + (1.0 - g_beta1) * gradient;
         g_weightsInputHidden[index] -= g_learningRate * g_momentumsInputHidden[index];
        }
      g_momentumBiasHidden[j] = g_beta1 * g_momentumBiasHidden[j] + (1.0 - g_beta1) * deltaHidden[j];
      g_biasHidden[j] -= g_learningRate * g_momentumBiasHidden[j];
     }
  }

//+------------------------------------------------------------------+
//| Sigmoid activation function                                      |
//+------------------------------------------------------------------+
double Sigmoid(double x)
  {
   return 1.0 / (1.0 + MathExp(-x));
  }

//+------------------------------------------------------------------+
//| Function to open trades based on neural network prediction       |
//+------------------------------------------------------------------+
void OpenTrades()
  {
   // Check for maximum spread
   double spread = (SymbolInfoDouble(Symbol(), SYMBOL_ASK) - SymbolInfoDouble(Symbol(), SYMBOL_BID)) / SymbolInfoDouble(Symbol(), SYMBOL_POINT);
   if(spread > MaxSpread)
     {
      PrintLog("Spread too high, skipping trade.");
      return;
     }

   // Prepare inputs
   MqlRates rates[];
   if(CopyRates(Symbol(), PERIOD_M15, 0, 2, rates) < 2)
      return;

   double inputs[INPUT_SIZE];
   inputs[0] = rates[1].open;
   inputs[1] = rates[1].high;
   inputs[2] = rates[1].low;
   inputs[3] = rates[1].close;
   inputs[4] = (double)rates[1].tick_volume; // Explicit cast to double

   // Normalize inputs
   NormalizeInputs(inputs);

   // Get prediction
   double prediction = ForwardPass(inputs);

   // Decide to buy or sell
   if(prediction > 0.6)
     {
      // Buy signal
      double lotSize = CalculateLotSize(Symbol());
      SendOrder(Symbol(), lotSize, ORDER_TYPE_BUY);
     }
   else if(prediction < 0.4)
     {
      // Sell signal
      double lotSize = CalculateLotSize(Symbol());
      SendOrder(Symbol(), lotSize, ORDER_TYPE_SELL);
     }
  }

//+------------------------------------------------------------------+
//| Function to calculate lot size based on equity                   |
//+------------------------------------------------------------------+
double CalculateLotSize(string symbol)
  {
   double equity = AccountInfoDouble(ACCOUNT_EQUITY);
   double riskAmount = equity * (MaxRiskPerTrade / 100.0);

   // Get dynamic Stop Loss using ATR
   double stopLossPips = GetDynamicStopLoss(symbol);

   // Calculate the value per pip
   double tickValue = SymbolInfoDouble(symbol, SYMBOL_TRADE_TICK_VALUE);
   double tickSize  = SymbolInfoDouble(symbol, SYMBOL_TRADE_TICK_SIZE);

   // Handle cases where tickSize is zero
   if(tickSize == 0.0)
      tickSize = SymbolInfoDouble(symbol, SYMBOL_POINT);

   double pipValue = (tickValue / tickSize) * SymbolInfoDouble(symbol, SYMBOL_POINT);

   double stopLossValue = stopLossPips * pipValue;

   // Avoid division by zero
   if(stopLossValue == 0.0)
      stopLossValue = 0.0001;

   // Calculate lot size
   double lotSize = riskAmount / stopLossValue;
   lotSize = NormalizeDouble(lotSize, 2);

   // Ensure lot size is within broker's limits
   double minLot = SymbolInfoDouble(symbol, SYMBOL_VOLUME_MIN);
   double maxLot = SymbolInfoDouble(symbol, SYMBOL_VOLUME_MAX);
   double lotStep = SymbolInfoDouble(symbol, SYMBOL_VOLUME_STEP);

   if(lotSize < minLot)
      lotSize = minLot;
   if(lotSize > maxLot)
      lotSize = maxLot;

   lotSize = MathFloor(lotSize / lotStep) * lotStep;

   return(lotSize);
  }

//+------------------------------------------------------------------+
//| Function to get dynamic Stop Loss using ATR                      |
//+------------------------------------------------------------------+
double GetDynamicStopLoss(string symbol)
  {
   int atrHandle = iATR(symbol, PERIOD_CURRENT, ATRPeriod);
   double atrValues[];
   if(CopyBuffer(atrHandle, 0, 0, 1, atrValues) > 0)
     {
      return atrValues[0] / SymbolInfoDouble(symbol, SYMBOL_POINT); // Return in pips
     }
   else
     {
      // Fallback to default stop loss of 50 pips
      return 50.0;
     }
  }

//+------------------------------------------------------------------+
//| Function to send an order                                        |
//+------------------------------------------------------------------+
bool SendOrder(string symbol, double lotSize, ENUM_ORDER_TYPE orderType)
  {
   MqlTradeRequest request;
   MqlTradeResult  result;
   ZeroMemory(request);
   ZeroMemory(result);

   request.action   = TRADE_ACTION_DEAL;
   request.symbol   = symbol;
   request.volume   = lotSize;
   request.type     = orderType;
   request.type_filling = ORDER_FILLING_IOC;
   request.type_time    = ORDER_TIME_GTC;
   request.deviation    = 10;
   request.magic        = MagicNumber;
   request.comment      = "Opened by NeuralNetEA";

   double price, stopLoss, takeProfit;
   double point = SymbolInfoDouble(symbol, SYMBOL_POINT);
   int digits = (int)SymbolInfoInteger(symbol, SYMBOL_DIGITS);

   // Get dynamic Stop Loss using ATR
   double stopLossPips = GetDynamicStopLoss(symbol);
   double takeProfitPips = stopLossPips * 2; // Risk to reward ratio of 1:2

   // Calculate price, Stop Loss, and Take Profit
   if(orderType == ORDER_TYPE_BUY)
     {
      price = SymbolInfoDouble(symbol, SYMBOL_ASK);
      stopLoss = price - (stopLossPips * point);
      takeProfit = price + (takeProfitPips * point);
     }
   else if(orderType == ORDER_TYPE_SELL)
     {
      price = SymbolInfoDouble(symbol, SYMBOL_BID);
      stopLoss = price + (stopLossPips * point);
      takeProfit = price - (takeProfitPips * point);
     }
   else
     {
      return(false);
     }

   request.price = price;
   request.sl    = NormalizeDouble(stopLoss, digits);
   request.tp    = NormalizeDouble(takeProfit, digits);

   // Send order
   if(!OrderSend(request, result))
     {
      PrintLog("OrderSend failed for " + symbol + ": " + result.comment + " Error: " + IntegerToString(GetLastError()));
      return(false);
     }
   else
     {
      PrintLog("Order placed: " + symbol + ", Ticket: " + IntegerToString((long)result.order));
     }

   return(true);
  }

//+------------------------------------------------------------------+
//| Function to save neural network parameters                       |
//+------------------------------------------------------------------+
void SaveNeuralNetwork()
  {
   int fileHandle = FileOpen(ModelFileName, FILE_BIN|FILE_WRITE|FILE_SHARE_READ|FILE_SHARE_WRITE);
   if(fileHandle != INVALID_HANDLE)
     {
      FileWriteArray(fileHandle, g_weightsInputHidden, 0, WHOLE_ARRAY);
      FileWriteArray(fileHandle, g_weightsHiddenOutput, 0, WHOLE_ARRAY);
      FileWriteArray(fileHandle, g_biasHidden, 0, WHOLE_ARRAY);
      FileWriteDouble(fileHandle, g_biasOutput);
      FileClose(fileHandle);
     }
   else
     {
      PrintLog("Failed to save neural network parameters. Error: " + IntegerToString(GetLastError()));
     }
  }

//+------------------------------------------------------------------+
//| Function to load neural network parameters                       |
//+------------------------------------------------------------------+
void LoadNeuralNetwork()
  {
   int fileHandle = FileOpen(ModelFileName, FILE_BIN|FILE_READ|FILE_SHARE_READ|FILE_SHARE_WRITE);
   if(fileHandle != INVALID_HANDLE)
     {
      FileReadArray(fileHandle, g_weightsInputHidden, 0, WHOLE_ARRAY);
      FileReadArray(fileHandle, g_weightsHiddenOutput, 0, WHOLE_ARRAY);
      FileReadArray(fileHandle, g_biasHidden, 0, WHOLE_ARRAY);
      g_biasOutput = FileReadDouble(fileHandle);
      FileClose(fileHandle);
      PrintLog("Neural network parameters loaded successfully.");
     }
   else
     {
      PrintLog("No saved neural network parameters found. Starting fresh.");
     }
  }

//+------------------------------------------------------------------+
//| Custom logging function                                          |
//+------------------------------------------------------------------+
void PrintLog(string message)
  {
   if(EnableLogging)
     {
      Print(TimeToString(TimeCurrent(), TIME_DATE|TIME_SECONDS) + " - " + message);
     }
  }
