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
from collections import deque

class statistics_repeating_behavior_strategy(Strategy):
    """Candle body statistics by time-of-day with martingale sizing."""
    def __init__(self):
        super(statistics_repeating_behavior_strategy, self).__init__()
        self._history_days = self.Param("HistoryDays", 3).SetGreaterThanZero().SetDisplay("History Days", "Number of days to collect statistics", "Parameters")
        self._minimum_body_points = self.Param("MinimumBodyPoints", 0).SetDisplay("Minimum Body (points)", "Ignore candles with smaller body", "Parameters")
        self._stop_loss_pips = self.Param("StopLossPips", 15).SetGreaterThanZero().SetDisplay("Stop Loss (pips)", "Stop loss distance in pips", "Risk")
        self._martingale_factor = self.Param("MartingaleFactor", 1.618).SetGreaterThanZero().SetDisplay("Martingale Factor", "Multiplier after losing trade", "Trading")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))).SetDisplay("Candle Type", "Candles for analysis", "General")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(statistics_repeating_behavior_strategy, self).OnReseted()
        self._body_stats = {}
        self._entry_price = 0
        self._stop_price = 0
        self._pos_dir = 0
        self._timeframe_minutes = 0

    def OnStarted2(self, time):
        super(statistics_repeating_behavior_strategy, self).OnStarted2(time)
        self._body_stats = {}
        self._entry_price = 0
        self._stop_price = 0
        self._pos_dir = 0

        ct = self.CandleType
        arg = ct.Arg
        if hasattr(arg, 'TotalMinutes'):
            self._timeframe_minutes = int(arg.TotalMinutes)
        else:
            self._timeframe_minutes = 1

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(self.OnProcess).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle):
        if candle.State != CandleStates.Finished:
            return

        open_time = candle.OpenTime
        next_key = self._get_minute_key_offset(open_time, self._timeframe_minutes)
        close = float(candle.ClosePrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        open_p = float(candle.OpenPrice)
        stop_pips = self._stop_loss_pips.Value

        # Close existing position
        if self._pos_dir != 0:
            exit_price = close
            stop_hit = False
            if self._pos_dir > 0:
                if low <= self._stop_price:
                    exit_price = self._stop_price
                    stop_hit = True
            else:
                if high >= self._stop_price:
                    exit_price = self._stop_price
                    stop_hit = True

            if self.Position > 0:
                self.SellMarket()
            elif self.Position < 0:
                self.BuyMarket()

            profit = (exit_price - self._entry_price) if self._pos_dir > 0 else (self._entry_price - exit_price)
            self._entry_price = 0
            self._stop_price = 0
            self._pos_dir = 0

        # Entry based on statistics
        if self._pos_dir == 0 and next_key in self._body_stats:
            stats = self._body_stats[next_key]
            if len(stats['values']) > 0:
                bull_sum = stats['bull_sum']
                bear_sum = stats['bear_sum']
                if bull_sum > bear_sum and self.Position <= 0:
                    self._entry_price = close
                    self._stop_price = close - stop_pips
                    self._pos_dir = 1
                    self.BuyMarket()
                elif bear_sum > bull_sum and self.Position >= 0:
                    self._entry_price = close
                    self._stop_price = close + stop_pips
                    self._pos_dir = -1
                    self.SellMarket()

        # Update statistics
        current_key = open_time.Hour * 60 + open_time.Minute
        if current_key not in self._body_stats:
            self._body_stats[current_key] = {'values': deque(), 'bull_sum': 0, 'bear_sum': 0}
        stats = self._body_stats[current_key]
        body = close - open_p
        min_body = self._minimum_body_points.Value
        abs_body = abs(body)
        if min_body > 0 and abs_body < min_body:
            return
        stats['values'].append(body)
        if body > 0:
            stats['bull_sum'] += body
        elif body < 0:
            stats['bear_sum'] += abs(body)
        while len(stats['values']) > self._history_days.Value:
            removed = stats['values'].popleft()
            if removed > 0:
                stats['bull_sum'] -= removed
            elif removed < 0:
                stats['bear_sum'] -= abs(removed)

    def _get_minute_key_offset(self, dt, offset_minutes):
        total = dt.Hour * 60 + dt.Minute + offset_minutes
        return total % 1440

    def CreateClone(self):
        return statistics_repeating_behavior_strategy()
