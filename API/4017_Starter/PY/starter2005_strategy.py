import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import CommodityChannelIndex, ExponentialMovingAverage, ExponentialMovingAverage as EMA
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Messages import Sides


class starter2005_strategy(Strategy):
    def __init__(self):
        super(starter2005_strategy, self).__init__()

        self._base_volume = self.Param("BaseVolume", 1.2) \
            .SetDisplay("Base Volume", "Initial lot size used when risk-based sizing is unavailable", "Risk Management")
        self._maximum_risk = self.Param("MaximumRisk", 0.036) \
            .SetDisplay("Base Volume", "Initial lot size used when risk-based sizing is unavailable", "Risk Management")
        self._risk_divider = self.Param("RiskDivider", 500) \
            .SetDisplay("Base Volume", "Initial lot size used when risk-based sizing is unavailable", "Risk Management")
        self._decrease_factor = self.Param("DecreaseFactor", 2) \
            .SetDisplay("Base Volume", "Initial lot size used when risk-based sizing is unavailable", "Risk Management")
        self._ma_period = self.Param("MaPeriod", 5) \
            .SetDisplay("Base Volume", "Initial lot size used when risk-based sizing is unavailable", "Risk Management")
        self._cci_period = self.Param("CciPeriod", 14) \
            .SetDisplay("Base Volume", "Initial lot size used when risk-based sizing is unavailable", "Risk Management")
        self._cci_threshold = self.Param("CciThreshold", 5) \
            .SetDisplay("Base Volume", "Initial lot size used when risk-based sizing is unavailable", "Risk Management")
        self._laguerre_gamma = self.Param("LaguerreGamma", 0.66) \
            .SetDisplay("Base Volume", "Initial lot size used when risk-based sizing is unavailable", "Risk Management")
        self._laguerre_entry_tolerance = self.Param("LaguerreEntryTolerance", 0.02) \
            .SetDisplay("Base Volume", "Initial lot size used when risk-based sizing is unavailable", "Risk Management")
        self._laguerre_exit_high = self.Param("LaguerreExitHigh", 0.9) \
            .SetDisplay("Base Volume", "Initial lot size used when risk-based sizing is unavailable", "Risk Management")
        self._laguerre_exit_low = self.Param("LaguerreExitLow", 0.1) \
            .SetDisplay("Base Volume", "Initial lot size used when risk-based sizing is unavailable", "Risk Management")
        self._take_profit_points = self.Param("TakeProfitPoints", 10) \
            .SetDisplay("Base Volume", "Initial lot size used when risk-based sizing is unavailable", "Risk Management")
        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(5) \
            .SetDisplay("Base Volume", "Initial lot size used when risk-based sizing is unavailable", "Risk Management")

        self._ema = null!
        self._cci = null!
        self._previous_ma = None
        self._lag_l0 = 0.0
        self._lag_l1 = 0.0
        self._lag_l2 = 0.0
        self._lag_l3 = 0.0
        self._laguerre_formed = False
        self._entry_price = None
        self._entry_volume = 0.0
        self._entry_side = None
        self._consecutive_losses = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(starter2005_strategy, self).OnReseted()
        self._ema = null!
        self._cci = null!
        self._previous_ma = None
        self._lag_l0 = 0.0
        self._lag_l1 = 0.0
        self._lag_l2 = 0.0
        self._lag_l3 = 0.0
        self._laguerre_formed = False
        self._entry_price = None
        self._entry_volume = 0.0
        self._entry_side = None
        self._consecutive_losses = 0.0

    def OnStarted(self, time):
        super(starter2005_strategy, self).OnStarted(time)

        self.__ema = EMA()
        self.__ema.Length = self.ma_period
        self.__cci = CommodityChannelIndex()
        self.__cci.Length = self.cci_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.__ema, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return starter2005_strategy()
