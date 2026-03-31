import clr
import math

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import HullMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class hull_ma_volatility_contraction_strategy(Strategy):
    """
    Hull MA with volatility contraction filter. Enters when HMA trends and ATR is contracted.
    """

    def __init__(self):
        super(hull_ma_volatility_contraction_strategy, self).__init__()

        self._hma_period = self.Param("HmaPeriod", 9) \
            .SetDisplay("Hull MA Period", "Hull Moving Average period", "Hull MA") \
            .SetCanOptimize(True) \
            .SetOptimize(5, 20, 1)

        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetDisplay("ATR Period", "Period for ATR volatility calculation", "Volatility") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 30, 2)

        self._volatility_contraction_factor = self.Param("VolatilityContractionFactor", 2.0) \
            .SetDisplay("Volatility Contraction Factor", "Standard deviation multiplier for volatility contraction", "Volatility") \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 3.0, 0.5)

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._prev_hma = 0.0
        self._cur_hma = 0.0
        self._atr_values = []
        self._is_long = False
        self._is_short = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.candle_type)]

    def OnReseted(self):
        super(hull_ma_volatility_contraction_strategy, self).OnReseted()
        self._prev_hma = 0.0
        self._cur_hma = 0.0
        self._atr_values = []
        self._is_long = False
        self._is_short = False

    def OnStarted2(self, time):
        super(hull_ma_volatility_contraction_strategy, self).OnStarted2(time)

        hma = HullMovingAverage()
        hma.Length = int(self._hma_period.Value)

        atr = AverageTrueRange()
        atr.Length = int(self._atr_period.Value)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(hma, atr, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, hma)
            self.DrawIndicator(area, atr)
            self.DrawOwnTrades(area)

        self.StartProtection(
            Unit(2, UnitTypes.Percent),
            Unit(2, UnitTypes.Percent)
        )

    def _is_volatility_contracted(self):
        period = int(self._atr_period.Value)
        if len(self._atr_values) < period:
            return False
        recent = self._atr_values[-period:]
        mean = sum(recent) / len(recent)
        sum_sq = sum((x - mean) ** 2 for x in recent)
        std = math.sqrt(sum_sq / len(recent))
        current = self._atr_values[-1]
        return current < (mean - std * float(self._volatility_contraction_factor.Value))

    def _process_candle(self, candle, hma_val, atr_val):
        if candle.State != CandleStates.Finished:
            return

        self._prev_hma = self._cur_hma
        self._cur_hma = float(hma_val)
        atr = float(atr_val)

        self._atr_values.append(atr)
        max_buf = int(self._atr_period.Value) * 2
        while len(self._atr_values) > max_buf:
            self._atr_values.pop(0)

        contracted = self._is_volatility_contracted()
        rising = self._cur_hma > self._prev_hma
        falling = self._cur_hma < self._prev_hma

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if rising and contracted and self.Position <= 0:
            self.BuyMarket(self.Volume)
            self._is_long = True
            self._is_short = False
        elif falling and contracted and self.Position >= 0:
            self.SellMarket(self.Volume + Math.Abs(self.Position))
            self._is_long = False
            self._is_short = True
        elif self._is_long and falling:
            self.SellMarket(self.Position)
            self._is_long = False
        elif self._is_short and rising:
            self.BuyMarket(Math.Abs(self.Position))
            self._is_short = False

    def CreateClone(self):
        return hull_ma_volatility_contraction_strategy()
