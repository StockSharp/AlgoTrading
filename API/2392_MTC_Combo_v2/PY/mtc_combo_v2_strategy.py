import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class mtc_combo_v2_strategy(Strategy):
    """
    MTC Combo v2: SMA slope + perceptron-based direction with manual SL/TP.
    """

    def __init__(self):
        super(mtc_combo_v2_strategy, self).__init__()
        self._ma_period = self.Param("MaPeriod", 10).SetDisplay("MA Period", "SMA period", "Indicators")
        self._p2 = self.Param("P2", 20).SetDisplay("P2", "Perceptron P2", "Signals")
        self._p3 = self.Param("P3", 20).SetDisplay("P3", "Perceptron P3", "Signals")
        self._p4 = self.Param("P4", 20).SetDisplay("P4", "Perceptron P4", "Signals")
        self._pass_val = self.Param("Pass", 10).SetDisplay("Pass", "Strategy pass", "Signals")
        self._sl1 = self.Param("Sl1", 50.0).SetDisplay("SL1", "Stop loss 1", "Risk")
        self._tp1 = self.Param("Tp1", 50.0).SetDisplay("TP1", "Take profit 1", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))).SetDisplay("Candle Type", "Candles", "General")

        self._prev_ma = None
        self._opens = []
        self._entry = 0.0
        self._sl = 50.0
        self._tp = 50.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(mtc_combo_v2_strategy, self).OnReseted()
        self._prev_ma = None
        self._opens = []
        self._entry = 0.0
        self._sl = float(self._sl1.Value)
        self._tp = float(self._tp1.Value)

    def OnStarted2(self, time):
        super(mtc_combo_v2_strategy, self).OnStarted2(time)
        ma = SimpleMovingAverage()
        ma.Length = self._ma_period.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ma, self._process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ma)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, ma_val):
        if candle.State != CandleStates.Finished:
            return
        ma = float(ma_val)
        close = float(candle.ClosePrice)
        open_p = float(candle.OpenPrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        self._opens.append(open_p)
        max_len = max(self._p2.Value, self._p3.Value, self._p4.Value) * 4 + 5
        while len(self._opens) > max_len:
            self._opens.pop(0)
        if self.Position != 0:
            step = 1.0
            stop = self._entry - self._sl * step if self.Position > 0 else self._entry + self._sl * step
            take = self._entry + self._tp * step if self.Position > 0 else self._entry - self._tp * step
            if (self.Position > 0 and low <= stop) or (self.Position < 0 and high >= stop):
                if self.Position > 0:
                    self.SellMarket()
                else:
                    self.BuyMarket()
                return
            if (self.Position > 0 and high >= take) or (self.Position < 0 and low <= take):
                if self.Position > 0:
                    self.SellMarket()
                else:
                    self.BuyMarket()
                return
        slope = ma - self._prev_ma if self._prev_ma is not None else 0.0
        self._prev_ma = ma
        if self.Position != 0:
            return
        self._sl = float(self._sl1.Value)
        self._tp = float(self._tp1.Value)
        direction = self._supervisor(slope)
        if direction > 0:
            self.BuyMarket()
            self._entry = close
        elif direction < 0:
            self.SellMarket()
            self._entry = close

    def _supervisor(self, slope):
        p = self._pass_val.Value
        if p == 4:
            if self._perceptron(self._p4.Value) > 0 and self._perceptron(self._p3.Value) > 0:
                return 1.0
            if self._perceptron(self._p4.Value) <= 0 and self._perceptron(self._p2.Value) < 0:
                return -1.0
        elif p == 3:
            if self._perceptron(self._p3.Value) > 0:
                return 1.0
        elif p == 2:
            if self._perceptron(self._p2.Value) < 0:
                return -1.0
        return slope

    def _perceptron(self, p):
        if len(self._opens) <= p * 4:
            return 0.0
        arr = self._opens
        n = len(arr)
        a1 = arr[n - 1] - arr[n - 1 - p]
        a2 = arr[n - 1 - p] - arr[n - 1 - p * 2]
        a3 = arr[n - 1 - p * 2] - arr[n - 1 - p * 3]
        a4 = arr[n - 1 - p * 3] - arr[n - 1 - p * 4]
        return a1 + a2 + a3 + a4

    def CreateClone(self):
        return mtc_combo_v2_strategy()
