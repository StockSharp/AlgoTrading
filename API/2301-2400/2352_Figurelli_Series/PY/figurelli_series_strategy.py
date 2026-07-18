import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

import math
from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *

class figurelli_series_strategy(Strategy):
    """
    FigurelliSeries: Multiple EMA comparison strategy.
    Computes N EMAs with increasing periods, counts how many price is above/below.
    Trades on sign change of the net score.
    """

    def __init__(self):
        super(figurelli_series_strategy, self).__init__()
        self._start_period = self.Param("StartPeriod", 3) \
            .SetDisplay("Start Period", "Initial period for moving averages", "Indicator")
        self._step = self.Param("Step", 2) \
            .SetDisplay("Step", "Step between moving average periods", "Indicator")
        self._total = self.Param("Total", 6) \
            .SetDisplay("Total", "Number of moving averages", "Indicator")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Type of candles for calculations", "General")

        self._last_sign = 0
        self._emas = []

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(figurelli_series_strategy, self).OnReseted()
        self._last_sign = 0
        self._emas = []

    def OnStarted2(self, time):
        super(figurelli_series_strategy, self).OnStarted2(time)

        self._last_sign = 0
        total = self._total.Value
        start = self._start_period.Value
        step = self._step.Value

        self._emas = []
        for i in range(total):
            ema = ExponentialMovingAverage()
            ema.Length = start + step * i
            self._emas.append(ema)
            self.Indicators.Add(ema)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        price = float(candle.ClosePrice)
        bids = 0
        asks = 0
        all_formed = True

        for ema in self._emas:
            result = process_float(ema, candle.ClosePrice, candle.OpenTime, True)
            if not ema.IsFormed:
                all_formed = False
                continue
            ma_val = float(result)
            if price > ma_val:
                bids += 1
            elif price < ma_val:
                asks += 1

        if not all_formed:
            return

        value = bids - asks
        sign = 1 if value > 0 else (-1 if value < 0 else 0)

        if sign == 0 or sign == self._last_sign:
            return

        if sign > 0 and self.Position <= 0:
            self.BuyMarket()
        elif sign < 0 and self.Position >= 0:
            self.SellMarket()

        self._last_sign = sign

    def CreateClone(self):
        return figurelli_series_strategy()
