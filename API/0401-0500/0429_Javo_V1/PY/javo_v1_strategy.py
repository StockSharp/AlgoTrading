import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *


class javo_v1_strategy(Strategy):
    """
    Javo v1 strategy.
    Builds Heikin Ashi candles and runs a fast and a slow EMA on the Heikin Ashi close. Goes long when the Heikin Ashi
    candle is bullish and the fast EMA is above the slow one, short when the candle is bearish and the fast EMA is below
    the slow one. The opposite signal reverses the position.
    """

    def __init__(self):
        super(javo_v1_strategy, self).__init__()
        self._fast_ema_period = self.Param("FastEmaPeriod", 1) \
            .SetGreaterThanZero() \
            .SetDisplay("Fast EMA", "Fast EMA period on the Heikin Ashi close", "Moving Averages")
        self._slow_ema_period = self.Param("SlowEmaPeriod", 30) \
            .SetGreaterThanZero() \
            .SetDisplay("Slow EMA", "Slow EMA period on the Heikin Ashi close", "Moving Averages")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle type", "Candle type for strategy calculation", "General")

        self._fast_ema = None
        self._slow_ema = None
        self._prev_ha_open = None
        self._prev_ha_close = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType)]

    def OnReseted(self):
        super(javo_v1_strategy, self).OnReseted()
        self._fast_ema = None
        self._slow_ema = None
        self._prev_ha_open = None
        self._prev_ha_close = None

    def OnStarted2(self, time):
        super(javo_v1_strategy, self).OnStarted2(time)

        self._prev_ha_open = None
        self._prev_ha_close = None
        self._fast_ema = ExponentialMovingAverage()
        self._fast_ema.Length = self._fast_ema_period.Value
        self._slow_ema = ExponentialMovingAverage()
        self._slow_ema.Length = self._slow_ema_period.Value

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._fast_ema)
            self.DrawIndicator(area, self._slow_ema)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        open_price = float(candle.OpenPrice)
        close = float(candle.ClosePrice)

        ha_close = (open_price + float(candle.HighPrice) + float(candle.LowPrice) + close) / 4.0
        if self._prev_ha_open is not None and self._prev_ha_close is not None:
            ha_open = (self._prev_ha_open + self._prev_ha_close) / 2.0
        else:
            ha_open = (open_price + close) / 2.0

        self._prev_ha_open = ha_open
        self._prev_ha_close = ha_close

        fast = float(to_decimal(process_float(self._fast_ema, ha_close, candle.ServerTime, True)))
        slow = float(to_decimal(process_float(self._slow_ema, ha_close, candle.ServerTime, True)))

        if not self._fast_ema.IsFormed or not self._slow_ema.IsFormed:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if ha_close > ha_open and fast > slow and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif ha_close < ha_open and fast < slow and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))

    def CreateClone(self):
        return javo_v1_strategy()
