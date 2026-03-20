import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class iu_gap_fill_strategy(Strategy):
    def __init__(self):
        super(iu_gap_fill_strategy, self).__init__()
        self._gap_percent = self.Param("GapPercent", 0.01) \
            .SetDisplay("Gap %", "Minimum percentage gap", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._current_day = None
        self._last_session_close = 0.0
        self._prev_day_close = 0.0
        self._gap_up = False
        self._gap_down = False
        self._valid_gap = False
        self._is_first_bar = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(iu_gap_fill_strategy, self).OnReseted()
        self._current_day = None
        self._last_session_close = 0.0
        self._prev_day_close = 0.0
        self._gap_up = False
        self._gap_down = False
        self._valid_gap = False
        self._is_first_bar = False

    def OnStarted(self, time):
        super(iu_gap_fill_strategy, self).OnStarted(time)
        ema1 = ExponentialMovingAverage()
        ema1.Length = 10
        ema2 = ExponentialMovingAverage()
        ema2.Length = 20
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ema1, ema2, self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, d1, d2):
        if candle.State != CandleStates.Finished:
            return
        day = candle.OpenTime.Date
        close = float(candle.ClosePrice)
        open_p = float(candle.OpenPrice)
        if self._current_day is None or self._current_day != day:
            self._prev_day_close = self._last_session_close
            self._current_day = day
            if self.Position > 0:
                self.SellMarket()
            elif self.Position < 0:
                self.BuyMarket()
            if self._prev_day_close > 0:
                self._gap_up = open_p > self._prev_day_close
                self._gap_down = open_p < self._prev_day_close
                gap_pct = float(self._gap_percent.Value)
                self._valid_gap = abs(self._prev_day_close - open_p) >= open_p * gap_pct / 100.0
            self._is_first_bar = True
        self._last_session_close = close
        if self._is_first_bar:
            self._is_first_bar = False
        elif self._valid_gap and self.Position == 0:
            if self._gap_up and close <= self._prev_day_close:
                self.BuyMarket()
            elif self._gap_down and close >= self._prev_day_close:
                self.SellMarket()

    def CreateClone(self):
        return iu_gap_fill_strategy()
