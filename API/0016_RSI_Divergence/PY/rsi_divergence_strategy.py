import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy

class rsi_divergence_strategy(Strategy):
    """
    Strategy based on RSI divergence detection.
    Uses RSI overbought/oversold level crossings to generate reversal signals.
    """

    def __init__(self):
        super(rsi_divergence_strategy, self).__init__()
        self._rsi_period = self.Param("RsiPeriod", 14).SetDisplay("RSI Period", "Period for RSI calculation", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")

        self._prev_rsi = 0.0
        self._has_prev_values = False
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(rsi_divergence_strategy, self).OnReseted()
        self._prev_rsi = 0.0
        self._has_prev_values = False
        self._cooldown = 0

    def OnStarted(self, time):
        super(rsi_divergence_strategy, self).OnStarted(time)

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

        rv = float(rsi_val)
        if rv == 0:
            return

        if not self._has_prev_values:
            self._has_prev_values = True
            self._prev_rsi = rv
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            self._prev_rsi = rv
            return

        # RSI crosses from oversold into neutral zone - buy
        if self._prev_rsi < 30 and rv >= 30 and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
            self._cooldown = 15
        # RSI crosses from overbought into neutral zone - sell
        elif self._prev_rsi > 70 and rv <= 70 and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
            self._cooldown = 15

        self._prev_rsi = rv

    def CreateClone(self):
        return rsi_divergence_strategy()
