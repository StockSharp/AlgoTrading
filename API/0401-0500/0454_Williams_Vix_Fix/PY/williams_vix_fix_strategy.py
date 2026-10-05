import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import BollingerBands, Highest, Lowest, SimpleMovingAverage, StandardDeviation
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *


class williams_vix_fix_strategy(Strategy):
    """
    Williams VIX Fix Strategy.
    VIX Fix = (highest close of WvfPeriod - low) / highest close * 100; the inverted VIX Fix uses (high - lowest close) / lowest close.
    Each value is extreme when it reaches its own Bollinger upper band (BbLength, BbMultiplier) or the highest value of the last
    WvfLookback bars times its percentile (HighestPercentile for the VIX Fix, LowestPercentile for the inverted one).
    A long opens on an extreme VIX Fix with the close below the lower price Bollinger Band and closes on an extreme inverted
    VIX Fix with the close above the upper band.
    """

    def __init__(self):
        super(williams_vix_fix_strategy, self).__init__()
        self._bb_length = self.Param("BbLength", 20).SetGreaterThanZero().SetDisplay("BB Length", "Bollinger Bands period", "Bollinger")
        self._bb_multiplier = self.Param("BbMultiplier", 2.0).SetGreaterThanZero().SetDisplay("BB Multiplier", "Bollinger Bands standard deviation multiplier", "Bollinger")
        self._wvf_period = self.Param("WvfPeriod", 20).SetGreaterThanZero().SetDisplay("WVF Period", "Lookback of the highest and lowest close", "VIX Fix")
        self._wvf_lookback = self.Param("WvfLookback", 50).SetGreaterThanZero().SetDisplay("WVF Lookback", "Lookback of the percentile thresholds", "VIX Fix")
        self._highest_percentile = self.Param("HighestPercentile", 0.85).SetGreaterThanZero().SetDisplay("Highest Percentile", "Percentile of the highest VIX Fix value", "VIX Fix")
        self._lowest_percentile = self.Param("LowestPercentile", 0.99).SetGreaterThanZero().SetDisplay("Lowest Percentile", "Percentile of the highest inverted VIX Fix value", "VIX Fix")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._highest_close = None
        self._lowest_close = None
        self._wvf_set = None
        self._inv_set = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _create_set(self):
        mean = SimpleMovingAverage()
        mean.Length = self._bb_length.Value
        deviation = StandardDeviation()
        deviation.Length = self._bb_length.Value
        rng = Highest()
        rng.Length = self._wvf_lookback.Value
        return (mean, deviation, rng)

    def OnStarted2(self, time):
        super(williams_vix_fix_strategy, self).OnStarted2(time)

        bollinger = BollingerBands()
        bollinger.Length = self._bb_length.Value
        bollinger.Width = Decimal(float(self._bb_multiplier.Value))

        self._highest_close = Highest()
        self._highest_close.Length = self._wvf_period.Value
        self._lowest_close = Lowest()
        self._lowest_close.Length = self._wvf_period.Value
        self._wvf_set = self._create_set()
        self._inv_set = self._create_set()

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(bollinger, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, bollinger)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, bollinger_value):
        if candle.State != CandleStates.Finished:
            return

        time = candle.OpenTime
        highest_close = float(process_float(self._highest_close, candle.ClosePrice, time, True).GetValue[Decimal](None))
        lowest_close = float(process_float(self._lowest_close, candle.ClosePrice, time, True).GetValue[Decimal](None))

        if not self._highest_close.IsFormed or not self._lowest_close.IsFormed or highest_close <= 0 or lowest_close <= 0:
            return

        wvf = (highest_close - float(candle.LowPrice)) / highest_close * 100.0
        inv = (float(candle.HighPrice) - lowest_close) / lowest_close * 100.0

        fear = self._is_extreme(wvf, time, self._wvf_set, float(self._highest_percentile.Value))
        complacency = self._is_extreme(inv, time, self._inv_set, float(self._lowest_percentile.Value))

        if fear is None or complacency is None:
            return

        if bollinger_value.UpBand is None or bollinger_value.LowBand is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        close = float(candle.ClosePrice)
        upper = float(bollinger_value.UpBand)
        lower = float(bollinger_value.LowBand)

        if fear and close < lower and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif complacency and close > upper and self.Position > 0:
            self.SellMarket(self.Position)

    def _is_extreme(self, value, time, indicators, percentile):
        mean, deviation, rng = indicators
        middle = float(process_float(mean, value, time, True).GetValue[Decimal](None))
        spread = float(process_float(deviation, value, time, True).GetValue[Decimal](None))
        highest = float(process_float(rng, value, time, True).GetValue[Decimal](None))

        if not mean.IsFormed or not deviation.IsFormed or not rng.IsFormed:
            return None

        upper_band = middle + float(self._bb_multiplier.Value) * spread
        return value >= upper_band or value >= highest * percentile

    def CreateClone(self):
        return williams_vix_fix_strategy()
