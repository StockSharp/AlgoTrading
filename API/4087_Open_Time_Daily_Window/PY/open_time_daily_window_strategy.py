import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class open_time_daily_window_strategy(Strategy):
    def __init__(self):
        super(open_time_daily_window_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(15) \
            .SetDisplay("Candle Type", "Timeframe.", "General")
        self._ema_length = self.Param("EmaLength", 20) \
            .SetDisplay("Candle Type", "Timeframe.", "General")
        self._trade_hour = self.Param("TradeHour", 10) \
            .SetDisplay("Candle Type", "Timeframe.", "General")
        self._window_minutes = self.Param("WindowMinutes", 120) \
            .SetDisplay("Candle Type", "Timeframe.", "General")
        self._close_hour = self.Param("CloseHour", 20) \
            .SetDisplay("Candle Type", "Timeframe.", "General")

        self._prev_ema = 0.0
        self._entry_price = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(open_time_daily_window_strategy, self).OnReseted()
        self._prev_ema = 0.0
        self._entry_price = 0.0

    def OnStarted(self, time):
        super(open_time_daily_window_strategy, self).OnStarted(time)

        self._ema = ExponentialMovingAverage()
        self._ema.Length = self.ema_length

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._ema, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return open_time_daily_window_strategy()
