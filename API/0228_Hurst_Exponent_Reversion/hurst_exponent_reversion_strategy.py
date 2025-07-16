import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math, Random
from StockSharp.Messages import DataType, UnitTypes, Unit, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class hurst_exponent_reversion_strategy(Strategy):
    """
    Strategy that trades based on Hurst Exponent mean reversion signals.
    Buys when Hurst exponent is below 0.5 (indicating mean reversion) and price is below average.
    Sells when Hurst exponent is below 0.5 and price is above average.
    """

    def __init__(self):
        super(hurst_exponent_reversion_strategy, self).__init__()

        # Initialize strategy parameters
        self._hurst_period = self.Param("HurstPeriod", 100) \
            .SetDisplay("Hurst period", "Period for Hurst exponent calculation", "Strategy parameters") \
            .SetCanOptimize(True) \
            .SetOptimize(50, 150, 10)

        self._average_period = self.Param("AveragePeriod", 20) \
            .SetDisplay("Average period", "Period for price average calculation", "Strategy parameters") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 50, 5)

        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetDisplay("Stop-loss %", "Stop-loss as percentage from entry price", "Risk management") \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 3.0, 0.5)

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle type", "Type of candles to use", "General")

        self._sma = None
        self._previous_hurst_value = 0.0
        self._current_price = 0.0

    @property
    def HurstPeriod(self):
        """Period for Hurst exponent calculation."""
        return self._hurst_period.Value

    @HurstPeriod.setter
    def HurstPeriod(self, value):
        self._hurst_period.Value = value

    @property
    def AveragePeriod(self):
        """Period for moving average calculation."""
        return self._average_period.Value

    @AveragePeriod.setter
    def AveragePeriod(self, value):
        self._average_period.Value = value

    @property
    def StopLossPercent(self):
        """Stop-loss percentage."""
        return self._stop_loss_percent.Value

    @StopLossPercent.setter
    def StopLossPercent(self, value):
        self._stop_loss_percent.Value = value

    @property
    def CandleType(self):
        """Type of candles to use."""
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def GetWorkingSecurities(self):
        """Return security and timeframe used by the strategy."""
        return [(self.Security, self.CandleType)]

    def OnStarted(self, time):
        """Called when the strategy starts."""
        super(hurst_exponent_reversion_strategy, self).OnStarted(time)

        self._previous_hurst_value = 0.0
        self._current_price = 0.0

        # Initialize the SMA indicator
        self._sma = SimpleMovingAverage()
        self._sma.Length = self.AveragePeriod

        # Create a subscription to candlesticks
        subscription = self.SubscribeCandles(self.CandleType)

        # Subscribe to candle processing
        subscription.Bind(self._sma, self.ProcessCandle).Start()

        # Start position protection
        self.StartProtection(
            Unit(self.StopLossPercent, UnitTypes.Percent),
            Unit(self.StopLossPercent * 1.5, UnitTypes.Percent)
        )

        # Setup chart if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._sma)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, sma_value):
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Store current price
        self._current_price = candle.ClosePrice

        # Calculate Hurst exponent (simplified approach)
        # In a real implementation, you would use a proper Hurst exponent calculation
        # This is a placeholder to demonstrate the concept
        hurst_value = self.CalculateSimplifiedHurst(candle)

        # Store for logging
        self._previous_hurst_value = hurst_value

        # Mean reversion market condition (Hurst < 0.5)
        if hurst_value < 0.5:
            # Price below average - buy signal
            if self._current_price < sma_value and self.Position <= 0:
                self.BuyMarket(self.Volume)
                self.LogInfo(f"Buy signal: Hurst={hurst_value}, Price={self._current_price}, SMA={sma_value}")
            # Price above average - sell signal
            elif self._current_price > sma_value and self.Position >= 0:
                self.SellMarket(self.Volume + Math.Abs(self.Position))
                self.LogInfo(f"Sell signal: Hurst={hurst_value}, Price={self._current_price}, SMA={sma_value}")

    def CalculateSimplifiedHurst(self, candle):
        # This is a simplified placeholder implementation
        # A real Hurst exponent would require more complex calculations
        # Simplified approach: if volatility is decreasing, return value below 0.5 (mean-reverting)
        # If volatility is increasing, return value above 0.5 (trending)

        # For demonstration only - in a real implementation,
        # use a proper Hurst exponent calculation based on R/S analysis or similar method
        rand = Random(int(candle.OpenTime.Ticks))
        return 0.3 + rand.NextDouble() * 0.4  # Returns value between 0.3 and 0.7

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return hurst_exponent_reversion_strategy()
