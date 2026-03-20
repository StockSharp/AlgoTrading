import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Messages import Sides


class martin_gale_breakout_strategy(Strategy):
    def __init__(self):
        super(martin_gale_breakout_strategy, self).__init__()

        self._required_history = self.Param("RequiredHistory", 10) \
            .SetDisplay("Lookback", "Number of candles for average range", "General")
        self._breakout_factor = self.Param("BreakoutFactor", 2.5) \
            .SetDisplay("Lookback", "Number of candles for average range", "General")
        self._take_profit_pct = self.Param("TakeProfitPct", 0.5) \
            .SetDisplay("Lookback", "Number of candles for average range", "General")
        self._stop_loss_pct = self.Param("StopLossPct", 0.3) \
            .SetDisplay("Lookback", "Number of candles for average range", "General")
        self._recovery_multiplier = self.Param("RecoveryMultiplier", 1.5) \
            .SetDisplay("Lookback", "Number of candles for average range", "General")
        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(5) \
            .SetDisplay("Lookback", "Number of candles for average range", "General")

        self._ranges = new()
        self._entry_price = 0.0
        self._entry_side = None
        self._recovering = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(martin_gale_breakout_strategy, self).OnReseted()
        self._ranges = new()
        self._entry_price = 0.0
        self._entry_side = None
        self._recovering = False

    def OnStarted(self, time):
        super(martin_gale_breakout_strategy, self).OnStarted(time)


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
        return martin_gale_breakout_strategy()
