import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy

class candlestick_stochastic_strategy(Strategy):
    def __init__(self):
        super(candlestick_stochastic_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30)))
        self._stoch_period = self.Param("StochPeriod", 14)
        self._stoch_low = self.Param("StochLow", 40.0)
        self._stoch_high = self.Param("StochHigh", 60.0)
        self._signal_cooldown_candles = self.Param("SignalCooldownCandles", 4)

        self._prev_candle = None
        self._candles_since_trade = 4

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
    def StochLow(self):
        return self._stoch_low.Value

    @StochLow.setter
    def StochLow(self, value):
        self._stoch_low.Value = value

    @property
    def StochHigh(self):
        return self._stoch_high.Value

    @StochHigh.setter
    def StochHigh(self, value):
        self._stoch_high.Value = value

    @property
    def SignalCooldownCandles(self):
        return self._signal_cooldown_candles.Value

    @SignalCooldownCandles.setter
    def SignalCooldownCandles(self, value):
        self._signal_cooldown_candles.Value = value

    def OnReseted(self):
        super(candlestick_stochastic_strategy, self).OnReseted()
        self._prev_candle = None
        self._candles_since_trade = self.SignalCooldownCandles

    def OnStarted(self, time):
        super(candlestick_stochastic_strategy, self).OnStarted(time)
        self._prev_candle = None
        self._candles_since_trade = self.SignalCooldownCandles

        rsi = RelativeStrengthIndex()
        rsi.Length = self.StochPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(rsi, self._process_candle).Start()

    def _process_candle(self, candle, stoch_value):
        if candle.State != CandleStates.Finished:
            return

        if self._candles_since_trade < self.SignalCooldownCandles:
            self._candles_since_trade += 1

        stoch_val = float(stoch_value)

        if self._prev_candle is not None:
            bullish_engulf = (float(self._prev_candle.OpenPrice) > float(self._prev_candle.ClosePrice) and
                              float(candle.ClosePrice) > float(candle.OpenPrice) and
                              float(candle.ClosePrice) > float(self._prev_candle.OpenPrice) and
                              float(candle.OpenPrice) < float(self._prev_candle.ClosePrice))

            bearish_engulf = (float(self._prev_candle.ClosePrice) > float(self._prev_candle.OpenPrice) and
                              float(candle.OpenPrice) > float(candle.ClosePrice) and
                              float(candle.OpenPrice) > float(self._prev_candle.ClosePrice) and
                              float(candle.ClosePrice) < float(self._prev_candle.OpenPrice))

            if bullish_engulf and stoch_val < self.StochLow and self.Position <= 0 and self._candles_since_trade >= self.SignalCooldownCandles:
                self.BuyMarket()
                self._candles_since_trade = 0
            elif bearish_engulf and stoch_val > self.StochHigh and self.Position >= 0 and self._candles_since_trade >= self.SignalCooldownCandles:
                self.SellMarket()
                self._candles_since_trade = 0

        self._prev_candle = candle

    def CreateClone(self):
        return candlestick_stochastic_strategy()
