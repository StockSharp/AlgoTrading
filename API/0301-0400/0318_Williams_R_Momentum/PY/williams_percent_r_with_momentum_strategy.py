import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import WilliamsR, Momentum, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *

class williams_percent_r_with_momentum_strategy(Strategy):
    """
    Strategy based on Williams %R with Momentum filter.
    """

    def __init__(self):
        super(williams_percent_r_with_momentum_strategy, self).__init__()

        self._williams_r_period = self.Param("WilliamsRPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("Williams %R Period", "Period for Williams %R calculation", "Indicators")

        self._momentum_period = self.Param("MomentumPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("Momentum Period", "Period for Momentum calculation", "Indicators")

        self._williams_r_oversold = self.Param("WilliamsROversold", -80.0) \
            .SetDisplay("Williams %R Oversold", "Williams %R oversold level", "Indicators")

        self._williams_r_overbought = self.Param("WilliamsROverbought", -20.0) \
            .SetDisplay("Williams %R Overbought", "Williams %R overbought level", "Indicators")

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(williams_percent_r_with_momentum_strategy, self).OnReseted()

    def OnStarted2(self, time):
        super(williams_percent_r_with_momentum_strategy, self).OnStarted2(time)

        williams_r = WilliamsR()
        williams_r.Length = int(self._williams_r_period.Value)
        momentum = Momentum()
        momentum.Length = int(self._momentum_period.Value)
        self._momentum_sma = SimpleMovingAverage()
        self._momentum_sma.Length = int(self._momentum_period.Value)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(williams_r, momentum, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, williams_r)
            self.DrawIndicator(area, momentum)
            self.DrawOwnTrades(area)

        self.StartProtection(
            Unit(2, UnitTypes.Percent),
            Unit(1, UnitTypes.Percent)
        )

    def _process_candle(self, candle, williams_r_value, momentum_value):
        momentum_avg = float(process_float(self._momentum_sma, Decimal(float(momentum_value)), candle.ServerTime, True))

        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        wr = float(williams_r_value)
        mom = float(momentum_value)
        is_momentum_rising = mom > momentum_avg

        oversold = float(self._williams_r_oversold.Value)
        overbought = float(self._williams_r_overbought.Value)

        if wr < oversold and is_momentum_rising and self.Position <= 0:
            volume = self.Volume + Math.Abs(self.Position)
            self.BuyMarket(volume)
        elif wr > overbought and not is_momentum_rising and self.Position >= 0:
            volume = self.Volume + Math.Abs(self.Position)
            self.SellMarket(volume)

        if self.Position > 0 and wr > -50:
            self.SellMarket(Math.Abs(self.Position))
        elif self.Position < 0 and wr < -50:
            self.BuyMarket(Math.Abs(self.Position))

    def CreateClone(self):
        return williams_percent_r_with_momentum_strategy()
