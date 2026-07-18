import clr
import math
import random

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange
from StockSharp.Algo.Strategies import Strategy

class monte_carlo_range_forecast_strategy(Strategy):
    """
    Monte Carlo simulation to forecast price range using ATR-based volatility.
    """

    def __init__(self):
        super(monte_carlo_range_forecast_strategy, self).__init__()
        self._forecast_period = self.Param("ForecastPeriod", 20).SetDisplay("Forecast Period", "Forecast horizon", "General")
        self._simulations = self.Param("Simulations", 100).SetDisplay("Simulations", "Number of MC sims", "General")
        self._min_edge_pct = self.Param("MinForecastEdgePercent", 0.25).SetDisplay("Min Edge %", "Min forecast edge", "General")
        self._cooldown_bars = self.Param("SignalCooldownBars", 10).SetDisplay("Cooldown", "Min bars between entries", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))).SetDisplay("Candle Type", "Candles", "General")

        self._bars_from_signal = 10

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(monte_carlo_range_forecast_strategy, self).OnReseted()
        self._bars_from_signal = self._cooldown_bars.Value

    def OnStarted2(self, time):
        super(monte_carlo_range_forecast_strategy, self).OnStarted2(time)
        atr = AverageTrueRange()
        atr.Length = 14
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(atr, self._process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, atr)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, atr_val):
        if candle.State != CandleStates.Finished:
            return
        current = float(candle.ClosePrice)
        atr = float(atr_val)
        if current <= 0 or atr <= 0:
            return
        self._bars_from_signal += 1
        step_vol = atr / current
        total = 0.0
        sims = self._simulations.Value
        fp = self._forecast_period.Value
        ticks = int(candle.OpenTime.Ticks)
        seed = ticks & 0xFFFFFFFF
        if seed > 0x7FFFFFFF:
            seed -= 0x100000000
        rng = random.Random(int(seed))
        for i in range(sims):
            price = current
            for j in range(fp):
                u1 = 1.0 - rng.random()
                u2 = 1.0 - rng.random()
                g = math.sqrt(-2.0 * math.log(u1)) * math.cos(2.0 * math.pi * u2)
                price += price * step_vol * g
            total += price
        mean = total / sims
        edge_pct = (mean - current) / current * 100.0
        min_edge = float(self._min_edge_pct.Value)
        if self._bars_from_signal >= self._cooldown_bars.Value and edge_pct >= min_edge and self.Position <= 0:
            self.BuyMarket()
            self._bars_from_signal = 0
        elif self._bars_from_signal >= self._cooldown_bars.Value and edge_pct <= -min_edge and self.Position >= 0:
            self.SellMarket()
            self._bars_from_signal = 0

    def CreateClone(self):
        return monte_carlo_range_forecast_strategy()
