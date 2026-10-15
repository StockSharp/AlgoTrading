import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy

class mnq_ema_strategy(Strategy):
    """
    MNQ strategy based on multiple EMA levels.
    EMA5/13 crossover with EMA30/200 trend filter.
    """

    def __init__(self):
        super(mnq_ema_strategy, self).__init__()
        self._ema5_len = self.Param("Ema5Length", 5).SetDisplay("EMA 5", "EMA 5 length", "Indicators")
        self._ema13_len = self.Param("Ema13Length", 13).SetDisplay("EMA 13", "EMA 13 length", "Indicators")
        self._ema30_len = self.Param("Ema30Length", 30).SetDisplay("EMA 30", "EMA 30 length", "Indicators")
        self._ema200_len = self.Param("Ema200Length", 50).SetDisplay("EMA 200", "EMA 200 length", "Indicators")
        self._cooldown_bars = self.Param("SignalCooldownBars", 12).SetDisplay("Cooldown", "Min bars between signals", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))).SetDisplay("Candle Type", "Timeframe", "General")

        self._prev_e5 = 0.0
        self._prev_e13 = 0.0
        self._has_prev = False
        self._bars_from_signal = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(mnq_ema_strategy, self).OnReseted()
        self._prev_e5 = 0.0
        self._prev_e13 = 0.0
        self._has_prev = False
        self._bars_from_signal = 0

    def OnStarted2(self, time):
        super(mnq_ema_strategy, self).OnStarted2(time)
        self._prev_e5 = 0.0
        self._prev_e13 = 0.0
        self._has_prev = False
        self._bars_from_signal = self._cooldown_bars.Value
        e5 = ExponentialMovingAverage()
        e5.Length = self._ema5_len.Value
        e13 = ExponentialMovingAverage()
        e13.Length = self._ema13_len.Value
        e30 = ExponentialMovingAverage()
        e30.Length = self._ema30_len.Value
        e200 = ExponentialMovingAverage()
        e200.Length = self._ema200_len.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(e5, e13, e30, e200, self._process_candle).Start()

    def _process_candle(self, candle, e5v, e13v, e30v, e200v):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        e5 = float(e5v)
        e13 = float(e13v)
        e30 = float(e30v)
        e200 = float(e200v)
        close = float(candle.ClosePrice)
        self._bars_from_signal += 1
        if not self._has_prev:
            self._prev_e5 = e5
            self._prev_e13 = e13
            self._has_prev = True
            return
        cross_up = self._prev_e5 <= self._prev_e13 and e5 > e13
        cross_down = self._prev_e5 >= self._prev_e13 and e5 < e13
        long_trend = close > e200 and e30 > e200
        short_trend = close < e200 and e30 < e200
        if self._bars_from_signal >= self._cooldown_bars.Value and cross_up and long_trend and self.Position <= 0:
            self.BuyMarket()
            self._bars_from_signal = 0
        elif self._bars_from_signal >= self._cooldown_bars.Value and cross_down and short_trend and self.Position >= 0:
            self.SellMarket()
            self._bars_from_signal = 0
        elif self.Position > 0 and cross_down:
            self.SellMarket()
        elif self.Position < 0 and cross_up:
            self.BuyMarket()
        self._prev_e5 = e5
        self._prev_e13 = e13

    def CreateClone(self):
        return mnq_ema_strategy()
