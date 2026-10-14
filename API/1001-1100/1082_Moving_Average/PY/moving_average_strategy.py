import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import (SimpleMovingAverage, ExponentialMovingAverage, DoubleExponentialMovingAverage,
                                        TripleExponentialMovingAverage, WeightedMovingAverage, DecimalIndicatorValue)
from StockSharp.Algo.Strategies import Strategy


class _moving_average_line(object):
    """A moving average fed with an arbitrary price; VWMA is calculated from the price and candle volume."""

    def __init__(self, ma_type, length):
        self._length = length
        self._window = []
        if ma_type == "SMA":
            self._indicator = SimpleMovingAverage()
        elif ma_type == "DEMA":
            self._indicator = DoubleExponentialMovingAverage()
        elif ma_type == "TEMA":
            self._indicator = TripleExponentialMovingAverage()
        elif ma_type == "WMA":
            self._indicator = WeightedMovingAverage()
        elif ma_type == "VWMA":
            self._indicator = None
        else:
            self._indicator = ExponentialMovingAverage()
        if self._indicator is not None:
            self._indicator.Length = length

    def process(self, price, volume, time):
        if self._indicator is None:
            self._window.append((price, volume))
            if len(self._window) > self._length:
                self._window.pop(0)
            if len(self._window) < self._length:
                return None
            sum_pv = sum(p * v for p, v in self._window)
            sum_v = sum(v for p, v in self._window)
            return price if sum_v == 0 else sum_pv / sum_v

        indicator_input = DecimalIndicatorValue(self._indicator, Decimal(price), time)
        indicator_input.IsFinal = True
        value = self._indicator.Process(indicator_input)
        if not self._indicator.IsFormed or value.IsEmpty:
            return None
        return float(value.GetValue[Decimal](None))


class moving_average_strategy(Strategy):
    """
    Moving average crossover strategy.
    A short and a long moving average of the selected type are calculated on the selected candle price.
    A long opens when the short average crosses above the long one and closes when it crosses back below. Long only, no stops.
    """

    def __init__(self):
        super(moving_average_strategy, self).__init__()
        self._ma_type = self.Param("MaType", "EMA").SetDisplay("MA Type", "Moving average type: SMA, EMA, DEMA, TEMA, WMA or VWMA", "Indicators")
        self._short_length = self.Param("ShortLength", 1).SetGreaterThanZero().SetDisplay("Short Length", "Short moving average length", "Indicators")
        self._long_length = self.Param("LongLength", 20).SetGreaterThanZero().SetDisplay("Long Length", "Long moving average length", "Indicators")
        self._price_type = self.Param("PriceType", "Typical").SetDisplay("Price Type", "Candle price: Close, High, Open, Low, Typical or Center", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._short_ma = None
        self._long_ma = None
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._prev_short = None
        self._prev_long = None

    def OnReseted(self):
        super(moving_average_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(moving_average_strategy, self).OnStarted2(time)

        self._reset_state()
        ma_type = str(self._ma_type.Value).upper()
        self._short_ma = _moving_average_line(ma_type, self._short_length.Value)
        self._long_ma = _moving_average_line(ma_type, self._long_length.Value)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _get_price(self, candle):
        price_type = str(self._price_type.Value)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        close = float(candle.ClosePrice)
        if price_type == "High":
            return high
        if price_type == "Open":
            return float(candle.OpenPrice)
        if price_type == "Low":
            return low
        if price_type == "Typical":
            return (high + low + close) / 3.0
        if price_type == "Center":
            return (high + low) / 2.0
        return close

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        price = self._get_price(candle)
        volume = float(candle.TotalVolume)
        short_ma = self._short_ma.process(price, volume, candle.OpenTime)
        long_ma = self._long_ma.process(price, volume, candle.OpenTime)

        if short_ma is None or long_ma is None:
            return

        prev_short = self._prev_short
        prev_long = self._prev_long
        self._prev_short = short_ma
        self._prev_long = long_ma

        if prev_short is None or prev_long is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if prev_short <= prev_long and short_ma > long_ma and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif prev_short >= prev_long and short_ma < long_ma and self.Position > 0:
            self.SellMarket(self.Position)

    def CreateClone(self):
        return moving_average_strategy()
