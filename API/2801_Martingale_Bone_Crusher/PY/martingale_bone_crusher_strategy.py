import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Messages import Sides


class martingale_bone_crusher_strategy(Strategy):
    def __init__(self):
        super(martingale_bone_crusher_strategy, self).__init__()

        self._use_take_profit_money = self.Param("UseTakeProfitMoney", False) \
            .SetDisplay("Use Money TP", "Enable fixed money take profit", "Risk Management")
        self._take_profit_money = self.Param("TakeProfitMoney", 10) \
            .SetDisplay("Use Money TP", "Enable fixed money take profit", "Risk Management")
        self._use_take_profit_percent = self.Param("UseTakeProfitPercent", False) \
            .SetDisplay("Use Money TP", "Enable fixed money take profit", "Risk Management")
        self._take_profit_percent = self.Param("TakeProfitPercent", 10) \
            .SetDisplay("Use Money TP", "Enable fixed money take profit", "Risk Management")
        self._enable_trailing = self.Param("EnableTrailing", True) \
            .SetDisplay("Use Money TP", "Enable fixed money take profit", "Risk Management")
        self._trailing_take_profit_money = self.Param("TrailingTakeProfitMoney", 40) \
            .SetDisplay("Use Money TP", "Enable fixed money take profit", "Risk Management")
        self._trailing_stop_money = self.Param("TrailingStopMoney", 10) \
            .SetDisplay("Use Money TP", "Enable fixed money take profit", "Risk Management")
        self._martingale_mode = self.Param("MartingaleMode", MartingaleModes.Martingale2) \
            .SetDisplay("Use Money TP", "Enable fixed money take profit", "Risk Management")
        self._use_move_to_breakeven = self.Param("UseMoveToBreakeven", True) \
            .SetDisplay("Use Money TP", "Enable fixed money take profit", "Risk Management")
        self._move_to_breakeven_trigger = self.Param("MoveToBreakevenTrigger", 10) \
            .SetDisplay("Use Money TP", "Enable fixed money take profit", "Risk Management")
        self._breakeven_offset = self.Param("BreakevenOffset", 5) \
            .SetDisplay("Use Money TP", "Enable fixed money take profit", "Risk Management")
        self._multiply = self.Param("Multiply", 2) \
            .SetDisplay("Use Money TP", "Enable fixed money take profit", "Risk Management")
        self._initial_volume = self.Param("InitialVolume", 0.01) \
            .SetDisplay("Use Money TP", "Enable fixed money take profit", "Risk Management")
        self._double_lot_size = self.Param("DoubleLotSize", False) \
            .SetDisplay("Use Money TP", "Enable fixed money take profit", "Risk Management")
        self._lot_size_increment = self.Param("LotSizeIncrement", 0.01) \
            .SetDisplay("Use Money TP", "Enable fixed money take profit", "Risk Management")
        self._trailing_stop_steps = self.Param("TrailingStopSteps", 30) \
            .SetDisplay("Use Money TP", "Enable fixed money take profit", "Risk Management")
        self._stop_loss_steps = self.Param("StopLossSteps", 5) \
            .SetDisplay("Use Money TP", "Enable fixed money take profit", "Risk Management")
        self._take_profit_steps = self.Param("TakeProfitSteps", 5) \
            .SetDisplay("Use Money TP", "Enable fixed money take profit", "Risk Management")
        self._fast_period = self.Param("FastPeriod", 2) \
            .SetDisplay("Use Money TP", "Enable fixed money take profit", "Risk Management")
        self._slow_period = self.Param("SlowPeriod", 50) \
            .SetDisplay("Use Money TP", "Enable fixed money take profit", "Risk Management")
        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(30) \
            .SetDisplay("Use Money TP", "Enable fixed money take profit", "Risk Management")

        self._fast_ma = None
        self._slow_ma = None
        self._average_price = 0.0
        self._position_volume = 0.0
        self._current_volume = 0.0
        self._last_order_volume = 0.0
        self._last_trade_result = 0.0
        self._highest_price = 0.0
        self._lowest_price = 0.0
        self._breakeven_price = None
        self._max_floating_profit = 0.0
        self._initial_capital = 0.0
        self._last_position_side = None
        self._last_losing_side = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(martingale_bone_crusher_strategy, self).OnReseted()
        self._fast_ma = None
        self._slow_ma = None
        self._average_price = 0.0
        self._position_volume = 0.0
        self._current_volume = 0.0
        self._last_order_volume = 0.0
        self._last_trade_result = 0.0
        self._highest_price = 0.0
        self._lowest_price = 0.0
        self._breakeven_price = None
        self._max_floating_profit = 0.0
        self._initial_capital = 0.0
        self._last_position_side = None
        self._last_losing_side = None

    def OnStarted(self, time):
        super(martingale_bone_crusher_strategy, self).OnStarted(time)

        self.__fast_ma = SimpleMovingAverage()
        self.__fast_ma.Length = self.fast_period
        self.__slow_ma = SimpleMovingAverage()
        self.__slow_ma.Length = self.slow_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.__fast_ma, self.__slow_ma, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return martingale_bone_crusher_strategy()
