import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class rsi_reversion_strategy(Strategy):
    """
    Strategy based on RSI mean reversion.
    Buys when RSI crosses up from oversold zone, sells when RSI crosses down from overbought.
    Uses SMA as trend filter.
    """

    def __init__(self):
        super(rsi_reversion_strategy, self).__init__()
        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("RSI Period", "Period for RSI calculation", "Indicators")
        self._sma_period = self.Param("SmaPeriod", 50) \
            .SetDisplay("SMA Period", "Period for SMA trend filter", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._prev_rsi = 0.0
        self._has_prev_values = False
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(rsi_reversion_strategy, self).OnReseted()
        self._prev_rsi = 0.0
        self._has_prev_values = False
        self._cooldown = 0

    def OnStarted(self, time):
        super(rsi_reversion_strategy, self).OnStarted(time)

        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_period.Value
        sma = SimpleMovingAverage()
        sma.Length = self._sma_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(rsi, sma, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, rsi)
            self.DrawIndicator(area, sma)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, rsi_value, sma_value):
        if candle.State != CandleStates.Finished:
            return

        rv = float(rsi_value)
        sv = float(sma_value)
        if rv == 0 or sv == 0:
            return

        if not self._has_prev_values:
            self._has_prev_values = True
            self._prev_rsi = rv
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            self._prev_rsi = rv
            return

        if self._prev_rsi < 30 and rv >= 30 and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
            self._cooldown = 10
        elif self._prev_rsi > 70 and rv <= 70 and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
            self._cooldown = 10

        self._prev_rsi = rv

    def CreateClone(self):
        return rsi_reversion_strategy()
