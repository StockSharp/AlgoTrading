import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class exp_x2_ma_strategy(Strategy):
    def __init__(self):
        super(exp_x2_ma_strategy, self).__init__()
        self._first_ma_length = self.Param("FirstMaLength", 12) \
            .SetDisplay("First MA Length", "Period for first smoothing", "Indicators")
        self._second_ma_length = self.Param("SecondMaLength", 10) \
            .SetDisplay("Second MA Length", "Period for second smoothing", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._prev_prev_value = 0.0
        self._prev_value = 0.0
        self._bar_count = 0

    @property
    def first_ma_length(self):
        return self._first_ma_length.Value

    @property
    def second_ma_length(self):
        return self._second_ma_length.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(exp_x2_ma_strategy, self).OnReseted()
        self._prev_prev_value = 0.0
        self._prev_value = 0.0
        self._bar_count = 0

    def OnStarted2(self, time):
        super(exp_x2_ma_strategy, self).OnStarted2(time)
        ema1 = ExponentialMovingAverage()
        ema1.Length = self.first_ma_length
        ema2 = ExponentialMovingAverage()
        ema2.Length = self.second_ma_length
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ema1, ema2, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def on_process(self, candle, ema1_value, ema2_value):
        if candle.State != CandleStates.Finished:
            return
        self._bar_count += 1
        if self._bar_count >= 3:
            is_local_min = self._prev_value < self._prev_prev_value and ema2_value > self._prev_value
            is_local_max = self._prev_value > self._prev_prev_value and ema2_value < self._prev_value
            if is_local_min and self.Position <= 0:
                if self.Position < 0:
                    self.BuyMarket()
                self.BuyMarket()
            elif is_local_max and self.Position >= 0:
                if self.Position > 0:
                    self.SellMarket()
                self.SellMarket()
        self._prev_prev_value = self._prev_value
        self._prev_value = ema2_value

    def CreateClone(self):
        return exp_x2_ma_strategy()
