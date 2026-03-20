import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class n_candles_sequence_streak_strategy(Strategy):
    def __init__(self):
        super(n_candles_sequence_streak_strategy, self).__init__()

        self._consecutive_candles = self.Param("ConsecutiveCandles", 4) \
            .SetDisplay("Consecutive Candles", "Number of candles with identical direction", "Pattern")
        self._order_volume = self.Param("OrderVolume", 0.01) \
            .SetDisplay("Consecutive Candles", "Number of candles with identical direction", "Pattern")
        self._take_profit_pips = self.Param("TakeProfitPips", 50) \
            .SetDisplay("Consecutive Candles", "Number of candles with identical direction", "Pattern")
        self._stop_loss_pips = self.Param("StopLossPips", 50) \
            .SetDisplay("Consecutive Candles", "Number of candles with identical direction", "Pattern")
        self._trailing_stop_pips = self.Param("TrailingStopPips", 10) \
            .SetDisplay("Consecutive Candles", "Number of candles with identical direction", "Pattern")
        self._trailing_step_pips = self.Param("TrailingStepPips", 4) \
            .SetDisplay("Consecutive Candles", "Number of candles with identical direction", "Pattern")
        self._max_positions = self.Param("MaxPositions", 2) \
            .SetDisplay("Consecutive Candles", "Number of candles with identical direction", "Pattern")
        self._max_position_volume = self.Param("MaxPositionVolume", 2) \
            .SetDisplay("Consecutive Candles", "Number of candles with identical direction", "Pattern")
        self._use_trade_hours = self.Param("UseTradeHours", False) \
            .SetDisplay("Consecutive Candles", "Number of candles with identical direction", "Pattern")
        self._start_hour = self.Param("StartHour", 11) \
            .SetDisplay("Consecutive Candles", "Number of candles with identical direction", "Pattern")
        self._end_hour = self.Param("EndHour", 18) \
            .SetDisplay("Consecutive Candles", "Number of candles with identical direction", "Pattern")
        self._min_profit = self.Param("MinProfit", 3) \
            .SetDisplay("Consecutive Candles", "Number of candles with identical direction", "Pattern")
        self._closing_behavior = self.Param("ClosingBehavior", ClosingModes.All) \
            .SetDisplay("Consecutive Candles", "Number of candles with identical direction", "Pattern")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(4) \
            .SetDisplay("Consecutive Candles", "Number of candles with identical direction", "Pattern")

        self._streak_count = 0.0
        self._last_direction = 0.0
        self._pattern_direction = 0.0
        self._entries_in_direction = 0.0
        self._black_sheep_triggered = False
        self._has_position = False
        self._entry_price = 0.0
        self._pip_size = 0.0
        self._long_stop = None
        self._long_take = None
        self._short_stop = None
        self._short_take = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(n_candles_sequence_streak_strategy, self).OnReseted()
        self._streak_count = 0.0
        self._last_direction = 0.0
        self._pattern_direction = 0.0
        self._entries_in_direction = 0.0
        self._black_sheep_triggered = False
        self._has_position = False
        self._entry_price = 0.0
        self._pip_size = 0.0
        self._long_stop = None
        self._long_take = None
        self._short_stop = None
        self._short_take = None

    def OnStarted(self, time):
        super(n_candles_sequence_streak_strategy, self).OnStarted(time)


        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return n_candles_sequence_streak_strategy()
