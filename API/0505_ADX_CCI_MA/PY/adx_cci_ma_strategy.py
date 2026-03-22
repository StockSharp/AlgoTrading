import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import CommodityChannelIndex, AverageDirectionalIndex, SimpleMovingAverage, IndicatorHelper
from StockSharp.Algo.Strategies import Strategy


class adx_cci_ma_strategy(Strategy):
    def __init__(self):
        super(adx_cci_ma_strategy, self).__init__()
        self._cci_period = self.Param("CciPeriod", 15) \
            .SetGreaterThanZero() \
            .SetDisplay("CCI Period", "Period for CCI", "Indicators")
        self._adx_length = self.Param("AdxLength", 10) \
            .SetGreaterThanZero() \
            .SetDisplay("ADX Length", "Length for ADX", "Indicators")
        self._adx_threshold = self.Param("AdxThreshold", 20.0) \
            .SetDisplay("ADX Threshold", "ADX level to confirm trend", "Indicators")
        self._ma_length = self.Param("MaLength", 50) \
            .SetGreaterThanZero() \
            .SetDisplay("MA Length", "Length of moving average", "MA Trend")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Timeframe for candles", "General")
        self._cooldown_bars = self.Param("CooldownBars", 10) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "Risk")
        self._prev_plus_di = 0.0
        self._prev_minus_di = 0.0
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(adx_cci_ma_strategy, self).OnReseted()
        self._prev_plus_di = 0.0
        self._prev_minus_di = 0.0
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(adx_cci_ma_strategy, self).OnStarted(time)
        cci = CommodityChannelIndex()
        cci.Length = int(self._cci_period.Value)
        adx = AverageDirectionalIndex()
        adx.Length = int(self._adx_length.Value)
        ma = SimpleMovingAverage()
        ma.Length = int(self._ma_length.Value)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(adx, cci, ma, self._on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ma)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, adx_value, cci_value, ma_value):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        adx_ma = adx_value.MovingAverage
        if adx_ma is None:
            return
        adx_v = float(adx_ma)

        dx = adx_value.Dx
        plus_di_val = dx.Plus
        minus_di_val = dx.Minus
        if plus_di_val is None or minus_di_val is None:
            return

        plus_di = float(plus_di_val)
        minus_di = float(minus_di_val)
        cci_v = float(IndicatorHelper.ToDecimal(cci_value))
        ma_v = float(IndicatorHelper.ToDecimal(ma_value))
        close = float(candle.ClosePrice)

        if self._prev_plus_di == 0:
            self._prev_plus_di = plus_di
            self._prev_minus_di = minus_di
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            self._prev_plus_di = plus_di
            self._prev_minus_di = minus_di
            return

        threshold = float(self._adx_threshold.Value)
        cooldown = int(self._cooldown_bars.Value)
        long_signal = plus_di > minus_di and self._prev_plus_di <= self._prev_minus_di
        short_signal = minus_di > plus_di and self._prev_minus_di <= self._prev_plus_di

        if long_signal and cci_v > 0 and adx_v >= threshold and close > ma_v and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket(Math.Abs(self.Position))
            self.BuyMarket(self.Volume)
            self._cooldown_remaining = cooldown
        elif short_signal and cci_v < 0 and adx_v >= threshold and close < ma_v and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket(Math.Abs(self.Position))
            self.SellMarket(self.Volume)
            self._cooldown_remaining = cooldown

        self._prev_plus_di = plus_di
        self._prev_minus_di = minus_di

    def CreateClone(self):
        return adx_cci_ma_strategy()
