import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Highest, Lowest
from StockSharp.Algo.Strategies import Strategy


class silver_trend_strategy(Strategy):
    def __init__(self):
        super(silver_trend_strategy, self).__init__()
        self._ssp = self.Param("Ssp", 9) \
            .SetDisplay("SSP", "Lookback length for price channel", "Indicator")
        self._risk = self.Param("Risk", 3) \
            .SetDisplay("Risk", "Risk factor used to tighten the channel", "Indicator")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe for indicator", "General")
        self._uptrend = None

    @property
    def ssp(self):
        return self._ssp.Value

    @property
    def risk(self):
        return self._risk.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(silver_trend_strategy, self).OnReseted()
        self._uptrend = None

    def OnStarted(self, time):
        super(silver_trend_strategy, self).OnStarted(time)
        highest = Highest()
        highest.Length = self.ssp
        lowest = Lowest()
        lowest.Length = self.ssp
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(highest, lowest, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, max_high, min_low):
        if candle.State != CandleStates.Finished:
            return
        max_high = float(max_high)
        min_low = float(min_low)
        k = 33 - self.risk
        smin = min_low + (max_high - min_low) * k / 100.0
        smax = max_high - (max_high - min_low) * k / 100.0
        close = float(candle.ClosePrice)
        uptrend = self._uptrend if self._uptrend is not None else False
        if close < smin:
            uptrend = False
        elif close > smax:
            uptrend = True
        reversed_trend = self._uptrend is not None and uptrend != self._uptrend
        if reversed_trend:
            if uptrend and self.Position <= 0:
                self.BuyMarket()
            elif not uptrend and self.Position >= 0:
                self.SellMarket()
        self._uptrend = uptrend

    def CreateClone(self):
        return silver_trend_strategy()
