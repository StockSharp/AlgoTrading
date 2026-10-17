import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, OrderStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, BollingerBands
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *

class i_trend_strategy(Strategy):
    """
    i_Trend strategy using Bollinger Bands upper band and EMA.
    Generates signals when iTrend value crosses signal line.
    """

    def __init__(self):
        super(i_trend_strategy, self).__init__()
        self._price_type = self.Param("PriceType", 0).SetRange(0, 5)
        self._bb_mode = self.Param("BbMode", 0).SetRange(0, 2)
        self._pending = None
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
        self._pending = None
        self._ma = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(i_trend_strategy, self).OnReseted()
        self._prev_ind = 0.0
        self._prev_sign = 0.0
        self._is_initialized = False

    def OnStarted2(self, time):
        super(i_trend_strategy, self).OnStarted2(time)
        self._pending = None

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

        ma_result = process_float(self._ma, candle.ClosePrice, candle.OpenTime, True)
        if not ma_result.IsFormed:
            return

        ma_val = float(ma_result)
        upper_band, lower_band = bb_value.UpBand, bb_value.LowBand
        if upper_band is None or lower_band is None:
            return
        upper_band, lower_band = float(upper_band), float(lower_band)
        bb_mode = int(self._bb_mode.Value)
        selected_band = lower_band if bb_mode == 1 else ((upper_band + lower_band) / 2 if bb_mode == 2 else upper_band)

        price = float(candle.ClosePrice)
        low = float(candle.LowPrice)
        high = float(candle.HighPrice)

        kind = int(self._price_type.Value)
        selected_price = {0: price, 1: float(candle.OpenPrice), 2: high, 3: low,
                          4: (high + low) / 2, 5: (high + low + price) / 3}.get(kind, price)
        ind = selected_price - selected_band
        sign = 2.0 * ma_val - (low + high)

        if not self._is_initialized:
            self._prev_ind = ind
            self._prev_sign = sign
            self._is_initialized = True
            return

        cross_up = self._prev_ind <= self._prev_sign and ind > sign
        cross_down = self._prev_ind >= self._prev_sign and ind < sign

        if self.IsFormedAndOnlineAndAllowTrading() and (self._pending is None or self._pending.State in (OrderStates.Done, OrderStates.Failed)):
            if cross_up and self.Position <= 0:
                self._pending = self.BuyMarket(self.Volume - min(0, self.Position))
            elif cross_down and self.Position >= 0:
                self._pending = self.SellMarket(self.Volume + max(0, self.Position))

        self._prev_ind = ind
        self._prev_sign = sign

    def CreateClone(self):
        return i_trend_strategy()
