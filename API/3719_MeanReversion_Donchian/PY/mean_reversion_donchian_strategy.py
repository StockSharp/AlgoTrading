import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Messages import Sides


class mean_reversion_donchian_strategy(Strategy):
    def __init__(self):
        super(mean_reversion_donchian_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(15) \
            .SetDisplay("Candle Type", "Type of candles to analyze", "General")
        self._lookback_period = self.Param("LookbackPeriod", 200) \
            .SetDisplay("Candle Type", "Type of candles to analyze", "General")
        self._risk_percent = self.Param("RiskPercent", 1) \
            .SetDisplay("Candle Type", "Type of candles to analyze", "General")

        self._donchian = null!
        self._stop_price = None
        self._take_profit_price = None
        self._active_side = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(mean_reversion_donchian_strategy, self).OnReseted()
        self._donchian = null!
        self._stop_price = None
        self._take_profit_price = None
        self._active_side = None

    def OnStarted(self, time):
        super(mean_reversion_donchian_strategy, self).OnStarted(time)

        self.__donchian = DonchianChannels()
        self.__donchian.Length = self.lookback_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(self.__donchian, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return mean_reversion_donchian_strategy()
