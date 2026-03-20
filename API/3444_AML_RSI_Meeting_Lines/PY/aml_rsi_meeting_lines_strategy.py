import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class aml_rsi_meeting_lines_strategy(Strategy):
    def __init__(self):
        super(aml_rsi_meeting_lines_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(5) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._rsi_low = self.Param("RsiLow", 40) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._rsi_high = self.Param("RsiHigh", 60) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")

        self._prev_candle = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(aml_rsi_meeting_lines_strategy, self).OnReseted()
        self._prev_candle = None

    def OnStarted(self, time):
        super(aml_rsi_meeting_lines_strategy, self).OnStarted(time)

        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = self.rsi_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._rsi, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return aml_rsi_meeting_lines_strategy()
