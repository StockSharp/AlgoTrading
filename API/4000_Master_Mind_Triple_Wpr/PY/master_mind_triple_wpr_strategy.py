import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import WilliamsR
from StockSharp.Algo.Strategies import Strategy


class master_mind_triple_wpr_strategy(Strategy):
    def __init__(self):
        super(master_mind_triple_wpr_strategy, self).__init__()

        self._trade_volume = self.Param("TradeVolume", 1) \
            .SetDisplay("Trade Volume", "Target net position volume", "Trading")
        self._oversold_level = self.Param("OversoldLevel", -99.99) \
            .SetDisplay("Trade Volume", "Target net position volume", "Trading")
        self._overbought_level = self.Param("OverboughtLevel", -0.01) \
            .SetDisplay("Trade Volume", "Target net position volume", "Trading")
        self._stop_loss_steps = self.Param("StopLossSteps", 2000) \
            .SetDisplay("Trade Volume", "Target net position volume", "Trading")
        self._take_profit_steps = self.Param("TakeProfitSteps", 0) \
            .SetDisplay("Trade Volume", "Target net position volume", "Trading")
        self._trailing_stop_steps = self.Param("TrailingStopSteps", 0) \
            .SetDisplay("Trade Volume", "Target net position volume", "Trading")
        self._trailing_step_steps = self.Param("TrailingStepSteps", 1) \
            .SetDisplay("Trade Volume", "Target net position volume", "Trading")
        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(15) \
            .SetDisplay("Trade Volume", "Target net position volume", "Trading")

        self._wpr26 = null!
        self._wpr27 = null!
        self._wpr29 = null!
        self._wpr30 = null!
        self._long_entry_price = None
        self._short_entry_price = None
        self._long_stop_price = None
        self._short_stop_price = None
        self._long_take_price = None
        self._short_take_price = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(master_mind_triple_wpr_strategy, self).OnReseted()
        self._wpr26 = null!
        self._wpr27 = null!
        self._wpr29 = null!
        self._wpr30 = null!
        self._long_entry_price = None
        self._short_entry_price = None
        self._long_stop_price = None
        self._short_stop_price = None
        self._long_take_price = None
        self._short_take_price = None

    def OnStarted(self, time):
        super(master_mind_triple_wpr_strategy, self).OnStarted(time)
        self.StartProtection(None, None)

        self.__wpr26 = WilliamsR()
        self.__wpr26.Length = 26
        self.__wpr27 = WilliamsR()
        self.__wpr27.Length = 27
        self.__wpr29 = WilliamsR()
        self.__wpr29.Length = 29
        self.__wpr30 = WilliamsR()
        self.__wpr30.Length = 30

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.__wpr26, self.__wpr27, self.__wpr29, self.__wpr30, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return master_mind_triple_wpr_strategy()
