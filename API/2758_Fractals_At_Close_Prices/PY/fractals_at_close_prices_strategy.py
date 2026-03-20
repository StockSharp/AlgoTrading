import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class fractals_at_close_prices_strategy(Strategy):
    def __init__(self):
        super(fractals_at_close_prices_strategy, self).__init__()

        self._order_volume = self.Param("OrderVolume", 0.1) \
            .SetDisplay("Order Volume", "Volume used for entries", "General")
        self._start_hour = self.Param("StartHour", 0) \
            .SetDisplay("Order Volume", "Volume used for entries", "General")
        self._end_hour = self.Param("EndHour", 0) \
            .SetDisplay("Order Volume", "Volume used for entries", "General")
        self._stop_loss_pips = self.Param("StopLossPips", 200) \
            .SetDisplay("Order Volume", "Volume used for entries", "General")
        self._take_profit_pips = self.Param("TakeProfitPips", 400) \
            .SetDisplay("Order Volume", "Volume used for entries", "General")
        self._trailing_stop_pips = self.Param("TrailingStopPips", 15) \
            .SetDisplay("Order Volume", "Volume used for entries", "General")
        self._trailing_step_pips = self.Param("TrailingStepPips", 5) \
            .SetDisplay("Order Volume", "Volume used for entries", "General")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(4) \
            .SetDisplay("Order Volume", "Volume used for entries", "General")

        self._close_window = new(6)
        self._last_upper_fractal = None
        self._previous_upper_fractal = None
        self._last_lower_fractal = None
        self._previous_lower_fractal = None
        self._pip_value = 0.0
        self._stop_loss_distance = 0.0
        self._take_profit_distance = 0.0
        self._trailing_stop_distance = 0.0
        self._trailing_step_distance = 0.0
        self._entry_price = None
        self._long_stop = None
        self._long_take = None
        self._short_stop = None
        self._short_take = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(fractals_at_close_prices_strategy, self).OnReseted()
        self._close_window = new(6)
        self._last_upper_fractal = None
        self._previous_upper_fractal = None
        self._last_lower_fractal = None
        self._previous_lower_fractal = None
        self._pip_value = 0.0
        self._stop_loss_distance = 0.0
        self._take_profit_distance = 0.0
        self._trailing_stop_distance = 0.0
        self._trailing_step_distance = 0.0
        self._entry_price = None
        self._long_stop = None
        self._long_take = None
        self._short_stop = None
        self._short_take = None

    def OnStarted(self, time):
        super(fractals_at_close_prices_strategy, self).OnStarted(time)


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
        return fractals_at_close_prices_strategy()
