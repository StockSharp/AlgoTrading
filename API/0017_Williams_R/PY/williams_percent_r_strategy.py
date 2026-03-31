import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Highest, Lowest
from StockSharp.Algo.Strategies import Strategy

class williams_percent_r_strategy(Strategy):
    """
    Strategy based on Williams %R indicator.
    Buys when Williams %R crosses from oversold zone upward,
    sells when it crosses from overbought zone downward.
    """

    def __init__(self):
        super(williams_percent_r_strategy, self).__init__()
        self._period = self.Param("Period", 14) \
            .SetDisplay("Period", "Period for Williams %R calculation", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._prev_wr = 0.0
        self._has_prev_values = False
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(williams_percent_r_strategy, self).OnReseted()
        self._prev_wr = 0.0
        self._has_prev_values = False
        self._cooldown = 0

    def OnStarted2(self, time):
        super(williams_percent_r_strategy, self).OnStarted2(time)

        highest = Highest()
        highest.Length = self._period.Value
        lowest = Lowest()
        lowest.Length = self._period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(highest, lowest, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, highest)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, highest_val, lowest_val):
        if candle.State != CandleStates.Finished:
            return

        h = float(highest_val)
        l = float(lowest_val)
        rng = h - l
        if rng == 0:
            return

        wr = (h - float(candle.ClosePrice)) / rng * -100.0

        if not self._has_prev_values:
            self._has_prev_values = True
            self._prev_wr = wr
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            self._prev_wr = wr
            return

        if self._prev_wr < -80 and wr >= -80 and self.Position <= 0:
            self.BuyMarket()
            self._cooldown = 50
        elif self._prev_wr > -20 and wr <= -20 and self.Position >= 0:
            self.SellMarket()
            self._cooldown = 50

        self._prev_wr = wr

    def CreateClone(self):
        return williams_percent_r_strategy()
