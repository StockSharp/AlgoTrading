import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Messages import Unit, UnitTypes


class fx_chaos_scalp_strategy(Strategy):
    def __init__(self):
        super(fx_chaos_scalp_strategy, self).__init__()

        self._order_volume = self.Param("OrderVolume", 0.1) \
            .SetDisplay("Volume", "Order volume in lots", "Trading")
        self._stop_loss_points = self.Param("StopLossPoints", 50) \
            .SetDisplay("Volume", "Order volume in lots", "Trading")
        self._take_profit_points = self.Param("TakeProfitPoints", 50) \
            .SetDisplay("Volume", "Order volume in lots", "Trading")
        self._trading_candle_type = self.Param("TradingCandleType", TimeSpan.FromMinutes(5) \
            .SetDisplay("Volume", "Order volume in lots", "Trading")
        self._daily_candle_type = self.Param("DailyCandleType", TimeSpan.FromMinutes(5) \
            .SetDisplay("Volume", "Order volume in lots", "Trading")
        self._zig_zag_window_size = self.Param("ZigZagWindowSize", 5) \
            .SetDisplay("Volume", "Order volume in lots", "Trading")

        self._awesome_oscillator = None
        self._hourly_tracker = None
        self._daily_tracker = None
        self._previous_high = 0.0
        self._previous_low = 0.0
        self._has_previous = False
        self._entry_price = 0.0
        self._has_entry = False
        self._window_size = 0.0
        self._count = 0.0
        self._last_value = None
        self._direction = 0.0

    def OnReseted(self):
        super(fx_chaos_scalp_strategy, self).OnReseted()
        self._awesome_oscillator = None
        self._hourly_tracker = None
        self._daily_tracker = None
        self._previous_high = 0.0
        self._previous_low = 0.0
        self._has_previous = False
        self._entry_price = 0.0
        self._has_entry = False
        self._window_size = 0.0
        self._count = 0.0
        self._last_value = None
        self._direction = 0.0

    def OnStarted(self, time):
        super(fx_chaos_scalp_strategy, self).OnStarted(time)


        daily_subscription = self.SubscribeCandles(Dailyself.candle_type)
        daily_subscription.Bind(self._process_candle).Start()

        trading_subscription = self.SubscribeCandles(Tradingself.candle_type)
        trading_subscription.Bind(self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return fx_chaos_scalp_strategy()
