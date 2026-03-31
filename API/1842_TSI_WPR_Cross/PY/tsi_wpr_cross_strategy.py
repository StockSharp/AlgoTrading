import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import TrueStrengthIndex, WilliamsR
from StockSharp.Algo.Strategies import Strategy


class tsi_wpr_cross_strategy(Strategy):
    def __init__(self):
        super(tsi_wpr_cross_strategy, self).__init__()
        self._wpr_period = self.Param("WprPeriod", 14) \
            .SetDisplay("Williams %R Period", "Period for Williams %R", "Indicators")
        self._cooldown_bars = self.Param("CooldownBars", 4) \
            .SetDisplay("Cooldown Bars", "Completed candles to wait after a signal", "Signal")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Candle type for strategy", "General")
        self._prev_tsi = 0.0
        self._prev_signal = 0.0
        self._initialized = False
        self._cooldown_remaining = 0

    @property
    def wpr_period(self):
        return self._wpr_period.Value

    @property
    def cooldown_bars(self):
        return self._cooldown_bars.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(tsi_wpr_cross_strategy, self).OnReseted()
        self._prev_tsi = 0.0
        self._prev_signal = 0.0
        self._initialized = False
        self._cooldown_remaining = 0

    def OnStarted2(self, time):
        super(tsi_wpr_cross_strategy, self).OnStarted2(time)
        tsi = TrueStrengthIndex()
        wpr = WilliamsR()
        wpr.Length = self.wpr_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(tsi, wpr, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, tsi)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, tsi_value, wpr_value):
        if candle.State != CandleStates.Finished:
            return
        if not tsi_value.IsFinal or not wpr_value.IsFinal:
            return
        tsi_val = tsi_value.Tsi
        signal_val = tsi_value.Signal
        if tsi_val is None or signal_val is None:
            return
        tsi_val = float(tsi_val)
        signal_val = float(signal_val)
        wpr = float(wpr_value)
        if not self._initialized:
            self._prev_tsi = tsi_val
            self._prev_signal = signal_val
            self._initialized = True
            return
        crossed_up = self._prev_tsi <= self._prev_signal and tsi_val > signal_val
        crossed_down = self._prev_tsi >= self._prev_signal and tsi_val < signal_val
        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
        if crossed_up and wpr < -55.0 and self._cooldown_remaining == 0 and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._cooldown_remaining = self.cooldown_bars
        elif crossed_down and wpr > -45.0 and self._cooldown_remaining == 0 and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._cooldown_remaining = self.cooldown_bars
        self._prev_tsi = tsi_val
        self._prev_signal = signal_val

    def CreateClone(self):
        return tsi_wpr_cross_strategy()
