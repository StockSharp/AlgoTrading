import clr
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Messages")

from System import TimeSpan, Math
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Messages import CandleStates
from datatype_extensions import *

class full_candle_strategy(Strategy):
    """Full Candle breakout strategy.

    Trades when a candle closes beyond its EMA with a tiny shadow. Optional
    take-profit and stop-loss percentages manage risk on open positions.
    """

    def __init__(self):
        super(full_candle_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", tf(1)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._ema_len = self.Param("EmaLength", 10) \
            .SetDisplay("EMA Length", "Period of EMA", "Indicator")
        self._shadow_percent = self.Param("ShadowPercent", 5) \
            .SetDisplay("Shadow %", "Maximum wick percentage", "Rules")
        self._use_tp = self.Param("UseTP", False)
        self._tp = self.Param("TPPercent", 1.2)
        self._use_sl = self.Param("UseSL", False)
        self._sl = self.Param("SLPercent", 1.8)

        self._ema = ExponentialMovingAverage()
        self._entry = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(full_candle_strategy, self).OnReseted()
        self._ema = ExponentialMovingAverage()
        self._entry = 0.0

    def OnStarted(self, time):
        super(full_candle_strategy, self).OnStarted(time)
        self._ema.Length = self._ema_len.Value
        sub = self.SubscribeCandles(self.candle_type)
        sub.Bind(self._ema, self._on_process).Start()

    def _on_process(self, candle, ema_val):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        if not self._ema.IsFormed:
            return
        size = candle.HighPrice - candle.LowPrice
        if size == 0:
            return
        shadow = (candle.HighPrice - candle.ClosePrice) if candle.ClosePrice > candle.OpenPrice \
            else (candle.ClosePrice - candle.LowPrice)
        shadow_pct = shadow * 100 / size

        entry_long = (candle.ClosePrice > candle.OpenPrice and
                       candle.ClosePrice > ema_val and
                       shadow_pct <= self._shadow_percent.Value)
        entry_short = (candle.ClosePrice < candle.OpenPrice and
                        candle.ClosePrice < ema_val and
                        shadow_pct <= self._shadow_percent.Value)

        if self.Position > 0:
            exit_long = False
            if self._use_tp.Value:
                exit_long = exit_long or candle.ClosePrice >= self._entry * (1 + self._tp.Value/100)
            if self._use_sl.Value:
                exit_long = exit_long or candle.ClosePrice <= self._entry * (1 - self._sl.Value/100)
        else:
            exit_long = False
        if self.Position < 0:
            exit_short = False
            if self._use_tp.Value:
                exit_short = exit_short or candle.ClosePrice <= self._entry * (1 - self._tp.Value/100)
            if self._use_sl.Value:
                exit_short = exit_short or candle.ClosePrice >= self._entry * (1 + self._sl.Value/100)
        else:
            exit_short = False

        if exit_long or exit_short:
            self.ClosePosition()
            self._entry = 0.0
        elif entry_long and self.Position <= 0:
            self.BuyMarket(self.Volume + Math.Abs(self.Position))
            self._entry = candle.ClosePrice
        elif entry_short and self.Position >= 0:
            self.SellMarket(self.Volume + Math.Abs(self.Position))
            self._entry = candle.ClosePrice

    def CreateClone(self):
        return full_candle_strategy()
