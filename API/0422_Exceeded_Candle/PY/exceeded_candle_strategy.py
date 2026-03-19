import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import BollingerBands
from StockSharp.Algo.Strategies import Strategy

class exceeded_candle_strategy(Strategy):
    """
    Exceeded Candle Strategy: trades on candle engulfing patterns with BB filter.
    Buys when bullish engulfing below BB middle, sells on bearish engulfing above middle.
    Exits at BB upper/lower bands.
    """

    def __init__(self):
        super(exceeded_candle_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))) \
            .SetDisplay("Candle type", "Candle type for strategy", "General")
        self._bb_length = self.Param("BBLength", 20) \
            .SetDisplay("BB Period", "Bollinger Bands period", "Bollinger Bands")
        self._bb_multiplier = self.Param("BBMultiplier", 1.5) \
            .SetDisplay("BB StdDev", "Bollinger Bands std dev multiplier", "Bollinger Bands")
        self._cooldown_bars = self.Param("CooldownBars", 10) \
            .SetDisplay("Cooldown Bars", "Bars to wait between trades", "Risk")

        self._prev_candle = None
        self._prev_prev_candle = None
        self._prev_prev_prev_candle = None
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(exceeded_candle_strategy, self).OnReseted()
        self._prev_candle = None
        self._prev_prev_candle = None
        self._prev_prev_prev_candle = None
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(exceeded_candle_strategy, self).OnStarted(time)

        bb = BollingerBands()
        bb.Length = self._bb_length.Value
        bb.Width = self._bb_multiplier.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(bb, self._on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, bb)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, bb_value):
        if candle.State != CandleStates.Finished:
            return

        if bb_value.IsEmpty:
            self._update_history(candle)
            return

        upper_band = bb_value.UpBand
        lower_band = bb_value.LowBand
        middle_band = bb_value.MovingAverage

        if upper_band is None or lower_band is None or middle_band is None:
            self._update_history(candle)
            return

        upper_band = float(upper_band)
        lower_band = float(lower_band)
        middle_band = float(middle_band)

        if not self.IsFormedAndOnlineAndAllowTrading():
            self._update_history(candle)
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            self._update_history(candle)
            return

        close = float(candle.ClosePrice)
        open_p = float(candle.OpenPrice)

        green_exceeded = False
        red_exceeded = False

        if self._prev_candle is not None:
            prev_close = float(self._prev_candle["close"])
            prev_open = float(self._prev_candle["open"])
            green_exceeded = prev_close < prev_open and close > open_p and close > prev_open
            red_exceeded = prev_close > prev_open and close < open_p and close < prev_open

        last_3_red = False
        if (self._prev_candle is not None and self._prev_prev_candle is not None
                and self._prev_prev_prev_candle is not None):
            last_3_red = (self._prev_candle["close"] < self._prev_candle["open"] and
                          self._prev_prev_candle["close"] < self._prev_prev_candle["open"] and
                          self._prev_prev_prev_candle["close"] < self._prev_prev_prev_candle["open"])

        if green_exceeded and close < middle_band and not last_3_red and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._cooldown_remaining = self._cooldown_bars.Value
        elif red_exceeded and close > middle_band and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._cooldown_remaining = self._cooldown_bars.Value
        elif self.Position > 0 and close >= upper_band:
            self.SellMarket()
            self._cooldown_remaining = self._cooldown_bars.Value
        elif self.Position < 0 and close <= lower_band:
            self.BuyMarket()
            self._cooldown_remaining = self._cooldown_bars.Value

        self._update_history(candle)

    def _update_history(self, candle):
        self._prev_prev_prev_candle = self._prev_prev_candle
        self._prev_prev_candle = self._prev_candle
        self._prev_candle = {
            "open": float(candle.OpenPrice),
            "close": float(candle.ClosePrice)
        }

    def CreateClone(self):
        return exceeded_candle_strategy()
