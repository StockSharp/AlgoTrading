import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, BollingerBands
from StockSharp.Algo.Strategies import Strategy

class i_trend_strategy(Strategy):
    """
    i_Trend strategy using Bollinger Bands upper band and EMA.
    Generates signals when iTrend value crosses signal line.
    """

    def __init__(self):
        super(i_trend_strategy, self).__init__()
        self._ma_period = self.Param("MaPeriod", 13) \
            .SetDisplay("MA Period", "Moving average length", "Indicator")
        self._bb_period = self.Param("BbPeriod", 20) \
            .SetDisplay("BB Period", "Bollinger Bands period", "Indicator")
        self._bb_deviation = self.Param("BbDeviation", 2.0) \
            .SetDisplay("BB Deviation", "Standard deviation for BB", "Indicator")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Type of candles used", "General")

        self._prev_ind = 0.0
        self._prev_sign = 0.0
        self._is_initialized = False
        self._ma = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(i_trend_strategy, self).OnReseted()
        self._prev_ind = 0.0
        self._prev_sign = 0.0
        self._is_initialized = False

    def OnStarted(self, time):
        super(i_trend_strategy, self).OnStarted(time)

        self._ma = ExponentialMovingAverage()
        self._ma.Length = self._ma_period.Value

        bb = BollingerBands()
        bb.Length = self._bb_period.Value
        bb.Width = self._bb_deviation.Value

        self.Indicators.Add(self._ma)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(bb, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._ma)
            self.DrawIndicator(area, bb)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, bb_value):
        if candle.State != CandleStates.Finished:
            return

        if not bb_value.IsFormed:
            return

        ma_result = self._ma.Process(candle.ClosePrice, candle.OpenTime, True)
        if not ma_result.IsFormed:
            return

        ma_val = float(ma_result)
        upper_band = bb_value.UpBand
        if upper_band is None:
            return
        upper_band = float(upper_band)

        price = float(candle.ClosePrice)
        low = float(candle.LowPrice)
        high = float(candle.HighPrice)

        ind = price - upper_band
        sign = 2.0 * ma_val - (low + high)

        if not self._is_initialized:
            self._prev_ind = ind
            self._prev_sign = sign
            self._is_initialized = True
            return

        cross_up = self._prev_ind <= self._prev_sign and ind > sign
        cross_down = self._prev_ind >= self._prev_sign and ind < sign

        if cross_up and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif cross_down and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

        self._prev_ind = ind
        self._prev_sign = sign

    def CreateClone(self):
        return i_trend_strategy()
