import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class yeong_rrg_strategy(Strategy):
    def __init__(self):
        super(yeong_rrg_strategy, self).__init__()
        self._length = self.Param("Length", 20) \
            .SetDisplay("Length", "Period for calculations", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._rs_ratio_history = []
        self._rm_ratio_history = []
        self._prev_rs_ratio = 0.0

    @property
    def length(self):
        return self._length.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(yeong_rrg_strategy, self).OnReseted()
        self._rs_ratio_history = []
        self._rm_ratio_history = []
        self._prev_rs_ratio = 0.0

    def OnStarted(self, time):
        super(yeong_rrg_strategy, self).OnStarted(time)
        sma = SimpleMovingAverage()
        sma.Length = self.length
        self._rs_ratio_history = []
        self._rm_ratio_history = []
        self._prev_rs_ratio = 0.0
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(sma, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sma)
            self.DrawOwnTrades(area)

    def on_process(self, candle, sma_val):
        if candle.State != CandleStates.Finished:
            return
        if sma_val <= 0:
            return
        rs_ratio = (float(candle.ClosePrice) / float(sma_val)) * 100.0
        self._rs_ratio_history.append(rs_ratio)
        if len(self._rs_ratio_history) > self.length * 3:
            self._rs_ratio_history.pop(0)
        if self._prev_rs_ratio > 0:
            rm_ratio = rs_ratio - self._prev_rs_ratio
        else:
            rm_ratio = 0.0
        self._prev_rs_ratio = rs_ratio
        self._rm_ratio_history.append(rm_ratio)
        if len(self._rm_ratio_history) > self.length * 3:
            self._rm_ratio_history.pop(0)
        if len(self._rs_ratio_history) < self.length or len(self._rm_ratio_history) < self.length:
            return
        rs_slice = self._rs_ratio_history[-self.length:]
        rm_slice = self._rm_ratio_history[-self.length:]
        rs_mean = sum(rs_slice) / len(rs_slice)
        rs_std = self._std_dev(rs_slice)
        if rs_std == 0:
            rs_std = 1.0
        rm_mean = sum(rm_slice) / len(rm_slice)
        rm_std = self._std_dev(rm_slice)
        if rm_std == 0:
            rm_std = 1.0
        jdk_rs = 100.0 + ((rs_ratio - rs_mean) / rs_std)
        jdk_rm = 100.0 + ((rm_ratio - rm_mean) / rm_std)
        buy_signal = jdk_rs > 100.0 and jdk_rm > 100.0
        sell_signal = jdk_rs < 100.0 and jdk_rm < 100.0
        if buy_signal and self.Position <= 0:
            self.BuyMarket()
        elif sell_signal and self.Position >= 0:
            self.SellMarket()

    def _std_dev(self, values):
        if len(values) < 2:
            return 0.0
        mean = sum(values) / len(values)
        sum_sq = 0.0
        for v in values:
            sum_sq += (v - mean) * (v - mean)
        return Math.Sqrt(sum_sq / len(values))

    def CreateClone(self):
        return yeong_rrg_strategy()
