import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import StochasticOscillator
from StockSharp.Algo.Strategies import Strategy

class bullish_bearish_harami_stochastic_strategy(Strategy):
    def __init__(self):
        super(bullish_bearish_harami_stochastic_strategy, self).__init__()

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
        super(bullish_bearish_harami_stochastic_strategy, self).OnReseted()
        self._candles.clear()
        self._prev_k = 0.0
        self._has_prev_k = False
        self._candles_since_trade = self.SignalCooldownCandles

    def OnStarted(self, time):
        super(bullish_bearish_harami_stochastic_strategy, self).OnStarted(time)
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

        if len(self._candles) >= 2:
            curr = self._candles[-1]
            prev = self._candles[-2]

            # Bullish harami: prev bearish, curr bullish, curr body inside prev body
            bullish_harami = (float(prev.OpenPrice) > float(prev.ClosePrice)
                and float(curr.ClosePrice) > float(curr.OpenPrice)
                and float(curr.OpenPrice) > float(prev.ClosePrice)
                and float(curr.ClosePrice) < float(prev.OpenPrice))

            # Bearish harami: prev bullish, curr bearish, curr body inside prev body
            bearish_harami = (float(prev.ClosePrice) > float(prev.OpenPrice)
                and float(curr.OpenPrice) > float(curr.ClosePrice)
                and float(curr.ClosePrice) > float(prev.OpenPrice)
                and float(curr.OpenPrice) < float(prev.ClosePrice))

            if bullish_harami and k_value < self.Oversold and self.Position <= 0 and self._candles_since_trade >= self.SignalCooldownCandles:
                self.BuyMarket()
                self._candles_since_trade = 0
            elif bearish_harami and k_value > self.Overbought and self.Position >= 0 and self._candles_since_trade >= self.SignalCooldownCandles:
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
        return bullish_bearish_harami_stochastic_strategy()
