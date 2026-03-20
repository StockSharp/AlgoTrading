import clr
clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy

class e_turbo_fx_strategy(Strategy):
    def __init__(self):
        super(e_turbo_fx_strategy, self).__init__()
        self._ema_period = self.Param("EmaPeriod", 20) \
            .SetDisplay("EMA Period", "EMA trend filter period", "Parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candle type", "General")
        self._bear_count = 0
        self._bull_count = 0
        self._prev_body = 0.0
        self._has_prev = False
    @property
    def ema_period(self):
        return self._ema_period.Value
    @property
    def candle_type(self):
        return self._candle_type.Value
    def OnReseted(self):
        super(e_turbo_fx_strategy, self).OnReseted()
        self._bear_count = 0
        self._bull_count = 0
        self._prev_body = 0.0
        self._has_prev = False
    def OnStarted(self, time):
        super(e_turbo_fx_strategy, self).OnStarted(time)
        ema = ExponentialMovingAverage()
        ema.Length = self.ema_period
        self.SubscribeCandles(self.candle_type).Bind(ema, self.process_candle).Start()
    def process_candle(self, candle, ema_val):
        if candle.State != CandleStates.Finished:
            return
        body = abs(float(candle.ClosePrice) - float(candle.OpenPrice))
        if float(candle.ClosePrice) < float(candle.OpenPrice):
            self._bear_count += 1
            if self._has_prev and body > self._prev_body:
                self._bear_count = min(self._bear_count, 10)
            else:
                self._bear_count = 1
            self._bull_count = 0
        elif float(candle.ClosePrice) > float(candle.OpenPrice):
            self._bull_count += 1
            if self._has_prev and body > self._prev_body:
                self._bull_count = min(self._bull_count, 10)
            else:
                self._bull_count = 1
            self._bear_count = 0
        else:
            self._bear_count = 0
            self._bull_count = 0
        self._prev_body = body
        self._has_prev = True
        ev = float(ema_val)
        close = float(candle.ClosePrice)
        if self._bear_count >= 3 and close > ev and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif self._bull_count >= 3 and close < ev and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
    def CreateClone(self):
        return e_turbo_fx_strategy()
