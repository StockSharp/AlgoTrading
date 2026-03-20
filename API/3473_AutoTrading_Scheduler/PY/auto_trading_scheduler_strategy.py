import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Momentum
from StockSharp.Algo.Strategies import Strategy

class auto_trading_scheduler_strategy(Strategy):
    def __init__(self):
        super(auto_trading_scheduler_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(60)))
        self._momentum_period = self.Param("MomentumPeriod", 20)
        self._momentum_level = self.Param("MomentumLevel", 101.0)
        self._signal_cooldown_candles = self.Param("SignalCooldownCandles", 4)

        self._prev_mom = 0.0
        self._candles_since_trade = 4
        self._has_prev = False

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def MomentumPeriod(self):
        return self._momentum_period.Value

    @MomentumPeriod.setter
    def MomentumPeriod(self, value):
        self._momentum_period.Value = value

    @property
    def MomentumLevel(self):
        return self._momentum_level.Value

    @MomentumLevel.setter
    def MomentumLevel(self, value):
        self._momentum_level.Value = value

    @property
    def SignalCooldownCandles(self):
        return self._signal_cooldown_candles.Value

    @SignalCooldownCandles.setter
    def SignalCooldownCandles(self, value):
        self._signal_cooldown_candles.Value = value

    def OnReseted(self):
        super(auto_trading_scheduler_strategy, self).OnReseted()
        self._prev_mom = 0.0
        self._candles_since_trade = self.SignalCooldownCandles
        self._has_prev = False

    def OnStarted(self, time):
        super(auto_trading_scheduler_strategy, self).OnStarted(time)
        self._prev_mom = 0.0
        self._candles_since_trade = self.SignalCooldownCandles
        self._has_prev = False

        momentum = Momentum()
        momentum.Length = self.MomentumPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(momentum, self._process_candle).Start()

    def _process_candle(self, candle, mom_value):
        if candle.State != CandleStates.Finished:
            return

        if self._candles_since_trade < self.SignalCooldownCandles:
            self._candles_since_trade += 1

        mom_val = float(mom_value)

        if self._has_prev:
            if self._prev_mom < self.MomentumLevel and mom_val >= self.MomentumLevel and self.Position <= 0 and self._candles_since_trade >= self.SignalCooldownCandles:
                self.BuyMarket()
                self._candles_since_trade = 0
            elif self._prev_mom > (200.0 - self.MomentumLevel) and mom_val <= (200.0 - self.MomentumLevel) and self.Position >= 0 and self._candles_since_trade >= self.SignalCooldownCandles:
                self.SellMarket()
                self._candles_since_trade = 0

        self._prev_mom = mom_val
        self._has_prev = True

    def CreateClone(self):
        return auto_trading_scheduler_strategy()
