import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class get_trend_stochastic_strategy(Strategy):
    def __init__(self):
        super(get_trend_stochastic_strategy, self).__init__()
        self._rsi_period = self.Param("RsiPeriod", 14).SetDisplay("RSI Period", "RSI lookback", "Indicators")
        self._ema_period = self.Param("EmaPeriod", 20).SetDisplay("EMA Period", "EMA filter", "Indicators")
        self._oversold = self.Param("Oversold", 35.0).SetDisplay("Oversold", "RSI oversold level", "Levels")
        self._overbought = self.Param("Overbought", 65.0).SetDisplay("Overbought", "RSI overbought level", "Levels")
        self._cooldown_candles = self.Param("CooldownCandles", 30).SetDisplay("Cooldown", "Candles between signals", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Candle timeframe", "General")
        self._prev_rsi = 0.0
        self._has_prev = False
        self._cooldown_remaining = 0

    @property
    def rsi_period(self): return self._rsi_period.Value
    @property
    def ema_period(self): return self._ema_period.Value
    @property
    def oversold(self): return self._oversold.Value
    @property
    def overbought(self): return self._overbought.Value
    @property
    def cooldown_candles(self): return self._cooldown_candles.Value
    @property
    def candle_type(self): return self._candle_type.Value

    def OnReseted(self):
        super(get_trend_stochastic_strategy, self).OnReseted()
        self._prev_rsi = 0.0
        self._has_prev = False
        self._cooldown_remaining = 0

    def OnStarted2(self, time):
        super(get_trend_stochastic_strategy, self).OnStarted2(time)
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
        oversold = float(self.oversold)
        overbought = float(self.overbought)
        if self._prev_rsi <= oversold and rsi_val > oversold and close > ema_val and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._cooldown_remaining = self.cooldown_candles
        elif self._prev_rsi >= overbought and rsi_val < overbought and close < ema_val and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._cooldown_remaining = self.cooldown_candles
        self._prev_rsi = rsi_val

    def CreateClone(self):
        return get_trend_stochastic_strategy()
