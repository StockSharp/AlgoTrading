import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, StandardDeviation
from StockSharp.Algo.Strategies import Strategy


class z_score_rsi_strategy(Strategy):
    def __init__(self):
        super(z_score_rsi_strategy, self).__init__()
        self._z_score_length = self.Param("ZScoreLength", 20) \
            .SetDisplay("Z-Score Length", "Length for mean and deviation", "Indicators")
        self._rsi_length = self.Param("RsiLength", 9) \
            .SetDisplay("RSI Length", "Length for RSI", "Indicators")
        self._smoothing_length = self.Param("SmoothingLength", 15) \
            .SetDisplay("RSI EMA Length", "EMA length over RSI", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._closes = []
        self._prev_rsi_z = 0.0
        self._prev_rsi_ma = 0.0
        self._has_prev = False

    @property
    def z_score_length(self):
        return self._z_score_length.Value

    @property
    def rsi_length(self):
        return self._rsi_length.Value

    @property
    def smoothing_length(self):
        return self._smoothing_length.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(z_score_rsi_strategy, self).OnReseted()
        self._closes = []
        self._prev_rsi_z = 0.0
        self._prev_rsi_ma = 0.0
        self._has_prev = False

    def OnStarted(self, time):
        super(z_score_rsi_strategy, self).OnStarted(time)
        sma = SimpleMovingAverage()
        sma.Length = self.z_score_length
        std_dev = StandardDeviation()
        std_dev.Length = self.z_score_length
        self._closes = []
        self._prev_rsi_z = 0.0
        self._prev_rsi_ma = 0.0
        self._has_prev = False
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(sma, std_dev, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def on_process(self, candle, mean_val, std_val):
        if candle.State != CandleStates.Finished:
            return
        if std_val <= 0:
            return
        z = (float(candle.ClosePrice) - float(mean_val)) / float(std_val)
        self._closes.append(z)
        if len(self._closes) > self.rsi_length + self.smoothing_length + 10:
            self._closes.pop(0)
        if len(self._closes) < self.rsi_length + 1:
            return
        avg_gain = 0.0
        avg_loss = 0.0
        start = len(self._closes) - self.rsi_length
        for i in range(start, len(self._closes)):
            change = self._closes[i] - self._closes[i - 1]
            if change > 0:
                avg_gain += change
            else:
                avg_loss += abs(change)
        avg_gain /= self.rsi_length
        avg_loss /= self.rsi_length
        if avg_loss == 0:
            rsi_z = 100.0
        else:
            rs = avg_gain / avg_loss
            rsi_z = 100.0 - (100.0 / (1.0 + rs))
        if not self._has_prev:
            rsi_ma = rsi_z
            self._prev_rsi_z = rsi_z
            self._prev_rsi_ma = rsi_ma
            self._has_prev = True
            return
        k = 2.0 / (self.smoothing_length + 1)
        rsi_ma = rsi_z * k + self._prev_rsi_ma * (1.0 - k)
        cross_up = self._prev_rsi_z <= self._prev_rsi_ma and rsi_z > rsi_ma
        cross_down = self._prev_rsi_z >= self._prev_rsi_ma and rsi_z < rsi_ma
        if cross_up and self.Position <= 0:
            self.BuyMarket()
        elif cross_down and self.Position >= 0:
            self.SellMarket()
        self._prev_rsi_z = rsi_z
        self._prev_rsi_ma = rsi_ma

    def CreateClone(self):
        return z_score_rsi_strategy()
