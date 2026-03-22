import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class averaging_down_strategy(Strategy):
    """Averaging Down Strategy."""

    def __init__(self):
        super(averaging_down_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._ema_length = self.Param("EmaLength", 50) \
            .SetDisplay("EMA Length", "EMA period", "Indicators")
        self._atr_length = self.Param("AtrLength", 14) \
            .SetDisplay("ATR Length", "ATR period", "Indicators")
        self._atr_multiplier = self.Param("AtrMultiplier", 2.0) \
            .SetDisplay("ATR Multiplier", "ATR band multiplier", "Indicators")
        self._tp_percent = self.Param("TpPercent", 2.0) \
            .SetDisplay("TP %", "Take profit percent", "Trading")
        self._cooldown_bars = self.Param("CooldownBars", 10) \
            .SetDisplay("Cooldown Bars", "Bars to wait between trades", "Risk")

        self._ema = None
        self._atr = None
        self._entry_price = 0.0
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(averaging_down_strategy, self).OnReseted()
        self._ema = None
        self._atr = None
        self._entry_price = 0.0
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(averaging_down_strategy, self).OnStarted(time)

        self._ema = ExponentialMovingAverage()
        self._ema.Length = int(self._ema_length.Value)

        self._atr = AverageTrueRange()
        self._atr.Length = int(self._atr_length.Value)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._ema, self._atr, self._on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._ema)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, ema_val, atr_val):
        if candle.State != CandleStates.Finished:
            return

        if not self._ema.IsFormed or not self._atr.IsFormed:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            return

        price = float(candle.ClosePrice)
        ema_v = float(ema_val)
        atr_v = float(atr_val)
        multiplier = float(self._atr_multiplier.Value)
        upper_band = ema_v + atr_v * multiplier
        lower_band = ema_v - atr_v * multiplier
        tp_pct = float(self._tp_percent.Value)
        cooldown = int(self._cooldown_bars.Value)

        if price > upper_band and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket(Math.Abs(self.Position))
            self.BuyMarket(self.Volume)
            self._entry_price = price
            self._cooldown_remaining = cooldown
        elif price < lower_band and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket(Math.Abs(self.Position))
            self.SellMarket(self.Volume)
            self._entry_price = price
            self._cooldown_remaining = cooldown
        elif self.Position > 0 and self._entry_price > 0 and price >= self._entry_price * (1 + tp_pct / 100.0):
            self.SellMarket(Math.Abs(self.Position))
            self._entry_price = 0.0
            self._cooldown_remaining = cooldown
        elif self.Position < 0 and self._entry_price > 0 and price <= self._entry_price * (1 - tp_pct / 100.0):
            self.BuyMarket(Math.Abs(self.Position))
            self._entry_price = 0.0
            self._cooldown_remaining = cooldown
        elif self.Position > 0 and price < ema_v:
            self.SellMarket(Math.Abs(self.Position))
            self._entry_price = 0.0
            self._cooldown_remaining = cooldown
        elif self.Position < 0 and price > ema_v:
            self.BuyMarket(Math.Abs(self.Position))
            self._entry_price = 0.0
            self._cooldown_remaining = cooldown

    def CreateClone(self):
        return averaging_down_strategy()
