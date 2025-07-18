import clr

clr.AddReference("System.Drawing")
clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, Unit, UnitTypes, CandleStates, ICandleMessage
from StockSharp.Algo.Indicators import SimpleMovingAverage, Lowest
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class spring_reversal_strategy(Strategy):
    """
    Strategy based on Spring Reversal pattern, which occurs when price makes a new low below support
    but immediately reverses and closes above the support level, indicating a bullish reversal.
    """

    def __init__(self):
        super(spring_reversal_strategy, self).__init__()

        # Strategy parameters
        self._candle_type_param = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use for analysis", "General")
        self._lookback_period_param = self.Param("LookbackPeriod", 20) \
            .SetDisplay("Lookback Period", "Period for support level detection", "Range") \
            .SetRange(5, 50)
        self._ma_period_param = self.Param("MaPeriod", 20) \
            .SetDisplay("MA Period", "Period for moving average calculation", "Trend") \
            .SetRange(5, 50)
        self._stop_loss_percent_param = self.Param("StopLossPercent", 1.0) \
            .SetDisplay("Stop Loss %", "Stop-loss percentage from entry price", "Protection") \
            .SetRange(0.5, 3.0)

        # Indicators
        self._ma = None
        self._lowest = None
        self._last_lowest_value = 0

    @property
    def CandleType(self):
        """Candle type and timeframe for the strategy."""
        return self._candle_type_param.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type_param.Value = value

    @property
    def LookbackPeriod(self):
        """Period for low range detection."""
        return self._lookback_period_param.Value

    @LookbackPeriod.setter
    def LookbackPeriod(self, value):
        self._lookback_period_param.Value = value

    @property
    def MaPeriod(self):
        """Period for moving average calculation."""
        return self._ma_period_param.Value

    @MaPeriod.setter
    def MaPeriod(self, value):
        self._ma_period_param.Value = value

    @property
    def StopLossPercent(self):
        """Stop-loss percentage from entry price."""
        return self._stop_loss_percent_param.Value

    @StopLossPercent.setter
    def StopLossPercent(self, value):
        self._stop_loss_percent_param.Value = value

    def OnStarted(self, time):
        """
        Called when the strategy starts.
        """
        super(spring_reversal_strategy, self).OnStarted(time)

        self._last_lowest_value = 0

        # Initialize indicators
        self._ma = SimpleMovingAverage()
        self._ma.Length = self.MaPeriod
        self._lowest = Lowest()
        self._lowest.Length = self.LookbackPeriod

        # Create and setup subscription for candles
        subscription = self.SubscribeCandles(self.CandleType)

        # Bind indicators and processor
        subscription.Bind(self._ma, self._lowest, self.ProcessCandle).Start()

        # Enable stop-loss protection
        self.StartProtection(
            takeProfit=Unit(0),
            stopLoss=Unit(self.StopLossPercent, UnitTypes.Percent)
        )
        # Setup chart if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._ma)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, ma_value, lowest_value):
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Store the last lowest value
        self._last_lowest_value = lowest_value

        # Determine candle characteristics
        is_bullish = candle.ClosePrice > candle.OpenPrice
        pierces_below_support = candle.LowPrice < self._last_lowest_value
        close_above_support = candle.ClosePrice > self._last_lowest_value

        # Spring pattern:
        # 1. Price dips below recent low (support level)
        # 2. But closes above the support level (bullish rejection)
        if pierces_below_support and close_above_support and is_bullish:
            # Enter long position only if we're not already long
            if self.Position <= 0:
                volume = self.Volume + Math.Abs(self.Position)
                self.BuyMarket(volume)

                self.LogInfo("Spring Reversal detected. Support level: {0}, Low: {1}. Long entry at {2}".format(
                    self._last_lowest_value, candle.LowPrice, candle.ClosePrice))

        # Exit conditions
        if self.Position > 0:
            # Exit when price rises above the moving average (take profit)
            if candle.ClosePrice > ma_value:
                self.SellMarket(Math.Abs(self.Position))

                self.LogInfo("Exit signal: Price above MA. Closed long position at {0}".format(candle.ClosePrice))

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return spring_reversal_strategy()
