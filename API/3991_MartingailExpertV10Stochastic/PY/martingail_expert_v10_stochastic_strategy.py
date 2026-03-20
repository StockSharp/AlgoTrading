import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import StochasticOscillator
from StockSharp.Algo.Strategies import Strategy


class martingail_expert_v10_stochastic_strategy(Strategy):
    def __init__(self):
        super(martingail_expert_v10_stochastic_strategy, self).__init__()

        self._step_points = self.Param("StepPoints", 500) \
            .SetDisplay("Step", "Price step in points before averaging", "Martingale")
        self._step_mode = self.Param("StepMode", 0) \
            .SetDisplay("Step", "Price step in points before averaging", "Martingale")
        self._profit_factor_points = self.Param("ProfitFactorPoints", 300) \
            .SetDisplay("Step", "Price step in points before averaging", "Martingale")
        self._multiplier = self.Param("Multiplier", 1.5) \
            .SetDisplay("Step", "Price step in points before averaging", "Martingale")
        self._k_period = self.Param("KPeriod", 14) \
            .SetDisplay("Step", "Price step in points before averaging", "Martingale")
        self._d_period = self.Param("DPeriod", 3) \
            .SetDisplay("Step", "Price step in points before averaging", "Martingale")
        self._zone_buy = self.Param("ZoneBuy", 50) \
            .SetDisplay("Step", "Price step in points before averaging", "Martingale")
        self._zone_sell = self.Param("ZoneSell", 50) \
            .SetDisplay("Step", "Price step in points before averaging", "Martingale")
        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(10) \
            .SetDisplay("Step", "Price step in points before averaging", "Martingale")

        self._stochastic = None
        self._point_size = 0.0
        self._prev_k = None
        self._prev_d = None
        self._buy_last_price = 0.0
        self._buy_last_volume = 0.0
        self._buy_total_volume = 0.0
        self._buy_weighted_sum = 0.0
        self._buy_order_count = 0.0
        self._buy_take_profit = 0.0
        self._sell_last_price = 0.0
        self._sell_last_volume = 0.0
        self._sell_total_volume = 0.0
        self._sell_weighted_sum = 0.0
        self._sell_order_count = 0.0
        self._sell_take_profit = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(martingail_expert_v10_stochastic_strategy, self).OnReseted()
        self._stochastic = None
        self._point_size = 0.0
        self._prev_k = None
        self._prev_d = None
        self._buy_last_price = 0.0
        self._buy_last_volume = 0.0
        self._buy_total_volume = 0.0
        self._buy_weighted_sum = 0.0
        self._buy_order_count = 0.0
        self._buy_take_profit = 0.0
        self._sell_last_price = 0.0
        self._sell_last_volume = 0.0
        self._sell_total_volume = 0.0
        self._sell_weighted_sum = 0.0
        self._sell_order_count = 0.0
        self._sell_take_profit = 0.0

    def OnStarted(self, time):
        super(martingail_expert_v10_stochastic_strategy, self).OnStarted(time)


        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(_stochastic, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return martingail_expert_v10_stochastic_strategy()
