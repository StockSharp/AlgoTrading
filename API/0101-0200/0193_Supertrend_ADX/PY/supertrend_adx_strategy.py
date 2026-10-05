import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SuperTrend, AverageDirectionalIndex
from StockSharp.Algo.Strategies import Strategy

class supertrend_adx_strategy(Strategy):
    """
    Supertrend ADX strategy.
    A close above the Supertrend line while ADX is above AdxThreshold goes long and a close below it while ADX is above the threshold goes short,
    reversing an opposite position. The Supertrend line is the trailing stop: a long closes when price closes below it, as Supertrend flips
    down, and a short when price closes above it.
    """

    def __init__(self):
        super(supertrend_adx_strategy, self).__init__()
        self._supertrend_period = self.Param("SupertrendPeriod", 10).SetGreaterThanZero().SetDisplay("Supertrend Period", "ATR period of Supertrend", "Supertrend")
        self._supertrend_multiplier = self.Param("SupertrendMultiplier", 3.0).SetGreaterThanZero().SetDisplay("Supertrend Multiplier", "ATR multiplier of Supertrend", "Supertrend")
        self._adx_period = self.Param("AdxPeriod", 14).SetGreaterThanZero().SetDisplay("ADX Period", "Period of ADX", "ADX")
        self._adx_threshold = self.Param("AdxThreshold", 25.0).SetDisplay("ADX Threshold", "ADX level of a strong trend", "ADX")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))).SetDisplay("Candle Type", "Type of candles to use", "General")

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnStarted2(self, time):
        super(supertrend_adx_strategy, self).OnStarted2(time)

        supertrend = SuperTrend()
        supertrend.Length = self._supertrend_period.Value
        supertrend.Multiplier = Decimal(self._supertrend_multiplier.Value)
        adx = AverageDirectionalIndex()
        adx.Length = self._adx_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(supertrend, adx, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, supertrend)
            self.DrawOwnTrades(area)
            oscillators = self.CreateChartArea()
            if oscillators is not None:
                self.DrawIndicator(oscillators, adx)

    def _process_candle(self, candle, supertrend_value, adx_value):
        if candle.State != CandleStates.Finished:
            return

        if not supertrend_value.IsFormed or not adx_value.IsFormed or adx_value.MovingAverage is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        line = supertrend_value.Value
        is_up_trend = supertrend_value.IsUpTrend
        close = candle.ClosePrice
        strong = adx_value.MovingAverage > Decimal(self._adx_threshold.Value)

        if close > line and strong and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif close < line and strong and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
        elif self.Position > 0 and not is_up_trend:
            self.SellMarket(self.Position)
        elif self.Position < 0 and is_up_trend:
            self.BuyMarket(-self.Position)

    def CreateClone(self):
        return supertrend_adx_strategy()
