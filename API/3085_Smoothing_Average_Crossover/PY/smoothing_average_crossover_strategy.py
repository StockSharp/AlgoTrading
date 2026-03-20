import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage as EMA, SimpleMovingAverage as SMA, SmoothedMovingAverage, WeightedMovingAverage
from StockSharp.Algo.Strategies import Strategy


class smoothing_average_crossover_strategy(Strategy):
    def __init__(self):
        super(smoothing_average_crossover_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(2) \
            .SetDisplay("Candle Type", "Timeframe used for calculations", "General")
        self._ma_length = self.Param("MaLength", 60) \
            .SetDisplay("Candle Type", "Timeframe used for calculations", "General")
        self._ma_shift = self.Param("MaShift", 3) \
            .SetDisplay("Candle Type", "Timeframe used for calculations", "General")
        self._ma_type = self.Param("MaType", MovingAverageKinds.Simple) \
            .SetDisplay("Candle Type", "Timeframe used for calculations", "General")
        self._price_source = self.Param("PriceSource", CandlePrices.Typical) \
            .SetDisplay("Candle Type", "Timeframe used for calculations", "General")
        self._entry_delta_pips = self.Param("EntryDeltaPips", 60) \
            .SetDisplay("Candle Type", "Timeframe used for calculations", "General")
        self._close_delta_coefficient = self.Param("CloseDeltaCoefficient", 1.0) \
            .SetDisplay("Candle Type", "Timeframe used for calculations", "General")
        self._reverse_signals = self.Param("ReverseSignals", False) \
            .SetDisplay("Candle Type", "Timeframe used for calculations", "General")
        self._trade_volume = self.Param("TradeVolume", 1) \
            .SetDisplay("Candle Type", "Timeframe used for calculations", "General")

        self._ma_shift_buffer = new()
        self._entry_delta = 0.0
        self._close_delta = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(smoothing_average_crossover_strategy, self).OnReseted()
        self._ma_shift_buffer = new()
        self._entry_delta = 0.0
        self._close_delta = 0.0

    def OnStarted(self, time):
        super(smoothing_average_crossover_strategy, self).OnStarted(time)
        self.StartProtection(None, None)


        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(movingAverage, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return smoothing_average_crossover_strategy()
