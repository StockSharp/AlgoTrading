import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SuperTrend, SimpleMovingAverage, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy

class tradingview_supertrend_flip_strategy(Strategy):
    """
    Strategy based on Supertrend indicator flips.
    Detects when Supertrend direction changes and trades accordingly.
    """

    def __init__(self):
        super(tradingview_supertrend_flip_strategy, self).__init__()
        self._supertrend_period = self.Param("SupertrendPeriod", 10).SetGreaterThanZero().SetDisplay("Supertrend Period", "Period for Supertrend calculation", "Indicators")
        self._supertrend_multiplier = self.Param("SupertrendMultiplier", 3.0).SetGreaterThanZero().SetDisplay("Supertrend Multiplier", "Multiplier for Supertrend", "Indicators")
        self._volume_avg_period = self.Param("VolumeAvgPeriod", 20).SetGreaterThanZero()
        self._use_volume_filter = self.Param("UseVolumeFilter", True)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")

        self._volume_average = None
        self._prev_is_up_trend = False
        self._has_prev_values = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.candle_type)]

    def OnReseted(self):
        super(tradingview_supertrend_flip_strategy, self).OnReseted()
        self._volume_average = None
        self._prev_is_up_trend = False
        self._has_prev_values = False

    def OnStarted2(self, time):
        super(tradingview_supertrend_flip_strategy, self).OnStarted2(time)
        self._volume_average = SimpleMovingAverage()
        self._volume_average.Length = int(self._volume_avg_period.Value)

        supertrend = SuperTrend()
        supertrend.Length = self._supertrend_period.Value
        supertrend.Multiplier = Decimal(self._supertrend_multiplier.Value)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(supertrend, self._process_candle, allowEmpty=True).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, supertrend)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, st_val):
        if candle.State != CandleStates.Finished:
            return

        volume_input = DecimalIndicatorValue(self._volume_average, candle.TotalVolume, candle.OpenTime)
        volume_input.IsFinal = True
        volume_value = self._volume_average.Process(volume_input)
        if not st_val.IsFormed or st_val.IsEmpty or not self.IsFormedAndOnlineAndAllowTrading():
            return

        is_up_trend = st_val.IsUpTrend
        confirmed = not bool(self._use_volume_filter.Value) or (
            self._volume_average.IsFormed and candle.TotalVolume > volume_value.GetValue[Decimal](None))

        if not self._has_prev_values:
            self._has_prev_values = True
            self._prev_is_up_trend = is_up_trend
            return

        flipped_bullish = is_up_trend and not self._prev_is_up_trend
        flipped_bearish = not is_up_trend and self._prev_is_up_trend

        self._prev_is_up_trend = is_up_trend

        if flipped_bullish and self.Position <= 0:
            if confirmed:
                self.BuyMarket(self.Volume + Math.Abs(self.Position))
            elif self.Position < 0:
                self.BuyMarket(Math.Abs(self.Position))
        elif flipped_bearish and self.Position >= 0:
            if confirmed:
                self.SellMarket(self.Volume + Math.Abs(self.Position))
            elif self.Position > 0:
                self.SellMarket(Math.Abs(self.Position))

    def CreateClone(self):
        return tradingview_supertrend_flip_strategy()
