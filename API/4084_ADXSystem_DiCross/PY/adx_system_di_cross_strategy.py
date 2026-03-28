import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy

class adx_system_di_cross_strategy(Strategy):
    """
    ADX System DI Cross: trend-following using dual EMA crossover
    (proxy for +DI/-DI cross) with ATR-based volatility filter.
    """

    def __init__(self):
        super(adx_system_di_cross_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))) \
            .SetDisplay("Candle Type", "Timeframe.", "General")

        self._fast_length = self.Param("FastLength", 10) \
            .SetDisplay("Fast EMA", "Fast EMA period (proxy for +DI).", "Indicators")

        self._slow_length = self.Param("SlowLength", 20) \
            .SetDisplay("Slow EMA", "Slow EMA period (proxy for -DI).", "Indicators")

        self._atr_length = self.Param("AtrLength", 14) \
            .SetDisplay("ATR Length", "ATR period for trend strength.", "Indicators")

        self._atr_threshold = self.Param("AtrThreshold", 50.0) \
            .SetDisplay("ATR Threshold", "Min ATR value for entry.", "Indicators")

        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._entry_price = 0.0

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def FastLength(self):
        return self._fast_length.Value

    @FastLength.setter
    def FastLength(self, value):
        self._fast_length.Value = value

    @property
    def SlowLength(self):
        return self._slow_length.Value

    @SlowLength.setter
    def SlowLength(self, value):
        self._slow_length.Value = value

    @property
    def AtrLength(self):
        return self._atr_length.Value

    @AtrLength.setter
    def AtrLength(self, value):
        self._atr_length.Value = value

    @property
    def AtrThreshold(self):
        return self._atr_threshold.Value

    @AtrThreshold.setter
    def AtrThreshold(self, value):
        self._atr_threshold.Value = value

    def OnReseted(self):
        super(adx_system_di_cross_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._entry_price = 0.0

    def OnStarted(self, time):
        super(adx_system_di_cross_strategy, self).OnStarted(time)

        fast = ExponentialMovingAverage()
        fast.Length = self.FastLength
        slow = ExponentialMovingAverage()
        slow.Length = self.SlowLength
        atr = AverageTrueRange()
        atr.Length = self.AtrLength

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(fast, slow, atr, self.ProcessCandle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast)
            self.DrawIndicator(area, slow)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, fast_val, slow_val, atr_val):
        if candle.State != CandleStates.Finished:
            return

        if self._prev_fast == 0 or self._prev_slow == 0:
            self._prev_fast = fast_val
            self._prev_slow = slow_val
            return

        bullish_cross = self._prev_fast <= self._prev_slow and fast_val > slow_val
        bearish_cross = self._prev_fast >= self._prev_slow and fast_val < slow_val

        # Exit on opposite cross
        if self.Position > 0 and bearish_cross:
            self.SellMarket()
            self._entry_price = 0.0
        elif self.Position < 0 and bullish_cross:
            self.BuyMarket()
            self._entry_price = 0.0

        # Entry on cross + volatility filter
        if self.Position == 0 and atr_val >= self.AtrThreshold:
            if bullish_cross:
                self._entry_price = float(candle.ClosePrice)
                self.BuyMarket()
            elif bearish_cross:
                self._entry_price = float(candle.ClosePrice)
                self.SellMarket()

        self._prev_fast = fast_val
        self._prev_slow = slow_val

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return adx_system_di_cross_strategy()
