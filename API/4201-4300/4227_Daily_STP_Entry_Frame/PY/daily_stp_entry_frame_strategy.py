import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy

class daily_stp_entry_frame_strategy(Strategy):
    def __init__(self):
        super(daily_stp_entry_frame_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Time-frame for monitoring", "General")
        self._stop_loss_points = self.Param("StopLossPoints", 80.0) \
            .SetDisplay("Stop-Loss (points)", "Stop-loss distance", "Risk")
        self._take_profit_points = self.Param("TakeProfitPoints", 200.0) \
            .SetDisplay("Take-Profit (points)", "Take-profit distance", "Risk")
        self._prev_day_high = None
        self._prev_day_low = None
        self._cur_day_high = 0.0
        self._cur_day_low = 0.0
        self._current_day = None
        self._traded_today = False
        self._entry_price = 0.0
        self._long_stop = None
        self._long_take = None
        self._short_stop = None
        self._short_take = None

    @property
    def CandleType(self):
        return self._candle_type.Value
    @property
    def StopLossPoints(self):
        return float(self._stop_loss_points.Value)
    @property
    def TakeProfitPoints(self):
        return float(self._take_profit_points.Value)

    def OnStarted2(self, time):
        super(daily_stp_entry_frame_strategy, self).OnStarted2(time)
        self._pip_size = 0.01
        sec = self.Security
        if sec is not None:
            ps = sec.PriceStep
            if ps is not None and float(ps) > 0:
                self._pip_size = float(ps)
        self._sl_offset = self.StopLossPoints * self._pip_size
        self._tp_offset = self.TakeProfitPoints * self._pip_size
        self._prev_day_high = None
        self._prev_day_low = None
        self._current_day = None
        self._traded_today = False
        self._entry_price = 0.0
        self._long_stop = None
        self._long_take = None
        self._short_stop = None
        self._short_take = None
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.ProcessCandle).Start()

    def ProcessCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return
        dt = candle.OpenTime
        day = dt.Date
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        close = float(candle.ClosePrice)
        if self._current_day is None or self._current_day != day:
            if self._current_day is not None:
                self._prev_day_high = self._cur_day_high
                self._prev_day_low = self._cur_day_low
            self._current_day = day
            self._cur_day_high = high
            self._cur_day_low = low
            self._traded_today = False
        else:
            if high > self._cur_day_high:
                self._cur_day_high = high
            if low < self._cur_day_low:
                self._cur_day_low = low
        # manage position
        if self.Position > 0:
            if self._long_stop is not None and low <= self._long_stop:
                self.SellMarket()
                self._long_stop = None
                self._long_take = None
                return
            if self._long_take is not None and high >= self._long_take:
                self.SellMarket()
                self._long_stop = None
                self._long_take = None
                return
        elif self.Position < 0:
            if self._short_stop is not None and high >= self._short_stop:
                self.BuyMarket()
                self._short_stop = None
                self._short_take = None
                return
            if self._short_take is not None and low <= self._short_take:
                self.BuyMarket()
                self._short_stop = None
                self._short_take = None
                return
        # entries
        if self._prev_day_high is None or self._prev_day_low is None:
            return
        if self._traded_today or self.Position != 0:
            return
        if close > self._prev_day_high:
            self._entry_price = close
            self._long_stop = close - self._sl_offset if self._sl_offset > 0 else None
            self._long_take = close + self._tp_offset if self._tp_offset > 0 else None
            self._short_stop = None
            self._short_take = None
            self.BuyMarket()
            self._traded_today = True
        elif close < self._prev_day_low:
            self._entry_price = close
            self._short_stop = close + self._sl_offset if self._sl_offset > 0 else None
            self._short_take = close - self._tp_offset if self._tp_offset > 0 else None
            self._long_stop = None
            self._long_take = None
            self.SellMarket()
            self._traded_today = True

    def OnReseted(self):
        super(daily_stp_entry_frame_strategy, self).OnReseted()
        self._prev_day_high = None
        self._prev_day_low = None
        self._cur_day_high = 0.0
        self._cur_day_low = 0.0
        self._current_day = None
        self._traded_today = False
        self._entry_price = 0.0
        self._long_stop = None
        self._long_take = None
        self._short_stop = None
        self._short_take = None

    def CreateClone(self):
        return daily_stp_entry_frame_strategy()
