import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import StochasticOscillator
from StockSharp.Algo.Strategies import Strategy

class morning_evening_stochastic_strategy(Strategy):
    def __init__(self):
        super(morning_evening_stochastic_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30)))
        self._stoch_period = self.Param("StochPeriod", 14)
        self._oversold = self.Param("Oversold", 30.0)
        self._overbought = self.Param("Overbought", 70.0)
        self._signal_cooldown_candles = self.Param("SignalCooldownCandles", 6)

        self._candles = []
        self._prev_k = 0.0
        self._has_prev_k = False
        self._candles_since_trade = 6

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def StochPeriod(self):
        return self._stoch_period.Value

    @StochPeriod.setter
    def StochPeriod(self, value):
        self._stoch_period.Value = value

    @property
    def Oversold(self):
        return self._oversold.Value

    @Oversold.setter
    def Oversold(self, value):
        self._oversold.Value = value

    @property
    def Overbought(self):
        return self._overbought.Value

    @Overbought.setter
    def Overbought(self, value):
        self._overbought.Value = value

    @property
    def SignalCooldownCandles(self):
        return self._signal_cooldown_candles.Value

    @SignalCooldownCandles.setter
    def SignalCooldownCandles(self, value):
        self._signal_cooldown_candles.Value = value

    def OnReseted(self):
        super(morning_evening_stochastic_strategy, self).OnReseted()
        self._candles.clear()
        self._prev_k = 0.0
        self._has_prev_k = False
        self._candles_since_trade = self.SignalCooldownCandles

    def OnStarted(self, time):
        super(morning_evening_stochastic_strategy, self).OnStarted(time)
        self._candles.clear()
        self._has_prev_k = False
        self._candles_since_trade = self.SignalCooldownCandles

        stoch = StochasticOscillator()
        stoch.K.Length = self.StochPeriod
        stoch.D.Length = 3

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(stoch, self._process_candle).Start()

    def _process_candle(self, candle, stoch_value):
        if candle.State != CandleStates.Finished:
            return

        if self._candles_since_trade < self.SignalCooldownCandles:
            self._candles_since_trade += 1

        k_val = stoch_value.K
        if k_val is None:
            return
        k_value = float(k_val)

        self._candles.append(candle)
        if len(self._candles) > 5:
            self._candles.pop(0)

        if len(self._candles) >= 3:
            c3 = self._candles[-1]
            c2 = self._candles[-2]
            c1 = self._candles[-3]

            body1 = abs(float(c1.ClosePrice) - float(c1.OpenPrice))
            body2 = abs(float(c2.ClosePrice) - float(c2.OpenPrice))

            is_morning_star = (float(c1.OpenPrice) > float(c1.ClosePrice)
                and body2 < body1 * 0.5
                and float(c3.ClosePrice) > float(c3.OpenPrice)
                and float(c3.ClosePrice) > (float(c1.OpenPrice) + float(c1.ClosePrice)) / 2.0)

            is_evening_star = (float(c1.ClosePrice) > float(c1.OpenPrice)
                and body2 < body1 * 0.5
                and float(c3.OpenPrice) > float(c3.ClosePrice)
                and float(c3.ClosePrice) < (float(c1.OpenPrice) + float(c1.ClosePrice)) / 2.0)

            if is_morning_star and k_value < self.Oversold and self.Position <= 0 and self._candles_since_trade >= self.SignalCooldownCandles:
                self.BuyMarket()
                self._candles_since_trade = 0
            elif is_evening_star and k_value > self.Overbought and self.Position >= 0 and self._candles_since_trade >= self.SignalCooldownCandles:
                self.SellMarket()
                self._candles_since_trade = 0

        if self._has_prev_k:
            if self.Position > 0 and self._prev_k >= self.Overbought and k_value < self.Overbought and self._candles_since_trade >= self.SignalCooldownCandles:
                self.SellMarket()
                self._candles_since_trade = 0
            elif self.Position < 0 and self._prev_k <= self.Oversold and k_value > self.Oversold and self._candles_since_trade >= self.SignalCooldownCandles:
                self.BuyMarket()
                self._candles_since_trade = 0

        self._prev_k = k_value
        self._has_prev_k = True

    def CreateClone(self):
        return morning_evening_stochastic_strategy()
