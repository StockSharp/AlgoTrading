import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class opening_closing_on_time_strategy(Strategy):
    def __init__(self):
        super(opening_closing_on_time_strategy, self).__init__()

        self._open_time = self.Param("OpenTime", TimeSpan(0, 0, 0))
        self._close_time = self.Param("CloseTime", TimeSpan(12, 0, 0))
        self._is_buy = self.Param("IsBuy", True)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))

        self._position_opened = False

    @property
    def OpenTime(self):
        return self._open_time.Value

    @OpenTime.setter
    def OpenTime(self, value):
        self._open_time.Value = value

    @property
    def CloseTime(self):
        return self._close_time.Value

    @CloseTime.setter
    def CloseTime(self, value):
        self._close_time.Value = value

    @property
    def IsBuy(self):
        return self._is_buy.Value

    @IsBuy.setter
    def IsBuy(self, value):
        self._is_buy.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted2(self, time):
        super(opening_closing_on_time_strategy, self).OnStarted2(time)

        self._position_opened = self.Position != 0

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.ProcessCandle).Start()

    def ProcessCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        t = candle.OpenTime.TimeOfDay

        if not self._position_opened and t >= self.OpenTime and t < self.CloseTime:
            if self.IsBuy:
                self.BuyMarket()
            else:
                self.SellMarket()
            self._position_opened = True
            return

        if self._position_opened and t >= self.CloseTime:
            if self.Position > 0:
                self.SellMarket()
            elif self.Position < 0:
                self.BuyMarket()
            self._position_opened = False

    def OnReseted(self):
        super(opening_closing_on_time_strategy, self).OnReseted()
        self._position_opened = False

    def CreateClone(self):
        return opening_closing_on_time_strategy()
