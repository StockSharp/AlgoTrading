import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class two_direction_martin_stylized_strategy(Strategy):
    def __init__(self):
        super(two_direction_martin_stylized_strategy, self).__init__()
        self._take_profit_percent = self.Param("TakeProfitPercent", 0.35) \
            .SetDisplay("Take Profit %", "Take profit as percent of price", "General")
        self._max_steps = self.Param("MaxSteps", 3) \
            .SetDisplay("Max Steps", "Maximum martingale doublings", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._cooldown_bars = self.Param("CooldownBars", 4) \
            .SetDisplay("Cooldown Bars", "Completed candles to wait after a full cycle", "General")
        self._entry_price = 0.0
        self._step_count = 0
        self._direction = 0
        self._cooldown_remaining = 0

    @property
    def take_profit_percent(self):
        return self._take_profit_percent.Value
    @property
    def max_steps(self):
        return self._max_steps.Value
    @property
    def candle_type(self):
        return self._candle_type.Value
    @property
    def cooldown_bars(self):
        return self._cooldown_bars.Value

    def OnReseted(self):
        super(two_direction_martin_stylized_strategy, self).OnReseted()
        self._entry_price = 0.0
        self._step_count = 0
        self._direction = 0
        self._cooldown_remaining = 0

    def OnStarted2(self, time):
        super(two_direction_martin_stylized_strategy, self).OnStarted2(time)
        ema = ExponentialMovingAverage()
        ema.Length = 20
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ema, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, ema_value):
        if candle.State != CandleStates.Finished:
            return
        ema_value = float(ema_value)
        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
        price = float(candle.ClosePrice)
        if self.Position == 0:
            if self._cooldown_remaining > 0:
                return
            if price > ema_value:
                self.BuyMarket()
                self._direction = 1
            else:
                self.SellMarket()
                self._direction = -1
            self._entry_price = price
            self._step_count = 0
            return
        tp_pct = float(self.take_profit_percent)
        tp = self._entry_price * tp_pct / 100.0
        if self._direction == 1 and price >= self._entry_price + tp:
            while self.Position > 0:
                self.SellMarket()
            self._step_count = 0
            self._cooldown_remaining = self.cooldown_bars
        elif self._direction == -1 and price <= self._entry_price - tp:
            while self.Position < 0:
                self.BuyMarket()
            self._step_count = 0
            self._cooldown_remaining = self.cooldown_bars
        elif self._direction == 1 and price <= self._entry_price - tp and self._step_count < self.max_steps:
            self.BuyMarket()
            self._entry_price = (self._entry_price + price) / 2.0
            self._step_count += 1
        elif self._direction == -1 and price >= self._entry_price + tp and self._step_count < self.max_steps:
            self.SellMarket()
            self._entry_price = (self._entry_price + price) / 2.0
            self._step_count += 1
        elif self._step_count >= self.max_steps:
            if self.Position > 0:
                self.SellMarket()
            elif self.Position < 0:
                self.BuyMarket()
            self._step_count = 0
            self._cooldown_remaining = self.cooldown_bars

    def CreateClone(self):
        return two_direction_martin_stylized_strategy()
