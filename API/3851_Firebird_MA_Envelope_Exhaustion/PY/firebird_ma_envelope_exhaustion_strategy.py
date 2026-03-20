import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import BollingerBands
from StockSharp.Algo.Strategies import Strategy


class firebird_ma_envelope_exhaustion_strategy(Strategy):
    def __init__(self):
        super(firebird_ma_envelope_exhaustion_strategy, self).__init__()

        self._bb_period = self.Param("BbPeriod", 10) \
            .SetDisplay("BB Period", "Bollinger Bands period", "Indicators")
        self._bb_width = self.Param("BbWidth", 2) \
            .SetDisplay("BB Period", "Bollinger Bands period", "Indicators")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(4) \
            .SetDisplay("BB Period", "Bollinger Bands period", "Indicators")

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(firebird_ma_envelope_exhaustion_strategy, self).OnReseted()
        pass

    def OnStarted(self, time):
        super(firebird_ma_envelope_exhaustion_strategy, self).OnStarted(time)

        self._bb = BollingerBands()
        self._bb.Length = self.bb_period
        self._bb.Width = self.bb_width

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(self._bb, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return firebird_ma_envelope_exhaustion_strategy()
