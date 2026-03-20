import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage as EMA, MovingAverageConvergenceDivergence, WeightedMovingAverage
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Messages import Unit, UnitTypes


class mamacd_novlt_strategy(Strategy):
    def __init__(self):
        super(mamacd_novlt_strategy, self).__init__()

        self._first_low_wma_period = self.Param("FirstLowWmaPeriod", 85) \
            .SetDisplay("First LWMA Period", "First LWMA period on lows", "Indicators")
        self._second_low_wma_period = self.Param("SecondLowWmaPeriod", 75) \
            .SetDisplay("First LWMA Period", "First LWMA period on lows", "Indicators")
        self._fast_ema_period = self.Param("FastEmaPeriod", 5) \
            .SetDisplay("First LWMA Period", "First LWMA period on lows", "Indicators")
        self._slow_ema_period = self.Param("SlowEmaPeriod", 26) \
            .SetDisplay("First LWMA Period", "First LWMA period on lows", "Indicators")
        self._fast_signal_ema_period = self.Param("FastSignalEmaPeriod", 15) \
            .SetDisplay("First LWMA Period", "First LWMA period on lows", "Indicators")
        self._stop_loss_points = self.Param("StopLossPoints", 500) \
            .SetDisplay("First LWMA Period", "First LWMA period on lows", "Indicators")
        self._take_profit_points = self.Param("TakeProfitPoints", 500) \
            .SetDisplay("First LWMA Period", "First LWMA period on lows", "Indicators")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(1) \
            .SetDisplay("First LWMA Period", "First LWMA period on lows", "Indicators")

        self._is_long_setup_prepared = False
        self._is_short_setup_prepared = False
        self._previous_macd = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(mamacd_novlt_strategy, self).OnReseted()
        self._is_long_setup_prepared = False
        self._is_short_setup_prepared = False
        self._previous_macd = None

    def OnStarted(self, time):
        super(mamacd_novlt_strategy, self).OnStarted(time)

        self._fast_close_ema = EMA()
        self._fast_close_ema.Length = self.fast_ema_period
        self._first_low_wma = WeightedMovingAverage()
        self._first_low_wma.Length = self.first_low_wma_period
        self._second_low_wma = WeightedMovingAverage()
        self._second_low_wma.Length = self.second_low_wma_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._fast_close_ema, self._first_low_wma, self._second_low_wma, macd, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return mamacd_novlt_strategy()
