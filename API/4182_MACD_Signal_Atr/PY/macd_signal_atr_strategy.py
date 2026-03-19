import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy

class macd_signal_atr_strategy(Strategy):
    """
    MACD Signal ATR: EMA crossover with ATR-based stops.
    """

    def __init__(self):
        super(macd_signal_atr_strategy, self).__init__()
        self._fast_ema_length = self.Param("FastEmaLength", 12).SetDisplay("Fast EMA", "Fast EMA period", "Indicators")
        self._slow_ema_length = self.Param("SlowEmaLength", 26).SetDisplay("Slow EMA", "Slow EMA period", "Indicators")
        self._atr_length = self.Param("AtrLength", 14).SetDisplay("ATR Length", "ATR period", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Candles", "General")

        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._entry_price = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(macd_signal_atr_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._entry_price = 0.0

    def OnStarted(self, time):
        super(macd_signal_atr_strategy, self).OnStarted(time)
        fast = ExponentialMovingAverage()
        fast.Length = self._fast_ema_length.Value
        slow = ExponentialMovingAverage()
        slow.Length = self._slow_ema_length.Value
        atr = AverageTrueRange()
        atr.Length = self._atr_length.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast, slow, atr, self._process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast)
            self.DrawIndicator(area, slow)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, fast_val, slow_val, atr_val):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        fast = float(fast_val)
        slow = float(slow_val)
        atr = float(atr_val)
        if self._prev_fast == 0.0 or self._prev_slow == 0.0 or atr <= 0:
            self._prev_fast = fast
            self._prev_slow = slow
            return
        close = float(candle.ClosePrice)
        if self.Position > 0:
            if (fast < slow and self._prev_fast >= self._prev_slow) or close <= self._entry_price - atr * 2.0:
                self.SellMarket()
                self._entry_price = 0.0
        elif self.Position < 0:
            if (fast > slow and self._prev_fast <= self._prev_slow) or close >= self._entry_price + atr * 2.0:
                self.BuyMarket()
                self._entry_price = 0.0
        if self.Position == 0:
            if fast > slow and self._prev_fast <= self._prev_slow:
                self._entry_price = close
                self.BuyMarket()
            elif fast < slow and self._prev_fast >= self._prev_slow:
                self._entry_price = close
                self.SellMarket()
        self._prev_fast = fast
        self._prev_slow = slow

    def CreateClone(self):
        return macd_signal_atr_strategy()
