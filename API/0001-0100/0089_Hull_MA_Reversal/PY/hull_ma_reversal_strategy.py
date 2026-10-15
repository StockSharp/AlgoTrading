import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import HullMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy

# Period of the ATR that sizes the stop.
ATR_PERIOD = 14

class hull_ma_reversal_strategy(Strategy):
    """
    Hull MA Reversal strategy.
    When the Hull MA turns from falling to rising the position turns long, and when it turns from rising to falling it turns short.
    The stop lies AtrMultiplier ATRs beyond the entry candle's low (for a long) or high (for a short).
    """

    def __init__(self):
        super(hull_ma_reversal_strategy, self).__init__()
        self._hma_period = self.Param("HmaPeriod", 9).SetGreaterThanZero().SetDisplay("HMA Period", "Period for Hull Moving Average", "Indicators")
        self._atr_multiplier = self.Param("AtrMultiplier", 2.0).SetGreaterThanZero().SetDisplay("ATR Multiplier", "Distance of the stop beyond the entry candle in ATR multiples", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))).SetDisplay("Candle Type", "Type of candles to use", "General")

        self._prev_hma = None
        # Direction of the latest move of the Hull MA: 1 rising, -1 falling, 0 none yet.
        self._slope = 0
        self._stop_price = Decimal(0)

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(hull_ma_reversal_strategy, self).OnReseted()
        self._prev_hma = None
        self._slope = 0
        self._stop_price = Decimal(0)

    def OnStarted2(self, time):
        super(hull_ma_reversal_strategy, self).OnStarted2(time)

        self._prev_hma = None
        self._slope = 0
        self._stop_price = Decimal(0)

        hma = HullMovingAverage()
        hma.Length = self._hma_period.Value
        atr = AverageTrueRange()
        atr.Length = ATR_PERIOD

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(hma, atr, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, hma)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, hma_value, atr_value):
        if candle.State != CandleStates.Finished or not hma_value.IsFormed or not atr_value.IsFormed:
            return

        hma = hma_value.GetValue[Decimal](None)
        prev_hma = self._prev_hma
        self._prev_hma = hma

        if prev_hma is None:
            return

        previous_slope = self._slope
        slope = 1 if hma > prev_hma else (-1 if hma < prev_hma else 0)
        if slope != 0:
            self._slope = slope

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        close = candle.ClosePrice
        distance = Decimal(self._atr_multiplier.Value) * atr_value.GetValue[Decimal](None)

        if previous_slope == -1 and slope == 1 and self.Position <= 0:
            self.BuyMarket(self.Volume + Math.Abs(self.Position))
            self._stop_price = candle.LowPrice - distance
        elif previous_slope == 1 and slope == -1 and self.Position >= 0:
            self.SellMarket(self.Volume + Math.Abs(self.Position))
            self._stop_price = candle.HighPrice + distance
        elif self.Position > 0 and close <= self._stop_price:
            self.SellMarket(self.Position)
        elif self.Position < 0 and close >= self._stop_price:
            self.BuyMarket(-self.Position)

    def CreateClone(self):
        return hull_ma_reversal_strategy()
