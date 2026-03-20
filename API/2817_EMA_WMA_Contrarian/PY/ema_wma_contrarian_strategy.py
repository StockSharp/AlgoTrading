import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import DecimalIndicatorValue, ExponentialMovingAverage, WeightedMovingAverage
from StockSharp.Algo.Strategies import Strategy


class ema_wma_contrarian_strategy(Strategy):
    def __init__(self):
        super(ema_wma_contrarian_strategy, self).__init__()

        self._ema_period = self.Param("EmaPeriod", 28) \
            .SetDisplay("EMA Period", "EMA length calculated on candle open prices", "Indicators")
        self._wma_period = self.Param("WmaPeriod", 8) \
            .SetDisplay("EMA Period", "EMA length calculated on candle open prices", "Indicators")
        self._stop_loss_points = self.Param("StopLossPoints", 50) \
            .SetDisplay("EMA Period", "EMA length calculated on candle open prices", "Indicators")
        self._take_profit_points = self.Param("TakeProfitPoints", 50) \
            .SetDisplay("EMA Period", "EMA length calculated on candle open prices", "Indicators")
        self._trailing_stop_points = self.Param("TrailingStopPoints", 50) \
            .SetDisplay("EMA Period", "EMA length calculated on candle open prices", "Indicators")
        self._trailing_step_points = self.Param("TrailingStepPoints", 10) \
            .SetDisplay("EMA Period", "EMA length calculated on candle open prices", "Indicators")
        self._risk_percent = self.Param("RiskPercent", 10) \
            .SetDisplay("EMA Period", "EMA length calculated on candle open prices", "Indicators")
        self._base_volume = self.Param("BaseVolume", 1) \
            .SetDisplay("EMA Period", "EMA length calculated on candle open prices", "Indicators")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(4) \
            .SetDisplay("EMA Period", "EMA length calculated on candle open prices", "Indicators")

        self._ema = None
        self._wma = None
        self._has_previous = False
        self._previous_ema = 0.0
        self._previous_wma = 0.0
        self._entry_price = None
        self._stop_loss_price = None
        self._take_profit_price = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(ema_wma_contrarian_strategy, self).OnReseted()
        self._ema = None
        self._wma = None
        self._has_previous = False
        self._previous_ema = 0.0
        self._previous_wma = 0.0
        self._entry_price = None
        self._stop_loss_price = None
        self._take_profit_price = None

    def OnStarted(self, time):
        super(ema_wma_contrarian_strategy, self).OnStarted(time)

        self.__ema = ExponentialMovingAverage()
        self.__ema.Length = self.ema_period
        self.__wma = WeightedMovingAverage()
        self.__wma.Length = self.wma_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return ema_wma_contrarian_strategy()
