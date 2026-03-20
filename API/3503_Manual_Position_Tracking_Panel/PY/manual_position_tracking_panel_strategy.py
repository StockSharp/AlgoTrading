import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import MoneyFlowIndex
from StockSharp.Algo.Strategies import Strategy


class manual_position_tracking_panel_strategy(Strategy):
    def __init__(self):
        super(manual_position_tracking_panel_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(60) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._period = self.Param("Period", 14) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._signal_cooldown_candles = self.Param("SignalCooldownCandles", 4) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")

        self._prev_mfi = 0.0
        self._candles_since_trade = 0.0
        self._has_prev = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(manual_position_tracking_panel_strategy, self).OnReseted()
        self._prev_mfi = 0.0
        self._candles_since_trade = 0.0
        self._has_prev = False

    def OnStarted(self, time):
        super(manual_position_tracking_panel_strategy, self).OnStarted(time)

        self._mfi = MoneyFlowIndex()
        self._mfi.Length = self.period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._mfi, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return manual_position_tracking_panel_strategy()
