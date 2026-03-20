import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, Momentum
from StockSharp.Algo.Strategies import Strategy


class fx_chaos_scalp_mt4_strategy(Strategy):
    def __init__(self):
        super(fx_chaos_scalp_mt4_strategy, self).__init__()
        self._ema_period = self.Param("EmaPeriod", 14).SetDisplay("EMA Period", "EMA filter", "Indicators")
        self._momentum_period = self.Param("MomentumPeriod", 10).SetDisplay("Momentum", "Momentum period", "Indicators")
        self._cooldown_candles = self.Param("CooldownCandles", 200).SetDisplay("Cooldown", "Candles between signals", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Candle timeframe", "General")
        self._prev_mom = 0.0
        self._has_prev = False
        self._cooldown_remaining = 0

    @property
    def ema_period(self): return self._ema_period.Value
    @property
    def momentum_period(self): return self._momentum_period.Value
    @property
    def cooldown_candles(self): return self._cooldown_candles.Value
    @property
    def candle_type(self): return self._candle_type.Value

    def OnReseted(self):
        super(fx_chaos_scalp_mt4_strategy, self).OnReseted()
        self._prev_mom = 0.0
        self._has_prev = False
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(fx_chaos_scalp_mt4_strategy, self).OnStarted(time)
        self._prev_mom = 0.0
        self._has_prev = False
        self._cooldown_remaining = 0
        ema = ExponentialMovingAverage()
        ema.Length = self.ema_period
        mom = Momentum()
        mom.Length = self.momentum_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ema, mom, self.process_candle).Start()

    def process_candle(self, candle, ema, mom):
        if candle.State != CandleStates.Finished:
            return
        close = float(candle.ClosePrice)
        ema_val = float(ema)
        mom_val = float(mom)
        if not self._has_prev:
            self._prev_mom = mom_val
            self._has_prev = True
            return
        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            self._prev_mom = mom_val
            return
        if close > ema_val and self._prev_mom <= 0 and mom_val > 0 and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._cooldown_remaining = self.cooldown_candles
        elif close < ema_val and self._prev_mom >= 0 and mom_val < 0 and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._cooldown_remaining = self.cooldown_candles
        self._prev_mom = mom_val

    def CreateClone(self):
        return fx_chaos_scalp_mt4_strategy()
