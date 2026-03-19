import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import CandleStates
from StockSharp.Algo.Indicators import BollingerBands
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class alerting_system_strategy(Strategy):
    """
    Alerting System strategy: Bollinger Band breakout.
    Buys when price crosses below lower band, sells when above upper band.
    """

    def __init__(self):
        super(alerting_system_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")

        self._bb_period = self.Param("BbPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("BB Period", "Bollinger Bands period", "Indicators")

        self._bb_width = self.Param("BbWidth", 2.0) \
            .SetDisplay("BB Width", "Bollinger Bands width multiplier", "Indicators")

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def BbPeriod(self):
        return self._bb_period.Value

    @BbPeriod.setter
    def BbPeriod(self, value):
        self._bb_period.Value = value

    @property
    def BbWidth(self):
        return self._bb_width.Value

    @BbWidth.setter
    def BbWidth(self, value):
        self._bb_width.Value = value

    def OnStarted(self, time):
        super(alerting_system_strategy, self).OnStarted(time)

        bb = BollingerBands()
        bb.Length = self.BbPeriod
        bb.Width = self.BbWidth

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(bb, self.ProcessCandle).Start()

    def ProcessCandle(self, candle, bb_value):
        if candle.State != CandleStates.Finished:
            return

        if not bb_value.IsFinal:
            return

        upper = get_bb_upper(bb_value)
        lower = get_bb_lower(bb_value)

        if upper is None or lower is None:
            return

        close = float(candle.ClosePrice)

        # Mean reversion: buy at lower band, sell at upper band
        if close < lower and self.Position <= 0:
            self.BuyMarket()
        elif close > upper and self.Position >= 0:
            self.SellMarket()

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return alerting_system_strategy()
