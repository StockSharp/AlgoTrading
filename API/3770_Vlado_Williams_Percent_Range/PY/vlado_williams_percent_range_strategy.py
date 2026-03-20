import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import WilliamsR
from StockSharp.Algo.Strategies import Strategy


class vlado_williams_percent_range_strategy(Strategy):
    def __init__(self):
        super(vlado_williams_percent_range_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(4) \
            .SetDisplay("Candle Type", "Primary timeframe for the strategy", "General")
        self._wpr_length = self.Param("WprLength", 100) \
            .SetDisplay("Candle Type", "Primary timeframe for the strategy", "General")
        self._wpr_level = self.Param("WprLevel", -50) \
            .SetDisplay("Candle Type", "Primary timeframe for the strategy", "General")
        self._use_risk_money_management = self.Param("UseRiskMoneyManagement", False) \
            .SetDisplay("Candle Type", "Primary timeframe for the strategy", "General")
        self._maximum_risk_percent = self.Param("MaximumRiskPercent", 10) \
            .SetDisplay("Candle Type", "Primary timeframe for the strategy", "General")

        self._buy_signal = False
        self._sell_signal = False
        self._last_signal = 0.0
        self._williams_r = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(vlado_williams_percent_range_strategy, self).OnReseted()
        self._buy_signal = False
        self._sell_signal = False
        self._last_signal = 0.0
        self._williams_r = None

    def OnStarted(self, time):
        super(vlado_williams_percent_range_strategy, self).OnStarted(time)

        self.__williams_r = WilliamsR()
        self.__williams_r.Length = self.wpr_length

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.__williams_r, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return vlado_williams_percent_range_strategy()
