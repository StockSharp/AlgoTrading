import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, CommodityChannelIndex
from StockSharp.Algo.Strategies import Strategy

class cci_macd_scalper_strategy(Strategy):
    def __init__(self):
        super(cci_macd_scalper_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30)))
        self._ema_period = self.Param("EmaPeriod", 21)
        self._cci_period = self.Param("CciPeriod", 14)

        self._prev_cci = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def EmaPeriod(self):
        return self._ema_period.Value

    @EmaPeriod.setter
    def EmaPeriod(self, value):
        self._ema_period.Value = value

    @property
    def CciPeriod(self):
        return self._cci_period.Value

    @CciPeriod.setter
    def CciPeriod(self, value):
        self._cci_period.Value = value

    def OnReseted(self):
        super(cci_macd_scalper_strategy, self).OnReseted()
        self._prev_cci = None

    def OnStarted(self, time):
        super(cci_macd_scalper_strategy, self).OnStarted(time)
        self._prev_cci = None

        ema = ExponentialMovingAverage()
        ema.Length = self.EmaPeriod
        cci = CommodityChannelIndex()
        cci.Length = self.CciPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(ema, cci, self._process_candle).Start()

    def _process_candle(self, candle, ema_value, cci_value):
        if candle.State != CandleStates.Finished:
            return

        ema_val = float(ema_value)
        cci_val = float(cci_value)
        close = float(candle.ClosePrice)

        if self._prev_cci is None:
            self._prev_cci = cci_val
            return

        # CCI crosses back above oversold zone with trend confirmation -> buy
        cci_cross_up = self._prev_cci <= -50.0 and cci_val > -50.0
        # CCI crosses back below overbought zone with trend confirmation -> sell
        cci_cross_down = self._prev_cci >= 50.0 and cci_val < 50.0

        if cci_cross_up and close > ema_val:
            if self.Position <= 0:
                self.BuyMarket()
        elif cci_cross_down and close < ema_val:
            if self.Position >= 0:
                self.SellMarket()

        self._prev_cci = cci_val

    def CreateClone(self):
        return cci_macd_scalper_strategy()
