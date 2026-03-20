import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import TripleExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class trix_crossover_strategy(Strategy):
    def __init__(self):
        super(trix_crossover_strategy, self).__init__()
        self._fast_period = self.Param("FastPeriod", 9) \
            .SetDisplay("Fast TRIX Period", "Period for the fast TRIX indicator", "Indicators")
        self._slow_period = self.Param("SlowPeriod", 21) \
            .SetDisplay("Slow TRIX Period", "Period for the slow TRIX indicator", "Indicators")
        self._min_trix = self.Param("MinTrix", 0.0005) \
            .SetDisplay("Min TRIX", "Minimum TRIX magnitude for signals", "Indicators")
        self._take_profit = self.Param("TakeProfit", 1500.0) \
            .SetDisplay("Take Profit", "Take profit in absolute price units", "Risk Management")
        self._stop_loss = self.Param("StopLoss", 500.0) \
            .SetDisplay("Stop Loss", "Stop loss in absolute price units", "Risk Management")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._fast_trix_prev1 = 0.0
        self._fast_trix_prev2 = 0.0
        self._slow_trix_prev = 0.0
        self._prev_fast_tema = 0.0
        self._prev_slow_tema = 0.0

    @property
    def fast_period(self):
        return self._fast_period.Value

    @property
    def slow_period(self):
        return self._slow_period.Value

    @property
    def min_trix(self):
        return self._min_trix.Value

    @property
    def take_profit(self):
        return self._take_profit.Value

    @property
    def stop_loss(self):
        return self._stop_loss.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(trix_crossover_strategy, self).OnReseted()
        self._fast_trix_prev1 = 0.0
        self._fast_trix_prev2 = 0.0
        self._slow_trix_prev = 0.0
        self._prev_fast_tema = 0.0
        self._prev_slow_tema = 0.0

    def OnStarted(self, time):
        super(trix_crossover_strategy, self).OnStarted(time)
        self._fast_trix_prev1 = 0.0
        self._fast_trix_prev2 = 0.0
        self._slow_trix_prev = 0.0
        self._prev_fast_tema = 0.0
        self._prev_slow_tema = 0.0
        fast_tema = TripleExponentialMovingAverage()
        fast_tema.Length = int(self.fast_period)
        slow_tema = TripleExponentialMovingAverage()
        slow_tema.Length = int(self.slow_period)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast_tema, slow_tema, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast_tema)
            self.DrawIndicator(area, slow_tema)
            self.DrawOwnTrades(area)
        self.StartProtection(
            takeProfit=Unit(float(self.take_profit), UnitTypes.Absolute),
            stopLoss=Unit(float(self.stop_loss), UnitTypes.Absolute))

    def process_candle(self, candle, fast_tema_value, slow_tema_value):
        if candle.State != CandleStates.Finished:
            return
        fast_tema_value = float(fast_tema_value)
        slow_tema_value = float(slow_tema_value)
        if self._prev_fast_tema == 0.0 or self._prev_slow_tema == 0.0:
            self._prev_fast_tema = fast_tema_value
            self._prev_slow_tema = slow_tema_value
            return
        fast_trix = (fast_tema_value - self._prev_fast_tema) / self._prev_fast_tema if self._prev_fast_tema != 0 else 0.0
        slow_trix = (slow_tema_value - self._prev_slow_tema) / self._prev_slow_tema if self._prev_slow_tema != 0 else 0.0
        self._prev_fast_tema = fast_tema_value
        self._prev_slow_tema = slow_tema_value
        prev_fast_trix = self._fast_trix_prev1
        self._fast_trix_prev2 = self._fast_trix_prev1
        self._fast_trix_prev1 = fast_trix
        slow_trix_prev = self._slow_trix_prev
        self._slow_trix_prev = slow_trix
        if self._fast_trix_prev2 == 0.0 or slow_trix_prev == 0.0:
            return
        mt = float(self.min_trix)
        cross_up = prev_fast_trix <= 0 and fast_trix > 0
        cross_down = prev_fast_trix >= 0 and fast_trix < 0
        if cross_up and slow_trix > mt and self.Position <= 0:
            self.BuyMarket()
        elif cross_down and slow_trix < -mt and self.Position >= 0:
            self.SellMarket()

    def CreateClone(self):
        return trix_crossover_strategy()
