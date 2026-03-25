import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, Highest, Lowest, WeightedMovingAverage, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy


class chaikin_volatility_stochastic_strategy(Strategy):
    def __init__(self):
        super(chaikin_volatility_stochastic_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candles for calculation", "General")
        self._ema_length = self.Param("EmaLength", 10) \
            .SetDisplay("EMA Length", "Length for smoothing high-low range", "Indicator")
        self._stoch_length = self.Param("StochLength", 5) \
            .SetDisplay("Stochastic Length", "Lookback for stochastic calculation", "Indicator")
        self._wma_length = self.Param("WmaLength", 5) \
            .SetDisplay("WMA Length", "Weighted moving average period", "Indicator")
        self._range_ema = None
        self._highest = None
        self._lowest = None
        self._wma = None
        self._prev = None
        self._prev_prev = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    @property
    def ema_length(self):
        return self._ema_length.Value

    @property
    def stoch_length(self):
        return self._stoch_length.Value

    @property
    def wma_length(self):
        return self._wma_length.Value

    def OnReseted(self):
        super(chaikin_volatility_stochastic_strategy, self).OnReseted()
        self._range_ema = None
        self._highest = None
        self._lowest = None
        self._wma = None
        self._prev = None
        self._prev_prev = None

    def OnStarted(self, time):
        super(chaikin_volatility_stochastic_strategy, self).OnStarted(time)
        self._prev = None
        self._prev_prev = None
        self._range_ema = ExponentialMovingAverage()
        self._range_ema.Length = self.ema_length
        self._highest = Highest()
        self._highest.Length = self.stoch_length
        self._lowest = Lowest()
        self._lowest.Length = self.stoch_length
        self._wma = WeightedMovingAverage()
        self._wma.Length = self.wma_length
        self.Indicators.Add(self._range_ema)
        self.Indicators.Add(self._highest)
        self.Indicators.Add(self._lowest)
        self.Indicators.Add(self._wma)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return
        t = candle.ServerTime
        range_val = float(candle.HighPrice) - float(candle.LowPrice)
        input_ema = DecimalIndicatorValue(self._range_ema, range_val, t)
        input_ema.IsFinal = True
        ema_result = self._range_ema.Process(input_ema)
        if not self._range_ema.IsFormed:
            return
        ema_val = float(ema_result)
        input_high = DecimalIndicatorValue(self._highest, ema_val, t)
        input_high.IsFinal = True
        high_result = self._highest.Process(input_high)
        input_low = DecimalIndicatorValue(self._lowest, ema_val, t)
        input_low.IsFinal = True
        low_result = self._lowest.Process(input_low)
        if not self._highest.IsFormed or not self._lowest.IsFormed:
            return
        hh = float(high_result)
        ll = float(low_result)
        if hh == ll:
            return
        percent = (ema_val - ll) / (hh - ll) * 100.0
        input_wma = DecimalIndicatorValue(self._wma, percent, t)
        input_wma.IsFinal = True
        smooth_result = self._wma.Process(input_wma)
        if not self._wma.IsFormed:
            return
        current = float(smooth_result)
        if self._prev is not None and self._prev_prev is not None:
            was_rising = self._prev > self._prev_prev
            is_falling = current < self._prev
            was_falling = self._prev < self._prev_prev
            is_rising = current > self._prev
            if was_rising and is_falling and self.Position >= 0:
                self.SellMarket()
            elif was_falling and is_rising and self.Position <= 0:
                self.BuyMarket()
        self._prev_prev = self._prev
        self._prev = current

    def CreateClone(self):
        return chaikin_volatility_stochastic_strategy()
