import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergenceSignal, VolumeWeightedMovingAverage
from StockSharp.Algo.Strategies import Strategy

class macd_vwap_strategy(Strategy):
    """
    MACD + VWAP: enters long when MACD crosses above signal and price above VWAP.
    """

    def __init__(self):
        super(macd_vwap_strategy, self).__init__()
        self._macd_fast = self.Param("MacdFast", 12).SetDisplay("MACD Fast", "Fast EMA period", "Indicators")
        self._macd_slow = self.Param("MacdSlow", 26).SetDisplay("MACD Slow", "Slow EMA period", "Indicators")
        self._macd_signal = self.Param("MacdSignal", 9).SetDisplay("MACD Signal", "Signal line period", "Indicators")
        self._cooldown_bars = self.Param("CooldownBars", 35).SetDisplay("Cooldown Bars", "Bars between entries", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Timeframe", "General")

        self._cooldown = 0
        self._has_prev_diff = False
        self._prev_diff = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(macd_vwap_strategy, self).OnReseted()
        self._cooldown = 0
        self._has_prev_diff = False
        self._prev_diff = 0.0

    def OnStarted(self, time):
        super(macd_vwap_strategy, self).OnStarted(time)
        macd = MovingAverageConvergenceDivergenceSignal()
        macd.Macd.ShortMa.Length = self._macd_fast.Value
        macd.Macd.LongMa.Length = self._macd_slow.Value
        macd.SignalMa.Length = self._macd_signal.Value
        vwap = VolumeWeightedMovingAverage()
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(macd, vwap, self._process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, macd)
            self.DrawIndicator(area, vwap)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, macd_value, vwap_value):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        typed_val = macd_value
        macd_line = typed_val.Macd
        signal_line = typed_val.Signal
        if macd_line is None or signal_line is None:
            return
        macd_f = float(macd_line)
        signal_f = float(signal_line)
        vwap_f = float(vwap_value.ToDecimal())
        price = float(candle.ClosePrice)
        diff = macd_f - signal_f
        if not self._has_prev_diff:
            self._has_prev_diff = True
            self._prev_diff = diff
            return
        cross_up = self._prev_diff <= 0 and diff > 0
        cross_down = self._prev_diff >= 0 and diff < 0
        if self._cooldown > 0:
            self._cooldown -= 1
            self._prev_diff = diff
            return
        if cross_up and price > vwap_f * 1.001 and self.Position <= 0:
            self.BuyMarket()
            self._cooldown = self._cooldown_bars.Value
        elif cross_down and price < vwap_f * 0.999 and self.Position >= 0:
            self.SellMarket()
            self._cooldown = self._cooldown_bars.Value
        elif cross_down and self.Position > 0:
            self.SellMarket()
            self._cooldown = self._cooldown_bars.Value
        elif cross_up and self.Position < 0:
            self.BuyMarket()
            self._cooldown = self._cooldown_bars.Value
        self._prev_diff = diff

    def CreateClone(self):
        return macd_vwap_strategy()
