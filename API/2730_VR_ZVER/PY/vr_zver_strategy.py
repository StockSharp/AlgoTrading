import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan

from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import (
    ExponentialMovingAverage, StochasticOscillator, RelativeStrengthIndex
)
from StockSharp.Algo.Strategies import Strategy


class vr_zver_strategy(Strategy):
    def __init__(self):
        super(vr_zver_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4)))
        self._use_moving_average = self.Param("UseMovingAverage", True)
        self._fast_ma_period = self.Param("FastMaPeriod", 3)
        self._slow_ma_period = self.Param("SlowMaPeriod", 5)
        self._very_slow_ma_period = self.Param("VerySlowMaPeriod", 7)
        self._use_stochastic = self.Param("UseStochastic", True)
        self._stochastic_k_period = self.Param("StochasticKPeriod", 42)
        self._stochastic_d_period = self.Param("StochasticDPeriod", 5)
        self._stochastic_slowing = self.Param("StochasticSlowing", 7)
        self._stochastic_upper_level = self.Param("StochasticUpperLevel", 55)
        self._stochastic_lower_level = self.Param("StochasticLowerLevel", 50)
        self._use_rsi = self.Param("UseRsi", True)
        self._rsi_period = self.Param("RsiPeriod", 14)
        self._rsi_upper_level = self.Param("RsiUpperLevel", 55)
        self._rsi_lower_level = self.Param("RsiLowerLevel", 50)
        self._stop_loss_pips = self.Param("StopLossPips", 50)
        self._take_profit_pips = self.Param("TakeProfitPips", 70)
        self._breakeven_pips = self.Param("BreakevenPips", 20)

        self._fast_ma = None
        self._slow_ma = None
        self._very_slow_ma = None
        self._stochastic = None
        self._rsi = None
        self._pip_size = 0.0
        self._long_entry_price = None
        self._short_entry_price = None
        self._long_stop_price = None
        self._short_stop_price = None
        self._long_take_price = None
        self._short_take_price = None
        self._long_breakeven_trigger = None
        self._short_breakeven_trigger = None
        self._long_breakeven_armed = False
        self._short_breakeven_armed = False

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def UseMovingAverage(self):
        return self._use_moving_average.Value

    @property
    def FastMaPeriod(self):
        return self._fast_ma_period.Value

    @property
    def SlowMaPeriod(self):
        return self._slow_ma_period.Value

    @property
    def VerySlowMaPeriod(self):
        return self._very_slow_ma_period.Value

    @property
    def UseStochastic(self):
        return self._use_stochastic.Value

    @property
    def StochasticKPeriod(self):
        return self._stochastic_k_period.Value

    @property
    def StochasticDPeriod(self):
        return self._stochastic_d_period.Value

    @property
    def StochasticUpperLevel(self):
        return self._stochastic_upper_level.Value

    @property
    def StochasticLowerLevel(self):
        return self._stochastic_lower_level.Value

    @property
    def UseRsi(self):
        return self._use_rsi.Value

    @property
    def RsiPeriod(self):
        return self._rsi_period.Value

    @property
    def RsiUpperLevel(self):
        return self._rsi_upper_level.Value

    @property
    def RsiLowerLevel(self):
        return self._rsi_lower_level.Value

    @property
    def StopLossPips(self):
        return self._stop_loss_pips.Value

    @property
    def TakeProfitPips(self):
        return self._take_profit_pips.Value

    @property
    def BreakevenPips(self):
        return self._breakeven_pips.Value

    def OnStarted(self, time):
        super(vr_zver_strategy, self).OnStarted(time)

        self._fast_ma = ExponentialMovingAverage()
        self._fast_ma.Length = self.FastMaPeriod
        self._slow_ma = ExponentialMovingAverage()
        self._slow_ma.Length = self.SlowMaPeriod
        self._very_slow_ma = ExponentialMovingAverage()
        self._very_slow_ma.Length = self.VerySlowMaPeriod

        self._stochastic = StochasticOscillator()
        self._stochastic.K.Length = self.StochasticKPeriod
        self._stochastic.D.Length = self.StochasticDPeriod

        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = self.RsiPeriod

        self._pip_size = self._calculate_pip_size()

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(self._fast_ma, self._slow_ma, self._very_slow_ma, self._stochastic, self._rsi, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._fast_ma)
            self.DrawIndicator(area, self._slow_ma)
            self.DrawIndicator(area, self._very_slow_ma)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, fast_v, slow_v, vs_v, stoch_v, rsi_v):
        if candle.State != CandleStates.Finished:
            return
        if not fast_v.IsFinal or not slow_v.IsFinal or not vs_v.IsFinal or not stoch_v.IsFinal or not rsi_v.IsFinal:
            return

        fast = float(fast_v.GetValue[float]())
        slow = float(slow_v.GetValue[float]())
        very_slow = float(vs_v.GetValue[float]())

        stoch_k = stoch_v.K
        stoch_d = stoch_v.D
        if stoch_k is None or stoch_d is None:
            return
        stoch_k = float(stoch_k)
        stoch_d = float(stoch_d)
        rsi = float(rsi_v.GetValue[float]())

        long_closed = self._handle_long(candle)
        if not long_closed and self.Position > 0:
            return

        short_closed = self._handle_short(candle)
        if not short_closed and self.Position < 0:
            return

        if self.Position == 0:
            self._reset_state()

        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        if self.Position != 0:
            return
        if not self.UseMovingAverage and not self.UseStochastic and not self.UseRsi:
            return

        long_signal = (
            (not self.UseMovingAverage or (fast > slow and slow > very_slow))
            and (not self.UseStochastic or (stoch_d < stoch_k and stoch_k < self.StochasticLowerLevel))
            and (not self.UseRsi or rsi < self.RsiLowerLevel)
        )

        short_signal = (
            (not self.UseMovingAverage or (very_slow > slow and slow > fast))
            and (not self.UseStochastic or (stoch_d > stoch_k and stoch_k > self.StochasticUpperLevel))
            and (not self.UseRsi or rsi > self.RsiUpperLevel)
        )

        if long_signal and self.Volume > 0:
            self._enter_long(float(candle.ClosePrice))
        elif short_signal and self.Volume > 0:
            self._enter_short(float(candle.ClosePrice))

    def _enter_long(self, price):
        self.BuyMarket()
        self._long_entry_price = price
        self._long_stop_price = price - self.StopLossPips * self._pip_size if self.StopLossPips > 0 else None
        self._long_take_price = price + self.TakeProfitPips * self._pip_size if self.TakeProfitPips > 0 else None
        self._long_breakeven_trigger = price + self.BreakevenPips * self._pip_size if self.BreakevenPips > 0 else None
        self._long_breakeven_armed = False

    def _enter_short(self, price):
        self.SellMarket()
        self._short_entry_price = price
        self._short_stop_price = price + self.StopLossPips * self._pip_size if self.StopLossPips > 0 else None
        self._short_take_price = price - self.TakeProfitPips * self._pip_size if self.TakeProfitPips > 0 else None
        self._short_breakeven_trigger = price - self.BreakevenPips * self._pip_size if self.BreakevenPips > 0 else None
        self._short_breakeven_armed = False

    def _handle_long(self, candle):
        if self.Position <= 0 or self._long_entry_price is None:
            return False
        if self._long_stop_price is not None and float(candle.LowPrice) <= self._long_stop_price:
            self.SellMarket(); self._reset_state(); return True
        if self._long_take_price is not None and float(candle.HighPrice) >= self._long_take_price:
            self.SellMarket(); self._reset_state(); return True
        if not self._long_breakeven_armed and self._long_breakeven_trigger is not None and float(candle.HighPrice) >= self._long_breakeven_trigger:
            self._long_breakeven_armed = True
        if self._long_breakeven_armed and float(candle.LowPrice) <= self._long_entry_price:
            self.SellMarket(); self._reset_state(); return True
        return False

    def _handle_short(self, candle):
        if self.Position >= 0 or self._short_entry_price is None:
            return False
        if self._short_stop_price is not None and float(candle.HighPrice) >= self._short_stop_price:
            self.BuyMarket(); self._reset_state(); return True
        if self._short_take_price is not None and float(candle.LowPrice) <= self._short_take_price:
            self.BuyMarket(); self._reset_state(); return True
        if not self._short_breakeven_armed and self._short_breakeven_trigger is not None and float(candle.LowPrice) <= self._short_breakeven_trigger:
            self._short_breakeven_armed = True
        if self._short_breakeven_armed and float(candle.HighPrice) >= self._short_entry_price:
            self.BuyMarket(); self._reset_state(); return True
        return False

    def _reset_state(self):
        self._long_entry_price = None
        self._short_entry_price = None
        self._long_stop_price = None
        self._short_stop_price = None
        self._long_take_price = None
        self._short_take_price = None
        self._long_breakeven_trigger = None
        self._short_breakeven_trigger = None
        self._long_breakeven_armed = False
        self._short_breakeven_armed = False

    def _calculate_pip_size(self):
        sec = self.Security
        step = float(sec.PriceStep) if sec is not None and sec.PriceStep is not None else 0.0001
        if step <= 0:
            step = 0.0001
        decimals = sec.Decimals if sec is not None and sec.Decimals is not None else 0
        return step * 10.0 if (decimals == 3 or decimals == 5) else step

    def OnReseted(self):
        super(vr_zver_strategy, self).OnReseted()
        self._pip_size = 0.0
        self._reset_state()

    def CreateClone(self):
        return vr_zver_strategy()
