import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class mp_candlestick_strategy(Strategy):
    def __init__(self):
        super(mp_candlestick_strategy, self).__init__()

        self._risk_percent = self.Param("RiskPercent", 1) \
            .SetDisplay("Risk Percent", "Percentage of portfolio equity risked per trade", "Risk")
        self._risk_reward_ratio = self.Param("RiskRewardRatio", 1.5) \
            .SetDisplay("Risk Percent", "Percentage of portfolio equity risked per trade", "Risk")
        self._max_margin_usage = self.Param("MaxMarginUsage", 30) \
            .SetDisplay("Risk Percent", "Percentage of portfolio equity risked per trade", "Risk")
        self._stop_loss_pips = self.Param("StopLossPips", 50) \
            .SetDisplay("Risk Percent", "Percentage of portfolio equity risked per trade", "Risk")
        self._use_auto_sl = self.Param("UseAutoSl", True) \
            .SetDisplay("Risk Percent", "Percentage of portfolio equity risked per trade", "Risk")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(4) \
            .SetDisplay("Risk Percent", "Percentage of portfolio equity risked per trade", "Risk")

        self._atr = None
        self._entry_price = None
        self._stop_price = None
        self._take_profit_price = None
        self._is_long_position = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(mp_candlestick_strategy, self).OnReseted()
        self._atr = None
        self._entry_price = None
        self._stop_price = None
        self._take_profit_price = None
        self._is_long_position = False

    def OnStarted(self, time):
        super(mp_candlestick_strategy, self).OnStarted(time)

        self.__atr = AverageTrueRange()
        self.__atr.Length = 14

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.__atr, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return mp_candlestick_strategy()
