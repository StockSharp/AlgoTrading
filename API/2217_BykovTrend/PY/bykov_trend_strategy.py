import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import WilliamsR
from StockSharp.Algo.Strategies import Strategy


class bykov_trend_strategy(Strategy):
    def __init__(self):
        super(bykov_trend_strategy, self).__init__()
        self._risk = self.Param("Risk", 3) \
            .SetDisplay("Risk", "Risk parameter from original indicator", "Indicator")
        self._ssp = self.Param("Ssp", 9) \
            .SetDisplay("SSP", "Williams %R period", "Indicator")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe for indicator", "General")
        self._previous_uptrend = False

    @property
    def risk(self):
        return self._risk.Value

    @property
    def ssp(self):
        return self._ssp.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(bykov_trend_strategy, self).OnReseted()
        self._previous_uptrend = False

    def OnStarted2(self, time):
        super(bykov_trend_strategy, self).OnStarted2(time)
        wpr = WilliamsR()
        wpr.Length = self.ssp
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(wpr, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, wpr)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, wpr_value):
        if candle.State != CandleStates.Finished:
            return
        wpr_value = float(wpr_value)
        k = 33 - self.risk
        uptrend = self._previous_uptrend
        if wpr_value < -100 + k:
            uptrend = False
        elif wpr_value > -k:
            uptrend = True
        buy_signal = not self._previous_uptrend and uptrend
        sell_signal = self._previous_uptrend and not uptrend
        self._previous_uptrend = uptrend
        if buy_signal:
            self.BuyMarket()
        elif sell_signal:
            self.SellMarket()

    def CreateClone(self):
        return bykov_trend_strategy()
