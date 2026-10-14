import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class customizable_btc_seasonality_strategy(Strategy):
    """
    Customizable BTC seasonality strategy.
    Opens a long during the EntryHour UTC hour and closes it during the ExitHour UTC hour.
    """

    def __init__(self):
        super(customizable_btc_seasonality_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._entry_hour = self.Param("EntryHour", 21) \
            .SetRange(0, 23) \
            .SetDisplay("Entry Hour", "UTC hour when the long is opened", "Time")
        self._exit_hour = self.Param("ExitHour", 23) \
            .SetRange(0, 23) \
            .SetDisplay("Exit Hour", "UTC hour when the long is closed", "Time")

    @property
    def candle_type(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.candle_type)]

    def OnStarted2(self, time):
        super(customizable_btc_seasonality_strategy, self).OnStarted2(time)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        hour = candle.OpenTime.ToUniversalTime().Hour

        if hour == self._exit_hour.Value and self.Position > 0:
            self.SellMarket(self.Position)
        elif hour == self._entry_hour.Value and self.Position == 0:
            self.BuyMarket()

    def CreateClone(self):
        return customizable_btc_seasonality_strategy()
