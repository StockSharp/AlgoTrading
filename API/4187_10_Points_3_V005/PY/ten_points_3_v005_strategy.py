import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, RelativeStrengthIndex, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy

class ten_points_3_v005_strategy(Strategy):
    """Dual EMA crossover with RSI confirmation and ATR stops."""
    def __init__(self):
        super(ten_points_3_v005_strategy, self).__init__()
        self._fast_ema = self.Param("FastEmaLength", 10).SetDisplay("Fast EMA", "Fast EMA period", "Indicators")
        self._slow_ema = self.Param("SlowEmaLength", 30).SetDisplay("Slow EMA", "Slow EMA period", "Indicators")
        self._rsi_length = self.Param("RsiLength", 14).SetDisplay("RSI Length", "RSI period", "Indicators")
        self._atr_length = self.Param("AtrLength", 14).SetDisplay("ATR Length", "ATR period", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Timeframe", "General")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(ten_points_3_v005_strategy, self).OnReseted()
        self._prev_fast = 0
        self._prev_slow = 0
        self._entry_price = 0

    def OnStarted2(self, time):
        super(ten_points_3_v005_strategy, self).OnStarted2(time)
        self._prev_fast = 0
        self._prev_slow = 0
        self._entry_price = 0

        fast = ExponentialMovingAverage()
        fast.Length = self._fast_ema.Value
        slow = ExponentialMovingAverage()
        slow.Length = self._slow_ema.Value
        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_length.Value
        atr = AverageTrueRange()
        atr.Length = self._atr_length.Value

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(fast, slow, rsi, atr, self.OnProcess).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawIndicator(area, fast)
            self.DrawIndicator(area, slow)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, fast_val, slow_val, rsi_val, atr_val):
        if candle.State != CandleStates.Finished:
            return

        if self._prev_fast == 0 or self._prev_slow == 0 or atr_val <= 0:
            self._prev_fast = fast_val
            self._prev_slow = slow_val
            return

        close = float(candle.ClosePrice)

        if self.Position > 0:
            if (fast_val < slow_val and self._prev_fast >= self._prev_slow) or close <= self._entry_price - atr_val * 1.5:
                self.SellMarket()
                self._entry_price = 0
        elif self.Position < 0:
            if (fast_val > slow_val and self._prev_fast <= self._prev_slow) or close >= self._entry_price + atr_val * 1.5:
                self.BuyMarket()
                self._entry_price = 0

        if self.Position == 0:
            if fast_val > slow_val and self._prev_fast <= self._prev_slow and rsi_val > 50:
                self._entry_price = close
                self.BuyMarket()
            elif fast_val < slow_val and self._prev_fast >= self._prev_slow and rsi_val < 50:
                self._entry_price = close
                self.SellMarket()

        self._prev_fast = fast_val
        self._prev_slow = slow_val

    def CreateClone(self):
        return ten_points_3_v005_strategy()
