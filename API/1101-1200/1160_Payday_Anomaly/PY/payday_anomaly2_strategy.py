import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class payday_anomaly2_strategy(Strategy):
    def __init__(self):
        super(payday_anomaly2_strategy, self).__init__()
        self._trade_1st = self.Param("Trade1st", True)
        self._trade_2nd = self.Param("Trade2nd", True)
        self._trade_16th = self.Param("Trade16th", True)
        self._trade_31st = self.Param("Trade31st", True)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))
        self._trade_opened = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(payday_anomaly2_strategy, self).OnReseted()
        self._trade_opened = False

    def OnStarted2(self, time):
        super(payday_anomaly2_strategy, self).OnStarted2(time)
        self._trade_opened = False
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.OnProcess).Start()

    def OnProcess(self, candle):
        if candle.State != CandleStates.Finished:
            return
        day = candle.OpenTime.Day
        is_target_day = (day == 1 and self._trade_1st.Value) \
            or (day == 2 and self._trade_2nd.Value) \
            or (day == 16 and self._trade_16th.Value) \
            or (day == 31 and self._trade_31st.Value)
        if is_target_day and not self._trade_opened and self.Position <= 0:
            self.BuyMarket()
            self._trade_opened = True
        elif not is_target_day and self._trade_opened and self.Position > 0:
            self.SellMarket()
            self._trade_opened = False

    def CreateClone(self):
        return payday_anomaly2_strategy()
