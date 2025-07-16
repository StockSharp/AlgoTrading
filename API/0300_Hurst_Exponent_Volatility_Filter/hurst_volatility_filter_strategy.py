import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, Unit, UnitTypes, CandleStates, ICandleMessage
from StockSharp.Algo.Indicators import SimpleMovingAverage, AverageTrueRange, HurstExponent
from StockSharp.Algo.Strategies import Strategy

class hurst_volatility_filter_strategy(Strategy):
    """
    Strategy using the Hurst exponent to identify mean-reversion markets
    with an ATR-based volatility filter to confirm entry signals
    """

    def __init__(self):
        super(hurst_volatility_filter_strategy, self).__init__()

        # Strategy parameters
        self._hurst_period_param = self.Param("HurstPeriod", 100) \
            .SetDisplay("Hurst Period", "Period for calculating Hurst exponent", "Indicators") \
            .SetGreaterThanZero() \
            .SetCanOptimize(True) \
            .SetOptimize(50, 150, 10)

        self._ma_period_param = self.Param("MAPeriod", 20) \
            .SetDisplay("MA Period", "Period for calculating Moving Average", "Indicators") \
            .SetGreaterThanZero() \
            .SetCanOptimize(True) \
            .SetOptimize(10, 30, 5)

        self._atr_period_param = self.Param("ATRPeriod", 14) \
            .SetDisplay("ATR Period", "Period for calculating Average True Range", "Indicators") \
            .SetGreaterThanZero() \
            .SetCanOptimize(True) \
            .SetOptimize(7, 21, 7)

        self._stop_loss_param = self.Param("StopLoss", 2.0) \
            .SetDisplay("Stop Loss", "Stop loss percentage from entry price", "Risk Management") \
            .SetGreaterThanZero() \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 3.0, 0.5)

        self._candle_type_param = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        # Indicators
        self._sma = None
        self._atr = None
        self._hurst_exponent = None

        # Internal state variables
        self._average_atr = 0.0
        self._is_long_position = False
        self._position_entry_price = 0.0

    @property
    def HurstPeriod(self):
        """Period for Hurst exponent calculation"""
        return self._hurst_period_param.Value

    @HurstPeriod.setter
    def HurstPeriod(self, value):
        self._hurst_period_param.Value = value

    @property
    def MAPeriod(self):
        """Period for Moving Average calculation"""
        return self._ma_period_param.Value

    @MAPeriod.setter
    def MAPeriod(self, value):
        self._ma_period_param.Value = value

    @property
    def ATRPeriod(self):
        """Period for ATR calculation"""
        return self._atr_period_param.Value

    @ATRPeriod.setter
    def ATRPeriod(self, value):
        self._atr_period_param.Value = value

    @property
    def StopLoss(self):
        """Stop loss as percentage of entry price"""
        return self._stop_loss_param.Value

    @StopLoss.setter
    def StopLoss(self, value):
        self._stop_loss_param.Value = value

    @property
    def CandleType(self):
        """Candle type for strategy operation"""
        return self._candle_type_param.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type_param.Value = value

    def GetWorkingSecurities(self):
        """See base class for details."""
        return [(self.Security, self.CandleType)]

    def OnStarted(self, time):
        super(hurst_volatility_filter_strategy, self).OnStarted(time)

        # Reset state
        self._is_long_position = False
        self._position_entry_price = 0
        self._average_atr = 0

        # Create indicators
        self._sma = SimpleMovingAverage()
        self._sma.Length = self.MAPeriod
        self._atr = AverageTrueRange()
        self._atr.Length = self.ATRPeriod
        self._hurst_exponent = HurstExponent()
        # Configure Hurst exponent displacement indicator
        self._hurst_exponent.Length = self.HurstPeriod

        # Subscribe to candles
        subscription = self.SubscribeCandles(self.CandleType)

        # Bind indicators to candle events
        subscription.Bind(self._sma, self._atr, self.ProcessCandle).Start()

        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._sma)
            self.DrawIndicator(area, self._atr)
            self.DrawOwnTrades(area)

        # Enable position protection with stop loss
        self.StartProtection(
            takeProfit=Unit(0),  # No take-profit, using custom exit conditions
            stopLoss=Unit(self.StopLoss, UnitTypes.Percent),
        )

    def ProcessCandle(self, candle: ICandleMessage, sma_value: float, atr_value: float):
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Process Hurst exponent
        hurst_value = self.CalculateHurstExponentValue(candle)
        if hurst_value is None:
            return

        # Update average ATR
        self.UpdateAverageAtr(float(atr_value))

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Manage open positions
        if self.Position != 0:
            self.CheckExitConditions(candle.ClosePrice, float(sma_value))
        else:
            self.CheckEntryConditions(candle.ClosePrice, float(sma_value), hurst_value, float(atr_value))

    def CalculateHurstExponentValue(self, candle: ICandleMessage):
        # In a real implementation, this would use R/S analysis or other methods
        # to calculate the Hurst exponent. For this example, we'll use a placeholder
        # logic that estimates the Hurst exponent based on recent price behavior.

        # Process current price through the displacement indicator
        hurst_value = self._hurst_exponent.Process(candle)

        # For demonstration purposes - in a real implementation you'd use
        # a proper Hurst exponent calculation library or algorithm
        # This is just a placeholder that gives a value between 0 and 1
        return float(hurst_value) if hurst_value is not None else None

    def UpdateAverageAtr(self, atr_value: float):
        if self._average_atr == 0:
            self._average_atr = atr_value
        else:
            # Simple exponential smoothing
            self._average_atr = 0.9 * self._average_atr + 0.1 * atr_value

    def CheckEntryConditions(self, price: float, sma_value: float, hurst_value: float, atr_value: float):
        # Check for mean-reversion markets (Hurst < 0.5)
        if hurst_value < 0.5:
            # Check volatility is lower than average (filtered condition)
            if atr_value < self._average_atr:
                # Long signal: Price is below average in mean-reverting market with low volatility
                if price < sma_value:
                    self.EnterLong(price)
                # Short signal: Price is above average in mean-reverting market with low volatility
                elif price > sma_value:
                    self.EnterShort(price)

    def CheckExitConditions(self, price: float, sma_value: float):
        # Mean reversion exit strategy
        if self._is_long_position and price > sma_value:
            self.ExitPosition(price)
        elif not self._is_long_position and price < sma_value:
            self.ExitPosition(price)

    def EnterLong(self, price: float):
        # Create and send a buy market order
        volume = self.Volume
        self.BuyMarket(volume)

        # Update internal state
        self._is_long_position = True
        self._position_entry_price = price

        self.LogInfo(f"Enter LONG at {price}, Hurst shows mean-reversion market with low volatility")

    def EnterShort(self, price: float):
        # Create and send a sell market order
        volume = self.Volume
        self.SellMarket(volume)

        # Update internal state
        self._is_long_position = False
        self._position_entry_price = price

        self.LogInfo(f"Enter SHORT at {price}, Hurst shows mean-reversion market with low volatility")

    def ExitPosition(self, price: float):
        # Close position at market
        self.ClosePosition()

        # Calculate profit/loss for logging
        if self._is_long_position:
            pnl = (price - self._position_entry_price) / self._position_entry_price * 100
        else:
            pnl = (self._position_entry_price - price) / self._position_entry_price * 100

        self.LogInfo(f"Exit position at {price}, P&L: {pnl:.2f}%, Mean reversion complete")

        # Reset position tracking
        self._position_entry_price = 0

    def OnStopped(self):
        # Close any open positions when strategy stops
        if self.Position != 0:
            self.ClosePosition()
        super(hurst_volatility_filter_strategy, self).OnStopped()

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return hurst_volatility_filter_strategy()
