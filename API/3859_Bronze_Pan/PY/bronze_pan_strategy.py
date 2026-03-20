import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import CommodityChannelIndex, Momentum
from StockSharp.Algo.Strategies import Strategy


class bronze_pan_strategy(Strategy):
    def __init__(self):
        super(bronze_pan_strategy, self).__init__()

        self._cci_period = self.Param("CciPeriod", 14) \
            .SetDisplay("CCI Period", "CCI lookback", "Indicators")
        self._cci_level = self.Param("CciLevel", 100) \
            .SetDisplay("CCI Period", "CCI lookback", "Indicators")
        self._momentum_period = self.Param("MomentumPeriod", 14) \
            .SetDisplay("CCI Period", "CCI lookback", "Indicators")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(4) \
            .SetDisplay("CCI Period", "CCI lookback", "Indicators")

        self._prev_cci = 0.0
        self._has_prev = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(bronze_pan_strategy, self).OnReseted()
        self._prev_cci = 0.0
        self._has_prev = False

    def OnStarted(self, time):
        super(bronze_pan_strategy, self).OnStarted(time)

        self._cci = CommodityChannelIndex()
        self._cci.Length = self.cci_period
        self._mom = Momentum()
        self._mom.Length = self.momentum_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._cci, self._mom, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return bronze_pan_strategy()
