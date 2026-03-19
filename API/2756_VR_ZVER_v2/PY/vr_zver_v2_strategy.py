import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class vr_zver_v2_strategy(Strategy):
    """Triple EMA alignment with RSI filter for entries, manual SL/TP/trailing/breakeven management."""
    def __init__(self):
        super(vr_zver_v2_strategy, self).__init__()
        self._fast_period = self.Param("FastMaPeriod", 3).SetGreaterThanZero().SetDisplay("Fast EMA", "Fast EMA length", "Indicators")
        self._slow_period = self.Param("SlowMaPeriod", 5).SetGreaterThanZero().SetDisplay("Slow EMA", "Slow EMA length", "Indicators")
        self._very_slow_period = self.Param("VerySlowMaPeriod", 7).SetGreaterThanZero().SetDisplay("Very Slow EMA", "Very slow EMA length", "Indicators")
        self._rsi_period = self.Param("RsiPeriod", 14).SetGreaterThanZero().SetDisplay("RSI Period", "RSI length", "Indicators")
        self._rsi_upper = self.Param("RsiUpperLevel", 60).SetDisplay("RSI Upper", "Upper threshold for short", "Indicators")
        self._rsi_lower = self.Param("RsiLowerLevel", 40).SetDisplay("RSI Lower", "Lower threshold for long", "Indicators")
        self._sl = self.Param("StopLossPips", 50).SetDisplay("Stop Loss", "SL in pips", "Risk")
        self._tp = self.Param("TakeProfitPips", 70).SetDisplay("Take Profit", "TP in pips", "Risk")
        self._trailing = self.Param("TrailingStopPips", 15).SetDisplay("Trailing Stop", "Trailing distance", "Risk")
        self._trailing_step = self.Param("TrailingStepPips", 5).SetDisplay("Trailing Step", "Step before trailing updates", "Risk")
        self._breakeven = self.Param("BreakevenPips", 20).SetDisplay("Breakeven", "Profit for breakeven", "Risk")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(4).TimeFrame()).SetDisplay("Candle Type", "Timeframe", "General")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(vr_zver_v2_strategy, self).OnReseted()
        self._entry_price = 0
        self._stop_price = None
        self._take_price = None
        self._trail_stop = None
        self._be_activated = False

    def OnStarted(self, time):
        super(vr_zver_v2_strategy, self).OnStarted(time)
        self._entry_price = 0
        self._stop_price = None
        self._take_price = None
        self._trail_stop = None
        self._be_activated = False

        fast_ma = ExponentialMovingAverage()
        fast_ma.Length = self._fast_period.Value
        slow_ma = ExponentialMovingAverage()
        slow_ma.Length = self._slow_period.Value
        very_slow_ma = ExponentialMovingAverage()
        very_slow_ma.Length = self._very_slow_period.Value
        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_period.Value

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(fast_ma, slow_ma, very_slow_ma, rsi, self.OnProcess).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawIndicator(area, fast_ma)
            self.DrawIndicator(area, slow_ma)
            self.DrawIndicator(area, very_slow_ma)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, fast_val, slow_val, very_slow_val, rsi_val):
        if candle.State != CandleStates.Finished:
            return

        fv = float(fast_val)
        sv = float(slow_val)
        vsv = float(very_slow_val)
        rv = float(rsi_val)
        close = float(candle.ClosePrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        pip = 1.0
        sl_dist = self._sl.Value * pip
        tp_dist = self._tp.Value * pip
        be_dist = self._breakeven.Value * pip
        trail_dist = self._trailing.Value * pip
        trail_step = self._trailing_step.Value * pip

        # Manage long position
        if self.Position > 0:
            # Breakeven
            if not self._be_activated and be_dist > 0 and high >= self._entry_price + be_dist:
                self._be_activated = True
                if self._stop_price is None or self._entry_price > self._stop_price:
                    self._stop_price = self._entry_price

            # Trailing
            if trail_dist > 0:
                desired = close - trail_dist
                if self._trail_stop is None:
                    activation = self._entry_price + trail_dist + trail_step
                    if high >= activation:
                        self._trail_stop = desired
                        if self._stop_price is None or desired > self._stop_price:
                            self._stop_price = desired
                elif desired > self._trail_stop + trail_step:
                    self._trail_stop = desired
                    if self._stop_price is None or desired > self._stop_price:
                        self._stop_price = desired

            if self._stop_price is not None and low <= self._stop_price:
                self.SellMarket()
                self._reset_trade()
                return
            if self._take_price is not None and high >= self._take_price:
                self.SellMarket()
                self._reset_trade()
                return

        # Manage short position
        elif self.Position < 0:
            if not self._be_activated and be_dist > 0 and low <= self._entry_price - be_dist:
                self._be_activated = True
                if self._stop_price is None or self._entry_price < self._stop_price:
                    self._stop_price = self._entry_price

            if trail_dist > 0:
                desired = close + trail_dist
                if self._trail_stop is None:
                    activation = self._entry_price - trail_dist - trail_step
                    if low <= activation:
                        self._trail_stop = desired
                        if self._stop_price is None or desired < self._stop_price:
                            self._stop_price = desired
                elif desired < self._trail_stop - trail_step:
                    self._trail_stop = desired
                    if self._stop_price is None or desired < self._stop_price:
                        self._stop_price = desired

            if self._stop_price is not None and high >= self._stop_price:
                self.BuyMarket()
                self._reset_trade()
                return
            if self._take_price is not None and low <= self._take_price:
                self.BuyMarket()
                self._reset_trade()
                return

        # Entry signals
        if self.Position == 0:
            long_signal = fv > sv and sv > vsv and rv < self._rsi_lower.Value
            short_signal = fv < sv and sv < vsv and rv > self._rsi_upper.Value

            if long_signal:
                self.BuyMarket()
                self._entry_price = close
                self._be_activated = False
                self._trail_stop = None
                self._stop_price = close - sl_dist / 1.5 if sl_dist > 0 else None
                self._take_price = close + tp_dist if tp_dist > 0 else None
            elif short_signal:
                self.SellMarket()
                self._entry_price = close
                self._be_activated = False
                self._trail_stop = None
                self._stop_price = close + sl_dist / 1.5 if sl_dist > 0 else None
                self._take_price = close - tp_dist if tp_dist > 0 else None

    def _reset_trade(self):
        self._entry_price = 0
        self._stop_price = None
        self._take_price = None
        self._trail_stop = None
        self._be_activated = False

    def CreateClone(self):
        return vr_zver_v2_strategy()
