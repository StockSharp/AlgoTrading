import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageDirectionalIndex, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class adx_trend_strategy(Strategy):
    """
    Strategy based on Average Directional Index (ADX) trend.
    Enters long when ADX > 25 and price crosses above MA.
    Enters short when ADX > 25 and price crosses below MA.
    """

    def __init__(self):
        super(adx_trend_strategy, self).__init__()
        self._adx_period = self.Param("AdxPeriod", 50).SetDisplay("ADX Period", "Period for calculating ADX indicator", "Indicators")
        self._ma_period = self.Param("MaPeriod", 200).SetDisplay("MA Period", "Period for calculating Moving Average", "Indicators")
        self._atr_multiplier = self.Param("AtrMultiplier", 2.0).SetDisplay("ATR Multiplier", "Multiplier for stop-loss based on ATR", "Risk parameters")
        self._adx_exit_threshold = self.Param("AdxExitThreshold", 20).SetDisplay("ADX Exit Threshold", "ADX level below which to exit position", "Exit parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")

        self._adx_above_threshold = False
        self._prev_adx_value = 0.0
        self._prev_ma_value = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(adx_trend_strategy, self).OnReseted()
        self._adx_above_threshold = False
        self._prev_adx_value = 0.0
        self._prev_ma_value = 0.0

    def OnStarted2(self, time):
        super(adx_trend_strategy, self).OnStarted2(time)

        adx = AverageDirectionalIndex()
        adx.Length = self._adx_period.Value
        ma = SimpleMovingAverage()
        ma.Length = self._ma_period.Value

        self._current_adx_ma = 0.0

        subscription = self.SubscribeCandles(self.candle_type)
        subscription \
            .BindEx(adx, self._process_adx) \
            .Bind(ma, self._process_ma) \
            .Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, adx)
            self.DrawIndicator(area, ma)
            self.DrawOwnTrades(area)

    def _process_adx(self, candle, adx_val):
        if hasattr(adx_val, 'MovingAverage') and adx_val.MovingAverage is not None:
            self._current_adx_ma = float(adx_val.MovingAverage)

    def _process_ma(self, candle, ma_val):
        if candle.State != CandleStates.Finished:
            return

        adx_ma = self._current_adx_ma
        ma_value = float(ma_val)

        if adx_ma == 0 or ma_value == 0:
            self._prev_ma_value = ma_value
            self._prev_adx_value = adx_ma
            return

        is_price_above_ma = float(candle.ClosePrice) > ma_value
        was_price_above_ma = self._prev_ma_value != 0 and float(candle.OpenPrice) > self._prev_ma_value
        is_adx_strong = adx_ma > 25

        if self._prev_ma_value != 0 and is_adx_strong and was_price_above_ma != is_price_above_ma:
            if is_price_above_ma and self.Position <= 0:
                self.BuyMarket(self.Volume + abs(self.Position))
            elif not is_price_above_ma and self.Position >= 0:
                self.SellMarket(self.Volume + abs(self.Position))

        self._prev_adx_value = adx_ma
        self._prev_ma_value = ma_value
        self._adx_above_threshold = is_adx_strong

    def CreateClone(self):
        return adx_trend_strategy()
