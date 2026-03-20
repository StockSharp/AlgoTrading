import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Messages import Sides


class martingale_breakout_strategy(Strategy):
    def __init__(self):
        super(martingale_breakout_strategy, self).__init__()

        self._lookback = self.Param("Lookback", 10) \
            .SetDisplay("Lookback", "Number of candles for average range", "General")
        self._breakout_multiplier = self.Param("BreakoutMultiplier", 3) \
            .SetDisplay("Lookback", "Number of candles for average range", "General")
        self._take_profit_pct = self.Param("TakeProfitPct", 1) \
            .SetDisplay("Lookback", "Number of candles for average range", "General")
        self._stop_loss_pct = self.Param("StopLossPct", 0.5) \
            .SetDisplay("Lookback", "Number of candles for average range", "General")
        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(60) \
            .SetDisplay("Lookback", "Number of candles for average range", "General")

        self._range_buffer_count = 0.0
        self._range_buffer_index = 0.0
        self._range_buffer_sum = 0.0
        self._entry_price = 0.0
        self._entry_side = None
        self._last_was_loss = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(martingale_breakout_strategy, self).OnReseted()
        self._range_buffer_count = 0.0
        self._range_buffer_index = 0.0
        self._range_buffer_sum = 0.0
        self._entry_price = 0.0
        self._entry_side = None
        self._last_was_loss = False

    def OnStarted(self, time):
        super(martingale_breakout_strategy, self).OnStarted(time)


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
        return martingale_breakout_strategy()
