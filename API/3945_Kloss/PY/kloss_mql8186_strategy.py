import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import CommodityChannelIndex, StochasticOscillator
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Messages import Unit, UnitTypes


class kloss_mql8186_strategy(Strategy):
    def __init__(self):
        super(kloss_mql8186_strategy, self).__init__()

        self._cci_period = self.Param("CciPeriod", 10) \
            .SetDisplay("CCI Period", "Number of candles for the CCI calculation", "Indicators")
        self._cci_threshold = self.Param("CciThreshold", 150) \
            .SetDisplay("CCI Period", "Number of candles for the CCI calculation", "Indicators")
        self._stochastic_k_period = self.Param("StochasticKPeriod", 5) \
            .SetDisplay("CCI Period", "Number of candles for the CCI calculation", "Indicators")
        self._stochastic_d_period = self.Param("StochasticDPeriod", 3) \
            .SetDisplay("CCI Period", "Number of candles for the CCI calculation", "Indicators")
        self._stochastic_smooth = self.Param("StochasticSmooth", 3) \
            .SetDisplay("CCI Period", "Number of candles for the CCI calculation", "Indicators")
        self._stochastic_oversold = self.Param("StochasticOversold", 45) \
            .SetDisplay("CCI Period", "Number of candles for the CCI calculation", "Indicators")
        self._stochastic_overbought = self.Param("StochasticOverbought", 55) \
            .SetDisplay("CCI Period", "Number of candles for the CCI calculation", "Indicators")
        self._stop_loss_points = self.Param("StopLossPoints", 48) \
            .SetDisplay("CCI Period", "Number of candles for the CCI calculation", "Indicators")
        self._take_profit_points = self.Param("TakeProfitPoints", 152) \
            .SetDisplay("CCI Period", "Number of candles for the CCI calculation", "Indicators")
        self._fixed_volume = self.Param("FixedVolume", 0) \
            .SetDisplay("CCI Period", "Number of candles for the CCI calculation", "Indicators")
        self._risk_percent = self.Param("RiskPercent", 0.2) \
            .SetDisplay("CCI Period", "Number of candles for the CCI calculation", "Indicators")
        self._max_volume = self.Param("MaxVolume", 5) \
            .SetDisplay("CCI Period", "Number of candles for the CCI calculation", "Indicators")
        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(5) \
            .SetDisplay("CCI Period", "Number of candles for the CCI calculation", "Indicators")

        self._cci = null!
        self._stochastic = null!
        self._previous_open = None
        self._previous_close = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(kloss_mql8186_strategy, self).OnReseted()
        self._cci = null!
        self._stochastic = null!
        self._previous_open = None
        self._previous_close = None

    def OnStarted(self, time):
        super(kloss_mql8186_strategy, self).OnStarted(time)

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
        return kloss_mql8186_strategy()
