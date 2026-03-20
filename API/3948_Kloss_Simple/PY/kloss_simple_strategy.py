import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import CommodityChannelIndex, DecimalIndicatorValue, ExponentialMovingAverage, ExponentialMovingAverage as EMA, StochasticOscillator
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Messages import Unit, UnitTypes


class kloss_simple_strategy(Strategy):
    def __init__(self):
        super(kloss_simple_strategy, self).__init__()

        self._order_volume = self.Param("OrderVolume", 0.1) \
            .SetDisplay("Volume", "Base order volume", "Trading")
        self._ma_period = self.Param("MaPeriod", 5) \
            .SetDisplay("Volume", "Base order volume", "Trading")
        self._cci_period = self.Param("CciPeriod", 10) \
            .SetDisplay("Volume", "Base order volume", "Trading")
        self._cci_level = self.Param("CciLevel", 120) \
            .SetDisplay("Volume", "Base order volume", "Trading")
        self._stochastic_k_period = self.Param("StochasticKPeriod", 5) \
            .SetDisplay("Volume", "Base order volume", "Trading")
        self._stochastic_d_period = self.Param("StochasticDPeriod", 3) \
            .SetDisplay("Volume", "Base order volume", "Trading")
        self._stochastic_smooth = self.Param("StochasticSmooth", 3) \
            .SetDisplay("Volume", "Base order volume", "Trading")
        self._stochastic_level = self.Param("StochasticLevel", 25) \
            .SetDisplay("Volume", "Base order volume", "Trading")
        self._max_orders = self.Param("MaxOrders", 3) \
            .SetDisplay("Volume", "Base order volume", "Trading")
        self._stop_loss_points = self.Param("StopLossPoints", 0) \
            .SetDisplay("Volume", "Base order volume", "Trading")
        self._take_profit_points = self.Param("TakeProfitPoints", 0) \
            .SetDisplay("Volume", "Base order volume", "Trading")
        self._risk_percentage = self.Param("RiskPercentage", 10) \
            .SetDisplay("Volume", "Base order volume", "Trading")
        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(1) \
            .SetDisplay("Volume", "Base order volume", "Trading")

        self._ema = null!
        self._cci = null!
        self._stochastic = null!
        self._previous_close = None
        self._previous_ma = None
        self._previous_cci = None
        self._previous_stochastic = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(kloss_simple_strategy, self).OnReseted()
        self._ema = null!
        self._cci = null!
        self._stochastic = null!
        self._previous_close = None
        self._previous_ma = None
        self._previous_cci = None
        self._previous_stochastic = None

    def OnStarted(self, time):
        super(kloss_simple_strategy, self).OnStarted(time)

        self.__ema = EMA()
        self.__ema.Length = self.ma_period
        self.__cci = CommodityChannelIndex()
        self.__cci.Length = self.cci_period

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
        return kloss_simple_strategy()
