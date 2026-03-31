import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import BollingerBands
from StockSharp.Algo.Strategies import Strategy


class exceeded_candle_strategy(Strategy):
    """Exceeded Candle Strategy: trades on candle engulfing patterns with BB filter.
    Buys when bullish engulfing below BB middle, sells on bearish engulfing above middle.
    Exits at BB upper/lower bands."""

    def __init__(self):
        super(exceeded_candle_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))) \
            .SetDisplay("Candle type", "Candle type for strategy calculation.", "General")
        self._bb_length = self.Param("BBLength", 20) \
            .SetDisplay("BB Period", "Bollinger Bands period", "Bollinger Bands")
        self._bb_multiplier = self.Param("BBMultiplier", 1.5) \
            .SetDisplay("BB StdDev", "Bollinger Bands standard deviation multiplier", "Bollinger Bands")
        self._cooldown_bars = self.Param("CooldownBars", 10) \
            .SetDisplay("Cooldown Bars", "Bars to wait between trades", "Risk")

        self._bollinger = None
        self._prev_candle = None
        self._prev_prev_candle = None
        self._prev_prev_prev_candle = None
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(exceeded_candle_strategy, self).OnReseted()
        self._bollinger = None
        self._prev_candle = None
        self._prev_prev_candle = None
        self._prev_prev_prev_candle = None
        self._cooldown_remaining = 0

    def OnStarted2(self, time):
        super(exceeded_candle_strategy, self).OnStarted2(time)

        self._bollinger = BollingerBands()
        self._bollinger.Length = int(self._bb_length.Value)
        self._bollinger.Width = float(self._bb_multiplier.Value)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(self._bollinger, self._on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._bollinger)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, bb_value):
        if candle.State != CandleStates.Finished:
            return

        if not self._bollinger.IsFormed:
            self._update_history(candle)
            return

        if bb_value.UpBand is None or bb_value.LowBand is None or bb_value.MovingAverage is None:
            self._update_history(candle)
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            self._update_history(candle)
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            self._update_history(candle)
            return

        upper_band = float(bb_value.UpBand)
        lower_band = float(bb_value.LowBand)
        middle_band = float(bb_value.MovingAverage)
        close = float(candle.ClosePrice)
        opn = float(candle.OpenPrice)
        cooldown = int(self._cooldown_bars.Value)

        green_exceeded = False
        red_exceeded = False

        if self._prev_candle is not None:
            prev_close = self._prev_candle[0]
            prev_open = self._prev_candle[1]
            green_exceeded = prev_close < prev_open and close > opn and close > prev_open
            red_exceeded = prev_close > prev_open and close < opn and close < prev_open

        last_3_red = False
        if (self._prev_candle is not None and self._prev_prev_candle is not None
                and self._prev_prev_prev_candle is not None):
            last_3_red = (self._prev_candle[0] < self._prev_candle[1] and
                          self._prev_prev_candle[0] < self._prev_prev_candle[1] and
                          self._prev_prev_prev_candle[0] < self._prev_prev_prev_candle[1])

        if green_exceeded and close < middle_band and not last_3_red and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket(Math.Abs(self.Position))
            self.BuyMarket(self.Volume)
            self._cooldown_remaining = cooldown
        elif red_exceeded and close > middle_band and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket(Math.Abs(self.Position))
            self.SellMarket(self.Volume)
            self._cooldown_remaining = cooldown
        elif self.Position > 0 and close >= upper_band:
            self.SellMarket(Math.Abs(self.Position))
            self._cooldown_remaining = cooldown
        elif self.Position < 0 and close <= lower_band:
            self.BuyMarket(Math.Abs(self.Position))
            self._cooldown_remaining = cooldown

        self._update_history(candle)

    def _update_history(self, candle):
        self._prev_prev_prev_candle = self._prev_prev_candle
        self._prev_prev_candle = self._prev_candle
        self._prev_candle = (float(candle.ClosePrice), float(candle.OpenPrice))

    def CreateClone(self):
        return exceeded_candle_strategy()
