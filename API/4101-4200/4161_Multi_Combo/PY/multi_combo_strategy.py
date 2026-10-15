import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, RelativeStrengthIndex, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy

class multi_combo_strategy(Strategy):
    """
    Multi Combo: EMA crossover with RSI filter and ATR-based stops.
    """

    def __init__(self):
        super(multi_combo_strategy, self).__init__()
        self._fast_ema = self.Param("FastEmaLength", 9).SetDisplay("Fast EMA", "Fast EMA", "Indicators")
        self._slow_ema = self.Param("SlowEmaLength", 21).SetDisplay("Slow EMA", "Slow EMA", "Indicators")
        self._rsi_length = self.Param("RsiLength", 14).SetDisplay("RSI", "RSI period", "Indicators")
        self._atr_length = self.Param("AtrLength", 14).SetDisplay("ATR", "ATR period", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Candles", "General")

        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._entry_price = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(multi_combo_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._entry_price = 0.0

    def OnStarted2(self, time):
        super(multi_combo_strategy, self).OnStarted2(time)
        fast = ExponentialMovingAverage()
        fast.Length = self._fast_ema.Value
        slow = ExponentialMovingAverage()
        slow.Length = self._slow_ema.Value
        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_length.Value
        atr = AverageTrueRange()
        atr.Length = self._atr_length.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast, slow, rsi, atr, self._process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast)
            self.DrawIndicator(area, slow)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, fast_val, slow_val, rsi_val, atr_val):
        if candle.State != CandleStates.Finished:
            return
        fast = float(fast_val)
        slow = float(slow_val)
        rsi = float(rsi_val)
        atr = float(atr_val)
        close = float(candle.ClosePrice)
        if self._prev_fast == 0 or self._prev_slow == 0 or atr <= 0:
            self._prev_fast = fast
            self._prev_slow = slow
            return
        if self.Position > 0:
            if fast < slow and self._prev_fast >= self._prev_slow:
                self.SellMarket()
                self._entry_price = 0
            elif close <= self._entry_price - atr * 2.0:
                self.SellMarket()
                self._entry_price = 0
        elif self.Position < 0:
            if fast > slow and self._prev_fast <= self._prev_slow:
                self.BuyMarket()
                self._entry_price = 0
            elif close >= self._entry_price + atr * 2.0:
                self.BuyMarket()
                self._entry_price = 0
        if self.Position == 0:
            if fast > slow and self._prev_fast <= self._prev_slow and rsi > 45:
                self._entry_price = close
                self.BuyMarket()
            elif fast < slow and self._prev_fast >= self._prev_slow and rsi < 55:
                self._entry_price = close
                self.SellMarket()
        self._prev_fast = fast
        self._prev_slow = slow

    def CreateClone(self):
        return multi_combo_strategy()
