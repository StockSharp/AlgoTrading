import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class drag_sl_tp_strategy(Strategy):
    def __init__(self):
        super(drag_sl_tp_strategy, self).__init__()
        self._sl_points = self.Param("SlPoints", 500.0) \
            .SetDisplay("SL Points", "Stop-loss distance", "Risk")
        self._tp_points = self.Param("TpPoints", 1000.0) \
            .SetDisplay("TP Points", "Take-profit distance", "Risk")
        self._fast_period = self.Param("FastPeriod", 10) \
            .SetDisplay("Fast Period", "Fast EMA period", "Indicators")
        self._slow_period = self.Param("SlowPeriod", 30) \
            .SetDisplay("Slow Period", "Slow EMA period", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._is_initialized = False
        self._entry_price = 0.0

    @property
    def sl_points(self):
        return self._sl_points.Value

    @property
    def tp_points(self):
        return self._tp_points.Value

    @property
    def fast_period(self):
        return self._fast_period.Value

    @property
    def slow_period(self):
        return self._slow_period.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(drag_sl_tp_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._is_initialized = False
        self._entry_price = 0.0

    def OnStarted(self, time):
        super(drag_sl_tp_strategy, self).OnStarted(time)
        fast_ema = ExponentialMovingAverage()
        fast_ema.Length = self.fast_period
        slow_ema = ExponentialMovingAverage()
        slow_ema.Length = self.slow_period
        self.SubscribeCandles(self.candle_type).Bind(fast_ema, slow_ema, self.process_candle).Start()

    def process_candle(self, candle, fast, slow):
        if candle.State != CandleStates.Finished:
            return

        fv = float(fast)
        sv = float(slow)

        if not self._is_initialized:
            self._prev_fast = fv
            self._prev_slow = sv
            self._is_initialized = True
            return

        cross_up = self._prev_fast <= self._prev_slow and fv > sv
        cross_down = self._prev_fast >= self._prev_slow and fv < sv

        self._prev_fast = fv
        self._prev_slow = sv

        tp = float(self.tp_points)
        sl = float(self.sl_points)
        price = float(candle.ClosePrice)

        if self.Position == 0:
            if cross_up:
                self.BuyMarket()
                self._entry_price = price
            elif cross_down:
                self.SellMarket()
                self._entry_price = price
        elif self.Position > 0:
            if price - self._entry_price >= tp or self._entry_price - price >= sl or cross_down:
                self.SellMarket()
                if cross_down:
                    self.SellMarket()
                    self._entry_price = price
        elif self.Position < 0:
            if self._entry_price - price >= tp or price - self._entry_price >= sl or cross_up:
                self.BuyMarket()
                if cross_up:
                    self.BuyMarket()
                    self._entry_price = price

    def CreateClone(self):
        return drag_sl_tp_strategy()
