import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageDirectionalIndex
from StockSharp.Algo.Strategies import Strategy


class adx_expert_strategy(Strategy):
    def __init__(self):
        super(adx_expert_strategy, self).__init__()

        self._trade_volume = self.Param("TradeVolume", 0.1) \
            .SetDisplay("Trade volume", "Order volume used for entries", "Risk management")
        self._adx_period = self.Param("AdxPeriod", 14) \
            .SetDisplay("Trade volume", "Order volume used for entries", "Risk management")
        self._adx_threshold = self.Param("AdxThreshold", 20) \
            .SetDisplay("Trade volume", "Order volume used for entries", "Risk management")
        self._max_spread_points = self.Param("MaxSpreadPoints", 20) \
            .SetDisplay("Trade volume", "Order volume used for entries", "Risk management")
        self._stop_loss_points = self.Param("StopLossPoints", 200) \
            .SetDisplay("Trade volume", "Order volume used for entries", "Risk management")
        self._take_profit_points = self.Param("TakeProfitPoints", 400) \
            .SetDisplay("Trade volume", "Order volume used for entries", "Risk management")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(2) \
            .SetDisplay("Trade volume", "Order volume used for entries", "Risk management")

        self._adx = null!
        self._previous_plus_di = 0.0
        self._previous_minus_di = 0.0
        self._has_previous_di = False
        self._entry_price = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(adx_expert_strategy, self).OnReseted()
        self._adx = null!
        self._previous_plus_di = 0.0
        self._previous_minus_di = 0.0
        self._has_previous_di = False
        self._entry_price = 0.0

    def OnStarted(self, time):
        super(adx_expert_strategy, self).OnStarted(time)

        self.__adx = AverageDirectionalIndex()
        self.__adx.Length = self.adx_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return adx_expert_strategy()
