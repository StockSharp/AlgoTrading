import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class three_down_three_up_strategy(Strategy):
    """3 Down, 3 Up Strategy."""

    def __init__(self):
        super(three_down_three_up_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._buy_trigger = self.Param("BuyTrigger", 3) \
            .SetDisplay("Buy Trigger", "Consecutive down closes for entry", "Trading")
        self._sell_trigger = self.Param("SellTrigger", 3) \
            .SetDisplay("Sell Trigger", "Consecutive up closes for exit", "Trading")
        self._ema_length = self.Param("EmaLength", 50) \
            .SetDisplay("EMA Length", "EMA trend filter period", "Indicators")
        self._cooldown_bars = self.Param("CooldownBars", 12) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "Risk")

        self._ema = None
        self._up_count = 0
        self._down_count = 0
        self._prev_close = 0.0
        self._has_prev_close = False
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(three_down_three_up_strategy, self).OnReseted()
        self._ema = None
        self._up_count = 0
        self._down_count = 0
        self._prev_close = 0.0
        self._has_prev_close = False
        self._cooldown_remaining = 0

    def OnStarted2(self, time):
        super(three_down_three_up_strategy, self).OnStarted2(time)

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

        close = float(candle.ClosePrice)

        if self._has_prev_close:
            if close > self._prev_close:
                self._up_count += 1
                self._down_count = 0
            elif close < self._prev_close:
                self._down_count += 1
                self._up_count = 0
            else:
                self._up_count = 0
                self._down_count = 0

        self._prev_close = close
        self._has_prev_close = True

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            return

        ema_v = float(ema_val)
        buy_trig = int(self._buy_trigger.Value)
        sell_trig = int(self._sell_trigger.Value)
        cooldown = int(self._cooldown_bars.Value)

        if self._down_count >= buy_trig and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket(Math.Abs(self.Position))
            self.BuyMarket(self.Volume)
            self._up_count = 0
            self._cooldown_remaining = cooldown
        elif self._up_count >= sell_trig and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket(Math.Abs(self.Position))
            self.SellMarket(self.Volume)
            self._down_count = 0
            self._cooldown_remaining = cooldown
        elif self.Position > 0 and close < ema_v and self._up_count >= 2:
            self.SellMarket(Math.Abs(self.Position))
            self._cooldown_remaining = cooldown
        elif self.Position < 0 and close > ema_v and self._down_count >= 2:
            self.BuyMarket(Math.Abs(self.Position))
            self._cooldown_remaining = cooldown

    def CreateClone(self):
        return three_down_three_up_strategy()
