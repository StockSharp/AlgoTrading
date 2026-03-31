import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import RelativeStrengthIndex, DecimalIndicatorValue


class slime_mold_rsi_strategy(Strategy):
    """Slime Mold RSI perceptron: sums weighted RSI inputs for zero-crossing signals."""

    def __init__(self):
        super(slime_mold_rsi_strategy, self).__init__()

        self._weight1 = self.Param("Weight1", -100.0) \
            .SetDisplay("Weight 1", "Weight applied to the 12-period RSI input", "Perceptron")
        self._weight2 = self.Param("Weight2", -100.0) \
            .SetDisplay("Weight 2", "Weight applied to the 36-period RSI input", "Perceptron")
        self._weight3 = self.Param("Weight3", -100.0) \
            .SetDisplay("Weight 3", "Weight applied to the 108-period RSI input", "Perceptron")
        self._weight4 = self.Param("Weight4", -100.0) \
            .SetDisplay("Weight 4", "Weight applied to the 324-period RSI input", "Perceptron")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Timeframe for candles used in calculations", "General")

        self._prev_perceptron = None

    @property
    def Weight1(self):
        return float(self._weight1.Value)
    @property
    def Weight2(self):
        return float(self._weight2.Value)
    @property
    def Weight3(self):
        return float(self._weight3.Value)
    @property
    def Weight4(self):
        return float(self._weight4.Value)
    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnStarted2(self, time):
        super(slime_mold_rsi_strategy, self).OnStarted2(time)

        self._prev_perceptron = None

        self._rsi12 = RelativeStrengthIndex()
        self._rsi12.Length = 12
        self._rsi36 = RelativeStrengthIndex()
        self._rsi36.Length = 36
        self._rsi108 = RelativeStrengthIndex()
        self._rsi108.Length = 108
        self._rsi324 = RelativeStrengthIndex()
        self._rsi324.Length = 324

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.process_candle).Start()

    def process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        median_price = (candle.HighPrice + candle.LowPrice) / 2

        mp = Decimal(float(median_price))
        t = candle.ServerTime

        iv1 = DecimalIndicatorValue(self._rsi12, mp, t)
        iv1.IsFinal = True
        r12_val = self._rsi12.Process(iv1)

        iv2 = DecimalIndicatorValue(self._rsi36, mp, t)
        iv2.IsFinal = True
        r36_val = self._rsi36.Process(iv2)

        iv3 = DecimalIndicatorValue(self._rsi108, mp, t)
        iv3.IsFinal = True
        r108_val = self._rsi108.Process(iv3)

        iv4 = DecimalIndicatorValue(self._rsi324, mp, t)
        iv4.IsFinal = True
        r324_val = self._rsi324.Process(iv4)

        if not self._rsi12.IsFormed or not self._rsi36.IsFormed or not self._rsi108.IsFormed or not self._rsi324.IsFormed:
            return

        r12 = float(r12_val.Value)
        r36 = float(r36_val.Value)
        r108 = float(r108_val.Value)
        r324 = float(r324_val.Value)

        current = (self.Weight1 * self._norm(r12) +
                   self.Weight2 * self._norm(r36) +
                   self.Weight3 * self._norm(r108) +
                   self.Weight4 * self._norm(r324))

        if self._prev_perceptron is None:
            self._prev_perceptron = current
            return

        prev = self._prev_perceptron

        if prev < 0 and current > 0 and self.Position <= 0:
            self.BuyMarket()
        elif prev > 0 and current < 0 and self.Position >= 0:
            self.SellMarket()

        self._prev_perceptron = current

    def _norm(self, rsi_val):
        return (rsi_val / 100.0 - 0.5) * 2.0

    def OnReseted(self):
        super(slime_mold_rsi_strategy, self).OnReseted()
        self._prev_perceptron = None

    def CreateClone(self):
        return slime_mold_rsi_strategy()
