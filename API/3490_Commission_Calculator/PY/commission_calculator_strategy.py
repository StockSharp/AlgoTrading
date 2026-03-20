import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import CommodityChannelIndex
from StockSharp.Algo.Strategies import Strategy


class commission_calculator_strategy(Strategy):
    def __init__(self):
        super(commission_calculator_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(60) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._period = self.Param("Period", 30) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._signal_cooldown_candles = self.Param("SignalCooldownCandles", 4) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")

        self._prev_cci = 0.0
        self._candles_since_trade = 0.0
        self._has_prev = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(commission_calculator_strategy, self).OnReseted()
        self._prev_cci = 0.0
        self._candles_since_trade = 0.0
        self._has_prev = False

    def OnStarted(self, time):
        super(commission_calculator_strategy, self).OnStarted(time)

        self._cci = CommodityChannelIndex()
        self._cci.Length = self.period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._cci, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return commission_calculator_strategy()
