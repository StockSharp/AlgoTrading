import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class ch2010_structure_strategy(Strategy):
    BIAS_NEUTRAL = 0
    BIAS_LONG = 1
    BIAS_SHORT = 2

    def __init__(self):
        super(ch2010_structure_strategy, self).__init__()
        self._trade_volume = self.Param("TradeVolume", 1.0)
        self._stop_loss_percent = self.Param("StopLossPercent", 1.5)
        self._take_profit_percent = self.Param("TakeProfitPercent", 3.0)
        self._breakout_buffer_percent = self.Param("BreakoutBufferPercent", 10.0)
        self._daily_candle_type = self.Param("DailyCandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))
        self._intraday_candle_type = self.Param("IntradayCandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30)))

        self._daily_high = 0.0
        self._daily_low = 0.0
        self._daily_close = 0.0
        self._bias = self.BIAS_NEUTRAL
        self._has_levels = False
        self._long_triggered = False
        self._short_triggered = False
        self._daily_date = None
        self._entry_price = None
        self._stop_price = None
        self._take_profit_price = None
        self._entry_side = None

    @property
    def TradeVolume(self):
        return self._trade_volume.Value

    @property
    def StopLossPercent(self):
        return self._stop_loss_percent.Value

    @property
    def TakeProfitPercent(self):
        return self._take_profit_percent.Value

    @property
    def BreakoutBufferPercent(self):
        return self._breakout_buffer_percent.Value

    @property
    def DailyCandleType(self):
        return self._daily_candle_type.Value

    @property
    def IntradayCandleType(self):
        return self._intraday_candle_type.Value

    def OnStarted2(self, time):
        super(ch2010_structure_strategy, self).OnStarted2(time)

        daily_sub = self.SubscribeCandles(self.DailyCandleType)
        daily_sub.Bind(self._process_daily_candle).Start()

        intraday_sub = self.SubscribeCandles(self.IntradayCandleType)
        intraday_sub.Bind(self._process_intraday_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, intraday_sub)
            self.DrawOwnTrades(area)

    def _process_daily_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return
        self._daily_date = candle.OpenTime.Date
        self._daily_high = float(candle.HighPrice)
        self._daily_low = float(candle.LowPrice)
        self._daily_close = float(candle.ClosePrice)
        self._has_levels = True
        self._long_triggered = False
        self._short_triggered = False

        if float(candle.ClosePrice) > float(candle.OpenPrice):
            self._bias = self.BIAS_LONG
        elif float(candle.ClosePrice) < float(candle.OpenPrice):
            self._bias = self.BIAS_SHORT
        else:
            self._bias = self.BIAS_NEUTRAL

    def _process_intraday_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return
        if not self._has_levels:
            return

        close = float(candle.ClosePrice)
        pos = float(self.Position)

        # Manage open position
        if pos != 0:
            self._manage_open_position(pos, close)
            return

        rng = self._daily_high - self._daily_low
        if rng <= 0:
            return

        buffer = rng * (float(self.BreakoutBufferPercent) / 100.0)
        long_trigger = self._daily_high + buffer
        short_trigger = self._daily_low - buffer

        if not self._long_triggered and self._bias != self.BIAS_SHORT:
            if close > long_trigger:
                self.BuyMarket(float(self.TradeVolume))
                self._set_entry(True, close)
                self._long_triggered = True

        if not self._short_triggered and self._bias != self.BIAS_LONG:
            if close < short_trigger:
                self.SellMarket(float(self.TradeVolume))
                self._set_entry(False, close)
                self._short_triggered = True

    def _manage_open_position(self, pos, close):
        if self._entry_price is None:
            return
        is_long = pos > 0

        if self._stop_price is None or self._take_profit_price is None:
            entry = self._entry_price
            stop_off = entry * (float(self.StopLossPercent) / 100.0)
            take_off = entry * (float(self.TakeProfitPercent) / 100.0)
            if is_long:
                self._stop_price = entry - stop_off
                self._take_profit_price = entry + take_off
            else:
                self._stop_price = entry + stop_off
                self._take_profit_price = entry - take_off

        if is_long:
            if self._stop_price is not None and close <= self._stop_price:
                self.SellMarket(pos)
                self._reset_position()
                return
            if self._take_profit_price is not None and close >= self._take_profit_price:
                self.SellMarket(pos)
                self._reset_position()
        else:
            vol = abs(pos)
            if self._stop_price is not None and close >= self._stop_price:
                self.BuyMarket(vol)
                self._reset_position()
                return
            if self._take_profit_price is not None and close <= self._take_profit_price:
                self.BuyMarket(vol)
                self._reset_position()

    def _set_entry(self, is_long, price):
        self._entry_price = price
        stop_off = price * (float(self.StopLossPercent) / 100.0)
        take_off = price * (float(self.TakeProfitPercent) / 100.0)
        if is_long:
            self._stop_price = price - stop_off
            self._take_profit_price = price + take_off
        else:
            self._stop_price = price + stop_off
            self._take_profit_price = price - take_off

    def _reset_position(self):
        self._entry_price = None
        self._stop_price = None
        self._take_profit_price = None
        self._entry_side = None

    def OnReseted(self):
        super(ch2010_structure_strategy, self).OnReseted()
        self._daily_high = 0.0
        self._daily_low = 0.0
        self._daily_close = 0.0
        self._bias = self.BIAS_NEUTRAL
        self._has_levels = False
        self._long_triggered = False
        self._short_triggered = False
        self._daily_date = None
        self._reset_position()

    def CreateClone(self):
        return ch2010_structure_strategy()
