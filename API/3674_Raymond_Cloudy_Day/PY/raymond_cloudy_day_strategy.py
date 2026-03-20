import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class raymond_cloudy_day_strategy(Strategy):
    def __init__(self):
        super(raymond_cloudy_day_strategy, self).__init__()

        self._trade_volume = self.Param("TradeVolume", 1) \
            .SetDisplay("Trade Volume", "Order volume used for entries", "Trading")
        self._protective_offset_ticks = self.Param("ProtectiveOffsetTicks", 500) \
            .SetDisplay("Trade Volume", "Order volume used for entries", "Trading")
        self._signal_candle_type = self.Param("SignalCandleType", TimeSpan.FromMinutes(5) \
            .SetDisplay("Trade Volume", "Order volume used for entries", "Trading")
        self._pivot_candle_type = self.Param("PivotCandleType", TimeSpan.FromMinutes(5) \
            .SetDisplay("Trade Volume", "Order volume used for entries", "Trading")

        self._trade_session_level = None
        self._extended_buy_level = None
        self._extended_sell_level = None
        self._take_profit_buy_level = None
        self._take_profit_sell_level = None
        self._take_profit_buy_level2 = None
        self._take_profit_sell_level2 = None
        self._entry_price = None
        self._take_price = None
        self._stop_price = None

    def OnReseted(self):
        super(raymond_cloudy_day_strategy, self).OnReseted()
        self._trade_session_level = None
        self._extended_buy_level = None
        self._extended_sell_level = None
        self._take_profit_buy_level = None
        self._take_profit_sell_level = None
        self._take_profit_buy_level2 = None
        self._take_profit_sell_level2 = None
        self._entry_price = None
        self._take_price = None
        self._stop_price = None

    def OnStarted(self, time):
        super(raymond_cloudy_day_strategy, self).OnStarted(time)


        signal_subscription = self.SubscribeCandles(Signalself.candle_type)
        signal_subscription.Bind(self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return raymond_cloudy_day_strategy()
