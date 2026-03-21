import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class three_down_three_up_strategy(Strategy):
    """Mean reversion: buy after N consecutive down closes, sell after N up closes, with EMA filter."""

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

        self._up_count = 0
        self._down_count = 0
        self._prev_close = 0.0
        self._has_prev = False
        self._cooldown_remaining = 0

    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(three_down_three_up_strategy, self).OnReseted()
        self._up_count = 0
        self._down_count = 0
        self._prev_close = 0.0
        self._has_prev = False
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(three_down_three_up_strategy, self).OnStarted(time)

        ema = ExponentialMovingAverage()
        ema.Length = self._ema_length.Value

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(ema, self.OnProcess).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, ema_val):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        ema = float(ema_val)

        if self._has_prev:
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
        self._has_prev = True

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            return

        # Buy after consecutive down closes (mean reversion)
        if self._down_count >= self._buy_trigger.Value and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._up_count = 0
            self._cooldown_remaining = self._cooldown_bars.Value
        # Sell short after consecutive up closes
        elif self._up_count >= self._sell_trigger.Value and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._down_count = 0
            self._cooldown_remaining = self._cooldown_bars.Value
        # Exit long: price below EMA + up streak
        elif self.Position > 0 and close < ema and self._up_count >= 2:
            self.SellMarket()
            self._cooldown_remaining = self._cooldown_bars.Value
        # Exit short: price above EMA + down streak
        elif self.Position < 0 and close > ema and self._down_count >= 2:
            self.BuyMarket()
            self._cooldown_remaining = self._cooldown_bars.Value

    def CreateClone(self):
        return three_down_three_up_strategy()
