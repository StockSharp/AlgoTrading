import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class euro_surge_simplified_strategy(Strategy):
    def __init__(self):
        super(euro_surge_simplified_strategy, self).__init__()

        self._trade_size_type = self.Param("TradeSizeType", TradeSizeTypes.FixedSize) \
            .SetDisplay("Trade Size Mode", "How trading volume is calculated", "Money Management")
        self._fixed_volume = self.Param("FixedVolume", 1) \
            .SetDisplay("Trade Size Mode", "How trading volume is calculated", "Money Management")
        self._trade_size_percent = self.Param("TradeSizePercent", 1) \
            .SetDisplay("Trade Size Mode", "How trading volume is calculated", "Money Management")
        self._take_profit_points = self.Param("TakeProfitPoints", 1400) \
            .SetDisplay("Trade Size Mode", "How trading volume is calculated", "Money Management")
        self._stop_loss_points = self.Param("StopLossPoints", 900) \
            .SetDisplay("Trade Size Mode", "How trading volume is calculated", "Money Management")
        self._min_trade_interval_minutes = self.Param("MinTradeIntervalMinutes", 600) \
            .SetDisplay("Trade Size Mode", "How trading volume is calculated", "Money Management")
        self._ma_period = self.Param("MaPeriod", 52) \
            .SetDisplay("Trade Size Mode", "How trading volume is calculated", "Money Management")
        self._rsi_period = self.Param("RsiPeriod", 13) \
            .SetDisplay("Trade Size Mode", "How trading volume is calculated", "Money Management")
        self._rsi_buy_level = self.Param("RsiBuyLevel", 50) \
            .SetDisplay("Trade Size Mode", "How trading volume is calculated", "Money Management")
        self._rsi_sell_level = self.Param("RsiSellLevel", 50) \
            .SetDisplay("Trade Size Mode", "How trading volume is calculated", "Money Management")
        self._macd_fast = self.Param("MacdFast", 8) \
            .SetDisplay("Trade Size Mode", "How trading volume is calculated", "Money Management")
        self._macd_slow = self.Param("MacdSlow", 24) \
            .SetDisplay("Trade Size Mode", "How trading volume is calculated", "Money Management")
        self._macd_signal = self.Param("MacdSignal", 13) \
            .SetDisplay("Trade Size Mode", "How trading volume is calculated", "Money Management")
        self._bollinger_length = self.Param("BollingerLength", 25) \
            .SetDisplay("Trade Size Mode", "How trading volume is calculated", "Money Management")
        self._bollinger_width = self.Param("BollingerWidth", 2.5) \
            .SetDisplay("Trade Size Mode", "How trading volume is calculated", "Money Management")
        self._stochastic_length = self.Param("StochasticLength", 10) \
            .SetDisplay("Trade Size Mode", "How trading volume is calculated", "Money Management")
        self._stochastic_k = self.Param("StochasticK", 10) \
            .SetDisplay("Trade Size Mode", "How trading volume is calculated", "Money Management")
        self._stochastic_d = self.Param("StochasticD", 2) \
            .SetDisplay("Trade Size Mode", "How trading volume is calculated", "Money Management")
        self._use_ma = self.Param("UseMa", True) \
            .SetDisplay("Trade Size Mode", "How trading volume is calculated", "Money Management")
        self._use_rsi = self.Param("UseRsi", True) \
            .SetDisplay("Trade Size Mode", "How trading volume is calculated", "Money Management")
        self._use_macd = self.Param("UseMacd", True) \
            .SetDisplay("Trade Size Mode", "How trading volume is calculated", "Money Management")
        self._use_bollinger = self.Param("UseBollinger", False) \
            .SetDisplay("Trade Size Mode", "How trading volume is calculated", "Money Management")
        self._use_stochastic = self.Param("UseStochastic", True) \
            .SetDisplay("Trade Size Mode", "How trading volume is calculated", "Money Management")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(1) \
            .SetDisplay("Trade Size Mode", "How trading volume is calculated", "Money Management")

        self._last_trade_time = None
        self._fast_ma = null!
        self._slow_ma = null!
        self._rsi = null!
        self._fast_ma_value = 0.0
        self._slow_ma_value = 0.0
        self._rsi_value = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(euro_surge_simplified_strategy, self).OnReseted()
        self._last_trade_time = None
        self._fast_ma = null!
        self._slow_ma = null!
        self._rsi = null!
        self._fast_ma_value = 0.0
        self._slow_ma_value = 0.0
        self._rsi_value = 0.0

    def OnStarted(self, time):
        super(euro_surge_simplified_strategy, self).OnStarted(time)
        self.StartProtection(None, None)

        self.__fast_ma = SimpleMovingAverage()
        self.__fast_ma.Length = 20
        self.__slow_ma = SimpleMovingAverage()
        self.__slow_ma.Length = self.ma_period
        self.__rsi = RelativeStrengthIndex()
        self.__rsi.Length = self.rsi_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.__fast_ma, self.__slow_ma, self.__rsi, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return euro_surge_simplified_strategy()
