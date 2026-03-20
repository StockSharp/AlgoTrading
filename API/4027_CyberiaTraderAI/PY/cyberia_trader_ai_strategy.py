import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, ExponentialMovingAverage as EMA, MovingAverageConvergenceDivergenceSignal
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Messages import Unit, UnitTypes


class cyberia_trader_ai_strategy(Strategy):
    def __init__(self):
        super(cyberia_trader_ai_strategy, self).__init__()

        self._max_period = self.Param("MaxPeriod", 23) \
            .SetDisplay("Max Period", "Largest sampling stride tested by the probability engine", "Model")
        self._samples_per_period = self.Param("SamplesPerPeriod", 5) \
            .SetDisplay("Max Period", "Largest sampling stride tested by the probability engine", "Model")
        self._spread_threshold = self.Param("SpreadThreshold", 0) \
            .SetDisplay("Max Period", "Largest sampling stride tested by the probability engine", "Model")
        self._enable_cyberia_logic = self.Param("EnableCyberiaLogic", True) \
            .SetDisplay("Max Period", "Largest sampling stride tested by the probability engine", "Model")
        self._enable_macd = self.Param("EnableMacd", False) \
            .SetDisplay("Max Period", "Largest sampling stride tested by the probability engine", "Model")
        self._enable_ma = self.Param("EnableMa", False) \
            .SetDisplay("Max Period", "Largest sampling stride tested by the probability engine", "Model")
        self._enable_reversal_detector = self.Param("EnableReversalDetector", False) \
            .SetDisplay("Max Period", "Largest sampling stride tested by the probability engine", "Model")
        self._ma_period = self.Param("MaPeriod", 23) \
            .SetDisplay("Max Period", "Largest sampling stride tested by the probability engine", "Model")
        self._macd_fast = self.Param("MacdFast", 12) \
            .SetDisplay("Max Period", "Largest sampling stride tested by the probability engine", "Model")
        self._macd_slow = self.Param("MacdSlow", 26) \
            .SetDisplay("Max Period", "Largest sampling stride tested by the probability engine", "Model")
        self._macd_signal = self.Param("MacdSignal", 9) \
            .SetDisplay("Max Period", "Largest sampling stride tested by the probability engine", "Model")
        self._reversal_factor = self.Param("ReversalFactor", 3) \
            .SetDisplay("Max Period", "Largest sampling stride tested by the probability engine", "Model")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(2) \
            .SetDisplay("Max Period", "Largest sampling stride tested by the probability engine", "Model")
        self._take_profit_percent = self.Param("TakeProfitPercent", 0) \
            .SetDisplay("Max Period", "Largest sampling stride tested by the probability engine", "Model")
        self._stop_loss_percent = self.Param("StopLossPercent", 0) \
            .SetDisplay("Max Period", "Largest sampling stride tested by the probability engine", "Model")

        self._macd = None
        self._ema = None
        self._history = new()
        self._previous_ema = None
        self._previous_period = None
        self._current_stats = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(cyberia_trader_ai_strategy, self).OnReseted()
        self._macd = None
        self._ema = None
        self._history = new()
        self._previous_ema = None
        self._previous_period = None
        self._current_stats = None

    def OnStarted(self, time):
        super(cyberia_trader_ai_strategy, self).OnStarted(time)

        self.__ema = EMA()
        self.__ema.Length = self.ma_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(_macd, self.__ema, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return cyberia_trader_ai_strategy()
