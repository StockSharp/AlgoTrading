import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class two_pair_correlation_strategy(Strategy):
    def __init__(self):
        super(two_pair_correlation_strategy, self).__init__()

        self._max_drawdown_percent = self.Param("MaxDrawdownPercent", 20) \
            .SetDisplay("Max Drawdown %", "Maximum drawdown before trading is paused", "Risk")
        self._price_difference_threshold = self.Param("PriceDifferenceThreshold", 5) \
            .SetDisplay("Max Drawdown %", "Maximum drawdown before trading is paused", "Risk")
        self._minimum_total_profit = self.Param("MinimumTotalProfit", 3) \
            .SetDisplay("Max Drawdown %", "Maximum drawdown before trading is paused", "Risk")
        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetDisplay("Max Drawdown %", "Maximum drawdown before trading is paused", "Risk")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(1) \
            .SetDisplay("Max Drawdown %", "Maximum drawdown before trading is paused", "Risk")

        self._atr = None
        self._sma = None
        self._atr_value = 0.0
        self._entry_price = 0.0
        self._peak_equity = 0.0
        self._trading_paused = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(two_pair_correlation_strategy, self).OnReseted()
        self._atr = None
        self._sma = None
        self._atr_value = 0.0
        self._entry_price = 0.0
        self._peak_equity = 0.0
        self._trading_paused = False

    def OnStarted(self, time):
        super(two_pair_correlation_strategy, self).OnStarted(time)

        self.__atr = AverageTrueRange()
        self.__atr.Length = self.atr_period
        self.__sma = SimpleMovingAverage()
        self.__sma.Length = 20

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.__atr, self.__sma, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return two_pair_correlation_strategy()
