import clr
import math

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class machine_learning_logistic_regression_strategy(Strategy):
    def __init__(self):
        super(machine_learning_logistic_regression_strategy, self).__init__()
        self._lookback = self.Param("Lookback", 3) \
            .SetGreaterThanZero() \
            .SetDisplay("Lookback", "Number of bars for training", "General")
        self._learning_rate = self.Param("LearningRate", 0.0009) \
            .SetGreaterThanZero() \
            .SetDisplay("Learning Rate", "Gradient descent step", "General")
        self._iterations = self.Param("Iterations", 1000) \
            .SetGreaterThanZero() \
            .SetDisplay("Iterations", "Training iterations", "General")
        self._holding_period = self.Param("HoldingPeriod", 5) \
            .SetGreaterThanZero() \
            .SetDisplay("Holding Period", "Bars to hold position", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._base_series = []
        self._synth_series = []
        self._filled = 0
        self._signal = 0
        self._hp_counter = 0
        self._is_initialized = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(machine_learning_logistic_regression_strategy, self).OnReseted()
        lb = self._lookback.Value
        self._base_series = [0.0] * lb
        self._synth_series = [0.0] * lb
        self._filled = 0
        self._signal = 0
        self._hp_counter = 0
        self._is_initialized = False

    def OnStarted(self, time):
        super(machine_learning_logistic_regression_strategy, self).OnStarted(time)
        lb = self._lookback.Value
        self._base_series = [0.0] * lb
        self._synth_series = [0.0] * lb
        self._filled = 0
        self._signal = 0
        self._hp_counter = 0
        self._is_initialized = False
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _shift(self, buffer, value):
        for i in range(len(buffer) - 1):
            buffer[i] = buffer[i + 1]
        buffer[-1] = value

    def _sigmoid(self, z):
        try:
            exp_val = math.exp(-z)
        except OverflowError:
            exp_val = float('inf')
        return 1.0 / (1.0 + exp_val)

    def _run_logistic_regression(self, x, y, p, lr, iterations):
        w = 0.0
        for _ in range(iterations):
            gradient = 0.0
            for j in range(p):
                z = w * x[j]
                h = self._sigmoid(z)
                gradient += (h - y[j]) * x[j]
            gradient /= p
            w -= lr * gradient
        prediction = self._sigmoid(w * x[-1])
        return prediction

    def OnProcess(self, candle):
        if candle.State != CandleStates.Finished:
            return
        close = float(candle.ClosePrice)
        lb = self._lookback.Value
        self._shift(self._base_series, close)
        synthetic = math.log(abs(close * close - 1.0) + 0.5)
        self._shift(self._synth_series, synthetic)
        if self._filled < lb:
            self._filled += 1
            return
        if not self._is_initialized:
            self._is_initialized = True
            return
        if self._signal == 0:
            prev_close = self._base_series[-2] if len(self._base_series) >= 2 else 0.0
            self._signal = 1 if close >= prev_close else -1
            self._hp_counter = 0
            if self._signal == 1 and self.Position <= 0:
                self.BuyMarket()
            elif self._signal == -1 and self.Position >= 0:
                self.SellMarket()
            return
        lr = float(self._learning_rate.Value)
        iters = self._iterations.Value
        prediction = self._run_logistic_regression(
            self._base_series, self._synth_series, lb, lr, iters
        )
        new_signal = 1 if prediction > 0.5 else -1
        if new_signal != self._signal:
            self._hp_counter = 0
            if new_signal == 1 and self.Position <= 0:
                self.BuyMarket()
            elif new_signal == -1 and self.Position >= 0:
                self.SellMarket()
        else:
            self._hp_counter += 1
            hp = self._holding_period.Value
            if self._signal == 1 and self._hp_counter >= hp and self.Position > 0:
                self.SellMarket()
            elif self._signal == -1 and self._hp_counter >= hp and self.Position < 0:
                self.BuyMarket()
        self._signal = new_signal

    def CreateClone(self):
        return machine_learning_logistic_regression_strategy()
