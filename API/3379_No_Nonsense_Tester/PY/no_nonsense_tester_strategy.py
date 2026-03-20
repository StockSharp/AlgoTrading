import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import CommodityChannelIndex, ExponentialMovingAverage, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class no_nonsense_tester_strategy(Strategy):
    def __init__(self):
        super(no_nonsense_tester_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(15) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._ema_period = self.Param("EmaPeriod", 25) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._cci_period = self.Param("CciPeriod", 14) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")

        self._was_bullish = False
        self._has_prev_signal = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(no_nonsense_tester_strategy, self).OnReseted()
        self._was_bullish = False
        self._has_prev_signal = False

    def OnStarted(self, time):
        super(no_nonsense_tester_strategy, self).OnStarted(time)

        self._ema = ExponentialMovingAverage()
        self._ema.Length = self.ema_period
        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = self.rsi_period
        self._cci = CommodityChannelIndex()
        self._cci.Length = self.cci_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._ema, self._rsi, self._cci, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return no_nonsense_tester_strategy()
