import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex, SmoothedMovingAverage
from StockSharp.Algo.Strategies import Strategy


class improve_ma_rsi_hedge_strategy(Strategy):
    def __init__(self):
        super(improve_ma_rsi_hedge_strategy, self).__init__()

        self._profit_target = self.Param("ProfitTarget", 50) \
            .SetDisplay("Profit Target", "Combined profit target across both legs", "Risk")
        self._hedge_security = self.Param("HedgeSecurity", None) \
            .SetDisplay("Profit Target", "Combined profit target across both legs", "Risk")
        self._fast_period = self.Param("FastMaPeriod", 8) \
            .SetDisplay("Profit Target", "Combined profit target across both legs", "Risk")
        self._slow_period = self.Param("SlowMaPeriod", 21) \
            .SetDisplay("Profit Target", "Combined profit target across both legs", "Risk")
        self._rsi_period = self.Param("RsiPeriod", 21) \
            .SetDisplay("Profit Target", "Combined profit target across both legs", "Risk")
        self._oversold_level = self.Param("OversoldLevel", 30) \
            .SetDisplay("Profit Target", "Combined profit target across both legs", "Risk")
        self._overbought_level = self.Param("OverboughtLevel", 70) \
            .SetDisplay("Profit Target", "Combined profit target across both legs", "Risk")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(1) \
            .SetDisplay("Profit Target", "Combined profit target across both legs", "Risk")

        self._fast_ma = null!
        self._slow_ma = null!
        self._rsi = null!
        self._base_last_close = 0.0
        self._hedge_last_close = 0.0
        self._base_entry_price = 0.0
        self._hedge_entry_price = 0.0
        self._has_base_close = False
        self._has_hedge_close = False
        self._pair_direction = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(improve_ma_rsi_hedge_strategy, self).OnReseted()
        self._fast_ma = null!
        self._slow_ma = null!
        self._rsi = null!
        self._base_last_close = 0.0
        self._hedge_last_close = 0.0
        self._base_entry_price = 0.0
        self._hedge_entry_price = 0.0
        self._has_base_close = False
        self._has_hedge_close = False
        self._pair_direction = 0.0

    def OnStarted(self, time):
        super(improve_ma_rsi_hedge_strategy, self).OnStarted(time)

        self.__fast_ma = SmoothedMovingAverage()
        self.__fast_ma.Length = self.fast_ma_period
        self.__slow_ma = SmoothedMovingAverage()
        self.__slow_ma.Length = self.slow_ma_period
        self.__rsi = RelativeStrengthIndex()
        self.__rsi.Length = self.rsi_period

        base_subscription = self.SubscribeCandles(self.candle_type)
        base_subscription.Bind(self.__fast_ma, self.__slow_ma, self.__rsi, self._process_candle).Start()

        hedge_subscription = self.SubscribeCandles(self.candle_type, false, HedgeSecurity)
        hedge_subscription.Bind(self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return improve_ma_rsi_hedge_strategy()
