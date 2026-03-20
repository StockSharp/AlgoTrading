import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, ExponentialMovingAverage as EMA, MovingAverageConvergenceDivergence, SimpleMovingAverage, SimpleMovingAverage as SMA
from StockSharp.Algo.Strategies import Strategy


class macd_pattern_trader_v02_strategy(Strategy):
    def __init__(self):
        super(macd_pattern_trader_v02_strategy, self).__init__()

        self._stop_loss_bars = self.Param("StopLossBars", 6) \
            .SetDisplay("Stop-Loss Bars", "Number of candles for stop-loss calculation", "Risk")
        self._take_profit_bars = self.Param("TakeProfitBars", 20) \
            .SetDisplay("Stop-Loss Bars", "Number of candles for stop-loss calculation", "Risk")
        self._offset_points = self.Param("OffsetPoints", 10) \
            .SetDisplay("Stop-Loss Bars", "Number of candles for stop-loss calculation", "Risk")
        self._profit_threshold_points = self.Param("ProfitThresholdPoints", 500) \
            .SetDisplay("Stop-Loss Bars", "Number of candles for stop-loss calculation", "Risk")
        self._fast_ema_period = self.Param("FastEmaPeriod", 12) \
            .SetDisplay("Stop-Loss Bars", "Number of candles for stop-loss calculation", "Risk")
        self._slow_ema_period = self.Param("SlowEmaPeriod", 26) \
            .SetDisplay("Stop-Loss Bars", "Number of candles for stop-loss calculation", "Risk")
        self._max_threshold = self.Param("MaxThreshold", 50) \
            .SetDisplay("Stop-Loss Bars", "Number of candles for stop-loss calculation", "Risk")
        self._min_threshold = self.Param("MinThreshold", -50) \
            .SetDisplay("Stop-Loss Bars", "Number of candles for stop-loss calculation", "Risk")
        self._ema1_period = self.Param("Ema1Period", 7) \
            .SetDisplay("Stop-Loss Bars", "Number of candles for stop-loss calculation", "Risk")
        self._ema2_period = self.Param("Ema2Period", 21) \
            .SetDisplay("Stop-Loss Bars", "Number of candles for stop-loss calculation", "Risk")
        self._sma_period = self.Param("SmaPeriod", 98) \
            .SetDisplay("Stop-Loss Bars", "Number of candles for stop-loss calculation", "Risk")
        self._ema3_period = self.Param("Ema3Period", 365) \
            .SetDisplay("Stop-Loss Bars", "Number of candles for stop-loss calculation", "Risk")
        self._trade_volume = self.Param("TradeVolume", 0.1) \
            .SetDisplay("Stop-Loss Bars", "Number of candles for stop-loss calculation", "Risk")
        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(30) \
            .SetDisplay("Stop-Loss Bars", "Number of candles for stop-loss calculation", "Risk")
        self._max_history = self.Param("MaxHistory", 1024) \
            .SetDisplay("Stop-Loss Bars", "Number of candles for stop-loss calculation", "Risk")

        self._macd = null!
        self._ema1 = null!
        self._ema2 = null!
        self._sma = null!
        self._ema3 = null!
        self._history = new()
        self._ema1_prev = None
        self._ema2_prev = None
        self._sma_prev = None
        self._ema3_prev = None
        self._ema1_last = None
        self._ema2_last = None
        self._sma_last = None
        self._ema3_last = None
        self._macd_prev1 = None
        self._macd_prev2 = None
        self._macd_prev3 = None
        self._max_threshold_reached = False
        self._min_threshold_reached = False
        self._sell_pattern_ready = False
        self._buy_pattern_ready = False
        self._pattern_min_value = 0.0
        self._pattern_max_value = 0.0
        self._point_size = 0.0
        self._entry_direction = 0.0
        self._entry_price = 0.0
        self._open_volume = 0.0
        self._stop_loss_price = None
        self._take_profit_price = None
        self._long_partial_stage = 0.0
        self._short_partial_stage = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(macd_pattern_trader_v02_strategy, self).OnReseted()
        self._macd = null!
        self._ema1 = null!
        self._ema2 = null!
        self._sma = null!
        self._ema3 = null!
        self._history = new()
        self._ema1_prev = None
        self._ema2_prev = None
        self._sma_prev = None
        self._ema3_prev = None
        self._ema1_last = None
        self._ema2_last = None
        self._sma_last = None
        self._ema3_last = None
        self._macd_prev1 = None
        self._macd_prev2 = None
        self._macd_prev3 = None
        self._max_threshold_reached = False
        self._min_threshold_reached = False
        self._sell_pattern_ready = False
        self._buy_pattern_ready = False
        self._pattern_min_value = 0.0
        self._pattern_max_value = 0.0
        self._point_size = 0.0
        self._entry_direction = 0.0
        self._entry_price = 0.0
        self._open_volume = 0.0
        self._stop_loss_price = None
        self._take_profit_price = None
        self._long_partial_stage = 0.0
        self._short_partial_stage = 0.0

    def OnStarted(self, time):
        super(macd_pattern_trader_v02_strategy, self).OnStarted(time)

        self.__ema1 = EMA()
        self.__ema1.Length = self.ema1_period
        self.__ema2 = EMA()
        self.__ema2.Length = self.ema2_period
        self.__sma = SMA()
        self.__sma.Length = self.sma_period
        self.__ema3 = EMA()
        self.__ema3.Length = self.ema3_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(_macd, self.__ema1, self.__ema2, self.__sma, self.__ema3, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return macd_pattern_trader_v02_strategy()
