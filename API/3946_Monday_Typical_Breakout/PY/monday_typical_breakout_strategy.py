import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Messages import Unit, UnitTypes


class monday_typical_breakout_strategy(Strategy):
    def __init__(self):
        super(monday_typical_breakout_strategy, self).__init__()

        self._fixed_volume = self.Param("FixedVolume", 0.1) \
            .SetDisplay("Fixed Volume", "Lot size used for entries (set to 0 to enable equity scaling)", "Risk")
        self._open_hour = self.Param("OpenHour", 9) \
            .SetDisplay("Fixed Volume", "Lot size used for entries (set to 0 to enable equity scaling)", "Risk")
        self._stop_loss_points = self.Param("StopLossPoints", 50) \
            .SetDisplay("Fixed Volume", "Lot size used for entries (set to 0 to enable equity scaling)", "Risk")
        self._take_profit_points = self.Param("TakeProfitPoints", 20) \
            .SetDisplay("Fixed Volume", "Lot size used for entries (set to 0 to enable equity scaling)", "Risk")
        self._initial_equity = self.Param("InitialEquity", 600) \
            .SetDisplay("Fixed Volume", "Lot size used for entries (set to 0 to enable equity scaling)", "Risk")
        self._equity_step = self.Param("EquityStep", 300) \
            .SetDisplay("Fixed Volume", "Lot size used for entries (set to 0 to enable equity scaling)", "Risk")
        self._initial_step_volume = self.Param("InitialStepVolume", 0.4) \
            .SetDisplay("Fixed Volume", "Lot size used for entries (set to 0 to enable equity scaling)", "Risk")
        self._volume_step = self.Param("VolumeStep", 0.2) \
            .SetDisplay("Fixed Volume", "Lot size used for entries (set to 0 to enable equity scaling)", "Risk")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(1) \
            .SetDisplay("Fixed Volume", "Lot size used for entries (set to 0 to enable equity scaling)", "Risk")

        self._previous_candle = None
        self._last_signal_time = None
        self._price_step = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(monday_typical_breakout_strategy, self).OnReseted()
        self._previous_candle = None
        self._last_signal_time = None
        self._price_step = 0.0

    def OnStarted(self, time):
        super(monday_typical_breakout_strategy, self).OnStarted(time)


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
        return monday_typical_breakout_strategy()
