import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

import math

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, StandardDeviation
from StockSharp.Algo.Strategies import Strategy


class multi_band_comparison_strategy(Strategy):
    """
    Multi-band comparison strategy.
    The trigger line is the UpperQuantile quantile of the last Length closes minus BollingerMultiplier standard deviations.
    A long opens after EntryConfirmBars consecutive closes above the line and closes after ExitConfirmBars consecutive closes below it.
    The SMA middle band is drawn for comparison. Long only, no stops.
    """

    def __init__(self):
        super(multi_band_comparison_strategy, self).__init__()
        self._length = self.Param("Length", 20).SetGreaterThanZero().SetDisplay("Length", "Period of the SMA, standard deviation and quantile window", "Bands")
        self._bollinger_multiplier = self.Param("BollingerMultiplier", 1.0).SetNotNegative().SetDisplay("BB Mult", "Standard deviation multiplier", "Bands")
        self._upper_quantile = self.Param("UpperQuantile", 0.95).SetDisplay("Upper Quantile", "Quantile of the closes that forms the upper band", "Bands")
        self._entry_confirm_bars = self.Param("EntryConfirmBars", 1).SetGreaterThanZero().SetDisplay("Entry Confirm Bars", "Consecutive closes above the line required to enter", "Trading")
        self._exit_confirm_bars = self.Param("ExitConfirmBars", 1).SetGreaterThanZero().SetDisplay("Exit Confirm Bars", "Consecutive closes below the line required to exit", "Trading")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._closes = []
        self._above_count = 0
        self._below_count = 0

    def OnReseted(self):
        super(multi_band_comparison_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(multi_band_comparison_strategy, self).OnStarted2(time)

        self._reset_state()

        sma = SimpleMovingAverage()
        sma.Length = self._length.Value
        std = StandardDeviation()
        std.Length = self._length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(sma, std, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sma)
            self.DrawOwnTrades(area)

    def _get_quantile(self):
        ordered = sorted(self._closes)
        position = float(self._upper_quantile.Value) * (len(ordered) - 1)
        lower = int(math.floor(position))
        upper = min(lower + 1, len(ordered) - 1)
        fraction = position - lower
        return ordered[lower] + (ordered[upper] - ordered[lower]) * fraction

    def _process_candle(self, candle, sma_value, std_value):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        length = self._length.Value

        self._closes.append(close)
        if len(self._closes) > length:
            self._closes.pop(0)

        if not sma_value.IsFormed or not std_value.IsFormed or len(self._closes) < length:
            return

        std = float(std_value.GetValue[Decimal](None))
        line = self._get_quantile() - std * float(self._bollinger_multiplier.Value)

        if close > line:
            self._above_count += 1
            self._below_count = 0
        elif close < line:
            self._below_count += 1
            self._above_count = 0
        else:
            self._above_count = 0
            self._below_count = 0

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self.Position <= 0 and self._above_count >= self._entry_confirm_bars.Value:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif self.Position > 0 and self._below_count >= self._exit_confirm_bars.Value:
            self.SellMarket(self.Position)

    def CreateClone(self):
        return multi_band_comparison_strategy()
