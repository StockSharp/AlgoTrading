import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergence, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class momo_trades_strategy(Strategy):
    def __init__(self):
        super(momo_trades_strategy, self).__init__()

        self._sma_period = self.Param("SmaPeriod", 22) \
            .SetDisplay("SMA Period", "Period of the moving average", "Indicators")
        self._ma_bar_shift = self.Param("MaBarShift", 6) \
            .SetDisplay("SMA Period", "Period of the moving average", "Indicators")
        self._macd_fast = self.Param("MacdFast", 12) \
            .SetDisplay("SMA Period", "Period of the moving average", "Indicators")
        self._macd_slow = self.Param("MacdSlow", 26) \
            .SetDisplay("SMA Period", "Period of the moving average", "Indicators")
        self._macd_signal = self.Param("MacdSignal", 9) \
            .SetDisplay("SMA Period", "Period of the moving average", "Indicators")
        self._macd_bar_shift = self.Param("MacdBarShift", 2) \
            .SetDisplay("SMA Period", "Period of the moving average", "Indicators")
        self._stop_loss_pips = self.Param("StopLossPips", 25) \
            .SetDisplay("SMA Period", "Period of the moving average", "Indicators")
        self._take_profit_pips = self.Param("TakeProfitPips", 0) \
            .SetDisplay("SMA Period", "Period of the moving average", "Indicators")
        self._trailing_stop_pips = self.Param("TrailingStopPips", 0) \
            .SetDisplay("SMA Period", "Period of the moving average", "Indicators")
        self._trailing_step_pips = self.Param("TrailingStepPips", 5) \
            .SetDisplay("SMA Period", "Period of the moving average", "Indicators")
        self._breakeven_pips = self.Param("BreakevenPips", 10) \
            .SetDisplay("SMA Period", "Period of the moving average", "Indicators")
        self._price_shift_pips = self.Param("PriceShiftPips", 5) \
            .SetDisplay("SMA Period", "Period of the moving average", "Indicators")
        self._close_end_day = self.Param("CloseEndDay", True) \
            .SetDisplay("SMA Period", "Period of the moving average", "Indicators")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(1) \
            .SetDisplay("SMA Period", "Period of the moving average", "Indicators")

        self._sma = None
        self._macd = None
        self._macd_count = 0.0
        self._ma_count = 0.0
        self._close_count = 0.0
        self._entry_price = 0.0
        self._stop_price = None
        self._take_price = None
        self._breakeven_trigger = None
        self._trailing_distance = None
        self._trailing_step = None
        self._is_long_position = False
        self._cooldown_counter = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(momo_trades_strategy, self).OnReseted()
        self._sma = None
        self._macd = None
        self._macd_count = 0.0
        self._ma_count = 0.0
        self._close_count = 0.0
        self._entry_price = 0.0
        self._stop_price = None
        self._take_price = None
        self._breakeven_trigger = None
        self._trailing_distance = None
        self._trailing_step = None
        self._is_long_position = False
        self._cooldown_counter = 0.0

    def OnStarted(self, time):
        super(momo_trades_strategy, self).OnStarted(time)

        self.__sma = SimpleMovingAverage()
        self.__sma.Length = self.sma_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.__sma, _macd, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return momo_trades_strategy()
