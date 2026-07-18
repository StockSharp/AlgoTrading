import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class candle245_breakout_strategy(Strategy):
    """Candle 245 Breakout Strategy."""

    def __init__(self):
        super(candle245_breakout_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._ref_period = self.Param("RefPeriod", 10) \
            .SetDisplay("Ref Period", "Every N bars capture reference candle", "Trading")
        self._look_forward_bars = self.Param("LookForwardBars", 3) \
            .SetDisplay("Look Forward Bars", "Bars to watch for breakout", "Trading")
        self._ema_length = self.Param("EmaLength", 20) \
            .SetDisplay("EMA Length", "EMA period for trend filter", "Indicators")
        self._cooldown_bars = self.Param("CooldownBars", 10) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "Risk")

        self._ema = None
        self._ref_high = 0.0
        self._ref_low = 0.0
        self._bars_left = 0
        self._bar_count = 0
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(candle245_breakout_strategy, self).OnReseted()
        self._ema = None
        self._ref_high = 0.0
        self._ref_low = 0.0
        self._bars_left = 0
        self._bar_count = 0
        self._cooldown_remaining = 0

    def OnStarted2(self, time):
        super(candle245_breakout_strategy, self).OnStarted2(time)

        self._ema = ExponentialMovingAverage()
        self._ema.Length = int(self._ema_length.Value)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._ema, self._on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._ema)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, ema_val):
        if candle.State != CandleStates.Finished:
            return

        if not self._ema.IsFormed:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        self._bar_count += 1
        ref_period = int(self._ref_period.Value)
        look_fwd = int(self._look_forward_bars.Value)
        cooldown = int(self._cooldown_bars.Value)

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            if self._bars_left > 0:
                self._bars_left -= 1
            return

        if self._bar_count % ref_period == 0:
            self._ref_high = float(candle.HighPrice)
            self._ref_low = float(candle.LowPrice)
            self._bars_left = look_fwd
            return

        if self._bars_left <= 0:
            return

        self._bars_left -= 1

        price = float(candle.ClosePrice)
        ema_v = float(ema_val)

        if price > self._ref_high and price > ema_v and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket(Math.Abs(self.Position))
            self.BuyMarket(self.Volume)
            self._cooldown_remaining = cooldown
        elif price < self._ref_low and price < ema_v and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket(Math.Abs(self.Position))
            self.SellMarket(self.Volume)
            self._cooldown_remaining = cooldown

        if self._bars_left == 0 and self.Position != 0:
            if self.Position > 0:
                self.SellMarket(Math.Abs(self.Position))
            else:
                self.BuyMarket(Math.Abs(self.Position))
            self._cooldown_remaining = cooldown

    def CreateClone(self):
        return candle245_breakout_strategy()
