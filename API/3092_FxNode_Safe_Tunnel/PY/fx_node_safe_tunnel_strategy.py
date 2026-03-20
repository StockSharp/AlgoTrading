import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange, Highest, Lowest
from StockSharp.Algo.Strategies import Strategy


class fx_node_safe_tunnel_strategy(Strategy):
    def __init__(self):
        super(fx_node_safe_tunnel_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(5) \
            .SetDisplay("Candle Type", "Timeframe", "General")
        self._channel_period = self.Param("ChannelPeriod", 100) \
            .SetDisplay("Candle Type", "Timeframe", "General")
        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetDisplay("Candle Type", "Timeframe", "General")
        self._touch_pct = self.Param("TouchPct", 0.02) \
            .SetDisplay("Candle Type", "Timeframe", "General")

        self._entry_price = 0.0
        self._cooldown = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(fx_node_safe_tunnel_strategy, self).OnReseted()
        self._entry_price = 0.0
        self._cooldown = 0.0

    def OnStarted(self, time):
        super(fx_node_safe_tunnel_strategy, self).OnStarted(time)

        self._highest = Highest()
        self._highest.Length = self.channel_period
        self._lowest = Lowest()
        self._lowest.Length = self.channel_period
        self._atr = AverageTrueRange()
        self._atr.Length = self.atr_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._highest, self._lowest, self._atr, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return fx_node_safe_tunnel_strategy()
