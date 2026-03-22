import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import WeightedMovingAverage, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy


class market_slayer_strategy(Strategy):
    def __init__(self):
        super(market_slayer_strategy, self).__init__()
        self._short_length = self.Param("ShortLength", 10) \
            .SetDisplay("Short Length", "Short WMA period", "General")
        self._long_length = self.Param("LongLength", 20) \
            .SetDisplay("Long Length", "Long WMA period", "General")
        self._confirmation_trend_value = self.Param("ConfirmationTrendValue", 2) \
            .SetDisplay("Confirmation Trend Value", "Trend WMA period", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._trend_hlv = 0
        self._is_trend_bullish = False
        self._prev_short = None
        self._prev_long = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(market_slayer_strategy, self).OnReseted()
        self._trend_hlv = 0
        self._is_trend_bullish = False
        self._prev_short = None
        self._prev_long = None

    def OnStarted(self, time):
        super(market_slayer_strategy, self).OnStarted(time)
        self._trend_hlv = 0
        self._is_trend_bullish = False
        self._prev_short = None
        self._prev_long = None
        self._short_wma = WeightedMovingAverage()
        self._short_wma.Length = self._short_length.Value
        self._long_wma = WeightedMovingAverage()
        self._long_wma.Length = self._long_length.Value
        self._trend_wma_high = WeightedMovingAverage()
        self._trend_wma_high.Length = self._confirmation_trend_value.Value
        self._trend_wma_low = WeightedMovingAverage()
        self._trend_wma_low.Length = self._confirmation_trend_value.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._short_wma, self._long_wma, self.OnProcess).Start()

    def OnProcess(self, candle, short_wma, long_wma):
        if candle.State != CandleStates.Finished:
            return
        t = candle.ServerTime
        high_result = self._trend_wma_high.Process(DecimalIndicatorValue(self._trend_wma_high, candle.HighPrice, t))
        low_result = self._trend_wma_low.Process(DecimalIndicatorValue(self._trend_wma_low, candle.LowPrice, t))
        if self._trend_wma_high.IsFormed and self._trend_wma_low.IsFormed:
            high_v = float(high_result)
            low_v = float(low_result)
            close = float(candle.ClosePrice)
            if close > high_v:
                self._trend_hlv = 1
            elif close < low_v:
                self._trend_hlv = -1
            if self._trend_hlv < 0:
                ssl_down = high_v
                ssl_up = low_v
            else:
                ssl_down = low_v
                ssl_up = high_v
            self._is_trend_bullish = ssl_up > ssl_down
        sv = float(short_wma)
        lv = float(long_wma)
        if not self._short_wma.IsFormed or not self._long_wma.IsFormed:
            self._prev_short = sv
            self._prev_long = lv
            return
        if self._prev_short is not None and self._prev_long is not None:
            cross_up = self._prev_short <= self._prev_long and sv > lv
            cross_down = self._prev_short >= self._prev_long and sv < lv
            if cross_up and self._is_trend_bullish and self.Position <= 0:
                self.BuyMarket()
            if cross_down and not self._is_trend_bullish and self.Position >= 0:
                self.SellMarket()
            if self.Position > 0 and not self._is_trend_bullish:
                self.SellMarket()
            if self.Position < 0 and self._is_trend_bullish:
                self.BuyMarket()
        self._prev_short = sv
        self._prev_long = lv

    def CreateClone(self):
        return market_slayer_strategy()
