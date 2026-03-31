import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class open_time_strategy(Strategy):
    def __init__(self):
        super(open_time_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))).SetDisplay("Candle Type", "Candle subscription type", "General")
        self._trade_hour = self.Param("TradeHour", 10).SetDisplay("Trade Hour", "Hour to open positions", "Trading")
        self._trade_minute = self.Param("TradeMinute", 0).SetDisplay("Trade Minute", "Minute to open positions", "Trading")
        self._duration_seconds = self.Param("DurationSeconds", 18000).SetDisplay("Duration", "Window length in seconds", "Trading")
        self._enable_buy = self.Param("EnableBuy", True).SetDisplay("Enable Buy", "Allow long entries", "Trading")
        self._enable_sell = self.Param("EnableSell", True).SetDisplay("Enable Sell", "Allow short entries", "Trading")
        self._sl_pips = self.Param("StopLossPips", 500).SetDisplay("Stop Loss", "Initial stop loss in pips", "Risk")
        self._tp_pips = self.Param("TakeProfitPips", 1000).SetDisplay("Take Profit", "Initial take profit in pips", "Risk")
        self._use_close_time = self.Param("UseCloseTime", True).SetDisplay("Use Close Window", "Enable closing window", "Trading")
        self._close_hour = self.Param("CloseHour", 20).SetDisplay("Close Hour", "Hour for closing window", "Trading")
        self._close_minute = self.Param("CloseMinute", 50).SetDisplay("Close Minute", "Minute for closing window", "Trading")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(open_time_strategy, self).OnReseted()
        self._long_entry = None
        self._short_entry = None
        self._long_stop = None
        self._long_take = None
        self._short_stop = None
        self._short_take = None

    def OnStarted2(self, time):
        super(open_time_strategy, self).OnStarted2(time)
        self._long_entry = None
        self._short_entry = None
        self._long_stop = None
        self._long_take = None
        self._short_stop = None
        self._short_take = None
        step = 1.0
        if self.Security is not None and self.Security.PriceStep is not None and self.Security.PriceStep > 0:
            step = float(self.Security.PriceStep)
        self._pip_size = step
        self._stop_offset = self._sl_pips.Value * self._pip_size
        self._take_offset = self._tp_pips.Value * self._pip_size

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(self.OnProcess).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawOwnTrades(area)

    def _is_within_window(self, now, hour, minute, duration_sec):
        if duration_sec <= 0:
            return False
        total_min = now.Hour * 60 + now.Minute
        start_min = hour * 60 + minute
        end_sec = (start_min * 60) + duration_sec
        now_sec = now.Hour * 3600 + now.Minute * 60 + now.Second
        start_sec = start_min * 60
        return now_sec >= start_sec and now_sec < end_sec

    def OnProcess(self, candle):
        if candle.State != CandleStates.Finished:
            return

        now = candle.CloseTime

        # Force close in closing window
        if self._use_close_time.Value and self._is_within_window(now, self._close_hour.Value, self._close_minute.Value, self._duration_seconds.Value):
            if self.Position > 0:
                self.SellMarket()
                self._long_entry = None
                self._long_stop = None
                self._long_take = None
            elif self.Position < 0:
                self.BuyMarket()
                self._short_entry = None
                self._short_stop = None
                self._short_take = None
            return

        # Check SL/TP
        if self.Position > 0:
            if self._long_stop is not None and candle.LowPrice <= self._long_stop:
                self.SellMarket()
                self._long_entry = None
                self._long_stop = None
                self._long_take = None
                return
            if self._long_take is not None and candle.HighPrice >= self._long_take:
                self.SellMarket()
                self._long_entry = None
                self._long_stop = None
                self._long_take = None
                return
        elif self.Position < 0:
            if self._short_stop is not None and candle.HighPrice >= self._short_stop:
                self.BuyMarket()
                self._short_entry = None
                self._short_stop = None
                self._short_take = None
                return
            if self._short_take is not None and candle.LowPrice <= self._short_take:
                self.BuyMarket()
                self._short_entry = None
                self._short_stop = None
                self._short_take = None
                return

        if not self._is_within_window(now, self._trade_hour.Value, self._trade_minute.Value, self._duration_seconds.Value):
            return

        if self._enable_buy.Value and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
                self._short_entry = None
            self.BuyMarket()
            self._long_entry = candle.ClosePrice
            self._long_stop = candle.ClosePrice - self._stop_offset if self._sl_pips.Value > 0 else None
            self._long_take = candle.ClosePrice + self._take_offset if self._tp_pips.Value > 0 else None
        elif self._enable_sell.Value and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
                self._long_entry = None
            self.SellMarket()
            self._short_entry = candle.ClosePrice
            self._short_stop = candle.ClosePrice + self._stop_offset if self._sl_pips.Value > 0 else None
            self._short_take = candle.ClosePrice - self._take_offset if self._tp_pips.Value > 0 else None

    def CreateClone(self):
        return open_time_strategy()
