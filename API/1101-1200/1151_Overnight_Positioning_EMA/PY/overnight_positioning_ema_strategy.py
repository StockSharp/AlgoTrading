import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class overnight_positioning_ema_strategy(Strategy):
    def __init__(self):
        super(overnight_positioning_ema_strategy, self).__init__()
        self._ema_length = self.Param("EmaLength", 100) \
            .SetGreaterThanZero()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))
        self._current_day = None
        self._trade_taken_today = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(overnight_positioning_ema_strategy, self).OnReseted()
        self._current_day = None
        self._trade_taken_today = False

    def OnStarted2(self, time):
        super(overnight_positioning_ema_strategy, self).OnStarted2(time)
        self._current_day = None
        self._trade_taken_today = False
        self._ema = ExponentialMovingAverage()
        self._ema.Length = self._ema_length.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._ema, self.OnProcess).Start()

    def OnProcess(self, candle, ema_val):
        if candle.State != CandleStates.Finished:
            return
        if not self._ema.IsFormed:
            return
        ev = float(ema_val)
        close = float(candle.ClosePrice)
        day = candle.OpenTime.Date
        if self._current_day is None or self._current_day != day:
            self._current_day = day
            self._trade_taken_today = False
        if self._trade_taken_today:
            return
        hour = candle.OpenTime.Hour
        if hour >= 15 and hour < 16 and close > ev and self.Position <= 0:
            self.BuyMarket()
            self._trade_taken_today = True
        elif hour >= 9 and hour < 10 and self.Position > 0:
            self.SellMarket()
            self._trade_taken_today = True

    def CreateClone(self):
        return overnight_positioning_ema_strategy()
