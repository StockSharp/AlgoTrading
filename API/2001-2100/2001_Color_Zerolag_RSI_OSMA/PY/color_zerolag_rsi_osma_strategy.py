import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy

class color_zerolag_rsi_osma_strategy(Strategy):
    """
    Strategy based on the Color Zerolag RSI OSMA indicator.
    Uses RSI - 50 as OSMA and trades on direction changes.
    """

    def __init__(self):
        super(color_zerolag_rsi_osma_strategy, self).__init__()
        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("RSI Period", "RSI calculation period", "Indicator")
        self._smoothing = self.Param("Smoothing", 21) \
            .SetDisplay("Smoothing", "EMA smoothing period", "Indicator")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")

        self._prev_osma = 0.0
        self._prev_prev_osma = 0.0
        self._count = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(color_zerolag_rsi_osma_strategy, self).OnReseted()
        self._prev_osma = 0.0
        self._prev_prev_osma = 0.0
        self._count = 0

    def OnStarted2(self, time):
        super(color_zerolag_rsi_osma_strategy, self).OnStarted2(time)

        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_period.Value
        ema = ExponentialMovingAverage()
        ema.Length = self._smoothing.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(rsi, ema, self.on_process).Start()

    def on_process(self, candle, rsi_val, ema_val):
        if candle.State != CandleStates.Finished:
            return

        osma = rsi_val - 50.0
        self._count += 1

        if self._count < 3:
            self._prev_prev_osma = self._prev_osma
            self._prev_osma = osma
            return

        turn_up = self._prev_osma < self._prev_prev_osma and osma > self._prev_osma
        turn_down = self._prev_osma > self._prev_prev_osma and osma < self._prev_osma

        if turn_up and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif turn_down and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

        self._prev_prev_osma = self._prev_osma
        self._prev_osma = osma

    def CreateClone(self):
        return color_zerolag_rsi_osma_strategy()
