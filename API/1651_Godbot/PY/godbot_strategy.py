import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import BollingerBands, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class godbot_strategy(Strategy):
    def __init__(self):
        super(godbot_strategy, self).__init__()
        self._bollinger_period = self.Param("BollingerPeriod", 20) \
            .SetDisplay("BB Period", "Bollinger Bands period", "Indicators")
        self._bollinger_deviation = self.Param("BollingerDeviation", 2.0) \
            .SetDisplay("BB Deviation", "Bollinger Bands deviation", "Indicators")
        self._ma_period = self.Param("MaPeriod", 50) \
            .SetDisplay("EMA Period", "EMA period for trend", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candle type", "General")
        self._prev_ema = 0.0
        self._has_prev_ema = False

    @property
    def bollinger_period(self):
        return self._bollinger_period.Value

    @property
    def bollinger_deviation(self):
        return self._bollinger_deviation.Value

    @property
    def ma_period(self):
        return self._ma_period.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(godbot_strategy, self).OnReseted()
        self._prev_ema = 0.0
        self._has_prev_ema = False

    def OnStarted(self, time):
        super(godbot_strategy, self).OnStarted(time)
        bb = BollingerBands()
        bb.Length = self.bollinger_period
        bb.Width = self.bollinger_deviation
        ema = ExponentialMovingAverage()
        ema.Length = self.ma_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ema, self.on_ema)
        subscription.BindEx(bb, self.on_bb).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, bb)
            self.DrawOwnTrades(area)

    def on_ema(self, candle, ema_val):
        if candle.State != CandleStates.Finished:
            return
        self._prev_ema = float(ema_val)
        self._has_prev_ema = True

    def on_bb(self, candle, bb_value):
        if candle.State != CandleStates.Finished:
            return
        if not self._has_prev_ema:
            return
        if bb_value.UpBand is None or bb_value.LowBand is None or bb_value.MovingAverage is None:
            return
        upper = float(bb_value.UpBand)
        lower = float(bb_value.LowBand)
        close = float(candle.ClosePrice)
        if close < lower and self.Position <= 0:
            self.BuyMarket()
        elif close > upper and self.Position >= 0:
            self.SellMarket()

    def CreateClone(self):
        return godbot_strategy()
