import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class regularities_of_exchange_rates_strategy(Strategy):
    def __init__(self):
        super(regularities_of_exchange_rates_strategy, self).__init__()

        self._opening_hour = self.Param("OpeningHour", 9) \
            .SetDisplay("Opening Hour", "Hour (0-23) when breakout levels are set", "Schedule")
        self._closing_hour = self.Param("ClosingHour", 2) \
            .SetDisplay("Opening Hour", "Hour (0-23) when breakout levels are set", "Schedule")
        self._entry_offset_points = self.Param("EntryOffsetPoints", 20) \
            .SetDisplay("Opening Hour", "Hour (0-23) when breakout levels are set", "Schedule")
        self._take_profit_points = self.Param("TakeProfitPoints", 20) \
            .SetDisplay("Opening Hour", "Hour (0-23) when breakout levels are set", "Schedule")
        self._stop_loss_points = self.Param("StopLossPoints", 500) \
            .SetDisplay("Opening Hour", "Hour (0-23) when breakout levels are set", "Schedule")
        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(5) \
            .SetDisplay("Opening Hour", "Hour (0-23) when breakout levels are set", "Schedule")

        self._dummy_sma = None
        self._point_size = 0.0
        self._last_entry_date = None
        self._reference_price = 0.0
        self._entry_price = 0.0
        self._waiting_for_breakout = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(regularities_of_exchange_rates_strategy, self).OnReseted()
        self._dummy_sma = None
        self._point_size = 0.0
        self._last_entry_date = None
        self._reference_price = 0.0
        self._entry_price = 0.0
        self._waiting_for_breakout = False

    def OnStarted(self, time):
        super(regularities_of_exchange_rates_strategy, self).OnStarted(time)

        self.__dummy_sma = SimpleMovingAverage()
        self.__dummy_sma.Length = 2

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.__dummy_sma, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return regularities_of_exchange_rates_strategy()
