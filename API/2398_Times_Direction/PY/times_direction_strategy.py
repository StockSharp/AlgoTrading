import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, DayOfWeek
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class times_direction_strategy(Strategy):
    def __init__(self):
        super(times_direction_strategy, self).__init__()
        self._open_hour = self.Param("OpenHour", 2)
        self._close_hour = self.Param("CloseHour", 14)
        self._stop_loss = self.Param("StopLoss", 500.0)
        self._take_profit = self.Param("TakeProfit", 1000.0)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))
        self._entry_price = 0.0

    @property
    def OpenHour(self): return self._open_hour.Value
    @OpenHour.setter
    def OpenHour(self, v): self._open_hour.Value = v
    @property
    def CloseHour(self): return self._close_hour.Value
    @CloseHour.setter
    def CloseHour(self, v): self._close_hour.Value = v
    @property
    def StopLoss(self): return self._stop_loss.Value
    @StopLoss.setter
    def StopLoss(self, v): self._stop_loss.Value = v
    @property
    def TakeProfit(self): return self._take_profit.Value
    @TakeProfit.setter
    def TakeProfit(self, v): self._take_profit.Value = v
    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, v): self._candle_type.Value = v

    def OnStarted2(self, time):
        super(times_direction_strategy, self).OnStarted2(time)
        self._entry_price = 0.0
        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(self.ProcessCandle).Start()

    def ProcessCandle(self, candle):
        if candle.State != CandleStates.Finished: return
        hour = candle.OpenTime.Hour
        if self.Position == 0:
            if hour == self.OpenHour and candle.OpenTime.DayOfWeek == DayOfWeek.Monday:
                self._entry_price = float(candle.ClosePrice)
                self.BuyMarket()
        else:
            if hour == self.CloseHour and candle.OpenTime.DayOfWeek == DayOfWeek.Friday:
                self.SellMarket()
                self._entry_price = 0.0
                return
            if self._entry_price != 0.0 and self.Position > 0:
                sl = self._entry_price - float(self.StopLoss)
                tp = self._entry_price + float(self.TakeProfit)
                if float(candle.LowPrice) <= sl or float(candle.HighPrice) >= tp:
                    self.SellMarket()
                    self._entry_price = 0.0

    def OnReseted(self):
        super(times_direction_strategy, self).OnReseted()
        self._entry_price = 0.0

    def CreateClone(self):
        return times_direction_strategy()
