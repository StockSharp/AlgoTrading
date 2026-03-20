import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Messages import Unit, UnitTypes


class price_extreme_strategy(Strategy):
    def __init__(self):
        super(price_extreme_strategy, self).__init__()

        self._level_length = self.Param("LevelLength", 20) \
            .SetDisplay("Level Length", "Number of candles for price extremes", "Indicator")
        self._signal_shift = self.Param("SignalShift", 1) \
            .SetDisplay("Level Length", "Number of candles for price extremes", "Indicator")
        self._enable_long = self.Param("EnableLong", True) \
            .SetDisplay("Level Length", "Number of candles for price extremes", "Indicator")
        self._enable_short = self.Param("EnableShort", True) \
            .SetDisplay("Level Length", "Number of candles for price extremes", "Indicator")
        self._reverse_signals = self.Param("ReverseSignals", False) \
            .SetDisplay("Level Length", "Number of candles for price extremes", "Indicator")
        self._order_volume = self.Param("OrderVolume", 0.1) \
            .SetDisplay("Level Length", "Number of candles for price extremes", "Indicator")
        self._stop_loss_points = self.Param("StopLossPoints", 0) \
            .SetDisplay("Level Length", "Number of candles for price extremes", "Indicator")
        self._take_profit_points = self.Param("TakeProfitPoints", 0) \
            .SetDisplay("Level Length", "Number of candles for price extremes", "Indicator")
        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(5) \
            .SetDisplay("Level Length", "Number of candles for price extremes", "Indicator")

        self._history = new()
        self._highs = new()
        self._lows = new()
        self._stop_price = None
        self._take_price = None
        self._prev_position = 0.0
        self._entry_price = 0.0
        self._prev_upper = 0.0
        self._prev_lower = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(price_extreme_strategy, self).OnReseted()
        self._history = new()
        self._highs = new()
        self._lows = new()
        self._stop_price = None
        self._take_price = None
        self._prev_position = 0.0
        self._entry_price = 0.0
        self._prev_upper = 0.0
        self._prev_lower = 0.0

    def OnStarted(self, time):
        super(price_extreme_strategy, self).OnStarted(time)


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
        return price_extreme_strategy()
