import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

import math
from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Highest, Lowest, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy


class fisher_cyber_cycle_strategy(Strategy):

    def __init__(self):
        super(fisher_cyber_cycle_strategy, self).__init__()

        self._alpha = self.Param("Alpha", 0.07) \
            .SetDisplay("Alpha", "Smoothing factor", "Indicators")
        self._length = self.Param("Length", 8) \
            .SetDisplay("Length", "Normalization window", "Indicators")
        self._cooldown_bars = self.Param("CooldownBars", 1) \
            .SetDisplay("Cooldown Bars", "Bars to wait after a completed trade", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(8))) \
            .SetDisplay("Candle Type", "Type of candles", "General")

        self._prev_fisher = 0.0
        self._prev_trigger = 0.0
        self._initialized = False
        self._prev_fish = 0.0
        self._count = 0
        self._bars_since_trade = 0

        self._price = [0.0, 0.0, 0.0, 0.0]
        self._smooth = [0.0, 0.0, 0.0, 0.0]
        self._cycle = [0.0, 0.0, 0.0]

        self._highest = None
        self._lowest = None

    @property
    def Alpha(self):
        return self._alpha.Value

    @Alpha.setter
    def Alpha(self, value):
        self._alpha.Value = value

    @property
    def Length(self):
        return self._length.Value

    @Length.setter
    def Length(self, value):
        self._length.Value = value

    @property
    def CooldownBars(self):
        return self._cooldown_bars.Value

    @CooldownBars.setter
    def CooldownBars(self, value):
        self._cooldown_bars.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted2(self, time):
        super(fisher_cyber_cycle_strategy, self).OnStarted2(time)

        self._prev_fisher = 0.0
        self._prev_trigger = 0.0
        self._initialized = False
        self._prev_fish = 0.0
        self._count = 0
        self._bars_since_trade = self.CooldownBars
        self._price = [0.0, 0.0, 0.0, 0.0]
        self._smooth = [0.0, 0.0, 0.0, 0.0]
        self._cycle = [0.0, 0.0, 0.0]

        self._highest = Highest()
        self._highest.Length = self.Length
        self._lowest = Lowest()
        self._lowest.Length = self.Length

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.ProcessCandle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self._bars_since_trade < self.CooldownBars:
            self._bars_since_trade += 1

        price = (float(candle.HighPrice) + float(candle.LowPrice)) / 2.0
        t = candle.OpenTime
        alpha = float(self.Alpha)

        self._price[3] = self._price[2]
        self._price[2] = self._price[1]
        self._price[1] = self._price[0]
        self._price[0] = price

        self._smooth[3] = self._smooth[2]
        self._smooth[2] = self._smooth[1]
        self._smooth[1] = self._smooth[0]
        self._smooth[0] = (self._price[0] + 2.0 * self._price[1] + 2.0 * self._price[2] + self._price[3]) / 6.0

        self._cycle[2] = self._cycle[1]
        self._cycle[1] = self._cycle[0]

        if self._count < 3:
            self._cycle[0] = (self._price[0] + 2.0 * self._price[1] + self._price[2]) / 4.0
        else:
            k0 = (1.0 - 0.5 * alpha) ** 2
            k1 = 2.0
            k2 = 2.0 * (1.0 - alpha)
            k3 = (1.0 - alpha) ** 2
            self._cycle[0] = k0 * (self._smooth[0] - k1 * self._smooth[1] + self._smooth[2]) + k2 * self._cycle[1] - k3 * self._cycle[2]

        self._count += 1

        hi = DecimalIndicatorValue(self._highest, self._cycle[0], t)
        hi.IsFinal = True
        hh_result = self._highest.Process(hi)
        li = DecimalIndicatorValue(self._lowest, self._cycle[0], t)
        li.IsFinal = True
        ll_result = self._lowest.Process(li)

        if not self._highest.IsFormed or not self._lowest.IsFormed:
            return

        hh = float(hh_result)
        ll = float(ll_result)

        if hh != ll:
            value1 = (self._cycle[0] - ll) / (hh - ll)
        else:
            value1 = 0.0

        normalized = 1.98 * (value1 - 0.5)
        if normalized >= 0.999:
            normalized = 0.999
        if normalized <= -0.999:
            normalized = -0.999

        fish = 0.5 * math.log((1.0 + normalized) / (1.0 - normalized))
        trigger = self._prev_fish
        self._prev_fish = fish

        if not self._initialized:
            self._prev_fisher = fish
            self._prev_trigger = trigger
            self._initialized = True
            return

        cross_up = self._prev_fisher <= self._prev_trigger and fish > trigger
        cross_down = self._prev_fisher >= self._prev_trigger and fish < trigger

        if self._bars_since_trade >= self.CooldownBars:
            pos = self.Position
            if cross_up and pos <= 0:
                self.BuyMarket(self.Volume + abs(pos))
                self._bars_since_trade = 0
            elif cross_down and pos >= 0:
                self.SellMarket(self.Volume + abs(pos))
                self._bars_since_trade = 0

        self._prev_fisher = fish
        self._prev_trigger = trigger

    def OnReseted(self):
        super(fisher_cyber_cycle_strategy, self).OnReseted()
        self._prev_fisher = 0.0
        self._prev_trigger = 0.0
        self._initialized = False
        self._prev_fish = 0.0
        self._count = 0
        self._bars_since_trade = self.CooldownBars
        self._price = [0.0, 0.0, 0.0, 0.0]
        self._smooth = [0.0, 0.0, 0.0, 0.0]
        self._cycle = [0.0, 0.0, 0.0]

    def CreateClone(self):
        return fisher_cyber_cycle_strategy()
