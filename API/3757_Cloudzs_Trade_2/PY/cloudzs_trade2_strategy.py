import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import StochasticOscillator
from StockSharp.Algo.Strategies import Strategy


class cloudzs_trade2_strategy(Strategy):
    def __init__(self):
        super(cloudzs_trade2_strategy, self).__init__()

        self._lot_splitter = self.Param("LotSplitter", 0.1) \
            .SetDisplay("Lot Splitter", "Coefficient used to derive order size", "Trading")
        self._max_volume = self.Param("MaxVolume", 0) \
            .SetDisplay("Lot Splitter", "Coefficient used to derive order size", "Trading")
        self._take_profit_offset = self.Param("TakeProfitOffset", 0) \
            .SetDisplay("Lot Splitter", "Coefficient used to derive order size", "Trading")
        self._trailing_stop_offset = self.Param("TrailingStopOffset", 0.01) \
            .SetDisplay("Lot Splitter", "Coefficient used to derive order size", "Trading")
        self._stop_loss_offset = self.Param("StopLossOffset", 0.05) \
            .SetDisplay("Lot Splitter", "Coefficient used to derive order size", "Trading")
        self._min_profit_offset = self.Param("MinProfitOffset", 0) \
            .SetDisplay("Lot Splitter", "Coefficient used to derive order size", "Trading")
        self._profit_points_offset = self.Param("ProfitPointsOffset", 0) \
            .SetDisplay("Lot Splitter", "Coefficient used to derive order size", "Trading")
        self._k_period = self.Param("KPeriod", 8) \
            .SetDisplay("Lot Splitter", "Coefficient used to derive order size", "Trading")
        self._d_period = self.Param("DPeriod", 8) \
            .SetDisplay("Lot Splitter", "Coefficient used to derive order size", "Trading")
        self._slowing_period = self.Param("SlowingPeriod", 4) \
            .SetDisplay("Lot Splitter", "Coefficient used to derive order size", "Trading")
        self._method = self.Param("Method", 3) \
            .SetDisplay("Lot Splitter", "Coefficient used to derive order size", "Trading")
        self._price_mode = self.Param("PriceMode", 1) \
            .SetDisplay("Lot Splitter", "Coefficient used to derive order size", "Trading")
        self._use_stochastic_condition = self.Param("UseStochasticCondition", True) \
            .SetDisplay("Lot Splitter", "Coefficient used to derive order size", "Trading")
        self._use_fractal_condition = self.Param("UseFractalCondition", True) \
            .SetDisplay("Lot Splitter", "Coefficient used to derive order size", "Trading")
        self._close_on_opposite = self.Param("CloseOnOpposite", True) \
            .SetDisplay("Lot Splitter", "Coefficient used to derive order size", "Trading")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(4) \
            .SetDisplay("Lot Splitter", "Coefficient used to derive order size", "Trading")

        self._stochastic = None
        self._previous_k = 0.0
        self._previous_d = 0.0
        self._last_k = 0.0
        self._last_d = 0.0
        self._has_previous = False
        self._has_last = False
        self._high1 = 0.0
        self._high2 = 0.0
        self._high3 = 0.0
        self._high4 = 0.0
        self._high5 = 0.0
        self._low1 = 0.0
        self._low2 = 0.0
        self._low3 = 0.0
        self._low4 = 0.0
        self._low5 = 0.0
        self._latest_fractal = None
        self._previous_fractal = None
        self._fractal_seed_count = 0.0
        self._stop_price = None
        self._take_profit_price = None
        self._entry_price = 0.0
        self._max_favorable_move = 0.0
        self._last_exit_date = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(cloudzs_trade2_strategy, self).OnReseted()
        self._stochastic = None
        self._previous_k = 0.0
        self._previous_d = 0.0
        self._last_k = 0.0
        self._last_d = 0.0
        self._has_previous = False
        self._has_last = False
        self._high1 = 0.0
        self._high2 = 0.0
        self._high3 = 0.0
        self._high4 = 0.0
        self._high5 = 0.0
        self._low1 = 0.0
        self._low2 = 0.0
        self._low3 = 0.0
        self._low4 = 0.0
        self._low5 = 0.0
        self._latest_fractal = None
        self._previous_fractal = None
        self._fractal_seed_count = 0.0
        self._stop_price = None
        self._take_profit_price = None
        self._entry_price = 0.0
        self._max_favorable_move = 0.0
        self._last_exit_date = None

    def OnStarted(self, time):
        super(cloudzs_trade2_strategy, self).OnStarted(time)


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
        return cloudzs_trade2_strategy()
