import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, RelativeStrengthIndex, StochasticOscillator
from StockSharp.Algo.Strategies import Strategy


class vr_zver_strategy(Strategy):
    def __init__(self):
        super(vr_zver_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(4) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._use_moving_average = self.Param("UseMovingAverage", True) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._fast_ma_period = self.Param("FastMaPeriod", 3) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._slow_ma_period = self.Param("SlowMaPeriod", 5) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._very_slow_ma_period = self.Param("VerySlowMaPeriod", 7) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._use_stochastic = self.Param("UseStochastic", True) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._stochastic_k_period = self.Param("StochasticKPeriod", 42) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._stochastic_d_period = self.Param("StochasticDPeriod", 5) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._stochastic_slowing = self.Param("StochasticSlowing", 7) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._stochastic_upper_level = self.Param("StochasticUpperLevel", 55) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._stochastic_lower_level = self.Param("StochasticLowerLevel", 50) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._use_rsi = self.Param("UseRsi", True) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._rsi_upper_level = self.Param("RsiUpperLevel", 55) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._rsi_lower_level = self.Param("RsiLowerLevel", 50) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._stop_loss_pips = self.Param("StopLossPips", 50) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._take_profit_pips = self.Param("TakeProfitPips", 70) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._breakeven_pips = self.Param("BreakevenPips", 20) \
            .SetDisplay("Candle Type", "Type of candles", "General")

        self._fast_ma = null!
        self._slow_ma = null!
        self._very_slow_ma = null!
        self._stochastic = null!
        self._rsi = null!
        self._pip_size = 0.0
        self._long_entry_price = None
        self._short_entry_price = None
        self._long_stop_price = None
        self._short_stop_price = None
        self._long_take_price = None
        self._short_take_price = None
        self._long_breakeven_trigger = None
        self._short_breakeven_trigger = None
        self._long_breakeven_armed = False
        self._short_breakeven_armed = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(vr_zver_strategy, self).OnReseted()
        self._fast_ma = null!
        self._slow_ma = null!
        self._very_slow_ma = null!
        self._stochastic = null!
        self._rsi = null!
        self._pip_size = 0.0
        self._long_entry_price = None
        self._short_entry_price = None
        self._long_stop_price = None
        self._short_stop_price = None
        self._long_take_price = None
        self._short_take_price = None
        self._long_breakeven_trigger = None
        self._short_breakeven_trigger = None
        self._long_breakeven_armed = False
        self._short_breakeven_armed = False

    def OnStarted(self, time):
        super(vr_zver_strategy, self).OnStarted(time)

        self.__fast_ma = ExponentialMovingAverage()
        self.__fast_ma.Length = self.fast_ma_period
        self.__slow_ma = ExponentialMovingAverage()
        self.__slow_ma.Length = self.slow_ma_period
        self.__very_slow_ma = ExponentialMovingAverage()
        self.__very_slow_ma.Length = Veryself.slow_ma_period
        self.__rsi = RelativeStrengthIndex()
        self.__rsi.Length = self.rsi_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(self.__fast_ma, self.__slow_ma, self.__very_slow_ma, _stochastic, self.__rsi, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return vr_zver_strategy()
