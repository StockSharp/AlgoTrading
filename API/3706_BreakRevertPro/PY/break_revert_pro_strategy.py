import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange, DecimalIndicatorValue, ExponentialMovingAverage, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Messages import Unit, UnitTypes


class break_revert_pro_strategy(Strategy):
    def __init__(self):
        super(break_revert_pro_strategy, self).__init__()

        self._risk_per_trade = self.Param("RiskPerTrade", 1) \
            .SetDisplay("Risk %", "Risk per trade as percentage of portfolio value", "Risk")
        self._lookback_period = self.Param("LookbackPeriod", 20) \
            .SetDisplay("Risk %", "Risk per trade as percentage of portfolio value", "Risk")
        self._breakout_threshold = self.Param("BreakoutThreshold", 0.1) \
            .SetDisplay("Risk %", "Risk per trade as percentage of portfolio value", "Risk")
        self._mean_reversion_threshold = self.Param("MeanReversionThreshold", 0.6) \
            .SetDisplay("Risk %", "Risk per trade as percentage of portfolio value", "Risk")
        self._trade_delay_seconds = self.Param("TradeDelaySeconds", 86400) \
            .SetDisplay("Risk %", "Risk per trade as percentage of portfolio value", "Risk")
        self._max_positions = self.Param("MaxPositions", 1) \
            .SetDisplay("Risk %", "Risk per trade as percentage of portfolio value", "Risk")
        self._enable_safety_trade = self.Param("EnableSafetyTrade", True) \
            .SetDisplay("Risk %", "Risk per trade as percentage of portfolio value", "Risk")
        self._safety_trade_interval_seconds = self.Param("SafetyTradeIntervalSeconds", 900) \
            .SetDisplay("Risk %", "Risk per trade as percentage of portfolio value", "Risk")
        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(5) \
            .SetDisplay("Risk %", "Risk per trade as percentage of portfolio value", "Risk")

        self._m1_subscription = None
        self._m15_subscription = None
        self._h1_subscription = None
        self._m1_atr = None
        self._m1_trend_average = None
        self._m15_trend_average = None
        self._h1_trend_average = None
        self._event_frequency = None
        self._volatility_ema = None
        self._poisson_probability = 0.5
        self._weibull_probability = 0.5
        self._exponential_probability = 0.5
        self._m1_trend = 0.0
        self._m15_trend = 0.0
        self._h1_trend = 0.0
        self._h1_volatility = 0.0
        self._previous_m1_close = None
        self._latest_atr = 0.0
        self._last_trade_time = None
        self._last_safety_check = None
        self._safety_trade_sent = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(break_revert_pro_strategy, self).OnReseted()
        self._m1_subscription = None
        self._m15_subscription = None
        self._h1_subscription = None
        self._m1_atr = None
        self._m1_trend_average = None
        self._m15_trend_average = None
        self._h1_trend_average = None
        self._event_frequency = None
        self._volatility_ema = None
        self._poisson_probability = 0.5
        self._weibull_probability = 0.5
        self._exponential_probability = 0.5
        self._m1_trend = 0.0
        self._m15_trend = 0.0
        self._h1_trend = 0.0
        self._h1_volatility = 0.0
        self._previous_m1_close = None
        self._latest_atr = 0.0
        self._last_trade_time = None
        self._last_safety_check = None
        self._safety_trade_sent = False

    def OnStarted(self, time):
        super(break_revert_pro_strategy, self).OnStarted(time)

        self.__m1_atr = AverageTrueRange()
        self.__m1_atr.Length = lookback
        self.__m1_trend_average = SimpleMovingAverage()
        self.__m1_trend_average.Length = lookback
        self.__m15_trend_average = SimpleMovingAverage()
        self.__m15_trend_average.Length = lookback
        self.__h1_trend_average = SimpleMovingAverage()
        self.__h1_trend_average.Length = lookback
        self.__event_frequency = SimpleMovingAverage()
        self.__event_frequency.Length = lookback
        self.__volatility_ema = ExponentialMovingAverage()
        self.__volatility_ema.Length = lookback

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.__m1_atr, self._process_candle).Start()

        subscription = self.SubscribeCandles(TimeSpan.FromMinutes(15)
        subscription.Bind(self._process_candle).Start()

        subscription = self.SubscribeCandles(TimeSpan.FromHours(1)
        subscription.Bind(self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return break_revert_pro_strategy()
