import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class volatility_bias_model_strategy(Strategy):
    def __init__(self):
        super(volatility_bias_model_strategy, self).__init__()
        self._bias_window = self.Param("BiasWindow", 10) \
            .SetDisplay("Bias Window", "Bars for bias calculation", "Parameters")
        self._bias_threshold = self.Param("BiasThreshold", 0.6) \
            .SetDisplay("Bias Threshold", "Directional bias threshold", "Parameters")
        self._max_bars = self.Param("MaxBars", 100) \
            .SetDisplay("Max Bars", "Maximum bars to hold", "Parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles", "Parameters")
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._bias_queue = []
        self._bars_in_position = 0
        self._cooldown = 0

    @property
    def bias_window(self):
        return self._bias_window.Value

    @property
    def bias_threshold(self):
        return self._bias_threshold.Value

    @property
    def max_bars(self):
        return self._max_bars.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(volatility_bias_model_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._bias_queue = []
        self._bars_in_position = 0
        self._cooldown = 0

    def OnStarted2(self, time):
        super(volatility_bias_model_strategy, self).OnStarted2(time)
        fast_ema = ExponentialMovingAverage()
        fast_ema.Length = 10
        slow_ema = ExponentialMovingAverage()
        slow_ema.Length = 30
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._bias_queue = []
        self._bars_in_position = 0
        self._cooldown = 0
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast_ema, slow_ema, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast_ema)
            self.DrawIndicator(area, slow_ema)
            self.DrawOwnTrades(area)

    def on_process(self, candle, fast, slow):
        if candle.State != CandleStates.Finished:
            return
        fast = float(fast)
        slow = float(slow)
        # Track bias in rolling window
        self._bias_queue.append(float(candle.ClosePrice) > float(candle.OpenPrice))
        while len(self._bias_queue) > self.bias_window:
            self._bias_queue.pop(0)
        if self._cooldown > 0:
            self._cooldown -= 1
            if self.Position != 0:
                self._bars_in_position += 1
            self._prev_fast = fast
            self._prev_slow = slow
            return
        if self._prev_fast == 0:
            self._prev_fast = fast
            self._prev_slow = slow
            return
        # Time-based exit
        if self.Position != 0:
            self._bars_in_position += 1
            if self._bars_in_position >= self.max_bars:
                if self.Position > 0:
                    self.SellMarket()
                else:
                    self.BuyMarket()
                self._bars_in_position = 0
                self._cooldown = 50
                self._prev_fast = fast
                self._prev_slow = slow
                return
        if len(self._bias_queue) < self.bias_window:
            self._prev_fast = fast
            self._prev_slow = slow
            return
        bull_count = 0
        for b in self._bias_queue:
            if b:
                bull_count += 1
        bias_ratio = float(bull_count) / len(self._bias_queue)
        long_cross = self._prev_fast <= self._prev_slow and fast > slow
        short_cross = self._prev_fast >= self._prev_slow and fast < slow
        if long_cross and bias_ratio >= self.bias_threshold and self.Position <= 0:
            self.BuyMarket()
            self._bars_in_position = 0
            self._cooldown = 50
        elif short_cross and bias_ratio <= (1 - self.bias_threshold) and self.Position >= 0:
            self.SellMarket()
            self._bars_in_position = 0
            self._cooldown = 50
        self._prev_fast = fast
        self._prev_slow = slow

    def CreateClone(self):
        return volatility_bias_model_strategy()
