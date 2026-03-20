import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import WilliamsR
from StockSharp.Algo.Strategies import Strategy

class ifs_fractals_strategy(Strategy):
    def __init__(self):
        super(ifs_fractals_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(60)))
        self._wpr_period = self.Param("WprPeriod", 14)
        self._oversold = self.Param("Oversold", -85.0)
        self._overbought = self.Param("Overbought", -15.0)
        self._signal_cooldown_candles = self.Param("SignalCooldownCandles", 4)

        self._prev_wpr = 0.0
        self._candles_since_trade = 4
        self._has_prev = False

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def WprPeriod(self):
        return self._wpr_period.Value

    @WprPeriod.setter
    def WprPeriod(self, value):
        self._wpr_period.Value = value

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
        super(ifs_fractals_strategy, self).OnReseted()
        self._prev_wpr = 0.0
        self._candles_since_trade = self.SignalCooldownCandles
        self._has_prev = False

    def OnStarted(self, time):
        super(ifs_fractals_strategy, self).OnStarted(time)
        self._prev_wpr = 0.0
        self._candles_since_trade = self.SignalCooldownCandles
        self._has_prev = False

        wpr = WilliamsR()
        wpr.Length = self.WprPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(wpr, self._process_candle).Start()

    def _process_candle(self, candle, wpr_value):
        if candle.State != CandleStates.Finished:
            return

        if self._candles_since_trade < self.SignalCooldownCandles:
            self._candles_since_trade += 1

        wpr_val = float(wpr_value)

        if self._has_prev:
            if self._prev_wpr < self.Oversold and wpr_val >= self.Oversold and self.Position <= 0 and self._candles_since_trade >= self.SignalCooldownCandles:
                self.BuyMarket()
                self._candles_since_trade = 0
            elif self._prev_wpr > self.Overbought and wpr_val <= self.Overbought and self.Position >= 0 and self._candles_since_trade >= self.SignalCooldownCandles:
                self.SellMarket()
                self._candles_since_trade = 0

        self._prev_wpr = wpr_val
        self._has_prev = True

    def CreateClone(self):
        return ifs_fractals_strategy()
