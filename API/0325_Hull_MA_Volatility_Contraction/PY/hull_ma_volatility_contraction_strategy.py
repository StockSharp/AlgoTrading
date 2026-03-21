import clr
import math

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import HullMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy

class hull_ma_volatility_contraction_strategy(Strategy):
    """
    Hull MA with volatility contraction filter. Enters when HMA trends and ATR is contracted.
    """

    def __init__(self):
        super(hull_ma_volatility_contraction_strategy, self).__init__()
        self._hma_period = self.Param("HmaPeriod", 9).SetDisplay("HMA Period", "Hull MA period", "Indicators")
        self._atr_period = self.Param("AtrPeriod", 14).SetDisplay("ATR Period", "ATR period", "Volatility")
        self._contraction_factor = self.Param("ContractionFactor", 2.0).SetDisplay("Contraction Factor", "Stddev multiplier", "Volatility")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))).SetDisplay("Candle Type", "Timeframe", "General")

        self._prev_hma = 0.0
        self._cur_hma = 0.0
        self._atr_values = []
        self._is_long = False
        self._is_short = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(hull_ma_volatility_contraction_strategy, self).OnReseted()
        self._prev_hma = 0.0
        self._cur_hma = 0.0
        self._atr_values = []
        self._is_long = False
        self._is_short = False

    def OnStarted(self, time):
        super(hull_ma_volatility_contraction_strategy, self).OnStarted(time)
        hma = HullMovingAverage()
        hma.Length = self._hma_period.Value
        atr = AverageTrueRange()
        atr.Length = self._atr_period.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(hma, atr, self._process_candle).Start()
        self.StartProtection(Unit(2, UnitTypes.Percent), Unit(2, UnitTypes.Percent))
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, hma)
            self.DrawOwnTrades(area)

    def _is_volatility_contracted(self):
        period = self._atr_period.Value
        if len(self._atr_values) < period:
            return False
        recent = self._atr_values[-period:]
        mean = sum(recent) / len(recent)
        var = sum((x - mean) ** 2 for x in recent) / len(recent)
        std = math.sqrt(var)
        current = self._atr_values[-1]
        return current < (mean - std * self._contraction_factor.Value)

    def _process_candle(self, candle, hma_val, atr_val):
        if candle.State != CandleStates.Finished:
            return
        self._prev_hma = self._cur_hma
        self._cur_hma = float(hma_val)
        atr = float(atr_val)
        self._atr_values.append(atr)
        max_buf = self._atr_period.Value * 2
        if len(self._atr_values) > max_buf:
            self._atr_values = self._atr_values[-max_buf:]
        contracted = self._is_volatility_contracted()
        rising = self._cur_hma > self._prev_hma
        falling = self._cur_hma < self._prev_hma
        if rising and contracted and self.Position <= 0:
            self.BuyMarket()
            self._is_long = True
            self._is_short = False
        elif falling and contracted and self.Position >= 0:
            self.SellMarket()
            self._is_long = False
            self._is_short = True
        elif self._is_long and falling:
            self.SellMarket()
            self._is_long = False
        elif self._is_short and rising:
            self.BuyMarket()
            self._is_short = False

    def CreateClone(self):
        return hull_ma_volatility_contraction_strategy()
