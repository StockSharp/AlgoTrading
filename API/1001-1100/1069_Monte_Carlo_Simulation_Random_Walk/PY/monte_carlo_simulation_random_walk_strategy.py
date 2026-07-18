import clr
import math

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class monte_carlo_simulation_random_walk_strategy(Strategy):
    def __init__(self):
        super(monte_carlo_simulation_random_walk_strategy, self).__init__()
        self._forecast_bars = self.Param("ForecastBars", 10) \
            .SetGreaterThanZero()
        self._simulations = self.Param("Simulations", 100) \
            .SetGreaterThanZero()
        self._data_length = self.Param("DataLength", 100) \
            .SetGreaterThanZero()
        self._min_forecast_edge_percent = self.Param("MinForecastEdgePercent", 0.5) \
            .SetGreaterThanZero()
        self._signal_cooldown_bars = self.Param("SignalCooldownBars", 12) \
            .SetGreaterThanZero()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15)))
        self._returns = []
        self._prev_close = 0.0
        self._bars_from_signal = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(monte_carlo_simulation_random_walk_strategy, self).OnReseted()
        self._returns = []
        self._prev_close = 0.0
        self._bars_from_signal = 0

    def OnStarted2(self, time):
        super(monte_carlo_simulation_random_walk_strategy, self).OnStarted2(time)
        self._returns = []
        self._prev_close = 0.0
        self._bars_from_signal = self._signal_cooldown_bars.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.OnProcess).Start()

    def OnProcess(self, candle):
        if candle.State != CandleStates.Finished:
            return
        close = float(candle.ClosePrice)
        if self._prev_close > 0.0:
            ret = math.log(close / self._prev_close)
            self._returns.append(ret)
            dl = self._data_length.Value
            if len(self._returns) > dl:
                self._returns.pop(0)
        self._prev_close = close
        if len(self._returns) < 20:
            return
        self._bars_from_signal += 1
        history = self._returns[:]
        n = len(history)
        avg = sum(history) / n
        variance = sum((r - avg) ** 2 for r in history) / n
        drift = avg - variance / 2.0
        seed = int(candle.OpenTime.Ticks) & 0x7FFFFFFF
        import random as rnd_mod
        rng = rnd_mod.Random(seed)
        total = 0.0
        sims = self._simulations.Value
        fb = self._forecast_bars.Value
        for sim in range(sims):
            price = close
            for step in range(fb):
                idx = rng.randint(0, n - 1)
                price *= math.exp(history[idx] + drift)
            total += price
        mean_forecast = total / sims
        edge_percent = (mean_forecast - close) / close * 100.0
        min_edge = float(self._min_forecast_edge_percent.Value)
        cd = self._signal_cooldown_bars.Value
        if self._bars_from_signal >= cd and edge_percent >= min_edge and self.Position <= 0:
            self.BuyMarket()
            self._bars_from_signal = 0
        elif self._bars_from_signal >= cd and edge_percent <= -min_edge and self.Position >= 0:
            self.SellMarket()
            self._bars_from_signal = 0

    def CreateClone(self):
        return monte_carlo_simulation_random_walk_strategy()
