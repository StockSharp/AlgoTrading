import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, RateOfChange
from StockSharp.Algo.Strategies import Strategy


class pead_strategy(Strategy):
    def __init__(self):
        super(pead_strategy, self).__init__()
        self._gap_threshold = self.Param("GapThreshold", 1.0)
        self._perf_days = self.Param("PerfDays", 20)
        self._stop_pct = self.Param("StopPct", 8.0)
        self._ema_len = self.Param("EmaLen", 50)
        self._max_hold_bars = self.Param("MaxHoldBars", 50)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))
        self._prev_close = 0.0
        self._prev_roc = 0.0
        self._entry_price = 0.0
        self._bars_in_trade = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(pead_strategy, self).OnReseted()
        self._prev_close = 0.0
        self._prev_roc = 0.0
        self._entry_price = 0.0
        self._bars_in_trade = 0

    def OnStarted(self, time):
        super(pead_strategy, self).OnStarted(time)
        self._prev_close = 0.0
        self._prev_roc = 0.0
        self._entry_price = 0.0
        self._bars_in_trade = 0
        self._ema = ExponentialMovingAverage()
        self._ema.Length = self._ema_len.Value
        self._roc = RateOfChange()
        self._roc.Length = self._perf_days.Value + 1
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._ema, self._roc, self.OnProcess).Start()

    def OnProcess(self, candle, ema_val, roc_val):
        if candle.State != CandleStates.Finished:
            return
        close = float(candle.ClosePrice)
        opn = float(candle.OpenPrice)
        ev = float(ema_val)
        rv = float(roc_val)
        body_pct = abs(close - opn) / self._prev_close * 100.0 if self._prev_close != 0 else 0.0
        gt = float(self._gap_threshold.Value)
        strong_move = body_pct >= gt
        perf_pos = self._prev_roc > 0
        if perf_pos and strong_move and self.Position == 0:
            self.BuyMarket()
            self._entry_price = close
            self._bars_in_trade = 0
        if self.Position > 0:
            self._bars_in_trade += 1
            sp = float(self._stop_pct.Value)
            mhb = self._max_hold_bars.Value
            sl = self._entry_price * (1.0 - sp / 100.0)
            tp = self._entry_price * 1.03
            if close <= sl or close >= tp or self._bars_in_trade >= mhb:
                self.SellMarket()
                self._entry_price = 0.0
                self._bars_in_trade = 0
        self._prev_close = close
        self._prev_roc = rv

    def CreateClone(self):
        return pead_strategy()
