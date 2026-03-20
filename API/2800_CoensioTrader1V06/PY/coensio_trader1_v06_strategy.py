import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import BollingerBands, DoubleExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Messages import Unit, UnitTypes


class coensio_trader1_v06_strategy(Strategy):
    def __init__(self):
        super(coensio_trader1_v06_strategy, self).__init__()

        self._bollinger_period = self.Param("BollingerPeriod", 30) \
            .SetDisplay("Bollinger Period", "Length of Bollinger Bands", "Indicators")
        self._bollinger_deviation = self.Param("BollingerDeviation", 1.5) \
            .SetDisplay("Bollinger Period", "Length of Bollinger Bands", "Indicators")
        self._dema_period = self.Param("DemaPeriod", 20) \
            .SetDisplay("Bollinger Period", "Length of Bollinger Bands", "Indicators")
        self._stop_loss_distance = self.Param("StopLossDistance", new Unit(0m, UnitTypes.Absolute) \
            .SetDisplay("Bollinger Period", "Length of Bollinger Bands", "Indicators")
        self._take_profit_distance = self.Param("TakeProfitDistance", new Unit(0m, UnitTypes.Absolute) \
            .SetDisplay("Bollinger Period", "Length of Bollinger Bands", "Indicators")
        self._close_on_signal = self.Param("CloseOnSignal", False) \
            .SetDisplay("Bollinger Period", "Length of Bollinger Bands", "Indicators")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(1) \
            .SetDisplay("Bollinger Period", "Length of Bollinger Bands", "Indicators")

        self._prev_open = None
        self._prev_high = None
        self._prev_low = None
        self._prev_close = None
        self._prev2_high = None
        self._prev2_low = None
        self._prev3_high = None
        self._prev3_low = None
        self._prev_upper_band = None
        self._prev_lower_band = None
        self._prev_dema = None
        self._prev2_dema = None
        self._bollinger = None
        self._dema = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(coensio_trader1_v06_strategy, self).OnReseted()
        self._prev_open = None
        self._prev_high = None
        self._prev_low = None
        self._prev_close = None
        self._prev2_high = None
        self._prev2_low = None
        self._prev3_high = None
        self._prev3_low = None
        self._prev_upper_band = None
        self._prev_lower_band = None
        self._prev_dema = None
        self._prev2_dema = None
        self._bollinger = None
        self._dema = None

    def OnStarted(self, time):
        super(coensio_trader1_v06_strategy, self).OnStarted(time)

        self.__bollinger = BollingerBands()
        self.__bollinger.Length = self.bollinger_period
        self.__bollinger.Width = self.bollinger_deviation
        self.__dema = DoubleExponentialMovingAverage()
        self.__dema.Length = self.dema_period

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
        return coensio_trader1_v06_strategy()
