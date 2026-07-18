import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class mustang_algo_channel_strategy(Strategy):
    def __init__(self):
        super(mustang_algo_channel_strategy, self).__init__()
        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("RSI Period", "Period for RSI calculation", "Parameters")
        self._ema_len = self.Param("EmaLen", 10) \
            .SetDisplay("EMA Length", "EMA smoothing period", "Parameters")
        self._upper_bound = self.Param("UpperBound", 60) \
            .SetDisplay("Upper Bound", "Overbought threshold", "Signals")
        self._lower_bound = self.Param("LowerBound", 40) \
            .SetDisplay("Lower Bound", "Oversold threshold", "Signals")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._prev_rsi = 0.0
        self._prev_ema = 0.0
        self._is_ready = False

    @property
    def rsi_period(self):
        return self._rsi_period.Value

    @property
    def ema_len(self):
        return self._ema_len.Value

    @property
    def upper_bound(self):
        return self._upper_bound.Value

    @property
    def lower_bound(self):
        return self._lower_bound.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(mustang_algo_channel_strategy, self).OnReseted()
        self._prev_rsi = 0.0
        self._prev_ema = 0.0
        self._is_ready = False

    def OnStarted2(self, time):
        super(mustang_algo_channel_strategy, self).OnStarted2(time)
        rsi = RelativeStrengthIndex()
        rsi.Length = self.rsi_period
        ema = ExponentialMovingAverage()
        ema.Length = self.ema_len
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(rsi, ema, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def on_process(self, candle, rsi, ema):
        if candle.State != CandleStates.Finished:
            return
        if not self._is_ready:
            self._prev_rsi = rsi
            self._prev_ema = ema
            self._is_ready = True
            return
        # Buy when RSI crosses up from oversold, sell when it crosses down from overbought
        was_below = self._prev_rsi < self.lower_bound
        was_above = self._prev_rsi > self.upper_bound
        if was_below and rsi >= self.lower_bound and self.Position <= 0:
            self.BuyMarket()
        elif was_above and rsi <= self.upper_bound and self.Position >= 0:
            self.SellMarket()
        self._prev_rsi = rsi
        self._prev_ema = ema

    def CreateClone(self):
        return mustang_algo_channel_strategy()
