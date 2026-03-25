import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy


class heiken_ashi_smoothed_trend_strategy(Strategy):
    def __init__(self):
        super(heiken_ashi_smoothed_trend_strategy, self).__init__()
        self._ema_length = self.Param("EmaLength", 30) \
            .SetDisplay("EMA Length", "Length for smoothing", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._open_ema = None
        self._close_ema = None
        self._high_ema = None
        self._low_ema = None
        self._prev_ha_open = None
        self._prev_ha_close = None
        self._prev_is_green = None

    @property
    def ema_length(self):
        return self._ema_length.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(heiken_ashi_smoothed_trend_strategy, self).OnReseted()
        self._open_ema = None
        self._close_ema = None
        self._high_ema = None
        self._low_ema = None
        self._prev_ha_open = None
        self._prev_ha_close = None
        self._prev_is_green = None

    def OnStarted(self, time):
        super(heiken_ashi_smoothed_trend_strategy, self).OnStarted(time)
        self._open_ema = ExponentialMovingAverage()
        self._open_ema.Length = self.ema_length
        self._close_ema = ExponentialMovingAverage()
        self._close_ema.Length = self.ema_length
        self._high_ema = ExponentialMovingAverage()
        self._high_ema.Length = self.ema_length
        self._low_ema = ExponentialMovingAverage()
        self._low_ema.Length = self.ema_length
        self.Indicators.Add(self._open_ema)
        self.Indicators.Add(self._close_ema)
        self.Indicators.Add(self._high_ema)
        self.Indicators.Add(self._low_ema)
        warmup = ExponentialMovingAverage()
        warmup.Length = self.ema_length
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(warmup, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, _warmup_val):
        if candle.State != CandleStates.Finished:
            return
        t = candle.OpenTime
        d1 = DecimalIndicatorValue(self._open_ema, float(candle.OpenPrice), t)
        d1.IsFinal = True
        o_result = self._open_ema.Process(d1)
        d2 = DecimalIndicatorValue(self._close_ema, float(candle.ClosePrice), t)
        d2.IsFinal = True
        c_result = self._close_ema.Process(d2)
        d3 = DecimalIndicatorValue(self._high_ema, float(candle.HighPrice), t)
        d3.IsFinal = True
        h_result = self._high_ema.Process(d3)
        d4 = DecimalIndicatorValue(self._low_ema, float(candle.LowPrice), t)
        d4.IsFinal = True
        l_result = self._low_ema.Process(d4)
        if not o_result.IsFormed or not c_result.IsFormed or not h_result.IsFormed or not l_result.IsFormed:
            return
        open_ema = float(o_result)
        close_ema = float(c_result)
        high_ema = float(h_result)
        low_ema = float(l_result)
        ha_close = (open_ema + high_ema + low_ema + close_ema) / 4.0
        if self._prev_ha_open is None:
            ha_open = (open_ema + close_ema) / 2.0
        else:
            ha_open = (self._prev_ha_open + self._prev_ha_close) / 2.0
        is_green = ha_close >= ha_open
        buy_signal = is_green and self._prev_is_green == False
        sell_signal = not is_green and self._prev_is_green == True
        if buy_signal and self.Position <= 0:
            self.BuyMarket()
        elif sell_signal and self.Position >= 0:
            self.SellMarket()
        self._prev_ha_open = ha_open
        self._prev_ha_close = ha_close
        self._prev_is_green = is_green

    def CreateClone(self):
        return heiken_ashi_smoothed_trend_strategy()
