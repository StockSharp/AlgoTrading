import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy

class multi_layer_awesome_oscillator_saucer_strategy(Strategy):
    """
    MultiLayer Awesome Oscillator Saucer: EMA crossover with RSI filter.
    """

    def __init__(self):
        super(multi_layer_awesome_oscillator_saucer_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))).SetDisplay("Candle Type", "Candles", "General")

        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._initialized = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(multi_layer_awesome_oscillator_saucer_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._initialized = False

    def OnStarted(self, time):
        super(multi_layer_awesome_oscillator_saucer_strategy, self).OnStarted(time)
        fast = ExponentialMovingAverage()
        fast.Length = 8
        slow = ExponentialMovingAverage()
        slow.Length = 21
        rsi = RelativeStrengthIndex()
        rsi.Length = 14
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast, slow, rsi, self._process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast)
            self.DrawIndicator(area, slow)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, fast_val, slow_val, rsi_val):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        fast = float(fast_val)
        slow = float(slow_val)
        rsi = float(rsi_val)
        if not self._initialized:
            self._prev_fast = fast
            self._prev_slow = slow
            self._initialized = True
            return
        if self._prev_fast <= self._prev_slow and fast > slow and rsi > 45 and self.Position <= 0:
            self.BuyMarket()
        elif self._prev_fast >= self._prev_slow and fast < slow and rsi < 55 and self.Position > 0:
            self.SellMarket()
        self._prev_fast = fast
        self._prev_slow = slow

    def CreateClone(self):
        return multi_layer_awesome_oscillator_saucer_strategy()
