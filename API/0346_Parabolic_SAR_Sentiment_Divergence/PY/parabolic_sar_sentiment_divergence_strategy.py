import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import ParabolicSar
from StockSharp.Algo.Strategies import Strategy


class parabolic_sar_sentiment_divergence_strategy(Strategy):
    """Parabolic SAR strategy with sentiment divergence."""

    def __init__(self):
        super(parabolic_sar_sentiment_divergence_strategy, self).__init__()

        self._start_af = self.Param("StartAf", 0.02) \
            .SetRange(0.01, 0.1) \
            .SetDisplay("Starting AF", "Starting acceleration factor for Parabolic SAR", "SAR Parameters")

        self._max_af = self.Param("MaxAf", 0.2) \
            .SetRange(0.1, 0.5) \
            .SetDisplay("Maximum AF", "Maximum acceleration factor for Parabolic SAR", "SAR Parameters")

        self._cooldown_bars = self.Param("CooldownBars", 24) \
            .SetNotNegative() \
            .SetDisplay("Cooldown Bars", "Closed candles to wait before another position change", "General")

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._prev_sentiment = 0.0
        self._prev_price = 0.0
        self._prev_above_sar = False
        self._is_first_candle = True
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.candle_type)]

    def OnReseted(self):
        super(parabolic_sar_sentiment_divergence_strategy, self).OnReseted()
        self._prev_sentiment = 0.0
        self._prev_price = 0.0
        self._prev_above_sar = False
        self._is_first_candle = True
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(parabolic_sar_sentiment_divergence_strategy, self).OnStarted(time)

        sar = ParabolicSar()
        sar.Acceleration = Decimal(float(self._start_af.Value))
        sar.AccelerationMax = Decimal(float(self._max_af.Value))

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(sar, self.ProcessCandle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sar)
            self.DrawOwnTrades(area)

        self.StartProtection(
            Unit(2, UnitTypes.Percent),
            Unit(2, UnitTypes.Percent),
            True
        )

    def ProcessCandle(self, candle, sar_price):
        if candle.State != CandleStates.Finished:
            return

        price = float(candle.ClosePrice)
        sar = float(sar_price)
        price_above_sar = price > sar

        if self._is_first_candle:
            self._prev_price = price
            self._prev_above_sar = price_above_sar
            self._is_first_candle = False
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1

        cooldown = int(self._cooldown_bars.Value)

        bullish_flip = (not self._prev_above_sar) and price_above_sar
        bearish_flip = self._prev_above_sar and (not price_above_sar)

        if self._cooldown_remaining == 0 and self.Position == 0:
            if bullish_flip:
                self.BuyMarket()
                self._cooldown_remaining = cooldown
            elif bearish_flip:
                self.SellMarket()
                self._cooldown_remaining = cooldown

        self._prev_price = price
        self._prev_above_sar = price_above_sar

    def CreateClone(self):
        return parabolic_sar_sentiment_divergence_strategy()
