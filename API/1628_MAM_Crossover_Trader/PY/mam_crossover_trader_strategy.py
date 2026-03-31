import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class mam_crossover_trader_strategy(Strategy):
    def __init__(self):
        super(mam_crossover_trader_strategy, self).__init__()
        self._fast_period = self.Param("FastPeriod", 10) \
            .SetDisplay("Fast Period", "Fast SMA period", "Indicators")
        self._slow_period = self.Param("SlowPeriod", 20) \
            .SetDisplay("Slow Period", "Slow SMA period", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._prev_diff = 0.0

    @property
    def fast_period(self):
        return self._fast_period.Value

    @property
    def slow_period(self):
        return self._slow_period.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(mam_crossover_trader_strategy, self).OnReseted()
        self._prev_diff = 0.0

    def OnStarted2(self, time):
        super(mam_crossover_trader_strategy, self).OnStarted2(time)
        fast_sma = SimpleMovingAverage()
        fast_sma.Length = self.fast_period
        slow_sma = SimpleMovingAverage()
        slow_sma.Length = self.slow_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast_sma, slow_sma, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast_sma)
            self.DrawIndicator(area, slow_sma)
            self.DrawOwnTrades(area)

    def on_process(self, candle, fast, slow):
        if candle.State != CandleStates.Finished:
            return
        diff = fast - slow
        cross_up = self._prev_diff <= 0 and diff > 0
        cross_down = self._prev_diff >= 0 and diff < 0
        self._prev_diff = diff
        if cross_up and self.Position <= 0:
            self.BuyMarket()
        elif cross_down and self.Position >= 0:
            self.SellMarket()

    def CreateClone(self):
        return mam_crossover_trader_strategy()
