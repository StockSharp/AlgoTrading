import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Messages import Sides


class risk_management_atr_strategy(Strategy):
    def __init__(self):
        super(risk_management_atr_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(4) \
            .SetDisplay("Candle type", "Primary timeframe processed by the strategy.", "General")
        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetDisplay("Candle type", "Primary timeframe processed by the strategy.", "General")
        self._atr_multiplier = self.Param("AtrMultiplier", 2) \
            .SetDisplay("Candle type", "Primary timeframe processed by the strategy.", "General")
        self._risk_percentage = self.Param("RiskPercentage", 1) \
            .SetDisplay("Candle type", "Primary timeframe processed by the strategy.", "General")
        self._use_atr_stop_loss = self.Param("UseAtrStopLoss", True) \
            .SetDisplay("Candle type", "Primary timeframe processed by the strategy.", "General")
        self._fixed_stop_loss_points = self.Param("FixedStopLossPoints", 50) \
            .SetDisplay("Candle type", "Primary timeframe processed by the strategy.", "General")
        self._fast_ma_period = self.Param("FastMaPeriod", 10) \
            .SetDisplay("Candle type", "Primary timeframe processed by the strategy.", "General")
        self._slow_ma_period = self.Param("SlowMaPeriod", 20) \
            .SetDisplay("Candle type", "Primary timeframe processed by the strategy.", "General")

        self._atr = None
        self._fast_moving_average = None
        self._slow_moving_average = None
        self._last_atr_value = None
        self._stop_loss_order = None
        self._price_step = 0.0
        self._virtual_stop_price = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(risk_management_atr_strategy, self).OnReseted()
        self._atr = None
        self._fast_moving_average = None
        self._slow_moving_average = None
        self._last_atr_value = None
        self._stop_loss_order = None
        self._price_step = 0.0
        self._virtual_stop_price = None

    def OnStarted(self, time):
        super(risk_management_atr_strategy, self).OnStarted(time)

        self.__atr = AverageTrueRange()
        self.__atr.Length = self.atr_period
        self.__fast_moving_average = SimpleMovingAverage()
        self.__fast_moving_average.Length = self.fast_ma_period
        self.__slow_moving_average = SimpleMovingAverage()
        self.__slow_moving_average.Length = self.slow_ma_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.__atr, self.__fast_moving_average, self.__slow_moving_average, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return risk_management_atr_strategy()
