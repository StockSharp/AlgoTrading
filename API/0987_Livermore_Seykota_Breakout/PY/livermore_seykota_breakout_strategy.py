import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, AverageTrueRange, Highest, Lowest
from StockSharp.Algo.Strategies import Strategy


class livermore_seykota_breakout_strategy(Strategy):
    def __init__(self):
        super(livermore_seykota_breakout_strategy, self).__init__()
        self._ema_length = self.Param("EmaLength", 50) \
            .SetDisplay("EMA Length", "EMA trend period", "Indicators")
        self._pivot_length = self.Param("PivotLength", 30) \
            .SetDisplay("Pivot Length", "Bars for pivot high/low", "General")
        self._atr_length = self.Param("AtrLength", 14) \
            .SetDisplay("ATR Length", "ATR period", "Indicators")
        self._trail_atr_mult = self.Param("TrailAtrMultiplier", 10.0) \
            .SetDisplay("Trail ATR Mult", "ATR trailing mult", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))) \
            .SetDisplay("Candle Type", "Candles", "General")
        self._cooldown_bars = self.Param("CooldownBars", 50) \
            .SetDisplay("Cooldown Bars", "Min bars between signals", "General")
        self._prev_highest = 0.0
        self._prev_lowest = 0.0
        self._bars_since_signal = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(livermore_seykota_breakout_strategy, self).OnReseted()
        self._prev_highest = 0.0
        self._prev_lowest = 0.0
        self._bars_since_signal = 0

    def OnStarted(self, time):
        super(livermore_seykota_breakout_strategy, self).OnStarted(time)
        self._prev_highest = 0.0
        self._prev_lowest = 0.0
        self._bars_since_signal = 0
        self._ema = ExponentialMovingAverage()
        self._ema.Length = self._ema_length.Value
        self._atr = AverageTrueRange()
        self._atr.Length = self._atr_length.Value
        self._highest = Highest()
        self._highest.Length = self._pivot_length.Value
        self._lowest = Lowest()
        self._lowest.Length = self._pivot_length.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._ema, self._atr, self._highest, self._lowest, self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._ema)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, ema_val, atr_val, high_val, low_val):
        if candle.State != CandleStates.Finished:
            return
        self._bars_since_signal += 1
        ev = float(ema_val)
        av = float(atr_val)
        hv = float(high_val)
        lv = float(low_val)
        if not self._ema.IsFormed or not self._atr.IsFormed or not self._highest.IsFormed or not self._lowest.IsFormed:
            self._prev_highest = hv
            self._prev_lowest = lv
            return
        if av <= 0.0 or self._bars_since_signal < self._cooldown_bars.Value:
            self._prev_highest = hv
            self._prev_lowest = lv
            return
        close = float(candle.ClosePrice)
        if close > self._prev_highest and close > ev and self.Position <= 0:
            self.BuyMarket()
            self._bars_since_signal = 0
        elif close < self._prev_lowest and close < ev and self.Position >= 0:
            self.SellMarket()
            self._bars_since_signal = 0
        self._prev_highest = hv
        self._prev_lowest = lv

    def CreateClone(self):
        return livermore_seykota_breakout_strategy()
