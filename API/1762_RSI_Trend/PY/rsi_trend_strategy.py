import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex, StandardDeviation
from StockSharp.Algo.Strategies import Strategy


class rsi_trend_strategy(Strategy):
    def __init__(self):
        super(rsi_trend_strategy, self).__init__()
        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("RSI Period", "Period for RSI calculation", "RSI Settings")
        self._rsi_buy_level = self.Param("RsiBuyLevel", 60.0) \
            .SetDisplay("RSI Buy Level", "Upper RSI barrier for long entries", "RSI Settings")
        self._rsi_sell_level = self.Param("RsiSellLevel", 40.0) \
            .SetDisplay("RSI Sell Level", "Lower RSI barrier for short entries", "RSI Settings")
        self._stdev_period = self.Param("StdevPeriod", 20) \
            .SetDisplay("StdDev Period", "StdDev period for trailing stop", "Settings")
        self._stdev_multiple = self.Param("StdevMultiple", 2.0) \
            .SetDisplay("StdDev Multiple", "StdDev multiplier for trailing stop", "Settings")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles for processing", "General")
        self._previous_rsi = 0.0
        self._is_rsi_initialized = False
        self._stop_price = 0.0

    @property
    def rsi_period(self):
        return self._rsi_period.Value

    @property
    def rsi_buy_level(self):
        return self._rsi_buy_level.Value

    @property
    def rsi_sell_level(self):
        return self._rsi_sell_level.Value

    @property
    def stdev_period(self):
        return self._stdev_period.Value

    @property
    def stdev_multiple(self):
        return self._stdev_multiple.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(rsi_trend_strategy, self).OnReseted()
        self._previous_rsi = 0.0
        self._is_rsi_initialized = False
        self._stop_price = 0.0

    def OnStarted(self, time):
        super(rsi_trend_strategy, self).OnStarted(time)
        rsi = RelativeStrengthIndex()
        rsi.Length = self.rsi_period
        stdev = StandardDeviation()
        stdev.Length = self.stdev_period
        self.SubscribeCandles(self.candle_type).Bind(rsi, stdev, self.process_candle).Start()

    def process_candle(self, candle, rsi_value, stdev_value):
        if candle.State != CandleStates.Finished:
            return
        sv = float(stdev_value)
        if sv <= 0:
            return

        rv = float(rsi_value)

        if not self._is_rsi_initialized:
            self._previous_rsi = rv
            self._is_rsi_initialized = True
            return

        buy_level = float(self.rsi_buy_level)
        sell_level = float(self.rsi_sell_level)
        sm = float(self.stdev_multiple)
        close = float(candle.ClosePrice)

        bullish = rv > buy_level and self._previous_rsi <= buy_level
        bearish = rv < sell_level and self._previous_rsi >= sell_level

        if bullish and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._stop_price = close - sv * sm
        elif bearish and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._stop_price = close + sv * sm

        if self.Position > 0:
            new_stop = close - sv * sm
            if new_stop > self._stop_price:
                self._stop_price = new_stop
            if close <= self._stop_price:
                self.SellMarket()
        elif self.Position < 0:
            new_stop = close + sv * sm
            if new_stop < self._stop_price:
                self._stop_price = new_stop
            if close >= self._stop_price:
                self.BuyMarket()

        self._previous_rsi = rv

    def CreateClone(self):
        return rsi_trend_strategy()
