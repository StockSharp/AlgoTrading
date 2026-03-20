import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import WeightedMovingAverage, DeMarker
from StockSharp.Algo.Strategies import Strategy


class exp_skyscraper_fix_color_aml_mmrec_strategy(Strategy):
    def __init__(self):
        super(exp_skyscraper_fix_color_aml_mmrec_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe", "General")
        self._trend_period = self.Param("TrendPeriod", 18) \
            .SetDisplay("Trend Period", "Trend WMA period", "Indicators")
        self._filter_period = self.Param("FilterPeriod", 12) \
            .SetDisplay("Filter Period", "DeMarker period", "Indicators")
        self._upper_level = self.Param("UpperLevel", 0.55) \
            .SetDisplay("Upper Level", "Buy filter level", "Signals")
        self._lower_level = self.Param("LowerLevel", 0.45) \
            .SetDisplay("Lower Level", "Sell filter level", "Signals")

        self._previous_trend = None
        self._previous_close = None

    @property
    def CandleType(self):
        return self._candle_type.Value
    @property
    def TrendPeriod(self):
        return self._trend_period.Value
    @property
    def FilterPeriod(self):
        return self._filter_period.Value
    @property
    def UpperLevel(self):
        return self._upper_level.Value
    @property
    def LowerLevel(self):
        return self._lower_level.Value

    def OnReseted(self):
        super(exp_skyscraper_fix_color_aml_mmrec_strategy, self).OnReseted()
        self._previous_trend = None
        self._previous_close = None

    def OnStarted(self, time):
        super(exp_skyscraper_fix_color_aml_mmrec_strategy, self).OnStarted(time)
        self._previous_trend = None
        self._previous_close = None
        trend = WeightedMovingAverage()
        trend.Length = self.TrendPeriod
        filt = DeMarker()
        filt.Length = self.FilterPeriod
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(trend, filt, self._on_process).Start()

    def _on_process(self, candle, trend_value, filter_value):
        if candle.State != CandleStates.Finished:
            return
        tv = float(trend_value)
        fv = float(filter_value)
        previous_close = self._previous_close
        previous_trend = self._previous_trend
        self._previous_close = float(candle.ClosePrice)
        self._previous_trend = tv
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        if previous_close is None or previous_trend is None:
            return
        crossed_up = previous_close <= previous_trend and float(candle.ClosePrice) > tv
        crossed_down = previous_close >= previous_trend and float(candle.ClosePrice) < tv
        buy_signal = crossed_up and fv >= float(self.UpperLevel)
        sell_signal = crossed_down and fv <= float(self.LowerLevel)
        if buy_signal and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
                return
            self.BuyMarket()
        elif sell_signal and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
                return
            self.SellMarket()

    def CreateClone(self):
        return exp_skyscraper_fix_color_aml_mmrec_strategy()
