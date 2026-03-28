import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import BollingerBands, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy

class double_channel_ea_strategy(Strategy):
    def __init__(self):
        super(double_channel_ea_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._bb_period = self.Param("BbPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("BB Period", "Bollinger Bands period", "Indicators")
        self._ema_period = self.Param("EmaPeriod", 50) \
            .SetGreaterThanZero() \
            .SetDisplay("EMA Period", "EMA period for trend", "Indicators")

        self._prev_close = None
        self._prev_ema = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(double_channel_ea_strategy, self).OnReseted()
        self._prev_close = None
        self._prev_ema = None

    def OnStarted(self, time):
        super(double_channel_ea_strategy, self).OnStarted(time)

        self._bb = BollingerBands()
        self._bb.Length = self._bb_period.Value
        self._ema = ExponentialMovingAverage()
        self._ema.Length = self._ema_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(self._bb, self._ema, self._process_candle).Start()

    def _process_candle(self, candle, bb_val, ema_val):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if ema_val.IsEmpty:
            return

        ema_decimal = float(ema_val)
        close = float(candle.ClosePrice)

        if self._prev_close is not None and self._prev_ema is not None:
            cross_above = self._prev_close <= self._prev_ema and close > ema_decimal
            cross_below = self._prev_close >= self._prev_ema and close < ema_decimal

            if cross_above and self.Position <= 0:
                self.BuyMarket()
            elif cross_below and self.Position >= 0:
                self.SellMarket()

        self._prev_close = close
        self._prev_ema = ema_decimal

    def CreateClone(self):
        return double_channel_ea_strategy()
