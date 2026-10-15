import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy

class atr_mean_reversion_strategy(Strategy):
    """
    ATR Mean Reversion strategy.
    A close more than Multiplier times the AtrPeriod ATR below the MaPeriod simple moving average goes long and one that far above it goes
    short, reversing an opposite position. A long closes once the close is back at or above the average and a short once it is back at or
    below it. The stop lies Multiplier ATR from the entry close and is checked on candle closes.
    """

    def __init__(self):
        super(atr_mean_reversion_strategy, self).__init__()
        self._ma_period = self.Param("MaPeriod", 20).SetGreaterThanZero().SetDisplay("MA Period", "Period of the simple moving average", "Parameters")
        self._atr_period = self.Param("AtrPeriod", 14).SetGreaterThanZero().SetDisplay("ATR Period", "Period of the ATR", "Parameters")
        self._multiplier = self.Param("Multiplier", 2.0).SetGreaterThanZero().SetDisplay("Multiplier", "ATR multiplier for the entry distance and the stop", "Parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._stop_price = Decimal(0)

    def OnReseted(self):
        super(atr_mean_reversion_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(atr_mean_reversion_strategy, self).OnStarted2(time)

        self._reset_state()

        sma = SimpleMovingAverage()
        sma.Length = self._ma_period.Value
        atr = AverageTrueRange()
        atr.Length = self._atr_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(sma, atr, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sma)
            self.DrawOwnTrades(area)
            oscillators = self.CreateChartArea()
            if oscillators is not None:
                self.DrawIndicator(oscillators, atr)

    def _process_candle(self, candle, sma_value, atr_value):
        if candle.State != CandleStates.Finished:
            return

        if not sma_value.IsFormed or not atr_value.IsFormed:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        sma = sma_value.GetValue[Decimal](None)
        distance = Decimal(self._multiplier.Value) * atr_value.GetValue[Decimal](None)
        close = candle.ClosePrice

        if close < sma - distance and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
            self._stop_price = close - distance
        elif close > sma + distance and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
            self._stop_price = close + distance
        elif self.Position > 0 and (close >= sma or close <= self._stop_price):
            self.SellMarket(self.Position)
        elif self.Position < 0 and (close <= sma or close >= self._stop_price):
            self.BuyMarket(-self.Position)

    def CreateClone(self):
        return atr_mean_reversion_strategy()
