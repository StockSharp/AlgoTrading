import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ParabolicSar, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class sar_trading_v20_strategy(Strategy):
    def __init__(self):
        super(sar_trading_v20_strategy, self).__init__()

        self._ma_period = self.Param("MaPeriod", 18) \
            .SetDisplay("MA Period", "Number of bars for the simple moving average.", "Indicators")
        self._ma_shift = self.Param("MaShift", 2) \
            .SetDisplay("MA Period", "Number of bars for the simple moving average.", "Indicators")
        self._sar_step = self.Param("SarStep", 0.02) \
            .SetDisplay("MA Period", "Number of bars for the simple moving average.", "Indicators")
        self._sar_max_step = self.Param("SarMaxStep", 0.2) \
            .SetDisplay("MA Period", "Number of bars for the simple moving average.", "Indicators")
        self._stop_loss_pips = self.Param("StopLossPips", 50) \
            .SetDisplay("MA Period", "Number of bars for the simple moving average.", "Indicators")
        self._take_profit_pips = self.Param("TakeProfitPips", 50) \
            .SetDisplay("MA Period", "Number of bars for the simple moving average.", "Indicators")
        self._trailing_stop_pips = self.Param("TrailingStopPips", 15) \
            .SetDisplay("MA Period", "Number of bars for the simple moving average.", "Indicators")
        self._trailing_step_pips = self.Param("TrailingStepPips", 5) \
            .SetDisplay("MA Period", "Number of bars for the simple moving average.", "Indicators")
        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(15) \
            .SetDisplay("MA Period", "Number of bars for the simple moving average.", "Indicators")

        self._ma = null!
        self._parabolic_sar = null!
        self._close_history = new()
        self._entry_price = None
        self._stop_price = None
        self._take_profit_price = None
        self._pip_size = 0.0
        self._exit_pending = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(sar_trading_v20_strategy, self).OnReseted()
        self._ma = null!
        self._parabolic_sar = null!
        self._close_history = new()
        self._entry_price = None
        self._stop_price = None
        self._take_profit_price = None
        self._pip_size = 0.0
        self._exit_pending = False

    def OnStarted(self, time):
        super(sar_trading_v20_strategy, self).OnStarted(time)

        self.__ma = SimpleMovingAverage()
        self.__ma.Length = self.ma_period
        self.__parabolic_sar = ParabolicSar()
        self.__parabolic_sar.Acceleration = self.sar_step
        self.__parabolic_sar.AccelerationMax = self.sar_max_step

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.__ma, self.__parabolic_sar, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return sar_trading_v20_strategy()
