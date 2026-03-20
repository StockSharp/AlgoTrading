import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, MovingAverageConvergenceDivergence
from StockSharp.Algo.Strategies import Strategy


class dealers_trade_macd_strategy(Strategy):
    def __init__(self):
        super(dealers_trade_macd_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(4) \
            .SetDisplay("Candle Type", "Primary timeframe", "General")
        self._fixed_volume = self.Param("FixedVolume", 0.1) \
            .SetDisplay("Candle Type", "Primary timeframe", "General")
        self._risk_percent = self.Param("RiskPercent", 5) \
            .SetDisplay("Candle Type", "Primary timeframe", "General")
        self._stop_loss_points = self.Param("StopLossPoints", 90) \
            .SetDisplay("Candle Type", "Primary timeframe", "General")
        self._take_profit_points = self.Param("TakeProfitPoints", 30) \
            .SetDisplay("Candle Type", "Primary timeframe", "General")
        self._trailing_stop_points = self.Param("TrailingStopPoints", 15) \
            .SetDisplay("Candle Type", "Primary timeframe", "General")
        self._trailing_step_points = self.Param("TrailingStepPoints", 5) \
            .SetDisplay("Candle Type", "Primary timeframe", "General")
        self._max_positions = self.Param("MaxPositions", 2) \
            .SetDisplay("Candle Type", "Primary timeframe", "General")
        self._interval_points = self.Param("IntervalPoints", 50) \
            .SetDisplay("Candle Type", "Primary timeframe", "General")
        self._secure_profit = self.Param("SecureProfit", 50) \
            .SetDisplay("Candle Type", "Primary timeframe", "General")
        self._account_protection = self.Param("AccountProtection", True) \
            .SetDisplay("Candle Type", "Primary timeframe", "General")
        self._positions_for_protection = self.Param("PositionsForProtection", 3) \
            .SetDisplay("Candle Type", "Primary timeframe", "General")
        self._reverse_condition = self.Param("ReverseCondition", False) \
            .SetDisplay("Candle Type", "Primary timeframe", "General")
        self._macd_fast_period = self.Param("MacdFastPeriod", 14) \
            .SetDisplay("Candle Type", "Primary timeframe", "General")
        self._macd_slow_period = self.Param("MacdSlowPeriod", 26) \
            .SetDisplay("Candle Type", "Primary timeframe", "General")
        self._macd_signal_period = self.Param("MacdSignalPeriod", 1) \
            .SetDisplay("Candle Type", "Primary timeframe", "General")
        self._max_volume = self.Param("MaxVolume", 5) \
            .SetDisplay("Candle Type", "Primary timeframe", "General")
        self._volume_multiplier = self.Param("VolumeMultiplier", 1.6) \
            .SetDisplay("Candle Type", "Primary timeframe", "General")

        self._macd = null!
        self._previous_macd = None
        self._last_entry_price = 0.0
        self._cooldown = 0.0
        self._long_positions = new()
        self._short_positions = new()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(dealers_trade_macd_strategy, self).OnReseted()
        self._macd = null!
        self._previous_macd = None
        self._last_entry_price = 0.0
        self._cooldown = 0.0
        self._long_positions = new()
        self._short_positions = new()

    def OnStarted(self, time):
        super(dealers_trade_macd_strategy, self).OnStarted(time)


        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(_macd, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return dealers_trade_macd_strategy()
