import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

import math
from collections import deque
from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ChoppinessIndex, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class berlin_range_index_strategy(Strategy):
    """
    Berlin Range Index strategy.
    The Choppiness Index(Length) is multiplied by the ATR factor lowest ATR(AtrLength) over LowLookback bars / current ATR, so expanding
    volatility pushes it down. With UseNormalized the filtered value is rescaled to 0-100 so that its StdDevLength-bar mean minus and
    plus two standard deviations map to 0 and 100. Below ChopMin the strategy enters in the direction of the candle (reversing an
    opposite position) and above ChopMax it closes the position.
    """

    def __init__(self):
        super(berlin_range_index_strategy, self).__init__()
        self._length = self.Param("Length", 9).SetGreaterThanZero().SetDisplay("Length", "Choppiness Index period", "Indicators")
        self._chop_max = self.Param("ChopMax", 40.0).SetDisplay("Chop Max", "Index level above which the position closes", "Signals")
        self._chop_min = self.Param("ChopMin", 10.0).SetDisplay("Chop Min", "Index level below which a position opens", "Signals")
        self._atr_length = self.Param("AtrLength", 14).SetGreaterThanZero().SetDisplay("ATR Length", "ATR period of the filter factor", "Indicators")
        self._low_lookback = self.Param("LowLookback", 14).SetGreaterThanZero().SetDisplay("Low Lookback", "Bars over which the lowest ATR is taken", "Indicators")
        self._use_normalized = self.Param("UseNormalized", True).SetDisplay("Use Normalized", "Rescale the filtered index by its standard deviation", "Indicators")
        self._std_dev_length = self.Param("StdDevLength", 14).SetGreaterThanZero().SetDisplay("StdDev Length", "Bars of the normalization mean and standard deviation", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._atrs = deque()
        self._filtered = deque()

    def OnReseted(self):
        super(berlin_range_index_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(berlin_range_index_strategy, self).OnStarted2(time)

        self._reset_state()

        chop = ChoppinessIndex()
        chop.Length = self._length.Value
        atr = AverageTrueRange()
        atr.Length = self._atr_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(chop, atr, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)
            oscillators = self.CreateChartArea()
            if oscillators is not None:
                self.DrawIndicator(oscillators, chop)

    def _process_candle(self, candle, chop_value, atr_value):
        if candle.State != CandleStates.Finished:
            return

        if not chop_value.IsFormed or not atr_value.IsFormed:
            return

        chop = float(chop_value.GetValue[Decimal](None))
        atr = float(atr_value.GetValue[Decimal](None))

        low_lookback = self._low_lookback.Value
        self._atrs.append(atr)
        while len(self._atrs) > low_lookback:
            self._atrs.popleft()

        if len(self._atrs) < low_lookback or atr <= 0:
            return

        filtered = chop * min(self._atrs) / atr

        std_len = self._std_dev_length.Value
        self._filtered.append(filtered)
        while len(self._filtered) > std_len:
            self._filtered.popleft()

        if self._use_normalized.Value:
            if len(self._filtered) < std_len:
                return
            mean = sum(self._filtered) / len(self._filtered)
            variance = sum((v - mean) * (v - mean) for v in self._filtered) / len(self._filtered)
            std = math.sqrt(variance)
            if std <= 0:
                return
            index = min(100.0, max(0.0, 50.0 + 50.0 * (filtered - mean) / (2.0 * std)))
        else:
            index = filtered

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        bullish = candle.ClosePrice > candle.OpenPrice
        bearish = candle.ClosePrice < candle.OpenPrice

        if index < float(self._chop_min.Value):
            if bullish and self.Position <= 0:
                self.BuyMarket(self.Volume + abs(self.Position))
            elif bearish and self.Position >= 0:
                self.SellMarket(self.Volume + abs(self.Position))
        elif index > float(self._chop_max.Value):
            if self.Position > 0:
                self.SellMarket(self.Position)
            elif self.Position < 0:
                self.BuyMarket(-self.Position)

    def CreateClone(self):
        return berlin_range_index_strategy()
