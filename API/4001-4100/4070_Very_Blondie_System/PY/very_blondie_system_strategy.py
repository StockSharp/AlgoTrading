import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import Highest, Lowest, SimpleMovingAverage

class very_blondie_system_strategy(Strategy):
    def __init__(self):
        super(very_blondie_system_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe for analysis", "General")
        self._period_length = self.Param("PeriodLength", 30) \
            .SetDisplay("Period Length", "Period for Highest/Lowest channel", "Indicators")

    @property
    def CandleType(self):
        return self._candle_type.Value

    @property
    def PeriodLength(self):
        return self._period_length.Value

    def OnStarted2(self, time):
        super(very_blondie_system_strategy, self).OnStarted2(time)

        self._highest = Highest()
        self._highest.Length = self.PeriodLength
        self._lowest = Lowest()
        self._lowest.Length = self.PeriodLength
        self._sma = SimpleMovingAverage()
        self._sma.Length = self.PeriodLength

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._highest, self._lowest, self._sma, self.ProcessCandle).Start()

    def ProcessCandle(self, candle, highest_value, lowest_value, sma_value):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        high_val = float(highest_value)
        low_val = float(lowest_value)
        sma_val = float(sma_value)
        rng = high_val - low_val
        if rng <= 0:
            return

        position_pct = (close - low_val) / rng

        # Exit conditions: revert to mean
        if self.Position > 0 and close >= sma_val:
            self.SellMarket()
        elif self.Position < 0 and close <= sma_val:
            self.BuyMarket()

        # Entry: at channel extremes
        if self.Position == 0:
            if position_pct < 0.15:
                self.BuyMarket()
            elif position_pct > 0.85:
                self.SellMarket()

    def OnReseted(self):
        super(very_blondie_system_strategy, self).OnReseted()

    def CreateClone(self):
        return very_blondie_system_strategy()
