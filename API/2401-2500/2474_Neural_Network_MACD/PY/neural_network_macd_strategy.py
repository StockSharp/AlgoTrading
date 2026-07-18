import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math, Array
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergenceSignal, MovingAverageConvergenceDivergence, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class neural_network_macd_strategy(Strategy):
    def __init__(self):
        super(neural_network_macd_strategy, self).__init__()
        self._x11 = self.Param("X11", 120).SetDisplay("X11", "Weight 1 for perceptron 1", "Perceptron1")
        self._x12 = self.Param("X12", 80).SetDisplay("X12", "Weight 2 for perceptron 1", "Perceptron1")
        self._x13 = self.Param("X13", 110).SetDisplay("X13", "Weight 3 for perceptron 1", "Perceptron1")
        self._x14 = self.Param("X14", 90).SetDisplay("X14", "Weight 4 for perceptron 1", "Perceptron1")
        self._tp1 = self.Param("Tp1", 100.0).SetDisplay("Take Profit 1", "Take profit for perceptron 1", "Perceptron1")
        self._sl1 = self.Param("Sl1", 50.0).SetDisplay("Stop Loss 1", "Stop loss for perceptron 1", "Perceptron1")
        self._p1 = self.Param("P1", 10).SetDisplay("P1", "Shift for perceptron 1", "Perceptron1")
        self._x21 = self.Param("X21", 130).SetDisplay("X21", "Weight 1 for perceptron 2", "Perceptron2")
        self._x22 = self.Param("X22", 70).SetDisplay("X22", "Weight 2 for perceptron 2", "Perceptron2")
        self._x23 = self.Param("X23", 115).SetDisplay("X23", "Weight 3 for perceptron 2", "Perceptron2")
        self._x24 = self.Param("X24", 85).SetDisplay("X24", "Weight 4 for perceptron 2", "Perceptron2")
        self._tp2 = self.Param("Tp2", 100.0).SetDisplay("Take Profit 2", "Take profit for perceptron 2", "Perceptron2")
        self._sl2 = self.Param("Sl2", 50.0).SetDisplay("Stop Loss 2", "Stop loss for perceptron 2", "Perceptron2")
        self._p2 = self.Param("P2", 10).SetDisplay("P2", "Shift for perceptron 2", "Perceptron2")
        self._x31 = self.Param("X31", 125).SetDisplay("X31", "Weight 1 for perceptron 3", "Perceptron3")
        self._x32 = self.Param("X32", 75).SetDisplay("X32", "Weight 2 for perceptron 3", "Perceptron3")
        self._x33 = self.Param("X33", 105).SetDisplay("X33", "Weight 3 for perceptron 3", "Perceptron3")
        self._x34 = self.Param("X34", 95).SetDisplay("X34", "Weight 4 for perceptron 3", "Perceptron3")
        self._p3 = self.Param("P3", 10).SetDisplay("P3", "Shift for perceptron 3", "Perceptron3")
        self._pass_count = self.Param("Pass", 3).SetDisplay("Pass", "Number of perceptrons to use", "General")
        self._candle_type = self.Param("CandleType", tf(15)).SetDisplay("Candle Type", "Type of candles", "General")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(neural_network_macd_strategy, self).OnReseted()
        self._macd_initialized = False
        self._prev_macd = 0
        self._prev_signal = 0
        self._open_history = []
        self._current_close = 0
        self._current_sl = 0
        self._current_tp = 0
        self._entry_price = 0
        self._is_long = False

    def OnStarted2(self, time):
        super(neural_network_macd_strategy, self).OnStarted2(time)
        self._macd_initialized = False
        self._prev_macd = 0
        self._prev_signal = 0
        max_lag = max(self._p1.Value, self._p2.Value, self._p3.Value) * 4 + 1
        self._open_history = [0] * max_lag
        self._hist_index = 0
        self._hist_filled = False
        self._current_close = 0
        self._current_sl = 0
        self._current_tp = 0
        self._entry_price = 0
        self._is_long = False

        macd = MovingAverageConvergenceDivergenceSignal()
        macd.Macd.ShortMa.Length = 12
        macd.Macd.LongMa.Length = 26
        macd.SignalMa.Length = 9

        sub = self.SubscribeCandles(self.CandleType)
        sub.BindEx(macd, self.OnProcess).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawIndicator(area, macd)
            self.DrawOwnTrades(area)

    def _add_open(self, open_price):
        if len(self._open_history) == 0:
            return
        self._open_history[self._hist_index] = open_price
        self._hist_index += 1
        if self._hist_index >= len(self._open_history):
            self._hist_index = 0
            self._hist_filled = True

    def _try_get_open(self, shift):
        if len(self._open_history) == 0:
            return None
        if not self._hist_filled and shift >= self._hist_index:
            return None
        index = self._hist_index - 1 - shift
        if index < 0:
            index += len(self._open_history)
        return self._open_history[index]

    def _perceptron(self, p, x1, x2, x3, x4):
        o1 = self._try_get_open(p)
        o2 = self._try_get_open(p * 2)
        o3 = self._try_get_open(p * 3)
        o4 = self._try_get_open(p * 4)
        if o1 is None or o2 is None or o3 is None or o4 is None:
            return 0
        w1 = x1 - 100
        w2 = x2 - 100
        w3 = x3 - 100
        w4 = x4 - 100
        return w1 * (self._current_close - o1) + w2 * (o1 - o2) + w3 * (o2 - o3) + w4 * (o3 - o4)

    def _supervisor(self):
        pass_val = self._pass_count.Value
        if pass_val >= 3:
            if self._perceptron(self._p3.Value, self._x31.Value, self._x32.Value, self._x33.Value, self._x34.Value) > 0:
                if self._perceptron(self._p2.Value, self._x21.Value, self._x22.Value, self._x23.Value, self._x24.Value) > 0:
                    self._current_sl = self._sl2.Value
                    self._current_tp = self._tp2.Value
                    return 1
            else:
                if self._perceptron(self._p1.Value, self._x11.Value, self._x12.Value, self._x13.Value, self._x14.Value) < 0:
                    self._current_sl = self._sl1.Value
                    self._current_tp = self._tp1.Value
                    return -1
            return 0
        if pass_val == 2:
            if self._perceptron(self._p2.Value, self._x21.Value, self._x22.Value, self._x23.Value, self._x24.Value) > 0:
                self._current_sl = self._sl2.Value
                self._current_tp = self._tp2.Value
                return 1
            return 0
        if pass_val == 1:
            if self._perceptron(self._p1.Value, self._x11.Value, self._x12.Value, self._x13.Value, self._x14.Value) < 0:
                self._current_sl = self._sl1.Value
                self._current_tp = self._tp1.Value
                return -1
            return 0
        return 0

    def _evaluate_macd(self, macd_val, signal_val):
        if not self._macd_initialized:
            self._prev_macd = macd_val
            self._prev_signal = signal_val
            self._macd_initialized = True
            return 0
        result = 0
        if macd_val < 0 and macd_val >= signal_val and self._prev_macd <= self._prev_signal:
            result = 1
        elif macd_val > 0 and macd_val <= signal_val and self._prev_macd >= self._prev_signal:
            result = -1
        self._prev_macd = macd_val
        self._prev_signal = signal_val
        return result

    def OnProcess(self, candle, macd_ind):
        if candle.State != CandleStates.Finished:
            return

        macd_val = macd_ind.Macd if macd_ind.Macd is not None else 0
        signal_val = macd_ind.Signal if macd_ind.Signal is not None else 0
        self._current_close = candle.ClosePrice

        macd_dir = self._evaluate_macd(float(macd_val), float(signal_val))
        perc_dir = self._supervisor()

        if macd_dir > 0 and perc_dir > 0 and self.Position <= 0:
            self.BuyMarket(self.Volume + Math.Abs(self.Position))
            self._entry_price = candle.ClosePrice
            self._is_long = True
        elif macd_dir < 0 and perc_dir < 0 and self.Position >= 0:
            self.SellMarket(self.Volume + Math.Abs(self.Position))
            self._entry_price = candle.ClosePrice
            self._is_long = False

        if self.Position > 0 and self._is_long:
            if self._current_tp > 0 and candle.ClosePrice >= self._entry_price + self._current_tp:
                self.SellMarket(Math.Abs(self.Position))
            elif self._current_sl > 0 and candle.ClosePrice <= self._entry_price - self._current_sl:
                self.SellMarket(Math.Abs(self.Position))
        elif self.Position < 0 and not self._is_long:
            if self._current_tp > 0 and candle.ClosePrice <= self._entry_price - self._current_tp:
                self.BuyMarket(Math.Abs(self.Position))
            elif self._current_sl > 0 and candle.ClosePrice >= self._entry_price + self._current_sl:
                self.BuyMarket(Math.Abs(self.Position))

        self._add_open(candle.OpenPrice)

    def CreateClone(self):
        return neural_network_macd_strategy()
