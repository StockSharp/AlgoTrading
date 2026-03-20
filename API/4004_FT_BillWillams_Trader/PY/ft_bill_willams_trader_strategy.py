import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage as SMA
from StockSharp.Algo.Strategies import Strategy


class ft_bill_willams_trader_strategy(Strategy):
    def __init__(self):
        super(ft_bill_willams_trader_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(8) \
            .SetDisplay("Candle Type", "Candle type for strategy", "General")
        self._jaw_period = self.Param("JawPeriod", 13) \
            .SetDisplay("Candle Type", "Candle type for strategy", "General")
        self._teeth_period = self.Param("TeethPeriod", 8) \
            .SetDisplay("Candle Type", "Candle type for strategy", "General")
        self._lips_period = self.Param("LipsPeriod", 5) \
            .SetDisplay("Candle Type", "Candle type for strategy", "General")
        self._fractal_len = self.Param("FractalLen", 5) \
            .SetDisplay("Candle Type", "Candle type for strategy", "General")

        self._jaw = null!
        self._teeth = null!
        self._lips = null!
        self._buf_count = 0.0
        self._pending_buy_level = None
        self._pending_sell_level = None
        self._prev_jaw = 0.0
        self._prev_teeth = 0.0
        self._prev_lips = 0.0
        self._entry_price = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(ft_bill_willams_trader_strategy, self).OnReseted()
        self._jaw = null!
        self._teeth = null!
        self._lips = null!
        self._buf_count = 0.0
        self._pending_buy_level = None
        self._pending_sell_level = None
        self._prev_jaw = 0.0
        self._prev_teeth = 0.0
        self._prev_lips = 0.0
        self._entry_price = 0.0

    def OnStarted(self, time):
        super(ft_bill_willams_trader_strategy, self).OnStarted(time)

        self.__jaw = SMA()
        self.__jaw.Length = self.jaw_period
        self.__teeth = SMA()
        self.__teeth.Length = self.teeth_period
        self.__lips = SMA()
        self.__lips.Length = self.lips_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.__jaw, self.__teeth, self.__lips, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return ft_bill_willams_trader_strategy()
