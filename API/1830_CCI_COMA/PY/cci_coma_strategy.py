import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import CommodityChannelIndex, RelativeStrengthIndex, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class cci_coma_strategy(Strategy):
    def __init__(self):
        super(cci_coma_strategy, self).__init__()
        self._cci_period = self.Param("CciPeriod", 14) \
            .SetDisplay("CCI Period", "CCI length", "Indicators")
        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("RSI Period", "RSI length", "Indicators")
        self._ema_period = self.Param("EmaPeriod", 20) \
            .SetDisplay("EMA Period", "Trend EMA period", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candle type", "General")
        self._prev_cci = 0.0
        self._has_prev = False

    @property
    def cci_period(self):
        return self._cci_period.Value

    @property
    def rsi_period(self):
        return self._rsi_period.Value

    @property
    def ema_period(self):
        return self._ema_period.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(cci_coma_strategy, self).OnReseted()
        self._prev_cci = 0.0
        self._has_prev = False

    def OnStarted(self, time):
        super(cci_coma_strategy, self).OnStarted(time)
        cci = CommodityChannelIndex()
        cci.Length = self.cci_period
        rsi = RelativeStrengthIndex()
        rsi.Length = self.rsi_period
        ema = ExponentialMovingAverage()
        ema.Length = self.ema_period
        self.SubscribeCandles(self.candle_type) \
            .Bind(cci, rsi, ema, self.process_candle) \
            .Start()

    def process_candle(self, candle, cci_val, rsi_val, ema_val):
        if candle.State != CandleStates.Finished:
            return
        cci_val = float(cci_val)
        rsi_val = float(rsi_val)
        ema_val = float(ema_val)
        if not self._has_prev:
            self._prev_cci = cci_val
            self._has_prev = True
            return
        bullish = cci_val > 0 and rsi_val > 50.0 and float(candle.ClosePrice) > ema_val
        bearish = cci_val < 0 and rsi_val < 50.0 and float(candle.ClosePrice) < ema_val
        cci_cross_up = self._prev_cci <= 0 and cci_val > 0
        cci_cross_down = self._prev_cci >= 0 and cci_val < 0
        if cci_cross_up and bullish and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif cci_cross_down and bearish and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
        self._prev_cci = cci_val

    def CreateClone(self):
        return cci_coma_strategy()
