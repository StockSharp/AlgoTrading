import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class auto_kdj_strategy(Strategy):
    def __init__(self):
        super(auto_kdj_strategy, self).__init__()
        self._rsi_period = self.Param("RsiPeriod", 9) \
            .SetDisplay("RSI Period", "RSI lookback", "Indicators")
        self._ema_period = self.Param("EmaPeriod", 20) \
            .SetDisplay("EMA Period", "EMA filter", "Indicators")
        self._cooldown_candles = self.Param("CooldownCandles", 30) \
            .SetDisplay("Cooldown", "Candles between signals", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._prev_rsi = 0.0
        self._has_prev = False
        self._cooldown_remaining = 0

    @property
    def rsi_period(self):
        return self._rsi_period.Value
    @property
    def ema_period(self):
        return self._ema_period.Value
    @property
    def cooldown_candles(self):
        return self._cooldown_candles.Value
    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(auto_kdj_strategy, self).OnReseted()
        self._prev_rsi = 0.0
        self._has_prev = False
        self._cooldown_remaining = 0

    def OnStarted2(self, time):
        super(auto_kdj_strategy, self).OnStarted2(time)
        self._prev_rsi = 0.0
        self._has_prev = False
        self._cooldown_remaining = 0
        rsi = RelativeStrengthIndex()
        rsi.Length = self.rsi_period
        ema = ExponentialMovingAverage()
        ema.Length = self.ema_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(rsi, ema, self.process_candle).Start()

    def process_candle(self, candle, rsi, ema):
        if candle.State != CandleStates.Finished:
            return
        close = float(candle.ClosePrice)
        rsi_val = float(rsi)
        ema_val = float(ema)
        if not self._has_prev:
            self._prev_rsi = rsi_val
            self._has_prev = True
            return
        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            self._prev_rsi = rsi_val
            return
        if self._prev_rsi <= 20 and rsi_val > 20 and close > ema_val and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._cooldown_remaining = self.cooldown_candles
        elif self._prev_rsi >= 80 and rsi_val < 80 and close < ema_val and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._cooldown_remaining = self.cooldown_candles
        self._prev_rsi = rsi_val

    def CreateClone(self):
        return auto_kdj_strategy()
