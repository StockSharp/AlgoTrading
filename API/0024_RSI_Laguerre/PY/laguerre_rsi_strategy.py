import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy

class laguerre_rsi_strategy(Strategy):
    """
    RSI Laguerre-style: longer RSI period, trades on oversold/overbought crossings.
    """

    def __init__(self):
        super(laguerre_rsi_strategy, self).__init__()
        self._rsi_period = self.Param("RsiPeriod", 10).SetDisplay("RSI Period", "RSI period", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Timeframe", "General")

        self._prev_rsi = 0.0
        self._has_prev = False
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(laguerre_rsi_strategy, self).OnReseted()
        self._prev_rsi = 0.0
        self._has_prev = False
        self._cooldown = 0

    def OnStarted2(self, time):
        super(laguerre_rsi_strategy, self).OnStarted2(time)
        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_period.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(rsi, self._process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, rsi)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, rsi_val):
        if candle.State != CandleStates.Finished:
            return
        rsi = float(rsi_val)
        if rsi == 0:
            return
        if not self._has_prev:
            self._has_prev = True
            self._prev_rsi = rsi
            return
        if self._cooldown > 0:
            self._cooldown -= 1
            self._prev_rsi = rsi
            return
        if self._prev_rsi < 30 and rsi >= 30 and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
            self._cooldown = 12
        elif self._prev_rsi > 70 and rsi <= 70 and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
            self._cooldown = 12
        self._prev_rsi = rsi

    def CreateClone(self):
        return laguerre_rsi_strategy()
