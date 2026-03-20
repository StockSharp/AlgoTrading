import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergence
from StockSharp.Algo.Strategies import Strategy


class macd_four_colors2_martingale_strategy(Strategy):
    def __init__(self):
        super(macd_four_colors2_martingale_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(60) \
            .SetDisplay("Candle Type", "Type of candles for MACD analysis", "General")
        self._fast_ema_period = self.Param("FastEmaPeriod", 20) \
            .SetDisplay("Candle Type", "Type of candles for MACD analysis", "General")
        self._slow_ema_period = self.Param("SlowEmaPeriod", 50) \
            .SetDisplay("Candle Type", "Type of candles for MACD analysis", "General")
        self._signal_period = self.Param("SignalPeriod", 12) \
            .SetDisplay("Candle Type", "Type of candles for MACD analysis", "General")
        self._lot_coefficient = self.Param("LotCoefficient", 1.5) \
            .SetDisplay("Candle Type", "Type of candles for MACD analysis", "General")
        self._max_martingale = self.Param("MaxMartingale", 5) \
            .SetDisplay("Candle Type", "Type of candles for MACD analysis", "General")

        self._macd = None
        self._prev_histogram = None
        self._current_volume = 0.0
        self._consecutive_losses = 0.0
        self._entry_price = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(macd_four_colors2_martingale_strategy, self).OnReseted()
        self._macd = None
        self._prev_histogram = None
        self._current_volume = 0.0
        self._consecutive_losses = 0.0
        self._entry_price = 0.0

    def OnStarted(self, time):
        super(macd_four_colors2_martingale_strategy, self).OnStarted(time)


        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(_macd, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return macd_four_colors2_martingale_strategy()
