import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import StochasticOscillator, WilliamsR
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Messages import Unit, UnitTypes


class the_master_mind_reversal_strategy(Strategy):
    def __init__(self):
        super(the_master_mind_reversal_strategy, self).__init__()

        self._trade_volume = self.Param("TradeVolume", 1) \
            .SetDisplay("Volume", "Base order size", "Trading")
        self._stochastic_period = self.Param("StochasticPeriod", 100) \
            .SetDisplay("Volume", "Base order size", "Trading")
        self._k_period = self.Param("KPeriod", 3) \
            .SetDisplay("Volume", "Base order size", "Trading")
        self._d_period = self.Param("DPeriod", 3) \
            .SetDisplay("Volume", "Base order size", "Trading")
        self._williams_period = self.Param("WilliamsPeriod", 100) \
            .SetDisplay("Volume", "Base order size", "Trading")
        self._stochastic_buy_threshold = self.Param("StochasticBuyThreshold", 3) \
            .SetDisplay("Volume", "Base order size", "Trading")
        self._stochastic_sell_threshold = self.Param("StochasticSellThreshold", 97) \
            .SetDisplay("Volume", "Base order size", "Trading")
        self._williams_buy_level = self.Param("WilliamsBuyLevel", -99.5) \
            .SetDisplay("Volume", "Base order size", "Trading")
        self._williams_sell_level = self.Param("WilliamsSellLevel", -0.5) \
            .SetDisplay("Volume", "Base order size", "Trading")
        self._stop_loss = self.Param("StopLoss", 0) \
            .SetDisplay("Volume", "Base order size", "Trading")
        self._take_profit = self.Param("TakeProfit", 0) \
            .SetDisplay("Volume", "Base order size", "Trading")
        self._use_trailing_stop = self.Param("UseTrailingStop", False) \
            .SetDisplay("Volume", "Base order size", "Trading")
        self._trailing_stop = self.Param("TrailingStop", 0) \
            .SetDisplay("Volume", "Base order size", "Trading")
        self._trailing_step = self.Param("TrailingStep", 0) \
            .SetDisplay("Volume", "Base order size", "Trading")
        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(15) \
            .SetDisplay("Volume", "Base order size", "Trading")

        self._stochastic = null!
        self._williams = null!

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(the_master_mind_reversal_strategy, self).OnReseted()
        self._stochastic = null!
        self._williams = null!

    def OnStarted(self, time):
        super(the_master_mind_reversal_strategy, self).OnStarted(time)

        self.__williams = WilliamsR()
        self.__williams.Length = self.williams_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(_stochastic, self.__williams, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return the_master_mind_reversal_strategy()
