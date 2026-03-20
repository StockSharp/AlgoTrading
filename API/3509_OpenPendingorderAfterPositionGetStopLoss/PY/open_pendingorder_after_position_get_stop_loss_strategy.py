import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class open_pendingorder_after_position_get_stop_loss_strategy(Strategy):
    def __init__(self):
        super(open_pendingorder_after_position_get_stop_loss_strategy, self).__init__()

        self._k_period = self.Param("KPeriod", 22) \
            .SetDisplay("%K Period", "Number of bars for %K", "Indicators")
        self._d_period = self.Param("DPeriod", 7) \
            .SetDisplay("%K Period", "Number of bars for %K", "Indicators")
        self._slowing = self.Param("Slowing", 2) \
            .SetDisplay("%K Period", "Number of bars for %K", "Indicators")
        self._stop_loss_pct = self.Param("StopLossPct", 2) \
            .SetDisplay("%K Period", "Number of bars for %K", "Indicators")
        self._take_profit_pct = self.Param("TakeProfitPct", 3) \
            .SetDisplay("%K Period", "Number of bars for %K", "Indicators")
        self._signal_cooldown_candles = self.Param("SignalCooldownCandles", 4) \
            .SetDisplay("%K Period", "Number of bars for %K", "Indicators")
        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(60) \
            .SetDisplay("%K Period", "Number of bars for %K", "Indicators")

        self._last_k = None
        self._prev_k = None
        self._entry_price = 0.0
        self._candles_since_trade = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(open_pendingorder_after_position_get_stop_loss_strategy, self).OnReseted()
        self._last_k = None
        self._prev_k = None
        self._entry_price = 0.0
        self._candles_since_trade = 0.0

    def OnStarted(self, time):
        super(open_pendingorder_after_position_get_stop_loss_strategy, self).OnStarted(time)

        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = self.k_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._rsi, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return open_pendingorder_after_position_get_stop_loss_strategy()
