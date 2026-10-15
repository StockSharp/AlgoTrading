import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import BollingerBands
from StockSharp.Algo.Strategies import Strategy


class alerting_system_strategy(Strategy):
    def __init__(self):
        super(alerting_system_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._bb_period = self.Param("BbPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("BB Period", "Bollinger Bands period", "Indicators")
        self._bb_width = self.Param("BbWidth", 2.0) \
            .SetDisplay("BB Width", "Bollinger Bands width multiplier", "Indicators")

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnStarted2(self, time):
        super(alerting_system_strategy, self).OnStarted2(time)

        self._bb = BollingerBands()
        self._bb.Length = self._bb_period.Value
        self._bb.Width = self._bb_width.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(self._bb, self._process_candle).Start()

    def _process_candle(self, candle, bb_value):
        if candle.State != CandleStates.Finished:
            return

        if not bb_value.IsFinal:
            return

        upper = bb_value.UpBand
        lower = bb_value.LowBand

        if upper is None or lower is None:
            return

        close = float(candle.ClosePrice)
        upper_val = float(upper)
        lower_val = float(lower)

        if close < lower_val and self.Position <= 0:
            self.BuyMarket()
        elif close > upper_val and self.Position >= 0:
            self.SellMarket()

    def CreateClone(self):
        return alerting_system_strategy()
