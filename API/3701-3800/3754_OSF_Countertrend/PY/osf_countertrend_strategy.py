import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class osf_countertrend_strategy(Strategy):
    """RSI countertrend strategy. Sells when RSI is above 50 (overbought), buys when
    below 50 (oversold). Includes cooldown bars and virtual take-profit per direction."""

    def __init__(self):
        super(osf_countertrend_strategy, self).__init__()

        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("RSI Period", "RSI length used in oscillator", "General")
        self._volume_per_point = self.Param("VolumePerPoint", 0.01) \
            .SetDisplay("Volume per Point", "Order volume per RSI point from 50", "Risk")
        self._take_profit_points = self.Param("TakeProfitPoints", 150.0) \
            .SetDisplay("Take Profit", "Distance to take profit in points", "Risk")
        self._cooldown_bars = self.Param("CooldownBars", 5) \
            .SetDisplay("Cooldown Bars", "Finished candles to wait after trading", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Data series for processing", "General")

        self._rsi = None
        self._cooldown = 0
        self._long_target = 0.0
        self._short_target = 0.0

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def RsiPeriod(self):
        return self._rsi_period.Value

    @property
    def VolumePerPoint(self):
        return self._volume_per_point.Value

    @property
    def TakeProfitPoints(self):
        return self._take_profit_points.Value

    @property
    def CooldownBars(self):
        return self._cooldown_bars.Value

    def OnReseted(self):
        super(osf_countertrend_strategy, self).OnReseted()
        self._rsi = None
        self._cooldown = 0
        self._long_target = 0.0
        self._short_target = 0.0

    def OnStarted2(self, time):
        super(osf_countertrend_strategy, self).OnStarted2(time)

        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = self.RsiPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._rsi, self._process_candle).Start()

    def _process_candle(self, candle, rsi_value):
        if candle.State != CandleStates.Finished:
            return

        if self._rsi is None or not self._rsi.IsFormed:
            return

        step = 1.0
        if self.Security is not None and self.Security.PriceStep is not None:
            ps = float(self.Security.PriceStep)
            if ps > 0:
                step = ps

        tp_points = float(self.TakeProfitPoints)

        # Check active take-profit levels
        if self.Position > 0 and self._long_target > 0 and tp_points > 0:
            if float(candle.LowPrice) <= self._long_target:
                self.SellMarket()
                self._long_target = 0.0
        elif self.Position < 0 and self._short_target > 0 and tp_points > 0:
            if float(candle.HighPrice) >= self._short_target:
                self.BuyMarket()
                self._short_target = 0.0

        if self._cooldown > 0:
            self._cooldown -= 1
            return

        diff = float(rsi_value) - 50.0
        if diff == 0:
            return

        abs_diff = abs(diff)
        volume = abs_diff * float(self.VolumePerPoint)
        if volume <= 0:
            return

        close_price = float(candle.ClosePrice)

        if diff > 0 and self.Position <= 0:
            # RSI above 50: countertrend short
            self.SellMarket()
            self._short_target = close_price - step * tp_points if tp_points > 0 else 0.0
            self._long_target = 0.0
            self._cooldown = self.CooldownBars
        elif diff < 0 and self.Position >= 0:
            # RSI below 50: countertrend long
            self.BuyMarket()
            self._long_target = close_price + step * tp_points if tp_points > 0 else 0.0
            self._short_target = 0.0
            self._cooldown = self.CooldownBars

    def CreateClone(self):
        return osf_countertrend_strategy()
