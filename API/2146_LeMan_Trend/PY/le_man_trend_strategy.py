import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Highest, Lowest, ExponentialMovingAverage, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy


class le_man_trend_strategy(Strategy):
    def __init__(self):
        super(le_man_trend_strategy, self).__init__()
        self._min = self.Param("Min", 13) \
            .SetDisplay("Min Period", "Minimum lookback for highs/lows", "Indicator")
        self._midle = self.Param("Midle", 21) \
            .SetDisplay("Middle Period", "Middle lookback for highs/lows", "Indicator")
        self._max = self.Param("Max", 34) \
            .SetDisplay("Max Period", "Maximum lookback for highs/lows", "Indicator")
        self._period_ema = self.Param("PeriodEma", 3) \
            .SetDisplay("EMA Period", "Smoothing period for bulls/bears", "Indicator")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Time frame for calculations", "General")
        self._high_min = None
        self._high_midle = None
        self._high_max = None
        self._low_min = None
        self._low_midle = None
        self._low_max = None
        self._bulls_ema = None
        self._bears_ema = None
        self._prev_bulls = 0.0
        self._prev_bears = 0.0

    @property
    def min_period(self):
        return self._min.Value

    @property
    def midle_period(self):
        return self._midle.Value

    @property
    def max_period(self):
        return self._max.Value

    @property
    def period_ema(self):
        return self._period_ema.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(le_man_trend_strategy, self).OnReseted()
        self._high_min = None
        self._high_midle = None
        self._high_max = None
        self._low_min = None
        self._low_midle = None
        self._low_max = None
        self._bulls_ema = None
        self._bears_ema = None
        self._prev_bulls = 0.0
        self._prev_bears = 0.0

    def OnStarted(self, time):
        super(le_man_trend_strategy, self).OnStarted(time)
        self._high_min = Highest()
        self._high_min.Length = self.min_period
        self._high_midle = Highest()
        self._high_midle.Length = self.midle_period
        self._high_max = Highest()
        self._high_max.Length = self.max_period
        self._low_min = Lowest()
        self._low_min.Length = self.min_period
        self._low_midle = Lowest()
        self._low_midle.Length = self.midle_period
        self._low_max = Lowest()
        self._low_max.Length = self.max_period
        self._bulls_ema = ExponentialMovingAverage()
        self._bulls_ema.Length = self.period_ema
        self._bears_ema = ExponentialMovingAverage()
        self._bears_ema.Length = self.period_ema
        self.Indicators.Add(self._high_min)
        self.Indicators.Add(self._high_midle)
        self.Indicators.Add(self._high_max)
        self.Indicators.Add(self._low_min)
        self.Indicators.Add(self._low_midle)
        self.Indicators.Add(self._low_max)
        self.Indicators.Add(self._bulls_ema)
        self.Indicators.Add(self._bears_ema)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.process_candle).Start()

    def process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return
        close_val = candle.ClosePrice
        t = candle.OpenTime
        high_min_val = float(self._high_min.Process(DecimalIndicatorValue(self._high_min, close_val, t)))
        high_midle_val = float(self._high_midle.Process(DecimalIndicatorValue(self._high_midle, close_val, t)))
        high_max_val = float(self._high_max.Process(DecimalIndicatorValue(self._high_max, close_val, t)))
        low_min_val = float(self._low_min.Process(DecimalIndicatorValue(self._low_min, close_val, t)))
        low_midle_val = float(self._low_midle.Process(DecimalIndicatorValue(self._low_midle, close_val, t)))
        low_max_val = float(self._low_max.Process(DecimalIndicatorValue(self._low_max, close_val, t)))
        if not self._high_max.IsFormed or not self._low_max.IsFormed:
            return
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        hh = (high - high_min_val) + (high - high_midle_val) + (high - high_max_val)
        ll = (low_min_val - low) + (low_midle_val - low) + (low_max_val - low)
        bulls_val = float(self._bulls_ema.Process(DecimalIndicatorValue(self._bulls_ema, hh, t)))
        bears_val = float(self._bears_ema.Process(DecimalIndicatorValue(self._bears_ema, ll, t)))
        if not self._bulls_ema.IsFormed or not self._bears_ema.IsFormed:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        if self._prev_bulls <= self._prev_bears and bulls_val > bears_val and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif self._prev_bulls >= self._prev_bears and bulls_val < bears_val and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
        self._prev_bulls = bulls_val
        self._prev_bears = bears_val

    def CreateClone(self):
        return le_man_trend_strategy()
