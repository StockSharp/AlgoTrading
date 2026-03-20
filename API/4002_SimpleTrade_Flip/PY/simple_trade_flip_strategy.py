import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Messages import Unit, UnitTypes


class simple_trade_flip_strategy(Strategy):
    def __init__(self):
        super(simple_trade_flip_strategy, self).__init__()

        self._trade_volume = self.Param("TradeVolume", 1) \
            .SetDisplay("Trade Volume", "Order size in lots", "Trading")
        self._stop_loss_points = self.Param("StopLossPoints", 120) \
            .SetDisplay("Trade Volume", "Order size in lots", "Trading")
        self._lookback_bars = self.Param("LookbackBars", 10) \
            .SetDisplay("Trade Volume", "Order size in lots", "Trading")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(8) \
            .SetDisplay("Trade Volume", "Order size in lots", "Trading")

        self._open_history = new()
        self._cooldown = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(simple_trade_flip_strategy, self).OnReseted()
        self._open_history = new()
        self._cooldown = 0.0

    def OnStarted(self, time):
        super(simple_trade_flip_strategy, self).OnStarted(time)
        self.StartProtection(None, None)


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
        return simple_trade_flip_strategy()
