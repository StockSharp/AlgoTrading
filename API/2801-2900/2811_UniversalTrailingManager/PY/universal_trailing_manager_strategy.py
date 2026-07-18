import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class universal_trailing_manager_strategy(Strategy):
    """Simplified universal trailing manager: time-based entries with SL/TP/trailing management."""
    def __init__(self):
        super(universal_trailing_manager_strategy, self).__init__()
        self._tp_points = self.Param("TakeProfitPoints", 200).SetDisplay("Take Profit", "TP in points", "Risk")
        self._sl_points = self.Param("StopLossPoints", 100).SetDisplay("Stop Loss", "SL in points", "Risk")
        self._trailing_points = self.Param("TrailingStopPoints", 100).SetDisplay("Trailing Stop", "Trailing distance", "Risk")
        self._trailing_step = self.Param("TrailingStepPoints", 10).SetDisplay("Trailing Step", "Trailing step", "Risk")
        self._time_hour = self.Param("TimeHour", 23).SetDisplay("Hour", "Scheduled hour", "Time")
        self._time_minute = self.Param("TimeMinute", 59).SetDisplay("Minute", "Scheduled minute", "Time")
        self._time_buy = self.Param("TimeBuy", True).SetDisplay("Time Buy", "Open buy at time", "Time")
        self._time_sell = self.Param("TimeSell", True).SetDisplay("Time Sell", "Open sell at time", "Time")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Timeframe", "General")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(universal_trailing_manager_strategy, self).OnReseted()
        self._entry_price = 0
        self._stop_price = None
        self._take_price = None
        self._last_entry_day = -1

    def OnStarted2(self, time):
        super(universal_trailing_manager_strategy, self).OnStarted2(time)
        self._entry_price = 0
        self._stop_price = None
        self._take_price = None
        self._last_entry_day = -1

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(self.OnProcess).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        sl_dist = self._sl_points.Value
        tp_dist = self._tp_points.Value
        trail_dist = self._trailing_points.Value
        trail_step = self._trailing_step.Value

        # Manage existing position
        if self.Position > 0:
            # Trailing
            if trail_dist > 0 and self._entry_price > 0:
                activation = trail_dist + trail_step
                if close - self._entry_price > activation:
                    new_stop = close - trail_dist
                    if self._stop_price is None or new_stop > self._stop_price:
                        self._stop_price = new_stop

            if self._stop_price is not None and low <= self._stop_price:
                self.SellMarket()
                self._reset()
                return
            if self._take_price is not None and high >= self._take_price:
                self.SellMarket()
                self._reset()
                return

        elif self.Position < 0:
            if trail_dist > 0 and self._entry_price > 0:
                activation = trail_dist + trail_step
                if self._entry_price - close > activation:
                    new_stop = close + trail_dist
                    if self._stop_price is None or new_stop < self._stop_price:
                        self._stop_price = new_stop

            if self._stop_price is not None and high >= self._stop_price:
                self.BuyMarket()
                self._reset()
                return
            if self._take_price is not None and low <= self._take_price:
                self.BuyMarket()
                self._reset()
                return

        # Time-based entries
        hour = candle.CloseTime.Hour
        minute = candle.CloseTime.Minute
        day = candle.OpenTime.DayOfYear

        if hour == self._time_hour.Value and minute == self._time_minute.Value and day != self._last_entry_day:
            if self.Position == 0:
                if self._time_buy.Value:
                    self.BuyMarket()
                    self._entry_price = close
                    self._stop_price = close - sl_dist if sl_dist > 0 else None
                    self._take_price = close + tp_dist if tp_dist > 0 else None
                    self._last_entry_day = day
                elif self._time_sell.Value:
                    self.SellMarket()
                    self._entry_price = close
                    self._stop_price = close + sl_dist if sl_dist > 0 else None
                    self._take_price = close - tp_dist if tp_dist > 0 else None
                    self._last_entry_day = day

    def _reset(self):
        self._entry_price = 0
        self._stop_price = None
        self._take_price = None

    def CreateClone(self):
        return universal_trailing_manager_strategy()
