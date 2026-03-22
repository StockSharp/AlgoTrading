import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import BollingerBands, RelativeStrengthIndex, ExponentialMovingAverage, IndicatorHelper
from StockSharp.Algo.Strategies import Strategy

import sys


class ttm_squeeze_strategy(Strategy):
    """TTM Squeeze Strategy."""

    def __init__(self):
        super(ttm_squeeze_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._bb_length = self.Param("BbLength", 20) \
            .SetDisplay("BB Length", "Bollinger Bands period", "Indicators")
        self._rsi_length = self.Param("RsiLength", 14) \
            .SetDisplay("RSI Length", "RSI period", "Indicators")
        self._cooldown_bars = self.Param("CooldownBars", 15) \
            .SetDisplay("Cooldown Bars", "Bars to wait between trades", "Risk")

        self._bb = None
        self._rsi = None
        self._ema = None
        self._prev_bb_width = 0.0
        self._min_bb_width = float(sys.maxsize)
        self._narrow_bars = 0
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(ttm_squeeze_strategy, self).OnReseted()
        self._bb = None
        self._rsi = None
        self._ema = None
        self._prev_bb_width = 0.0
        self._min_bb_width = float(sys.maxsize)
        self._narrow_bars = 0
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(ttm_squeeze_strategy, self).OnStarted(time)

        bb_len = int(self._bb_length.Value)

        self._bb = BollingerBands()
        self._bb.Length = bb_len
        self._bb.Width = 2.0

        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = int(self._rsi_length.Value)

        self._ema = ExponentialMovingAverage()
        self._ema.Length = bb_len

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(self._bb, self._rsi, self._ema, self._on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._bb)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, bb_value, rsi_value, ema_value):
        if candle.State != CandleStates.Finished:
            return

        if not self._bb.IsFormed or not self._rsi.IsFormed or not self._ema.IsFormed:
            return

        if bb_value.IsEmpty or rsi_value.IsEmpty or ema_value.IsEmpty:
            return

        if bb_value.UpBand is None or bb_value.LowBand is None or bb_value.MovingAverage is None:
            return

        upper = float(bb_value.UpBand)
        lower = float(bb_value.LowBand)
        mid = float(bb_value.MovingAverage)
        rsi_val = float(IndicatorHelper.ToDecimal(rsi_value))
        ema_val = float(IndicatorHelper.ToDecimal(ema_value))

        bb_width = (upper - lower) / mid * 100 if mid > 0 else 0.0

        if not self.IsFormedAndOnlineAndAllowTrading():
            self._prev_bb_width = bb_width
            self._min_bb_width = min(self._min_bb_width, bb_width)
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            self._prev_bb_width = bb_width
            self._min_bb_width = min(self._min_bb_width, bb_width)
            return

        if self._prev_bb_width == 0.0:
            self._prev_bb_width = bb_width
            self._min_bb_width = bb_width
            return

        close = float(candle.ClosePrice)
        cooldown = int(self._cooldown_bars.Value)

        if bb_width <= self._min_bb_width * 1.1:
            self._narrow_bars += 1
            self._min_bb_width = min(self._min_bb_width, bb_width)
        elif bb_width > self._prev_bb_width and self._narrow_bars >= 3:
            if rsi_val > 50 and close > ema_val and self.Position <= 0:
                if self.Position < 0:
                    self.BuyMarket(Math.Abs(self.Position))
                self.BuyMarket(self.Volume)
                self._cooldown_remaining = cooldown
                self._narrow_bars = 0
                self._min_bb_width = bb_width
            elif rsi_val < 50 and close < ema_val and self.Position >= 0:
                if self.Position > 0:
                    self.SellMarket(Math.Abs(self.Position))
                self.SellMarket(self.Volume)
                self._cooldown_remaining = cooldown
                self._narrow_bars = 0
                self._min_bb_width = bb_width
            else:
                self._narrow_bars = 0
                self._min_bb_width = bb_width
        else:
            self._narrow_bars = 0
            self._min_bb_width = bb_width

        if self.Position > 0 and close < lower:
            self.SellMarket(Math.Abs(self.Position))
            self._cooldown_remaining = cooldown
        elif self.Position < 0 and close > upper:
            self.BuyMarket(Math.Abs(self.Position))
            self._cooldown_remaining = cooldown

        self._prev_bb_width = bb_width

    def CreateClone(self):
        return ttm_squeeze_strategy()
