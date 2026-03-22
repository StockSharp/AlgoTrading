import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import SuperTrend, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class supertrend_rsi_divergence_strategy(Strategy):
    """
    Strategy that uses Supertrend indicator along with RSI divergence to identify trading opportunities.
    """

    def __init__(self):
        super(supertrend_rsi_divergence_strategy, self).__init__()

        self._supertrend_period = self.Param("SupertrendPeriod", 10) \
            .SetDisplay("Supertrend Period", "Supertrend ATR period", "Supertrend")

        self._supertrend_multiplier = self.Param("SupertrendMultiplier", 3.0) \
            .SetDisplay("Supertrend Multiplier", "Supertrend ATR multiplier", "Supertrend")

        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("RSI Period", "RSI period for divergence detection", "RSI")

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._prices = []
        self._rsi_values = []
        self._supertrend_val = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(supertrend_rsi_divergence_strategy, self).OnReseted()
        self._prices = []
        self._rsi_values = []
        self._supertrend_val = 0.0

    def OnStarted(self, time):
        super(supertrend_rsi_divergence_strategy, self).OnStarted(time)

        supertrend = SuperTrend()
        supertrend.Length = int(self._supertrend_period.Value)
        supertrend.Multiplier = Decimal(self._supertrend_multiplier.Value)

        rsi = RelativeStrengthIndex()
        rsi.Length = int(self._rsi_period.Value)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(supertrend, rsi, self._process_candle).Start()

        self.StartProtection(
            Unit(2, UnitTypes.Percent),
            Unit(1, UnitTypes.Percent)
        )

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, supertrend)
            self.DrawIndicator(area, rsi)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, supertrend_value, rsi_value):
        if candle.State != CandleStates.Finished:
            return

        self._supertrend_val = float(supertrend_value)
        rsi = float(rsi_value)
        close_price = float(candle.ClosePrice)

        self._prices.append(close_price)
        self._rsi_values.append(rsi)

        while len(self._prices) > 50:
            self._prices.pop(0)
            self._rsi_values.pop(0)

        if self.Position != 0:
            return

        if close_price > self._supertrend_val and rsi < 60.0:
            self.BuyMarket()
        elif close_price < self._supertrend_val and rsi > 40.0:
            self.SellMarket()

    def CreateClone(self):
        return supertrend_rsi_divergence_strategy()
