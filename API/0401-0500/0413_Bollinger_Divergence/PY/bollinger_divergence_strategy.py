import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import BollingerBands
from StockSharp.Algo.Strategies import Strategy


class bollinger_divergence_strategy(Strategy):
    """
    Bollinger Divergence strategy.
    Goes long when a candle closes below the lower band while the upper band contracts, and short when a candle closes
    above the upper band while the lower band contracts. The contraction of the opposite band over the bar must reach
    CandlePercent percent of the signal candle's range. Positions exit on a return to the middle band or at the
    TakeProfit percentage.
    """

    def __init__(self):
        super(bollinger_divergence_strategy, self).__init__()
        self._bb_length = self.Param("BBLength", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("BB Period", "Bollinger Bands period", "Bollinger Bands")
        self._bb_multiplier = self.Param("BBMultiplier", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("BB StdDev", "Bollinger Bands standard deviation multiplier", "Bollinger Bands")
        self._candle_percent = self.Param("CandlePercent", 30.0) \
            .SetNotNegative() \
            .SetDisplay("Candle Percent", "Required contraction of the opposite band in percent of the candle range", "Signals")
        self._take_profit = self.Param("TakeProfit", 5.0) \
            .SetNotNegative() \
            .SetDisplay("Take Profit %", "Take profit percentage", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))) \
            .SetDisplay("Candle type", "Candle type for strategy calculation", "General")

        self._prev_upper_band = None
        self._prev_lower_band = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType)]

    def OnReseted(self):
        super(bollinger_divergence_strategy, self).OnReseted()
        self._prev_upper_band = None
        self._prev_lower_band = None

    def OnStarted2(self, time):
        super(bollinger_divergence_strategy, self).OnStarted2(time)

        self._prev_upper_band = None
        self._prev_lower_band = None

        bollinger = BollingerBands()
        bollinger.Length = self._bb_length.Value
        bollinger.Width = Decimal(self._bb_multiplier.Value)

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(bollinger, self._process_candle).Start()

        take_profit = float(self._take_profit.Value)
        self.StartProtection(
            Unit(Decimal(take_profit), UnitTypes.Percent) if take_profit > 0 else Unit(),
            Unit(),
            useMarketOrders=True)

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, bollinger)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, bollinger_value):
        if candle.State != CandleStates.Finished:
            return

        if not bollinger_value.IsFormed:
            return

        if bollinger_value.UpBand is None or bollinger_value.LowBand is None or bollinger_value.MovingAverage is None:
            return

        upper_band = float(bollinger_value.UpBand)
        lower_band = float(bollinger_value.LowBand)
        middle_band = float(bollinger_value.MovingAverage)

        prev_upper = self._prev_upper_band
        prev_lower = self._prev_lower_band

        self._prev_upper_band = upper_band
        self._prev_lower_band = lower_band

        if prev_upper is None or prev_lower is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        close = float(candle.ClosePrice)
        min_contraction = (float(candle.HighPrice) - float(candle.LowPrice)) * float(self._candle_percent.Value) / 100.0

        upper_contraction = prev_upper - upper_band
        lower_contraction = lower_band - prev_lower

        long_signal = close < lower_band and upper_contraction > 0 and upper_contraction >= min_contraction
        short_signal = close > upper_band and lower_contraction > 0 and lower_contraction >= min_contraction

        if long_signal and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif short_signal and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
        elif self.Position > 0 and close >= middle_band:
            self.SellMarket(self.Position)
        elif self.Position < 0 and close <= middle_band:
            self.BuyMarket(-self.Position)

    def CreateClone(self):
        return bollinger_divergence_strategy()
