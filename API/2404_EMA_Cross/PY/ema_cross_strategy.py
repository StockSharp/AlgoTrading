import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage as EMA
from StockSharp.Algo.Strategies import Strategy


class ema_cross_strategy(Strategy):
    def __init__(self):
        super(ema_cross_strategy, self).__init__()
        self._short_length = self.Param("ShortLength", 9)
        self._long_length = self.Param("LongLength", 45)
        self._take_profit = self.Param("TakeProfit", 25.0)
        self._stop_loss = self.Param("StopLoss", 105.0)
        self._trailing_stop = self.Param("TrailingStop", 20.0)
        self._reverse = self.Param("Reverse", False)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4)))
        self._entry_price = 0.0
        self._trail_price = 0.0
        self._is_long = False
        self._last_direction = 0

    @property
    def ShortLength(self): return self._short_length.Value
    @ShortLength.setter
    def ShortLength(self, v): self._short_length.Value = v
    @property
    def LongLength(self): return self._long_length.Value
    @LongLength.setter
    def LongLength(self, v): self._long_length.Value = v
    @property
    def TakeProfit(self): return self._take_profit.Value
    @TakeProfit.setter
    def TakeProfit(self, v): self._take_profit.Value = v
    @property
    def StopLoss(self): return self._stop_loss.Value
    @StopLoss.setter
    def StopLoss(self, v): self._stop_loss.Value = v
    @property
    def TrailingStop(self): return self._trailing_stop.Value
    @TrailingStop.setter
    def TrailingStop(self, v): self._trailing_stop.Value = v
    @property
    def Reverse(self): return self._reverse.Value
    @Reverse.setter
    def Reverse(self, v): self._reverse.Value = v
    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, v): self._candle_type.Value = v

    def OnStarted(self, time):
        super(ema_cross_strategy, self).OnStarted(time)
        fast_ema = EMA()
        fast_ema.Length = self.ShortLength
        slow_ema = EMA()
        slow_ema.Length = self.LongLength
        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(fast_ema, slow_ema, self.ProcessCandle).Start()

    def ProcessCandle(self, candle, fast, slow):
        if candle.State != CandleStates.Finished:
            return
        f = float(fast)
        s = float(slow)
        cross = self._get_cross(f, s) if self.Reverse else self._get_cross(s, f)
        if self.Position <= 0 and cross == 1:
            self._entry_price = float(candle.ClosePrice)
            self._trail_price = 0.0
            self._is_long = True
            self.BuyMarket()
        elif self.Position >= 0 and cross == 2:
            self._entry_price = float(candle.ClosePrice)
            self._trail_price = 0.0
            self._is_long = False
            self.SellMarket()
        if self.Position != 0:
            self._manage_position(candle)

    def _get_cross(self, line1, line2):
        d = 1 if line1 > line2 else 2
        if self._last_direction == 0:
            self._last_direction = d
            return 0
        if d != self._last_direction:
            self._last_direction = d
            return d
        return 0

    def _manage_position(self, candle):
        close = float(candle.ClosePrice)
        tp = float(self.TakeProfit)
        sl = float(self.StopLoss)
        ts = float(self.TrailingStop)
        if self._is_long:
            if tp > 0 and close >= self._entry_price + tp:
                self.SellMarket(); return
            if sl > 0 and close <= self._entry_price - sl:
                self.SellMarket(); return
            if ts > 0:
                ns = close - ts
                if self._trail_price < ns:
                    self._trail_price = ns
                if self._trail_price > 0 and close <= self._trail_price:
                    self.SellMarket()
        else:
            if tp > 0 and close <= self._entry_price - tp:
                self.BuyMarket(); return
            if sl > 0 and close >= self._entry_price + sl:
                self.BuyMarket(); return
            if ts > 0:
                ns = close + ts
                if self._trail_price == 0 or self._trail_price > ns:
                    self._trail_price = ns
                if close >= self._trail_price:
                    self.BuyMarket()

    def OnReseted(self):
        super(ema_cross_strategy, self).OnReseted()
        self._entry_price = 0.0
        self._trail_price = 0.0
        self._is_long = False
        self._last_direction = 0

    def CreateClone(self):
        return ema_cross_strategy()
