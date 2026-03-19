import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import HurstExponent, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class hurst_exponent_trend_strategy(Strategy):
    """
    Hurst Exponent Trend: trades when Hurst > threshold (trending market), exits otherwise.
    """

    def __init__(self):
        super(hurst_exponent_trend_strategy, self).__init__()
        self._hurst_period = self.Param("HurstPeriod", 100).SetDisplay("Hurst Period", "Hurst calculation period", "Indicators")
        self._ma_period = self.Param("MaPeriod", 20).SetDisplay("MA Period", "SMA period", "Indicators")
        self._threshold = self.Param("HurstThreshold", 0.55).SetDisplay("Threshold", "Hurst threshold", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Timeframe", "General")

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(hurst_exponent_trend_strategy, self).OnReseted()

    def OnStarted(self, time):
        super(hurst_exponent_trend_strategy, self).OnStarted(time)
        hurst = HurstExponent()
        hurst.Length = self._hurst_period.Value
        sma = SimpleMovingAverage()
        sma.Length = self._ma_period.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(hurst, sma, self._process_candle).Start()
        self.StartProtection(None, Unit(2, UnitTypes.Percent))
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sma)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, hurst_val, sma_val):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        hurst = float(hurst_val)
        sma = float(sma_val)
        close = float(candle.ClosePrice)
        trending = hurst > self._threshold.Value
        if trending:
            if close > sma and self.Position <= 0:
                self.BuyMarket()
            elif close < sma and self.Position >= 0:
                self.SellMarket()
        else:
            if self.Position > 0:
                self.SellMarket()
            elif self.Position < 0:
                self.BuyMarket()

    def CreateClone(self):
        return hurst_exponent_trend_strategy()
