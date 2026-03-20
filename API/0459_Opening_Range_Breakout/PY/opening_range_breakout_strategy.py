import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import BollingerBands, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class opening_range_breakout_strategy(Strategy):
    def __init__(self):
        super(opening_range_breakout_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._bb_length = self.Param("BbLength", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("BB Length", "Bollinger Bands period", "Indicators")
        self._ema_length = self.Param("EmaLength", 50) \
            .SetGreaterThanZero() \
            .SetDisplay("EMA Length", "EMA trend filter period", "Indicators")
        self._cooldown_bars = self.Param("CooldownBars", 10) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "Risk")
        self._cooldown_remaining = 0
        self._entry_price = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value
    @property
    def bb_length(self):
        return self._bb_length.Value
    @property
    def ema_length(self):
        return self._ema_length.Value
    @property
    def cooldown_bars(self):
        return self._cooldown_bars.Value

    def OnReseted(self):
        super(opening_range_breakout_strategy, self).OnReseted()
        self._cooldown_remaining = 0
        self._entry_price = 0.0

    def OnStarted(self, time):
        super(opening_range_breakout_strategy, self).OnStarted(time)
        self._bb = BollingerBands()
        self._bb.Length = self.bb_length
        self._bb.Width = 2.0
        self._ema = ExponentialMovingAverage()
        self._ema.Length = self.ema_length

        subscription = self.SubscribeCandles(self.candle_type)
        subscription \
            .BindEx(self._bb, self._ema, self.OnProcess) \
            .Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._bb)
            self.DrawIndicator(area, self._ema)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, bb_value, ema_value):
        if candle.State != CandleStates.Finished:
            return
        if not self._bb.IsFormed or not self._ema.IsFormed:
            return
        if bb_value.IsEmpty or ema_value.IsEmpty:
            return

        upper = bb_value.UpBand
        lower = bb_value.LowBand
        mid = bb_value.MovingAverage
        if upper is None or lower is None or mid is None:
            return

        upper = float(upper)
        lower = float(lower)
        mid = float(mid)
        ema_val = float(ema_value)

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            return

        price = float(candle.ClosePrice)

        if price > upper and price > ema_val and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket(abs(self.Position))
            self.BuyMarket()
            self._entry_price = price
            self._cooldown_remaining = self.cooldown_bars
        elif price < lower and price < ema_val and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket(abs(self.Position))
            self.SellMarket()
            self._entry_price = price
            self._cooldown_remaining = self.cooldown_bars
        elif self.Position > 0 and price < mid:
            self.SellMarket(abs(self.Position))
            self._entry_price = 0.0
            self._cooldown_remaining = self.cooldown_bars
        elif self.Position < 0 and price > mid:
            self.BuyMarket(abs(self.Position))
            self._entry_price = 0.0
            self._cooldown_remaining = self.cooldown_bars

    def CreateClone(self):
        return opening_range_breakout_strategy()
