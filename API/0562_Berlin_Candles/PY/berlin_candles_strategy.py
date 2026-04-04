import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.BusinessEntities")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, DonchianChannels, CandleIndicatorValue
from StockSharp.Algo.Strategies import Strategy


class berlin_candles_strategy(Strategy):
    def __init__(self):
        super(berlin_candles_strategy, self).__init__()
        self._smoothing = self.Param("Smoothing", 1) \
            .SetDisplay("Smoothing", "EMA smoothing for Berlin open", "Berlin")
        self._baseline_period = self.Param("BaselinePeriod", 26) \
            .SetDisplay("Baseline Period", "Donchian baseline period", "Berlin")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._prev_ema = 0.0
        self._is_initialized = False
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(berlin_candles_strategy, self).OnReseted()
        self._prev_ema = 0.0
        self._is_initialized = False
        self._cooldown = 0

    def OnStarted2(self, time):
        super(berlin_candles_strategy, self).OnStarted2(time)
        self._ema = ExponentialMovingAverage()
        self._ema.Length = self._smoothing.Value + 1
        self._donchian = DonchianChannels()
        self._donchian.Length = self._baseline_period.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._ema, self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._donchian)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, ema_val):
        if candle.State != CandleStates.Finished:
            return
        ema_v = float(ema_val)
        donchian_result = self._donchian.Process(CandleIndicatorValue(self._donchian, candle))
        middle = donchian_result.Middle
        if middle is None:
            return
        middle_v = float(middle)
        if not self._is_initialized:
            self._prev_ema = ema_v
            self._is_initialized = True
            return
        open_expr = self._prev_ema
        close_expr = float(candle.ClosePrice)
        high_v = float(candle.HighPrice)
        low_v = float(candle.LowPrice)
        if open_expr > close_expr:
            open_value = min(open_expr, high_v)
            close_value = max(close_expr, low_v)
        else:
            open_value = max(open_expr, low_v)
            close_value = min(close_expr, high_v)
        if self._cooldown > 0:
            self._cooldown -= 1
            self._prev_ema = ema_v
            return
        if close_value > open_value and close_value > middle_v and self.Position <= 0:
            self.BuyMarket()
            self._cooldown = 100
        elif close_value < open_value and close_value < middle_v and self.Position >= 0:
            self.SellMarket()
            self._cooldown = 100
        self._prev_ema = ema_v

    def CreateClone(self):
        return berlin_candles_strategy()
