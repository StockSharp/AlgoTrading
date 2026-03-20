import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergenceSignal, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class zone_recovery_area_strategy(Strategy):
    def __init__(self):
        super(zone_recovery_area_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(5) \
            .SetDisplay("Entry Candle", "Timeframe used for entries", "General")
        self._monthly_candle_type = self.Param("MonthlyCandleType", TimeSpan.FromHours(1) \
            .SetDisplay("Entry Candle", "Timeframe used for entries", "General")
        self._fast_ma_length = self.Param("FastMaLength", 20) \
            .SetDisplay("Entry Candle", "Timeframe used for entries", "General")
        self._slow_ma_length = self.Param("SlowMaLength", 200) \
            .SetDisplay("Entry Candle", "Timeframe used for entries", "General")
        self._take_profit_pips = self.Param("TakeProfitPips", 150) \
            .SetDisplay("Entry Candle", "Timeframe used for entries", "General")
        self._zone_recovery_pips = self.Param("ZoneRecoveryPips", 50) \
            .SetDisplay("Entry Candle", "Timeframe used for entries", "General")
        self._initial_volume = self.Param("InitialVolume", 1) \
            .SetDisplay("Entry Candle", "Timeframe used for entries", "General")
        self._use_volume_multiplier = self.Param("UseVolumeMultiplier", True) \
            .SetDisplay("Entry Candle", "Timeframe used for entries", "General")
        self._volume_multiplier = self.Param("VolumeMultiplier", 2) \
            .SetDisplay("Entry Candle", "Timeframe used for entries", "General")
        self._volume_increment = self.Param("VolumeIncrement", 0.5) \
            .SetDisplay("Entry Candle", "Timeframe used for entries", "General")
        self._max_trades = self.Param("MaxTrades", 6) \
            .SetDisplay("Entry Candle", "Timeframe used for entries", "General")
        self._use_money_take_profit = self.Param("UseMoneyTakeProfit", False) \
            .SetDisplay("Entry Candle", "Timeframe used for entries", "General")
        self._money_take_profit = self.Param("MoneyTakeProfit", 40) \
            .SetDisplay("Entry Candle", "Timeframe used for entries", "General")
        self._use_percent_take_profit = self.Param("UsePercentTakeProfit", False) \
            .SetDisplay("Entry Candle", "Timeframe used for entries", "General")
        self._percent_take_profit = self.Param("PercentTakeProfit", 5) \
            .SetDisplay("Entry Candle", "Timeframe used for entries", "General")
        self._enable_trailing = self.Param("EnableTrailing", True) \
            .SetDisplay("Entry Candle", "Timeframe used for entries", "General")
        self._trailing_start_profit = self.Param("TrailingStartProfit", 40) \
            .SetDisplay("Entry Candle", "Timeframe used for entries", "General")
        self._trailing_drawdown = self.Param("TrailingDrawdown", 10) \
            .SetDisplay("Entry Candle", "Timeframe used for entries", "General")

        self._fast_ma = null!
        self._slow_ma = null!
        self._monthly_macd = null!
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._ma_initialized = False
        self._macd_ready = False
        self._macd_main = 0.0
        self._macd_signal = 0.0
        self._is_long_cycle = False
        self._cycle_base_price = 0.0
        self._next_step_index = 0.0
        self._peak_cycle_profit = 0.0
        self._steps = new()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(zone_recovery_area_strategy, self).OnReseted()
        self._fast_ma = null!
        self._slow_ma = null!
        self._monthly_macd = null!
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._ma_initialized = False
        self._macd_ready = False
        self._macd_main = 0.0
        self._macd_signal = 0.0
        self._is_long_cycle = False
        self._cycle_base_price = 0.0
        self._next_step_index = 0.0
        self._peak_cycle_profit = 0.0
        self._steps = new()

    def OnStarted(self, time):
        super(zone_recovery_area_strategy, self).OnStarted(time)

        self.__fast_ma = SimpleMovingAverage()
        self.__fast_ma.Length = self.fast_ma_length
        self.__slow_ma = SimpleMovingAverage()
        self.__slow_ma.Length = self.slow_ma_length

        main_subscription = self.SubscribeCandles(self.candle_type)
        main_subscription.Bind(self.__fast_ma, self.__slow_ma, self._process_candle).Start()

        monthly_subscription = self.SubscribeCandles(Monthlyself.candle_type)
        monthly_subscription.Bind(self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return zone_recovery_area_strategy()
