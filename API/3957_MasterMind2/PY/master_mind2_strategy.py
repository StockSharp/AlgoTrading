import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import StochasticOscillator, WilliamsR
from StockSharp.Algo.Strategies import Strategy


class master_mind2_strategy(Strategy):
    def __init__(self):
        super(master_mind2_strategy, self).__init__()

        self._trade_volume = self.Param("TradeVolume", 0.1) \
            .SetDisplay("Trade Volume", "Trade volume in contracts", "General")
        self._stochastic_period = self.Param("StochasticPeriod", 100) \
            .SetDisplay("Trade Volume", "Trade volume in contracts", "General")
        self._stochastic_k = self.Param("StochasticK", 3) \
            .SetDisplay("Trade Volume", "Trade volume in contracts", "General")
        self._stochastic_d = self.Param("StochasticD", 3) \
            .SetDisplay("Trade Volume", "Trade volume in contracts", "General")
        self._williams_period = self.Param("WilliamsPeriod", 100) \
            .SetDisplay("Trade Volume", "Trade volume in contracts", "General")
        self._stop_loss_points = self.Param("StopLossPoints", 2000) \
            .SetDisplay("Trade Volume", "Trade volume in contracts", "General")
        self._take_profit_points = self.Param("TakeProfitPoints", 0) \
            .SetDisplay("Trade Volume", "Trade volume in contracts", "General")
        self._trailing_stop_points = self.Param("TrailingStopPoints", 0) \
            .SetDisplay("Trade Volume", "Trade volume in contracts", "General")
        self._trailing_step_points = self.Param("TrailingStepPoints", 1) \
            .SetDisplay("Trade Volume", "Trade volume in contracts", "General")
        self._break_even_points = self.Param("BreakEvenPoints", 0) \
            .SetDisplay("Trade Volume", "Trade volume in contracts", "General")
        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(30) \
            .SetDisplay("Trade Volume", "Trade volume in contracts", "General")

        self._entry_price = 0.0
        self._stop_price = 0.0
        self._take_profit_price = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(master_mind2_strategy, self).OnReseted()
        self._entry_price = 0.0
        self._stop_price = 0.0
        self._take_profit_price = 0.0

    def OnStarted(self, time):
        super(master_mind2_strategy, self).OnStarted(time)

        self._williams = WilliamsR()
        self._williams.Length = self.williams_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(stochastic, self._williams, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return master_mind2_strategy()
