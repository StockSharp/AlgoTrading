import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class heikin_ashi_v2_strategy(Strategy):
    """
    Heikin Ashi V2 strategy.
    Goes long when the Heikin Ashi candle is bullish and the close is above the EMA, and short when the Heikin Ashi
    candle is bearish and the close is below the EMA. The opposite signal reverses the position.
    """

    def __init__(self):
        super(heikin_ashi_v2_strategy, self).__init__()
        self._ema_length = self.Param("EmaLength", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("EMA Length", "EMA trend filter period", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle type", "Candle type for strategy calculation", "General")

        self._prev_ha_open = None
        self._prev_ha_close = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType)]

    def OnReseted(self):
        super(heikin_ashi_v2_strategy, self).OnReseted()
        self._prev_ha_open = None
        self._prev_ha_close = None

    def OnStarted2(self, time):
        super(heikin_ashi_v2_strategy, self).OnStarted2(time)

        self._prev_ha_open = None
        self._prev_ha_close = None

        ema = ExponentialMovingAverage()
        ema.Length = self._ema_length.Value

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(ema, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, ema_value):
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

        if not ema_value.IsFormed:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        ema = float(ema_value.GetValue[Decimal](None))

        if ha_close > ha_open and close > ema and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif ha_close < ha_open and close < ema and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))

    def CreateClone(self):
        return heikin_ashi_v2_strategy()
