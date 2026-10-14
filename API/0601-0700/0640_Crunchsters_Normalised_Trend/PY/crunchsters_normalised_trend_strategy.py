import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange, StandardDeviation, HullMovingAverage
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *


class crunchsters_normalised_trend_strategy(Strategy):
    """
    Crunchster's Normalised Trend strategy.
    Each close-to-close return is divided by the standard deviation of the last NormPeriod returns and the results are summed
    into a normalised price. A cross of the normalised price above its Hull moving average (taken HmaOffset bars back) goes long
    and a cross below goes short, reversing an opposite position. A stop StopMultiple ATRs (NormPeriod long) from the entry
    closes a losing trade.
    """

    def __init__(self):
        super(crunchsters_normalised_trend_strategy, self).__init__()
        self._norm_period = self.Param("NormPeriod", 14).SetRange(2, 10000).SetDisplay("Norm Period", "Period of the return standard deviation", "Indicators")
        self._hma_period = self.Param("HmaPeriod", 100).SetGreaterThanZero().SetDisplay("HMA Period", "Hull moving average period", "Indicators")
        self._hma_offset = self.Param("HmaOffset", 0).SetNotNegative().SetDisplay("HMA Offset", "Bars the Hull moving average is shifted back", "Indicators")
        self._stop_multiple = self.Param("StopMultiple", 1.0).SetNotNegative().SetDisplay("Stop Multiple", "ATR multiple for the stop", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._return_deviation = None
        self._hma = None
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._hma_history = []
        self._prev_close = None
        self._normalized_price = Decimal(0)
        self._prev_price = None
        self._prev_hma = None
        self._stop_price = None

    def OnReseted(self):
        super(crunchsters_normalised_trend_strategy, self).OnReseted()
        self._reset_state()
        self._return_deviation = None
        self._hma = None

    def OnStarted2(self, time):
        super(crunchsters_normalised_trend_strategy, self).OnStarted2(time)

        self._reset_state()

        atr = AverageTrueRange()
        atr.Length = self._norm_period.Value
        self._return_deviation = StandardDeviation()
        self._return_deviation.Length = self._norm_period.Value
        self._hma = HullMovingAverage()
        self._hma.Length = self._hma_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(atr, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, atr_value):
        if candle.State != CandleStates.Finished:
            return

        close = candle.ClosePrice
        last_close = self._prev_close
        self._prev_close = close

        if last_close is None or last_close == 0:
            return

        ret = (close - last_close) / last_close
        deviation_value = process_value(self._return_deviation, ret, candle.OpenTime, True)

        if not deviation_value.IsFormed:
            return

        deviation = deviation_value.GetValue[Decimal](None)
        if deviation > 0:
            self._normalized_price = self._normalized_price + ret / deviation

        price = self._normalized_price
        hma_value = process_value(self._hma, price, candle.OpenTime, True)

        if not hma_value.IsFormed:
            return

        offset = self._hma_offset.Value
        self._hma_history.append(hma_value.GetValue[Decimal](None))
        if len(self._hma_history) > offset + 1:
            self._hma_history.pop(0)

        if len(self._hma_history) <= offset:
            return

        hma = self._hma_history[0]
        prev_price = self._prev_price
        prev_hma = self._prev_hma
        self._prev_price = price
        self._prev_hma = hma

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self.Position > 0 and self._stop_price is not None and candle.LowPrice <= self._stop_price:
            self.SellMarket(self.Position)
            self._stop_price = None
            return

        if self.Position < 0 and self._stop_price is not None and candle.HighPrice >= self._stop_price:
            self.BuyMarket(-self.Position)
            self._stop_price = None
            return

        if prev_price is None or prev_hma is None or not atr_value.IsFormed:
            return

        stop_multiple = Decimal(self._stop_multiple.Value)
        stop_distance = atr_value.GetValue[Decimal](None) * stop_multiple

        if prev_price <= prev_hma and price > hma and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
            self._stop_price = close - stop_distance if stop_multiple > 0 else None
        elif prev_price >= prev_hma and price < hma and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
            self._stop_price = close + stop_distance if stop_multiple > 0 else None

    def CreateClone(self):
        return crunchsters_normalised_trend_strategy()
