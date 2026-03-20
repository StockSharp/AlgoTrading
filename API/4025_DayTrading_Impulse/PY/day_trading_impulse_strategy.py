import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Momentum, MovingAverageConvergenceDivergenceSignal, ParabolicSar, StochasticOscillator
from StockSharp.Algo.Strategies import Strategy


class day_trading_impulse_strategy(Strategy):
    def __init__(self):
        super(day_trading_impulse_strategy, self).__init__()

        self._lot_size = self.Param("LotSize", 1) \
            .SetDisplay("Order Volume", "Trade volume used for each market entry", "Trading")
        self._trailing_stop_points = self.Param("TrailingStopPoints", 15) \
            .SetDisplay("Order Volume", "Trade volume used for each market entry", "Trading")
        self._take_profit_points = self.Param("TakeProfitPoints", 20) \
            .SetDisplay("Order Volume", "Trade volume used for each market entry", "Trading")
        self._stop_loss_points = self.Param("StopLossPoints", 0) \
            .SetDisplay("Order Volume", "Trade volume used for each market entry", "Trading")
        self._slippage_points = self.Param("SlippagePoints", 3) \
            .SetDisplay("Order Volume", "Trade volume used for each market entry", "Trading")
        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(5) \
            .SetDisplay("Order Volume", "Trade volume used for each market entry", "Trading")
        self._macd_fast_period = self.Param("MacdFastPeriod", 12) \
            .SetDisplay("Order Volume", "Trade volume used for each market entry", "Trading")
        self._macd_slow_period = self.Param("MacdSlowPeriod", 26) \
            .SetDisplay("Order Volume", "Trade volume used for each market entry", "Trading")
        self._macd_signal_period = self.Param("MacdSignalPeriod", 9) \
            .SetDisplay("Order Volume", "Trade volume used for each market entry", "Trading")
        self._stochastic_length = self.Param("StochasticLength", 5) \
            .SetDisplay("Order Volume", "Trade volume used for each market entry", "Trading")
        self._stochastic_signal = self.Param("StochasticSignal", 3) \
            .SetDisplay("Order Volume", "Trade volume used for each market entry", "Trading")
        self._stochastic_slow = self.Param("StochasticSlow", 3) \
            .SetDisplay("Order Volume", "Trade volume used for each market entry", "Trading")
        self._stochastic_buy_threshold = self.Param("StochasticBuyThreshold", 35) \
            .SetDisplay("Order Volume", "Trade volume used for each market entry", "Trading")
        self._stochastic_sell_threshold = self.Param("StochasticSellThreshold", 60) \
            .SetDisplay("Order Volume", "Trade volume used for each market entry", "Trading")
        self._momentum_period = self.Param("MomentumPeriod", 14) \
            .SetDisplay("Order Volume", "Trade volume used for each market entry", "Trading")
        self._momentum_neutral_level = self.Param("MomentumNeutralLevel", 100) \
            .SetDisplay("Order Volume", "Trade volume used for each market entry", "Trading")
        self._sar_acceleration = self.Param("SarAcceleration", 0.02) \
            .SetDisplay("Order Volume", "Trade volume used for each market entry", "Trading")
        self._sar_step = self.Param("SarStep", 0.02) \
            .SetDisplay("Order Volume", "Trade volume used for each market entry", "Trading")
        self._sar_maximum = self.Param("SarMaximum", 0.2) \
            .SetDisplay("Order Volume", "Trade volume used for each market entry", "Trading")

        self._parabolic_sar = null!
        self._macd = null!
        self._stochastic = null!
        self._momentum = null!
        self._previous_sar = None
        self._long_stop_price = None
        self._short_stop_price = None
        self._long_take_profit = None
        self._short_take_profit = None
        self._long_entry_price = None
        self._short_entry_price = None
        self._point_size = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(day_trading_impulse_strategy, self).OnReseted()
        self._parabolic_sar = null!
        self._macd = null!
        self._stochastic = null!
        self._momentum = null!
        self._previous_sar = None
        self._long_stop_price = None
        self._short_stop_price = None
        self._long_take_profit = None
        self._short_take_profit = None
        self._long_entry_price = None
        self._short_entry_price = None
        self._point_size = 0.0

    def OnStarted(self, time):
        super(day_trading_impulse_strategy, self).OnStarted(time)

        self.__parabolic_sar = ParabolicSar()
        self.__parabolic_sar.Acceleration = self.sar_acceleration
        self.__parabolic_sar.AccelerationStep = self.sar_step
        self.__parabolic_sar.AccelerationMax = self.sar_maximu
        self.__momentum = Momentum()
        self.__momentum.Length = self.momentum_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(self.__parabolic_sar, _macd, _stochastic, self.__momentum, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return day_trading_impulse_strategy()
