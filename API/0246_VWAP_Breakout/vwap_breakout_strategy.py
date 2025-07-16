import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, Unit, UnitTypes, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange, VolumeWeightedMovingAverage
from StockSharp.Algo.Strategies import Strategy

class vwap_breakout_strategy(Strategy):
    """
    VWAP Breakout Strategy (246).
    Enter when price breaks out from VWAP by a certain ATR multiple.
    Exit when price returns to VWAP.

    See more examples: https://github.com/StockSharp/AlgoTrading
    """

    def __init__(self):
        super(vwap_breakout_strategy, self).__init__()

        # Strategy parameters
        self._k_param = self.Param("K", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR Multiplier", "ATR multiplier for entry distance from VWAP", "Strategy Parameters") \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 4.0, 0.5)

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "Strategy Parameters")

        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR Period", "ATR indicator period", "Strategy Parameters") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 20, 2)

        # Internal state
        self._atr = None
        self._vwap = None
        self._current_atr = 0
        self._current_vwap = 0
        self._current_price = 0

    @property
    def K(self):
        """ATR multiplier for entry."""
        return self._k_param.Value

    @K.setter
    def K(self, value):
        self._k_param.Value = value

    @property
    def CandleType(self):
        """Type of candles to use."""
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def AtrPeriod(self):
        """ATR period."""
        return self._atr_period.Value

    @AtrPeriod.setter
    def AtrPeriod(self, value):
        self._atr_period.Value = value

    def OnStarted(self, time):
        super(vwap_breakout_strategy, self).OnStarted(time)

        self._current_atr = 0
        self._current_vwap = 0
        self._current_price = 0

        # Create ATR indicator
        self._atr = AverageTrueRange()
        self._atr.Length = self.AtrPeriod
        self._vwap = VolumeWeightedMovingAverage()
        self._vwap.Length = self.AtrPeriod

        # Candle subscription
        subscription = self.SubscribeCandles(self.CandleType)

        # Bind ATR to candles
        subscription.Bind(self._atr, self.ProcessCandle).Start()

        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._atr)
            self.DrawOwnTrades(area)

        # Enable position protection
        self.StartProtection(
            takeProfit=Unit(5, UnitTypes.Percent),
            stopLoss=Unit(2, UnitTypes.Percent)
        )

    def ProcessCandle(self, candle, atr_value):
        if candle.State != CandleStates.Finished:
            return

        self._current_atr = atr_value
        self._current_price = candle.ClosePrice

        self._current_vwap = self._vwap.Process(candle).ToDecimal()

        self.UpdateStrategy()

    def UpdateStrategy(self):
        # Check if strategy is ready for trading
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Ensure we have valid values
        if self._current_vwap <= 0 or self._current_atr <= 0 or self._current_price <= 0:
            return

        # Calculate entry bands
        upper_band = self._current_vwap + self.K * self._current_atr
        lower_band = self._current_vwap - self.K * self._current_atr

        self.LogInfo("Price: {0}, VWAP: {1}, Upper: {2}, Lower: {3}".format(
            self._current_price, self._current_vwap, upper_band, lower_band))

        # Entry logic - BREAKOUT
        if self.Position == 0:
            # Long Entry: Price breaks above upper band
            if self._current_price > upper_band:
                self.LogInfo("Buy Signal - Price ({0}) > Upper Band ({1})".format(
                    self._current_price, upper_band))
                self.BuyMarket(self.Volume)
            # Short Entry: Price breaks below lower band
            elif self._current_price < lower_band:
                self.LogInfo("Sell Signal - Price ({0}) < Lower Band ({1})".format(
                    self._current_price, lower_band))
                self.SellMarket(self.Volume)
        # Exit logic
        elif self.Position > 0 and self._current_price < self._current_vwap:
            # Exit Long: Price returns below VWAP
            self.LogInfo("Exit Long - Price ({0}) < VWAP ({1})".format(
                self._current_price, self._current_vwap))
            self.SellMarket(abs(self.Position))
        elif self.Position < 0 and self._current_price > self._current_vwap:
            # Exit Short: Price returns above VWAP
            self.LogInfo("Exit Short - Price ({0}) > VWAP ({1})".format(
                self._current_price, self._current_vwap))
            self.BuyMarket(abs(self.Position))

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return vwap_breakout_strategy()
