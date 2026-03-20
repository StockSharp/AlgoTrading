import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class pivot_heiken_strategy(Strategy):
    def __init__(self):
        super(pivot_heiken_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4)))
        self._stop_loss_pips = self.Param("StopLossPips", 50)
        self._take_profit_pips = self.Param("TakeProfitPips", 100)
        self._trailing_stop_pips = self.Param("TrailingStopPips", 0)
        self._pivot = 0.0
        self._prev_high = 0.0
        self._prev_low = 0.0
        self._prev_close = 0.0
        self._daily_initialized = False
        self._ha_open = 0.0
        self._ha_close = 0.0
        self._ha_initialized = False
        self._entry_price = 0.0
        self._stop_price = 0.0
        self._take_price = 0.0
        self._trailing_stop = 0.0
        self._step = 0.0
        self._trailing_distance = 0.0
        self._previous_direction = 0

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, v): self._candle_type.Value = v
    @property
    def StopLossPips(self): return self._stop_loss_pips.Value
    @StopLossPips.setter
    def StopLossPips(self, v): self._stop_loss_pips.Value = v
    @property
    def TakeProfitPips(self): return self._take_profit_pips.Value
    @TakeProfitPips.setter
    def TakeProfitPips(self, v): self._take_profit_pips.Value = v
    @property
    def TrailingStopPips(self): return self._trailing_stop_pips.Value
    @TrailingStopPips.setter
    def TrailingStopPips(self, v): self._trailing_stop_pips.Value = v

    def OnStarted(self, time):
        super(pivot_heiken_strategy, self).OnStarted(time)
        self._step = float(self.Security.PriceStep) if self.Security is not None else 1.0
        self._trailing_distance = float(self.TrailingStopPips) * self._step
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.ProcessCandle).Start()
        daily_sub = self.SubscribeCandles(DataType.TimeFrame(TimeSpan.FromDays(1)))
        daily_sub.Bind(self.ProcessDailyCandle).Start()

    def ProcessDailyCandle(self, candle):
        if candle.State != CandleStates.Finished: return
        if self._daily_initialized:
            self._pivot = (self._prev_high + self._prev_low + self._prev_close) / 3.0
        self._prev_high = float(candle.HighPrice)
        self._prev_low = float(candle.LowPrice)
        self._prev_close = float(candle.ClosePrice)
        self._daily_initialized = True

    def ProcessCandle(self, candle):
        if candle.State != CandleStates.Finished: return
        if self._pivot == 0.0: return
        ha_close = (float(candle.OpenPrice) + float(candle.HighPrice) + float(candle.LowPrice) + float(candle.ClosePrice)) / 4.0
        if not self._ha_initialized:
            self._ha_open = (float(candle.OpenPrice) + float(candle.ClosePrice)) / 2.0
            self._ha_close = ha_close
            self._ha_initialized = True
            return
        ha_open = (self._ha_open + self._ha_close) / 2.0
        self._ha_open = ha_open
        self._ha_close = ha_close
        is_bullish = ha_close > ha_open
        is_bearish = ha_close < ha_open
        direction = 1 if is_bullish else (-1 if is_bearish else 0)
        close = float(candle.ClosePrice)
        low = float(candle.LowPrice)
        high = float(candle.HighPrice)
        if is_bullish and self._previous_direction != 1 and close > self._pivot and self.Position <= 0:
            self.BuyMarket()
            self._entry_price = close
            self._stop_price = self._entry_price - float(self.StopLossPips) * self._step
            self._take_price = self._entry_price + float(self.TakeProfitPips) * self._step
            self._trailing_stop = self._stop_price
        elif is_bearish and self._previous_direction != -1 and close < self._pivot and self.Position >= 0:
            self.SellMarket()
            self._entry_price = close
            self._stop_price = self._entry_price + float(self.StopLossPips) * self._step
            self._take_price = self._entry_price - float(self.TakeProfitPips) * self._step
            self._trailing_stop = self._stop_price
        if self.Position > 0:
            if low <= self._stop_price or low <= self._trailing_stop:
                self.SellMarket()
            elif high >= self._take_price:
                self.SellMarket()
            elif self.TrailingStopPips > 0:
                new_stop = close - self._trailing_distance
                if new_stop > self._trailing_stop: self._trailing_stop = new_stop
        elif self.Position < 0:
            if high >= self._stop_price or high >= self._trailing_stop:
                self.BuyMarket()
            elif low <= self._take_price:
                self.BuyMarket()
            elif self.TrailingStopPips > 0:
                new_stop = close + self._trailing_distance
                if new_stop < self._trailing_stop: self._trailing_stop = new_stop
        self._previous_direction = direction

    def OnReseted(self):
        super(pivot_heiken_strategy, self).OnReseted()
        self._pivot = 0.0
        self._prev_high = 0.0
        self._prev_low = 0.0
        self._prev_close = 0.0
        self._daily_initialized = False
        self._ha_open = 0.0
        self._ha_close = 0.0
        self._ha_initialized = False
        self._entry_price = 0.0
        self._stop_price = 0.0
        self._take_price = 0.0
        self._trailing_stop = 0.0
        self._previous_direction = 0

    def CreateClone(self):
        return pivot_heiken_strategy()
