import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class averaging_down_strategy(Strategy):
    def __init__(self):
        super(averaging_down_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._ema_length = self.Param("EmaLength", 50) \
            .SetGreaterThanZero() \
            .SetDisplay("EMA Length", "EMA period", "Indicators")
        self._atr_length = self.Param("AtrLength", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR Length", "ATR period", "Indicators")
        self._atr_multiplier = self.Param("AtrMultiplier", 2.0) \
            .SetDisplay("ATR Multiplier", "ATR band multiplier", "Indicators")
        self._tp_percent = self.Param("TpPercent", 2.0) \
            .SetDisplay("TP %", "Take profit percent", "Trading")
        self._cooldown_bars = self.Param("CooldownBars", 10) \
            .SetDisplay("Cooldown Bars", "Bars to wait between trades", "Risk")
        self._entry_price = 0.0
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value
    @property
    def ema_length(self):
        return self._ema_length.Value
    @property
    def atr_length(self):
        return self._atr_length.Value
    @property
    def atr_multiplier(self):
        return self._atr_multiplier.Value
    @property
    def tp_percent(self):
        return self._tp_percent.Value
    @property
    def cooldown_bars(self):
        return self._cooldown_bars.Value

    def OnReseted(self):
        super(averaging_down_strategy, self).OnReseted()
        self._entry_price = 0.0
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(averaging_down_strategy, self).OnStarted(time)
        self._ema = ExponentialMovingAverage()
        self._ema.Length = self.ema_length
        self._atr = AverageTrueRange()
        self._atr.Length = self.atr_length

        subscription = self.SubscribeCandles(self.candle_type)
        subscription \
            .Bind(self._ema, self._atr, self.OnProcess) \
            .Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._ema)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, ema_val, atr_val):
        if candle.State != CandleStates.Finished:
            return
        if not self._ema.IsFormed or not self._atr.IsFormed:
            return
        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            return

        price = float(candle.ClosePrice)
        ema_v = float(ema_val)
        atr_v = float(atr_val)
        upper_band = ema_v + atr_v * float(self.atr_multiplier)
        lower_band = ema_v - atr_v * float(self.atr_multiplier)

        if price > upper_band and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket(abs(self.Position))
            self.BuyMarket()
            self._entry_price = price
            self._cooldown_remaining = self.cooldown_bars
        elif price < lower_band and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket(abs(self.Position))
            self.SellMarket()
            self._entry_price = price
            self._cooldown_remaining = self.cooldown_bars
        elif self.Position > 0 and self._entry_price > 0 and price >= self._entry_price * (1 + float(self.tp_percent) / 100.0):
            self.SellMarket(abs(self.Position))
            self._entry_price = 0.0
            self._cooldown_remaining = self.cooldown_bars
        elif self.Position < 0 and self._entry_price > 0 and price <= self._entry_price * (1 - float(self.tp_percent) / 100.0):
            self.BuyMarket(abs(self.Position))
            self._entry_price = 0.0
            self._cooldown_remaining = self.cooldown_bars
        elif self.Position > 0 and price < ema_v:
            self.SellMarket(abs(self.Position))
            self._entry_price = 0.0
            self._cooldown_remaining = self.cooldown_bars
        elif self.Position < 0 and price > ema_v:
            self.BuyMarket(abs(self.Position))
            self._entry_price = 0.0
            self._cooldown_remaining = self.cooldown_bars

    def CreateClone(self):
        return averaging_down_strategy()
