import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class overnight_effect_high_volatility_crypto_strategy(Strategy):
    def __init__(self):
        super(overnight_effect_high_volatility_crypto_strategy, self).__init__()
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
        super(overnight_effect_high_volatility_crypto_strategy, self).OnReseted()
        self._current_day = None
        self._trade_taken_today = False

    def OnStarted2(self, time):
        super(overnight_effect_high_volatility_crypto_strategy, self).OnStarted2(time)
        self._current_day = None
        self._trade_taken_today = False
        self._sma = SimpleMovingAverage()
        self._sma.Length = 20
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._sma, self.OnProcess).Start()

    def OnProcess(self, candle, sma_val):
        if candle.State != CandleStates.Finished:
            return
        if not self._sma.IsFormed:
            return
        sv = float(sma_val)
        close = float(candle.ClosePrice)
        day = candle.OpenTime.Date
        if self._current_day is None or self._current_day != day:
            self._current_day = day
            self._trade_taken_today = False
        if self._trade_taken_today:
            return
        hour = candle.OpenTime.Hour
        if hour == 20 and self.Position <= 0 and close > sv:
            self.BuyMarket()
            self._trade_taken_today = True
        elif hour == 8 and self.Position >= 0 and close < sv:
            self.SellMarket()
            self._trade_taken_today = True

    def CreateClone(self):
        return overnight_effect_high_volatility_crypto_strategy()
