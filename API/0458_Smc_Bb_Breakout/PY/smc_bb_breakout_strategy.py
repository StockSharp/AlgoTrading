import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import BollingerBands
from StockSharp.Algo.Strategies import Strategy


class smc_bb_breakout_strategy(Strategy):
    def __init__(self):
        super(smc_bb_breakout_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._bb_length = self.Param("BbLength", 34) \
            .SetGreaterThanZero() \
            .SetDisplay("BB Length", "Bollinger Bands period", "Bollinger")
        self._bb_width = self.Param("BbWidth", 2.0) \
            .SetDisplay("BB Width", "Bollinger width multiplier", "Bollinger")
        self._momentum_body_percent = self.Param("MomentumBodyPercent", 0.5) \
            .SetDisplay("Momentum Body %", "Minimum body vs range ratio", "Momentum")
        self._cooldown_bars = self.Param("CooldownBars", 10) \
            .SetDisplay("Cooldown Bars", "Bars to wait between trades", "Risk")
        self._prev_close = 0.0
        self._prev_high = 0.0
        self._prev_low = 0.0
        self._has_prev = False
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value
    @property
    def bb_length(self):
        return self._bb_length.Value
    @property
    def bb_width(self):
        return self._bb_width.Value
    @property
    def momentum_body_percent(self):
        return self._momentum_body_percent.Value
    @property
    def cooldown_bars(self):
        return self._cooldown_bars.Value

    def OnReseted(self):
        super(smc_bb_breakout_strategy, self).OnReseted()
        self._prev_close = 0.0
        self._prev_high = 0.0
        self._prev_low = 0.0
        self._has_prev = False
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(smc_bb_breakout_strategy, self).OnStarted(time)
        self._bb = BollingerBands()
        self._bb.Length = self.bb_length
        self._bb.Width = self.bb_width

        subscription = self.SubscribeCandles(self.candle_type)
        subscription \
            .BindEx(self._bb, self.OnProcess) \
            .Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._bb)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, bb_value):
        if candle.State != CandleStates.Finished:
            return
        if not self._bb.IsFormed:
            return
        if bb_value.IsEmpty:
            return

        upper = bb_value.UpBand
        lower = bb_value.LowBand
        mid = bb_value.MovingAverage
        if upper is None or lower is None or mid is None:
            return

        upper = float(upper)
        lower = float(lower)
        mid = float(mid)

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            self._prev_close = float(candle.ClosePrice)
            self._prev_high = float(candle.HighPrice)
            self._prev_low = float(candle.LowPrice)
            self._has_prev = True
            return

        price = float(candle.ClosePrice)
        rng = float(candle.HighPrice) - float(candle.LowPrice)
        body = abs(float(candle.ClosePrice) - float(candle.OpenPrice))
        body_ratio = body / rng if rng > 0 else 0.0

        is_bullish_momentum = body_ratio >= float(self.momentum_body_percent) and float(candle.ClosePrice) > float(candle.OpenPrice)
        is_bearish_momentum = body_ratio >= float(self.momentum_body_percent) and float(candle.ClosePrice) < float(candle.OpenPrice)

        break_higher = self._has_prev and float(candle.HighPrice) > self._prev_high
        break_lower = self._has_prev and float(candle.LowPrice) < self._prev_low

        if price > upper and is_bullish_momentum and break_higher and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket(abs(self.Position))
            self.BuyMarket()
            self._cooldown_remaining = self.cooldown_bars
        elif price < lower and is_bearish_momentum and break_lower and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket(abs(self.Position))
            self.SellMarket()
            self._cooldown_remaining = self.cooldown_bars
        elif self.Position > 0 and price < mid:
            self.SellMarket(abs(self.Position))
            self._cooldown_remaining = self.cooldown_bars
        elif self.Position < 0 and price > mid:
            self.BuyMarket(abs(self.Position))
            self._cooldown_remaining = self.cooldown_bars

        self._prev_close = float(candle.ClosePrice)
        self._prev_high = float(candle.HighPrice)
        self._prev_low = float(candle.LowPrice)
        self._has_prev = True

    def CreateClone(self):
        return smc_bb_breakout_strategy()
