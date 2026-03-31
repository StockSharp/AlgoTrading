import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ParabolicSar, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class sar_rsi_mts_strategy(Strategy):
    def __init__(self):
        super(sar_rsi_mts_strategy, self).__init__()
        self._sl_pips = self.Param("StopLossPips", 10.0).SetNotNegative().SetDisplay("Stop Loss (pips)", "SL distance", "Risk")
        self._tp_pips = self.Param("TakeProfitPips", 40.0).SetNotNegative().SetDisplay("Take Profit (pips)", "TP distance", "Risk")
        self._trailing_pips = self.Param("TrailingStopPips", 15.0).SetNotNegative().SetDisplay("Trailing Stop (pips)", "Trailing stop distance", "Risk")
        self._trailing_step_pips = self.Param("TrailingStepPips", 5.0).SetNotNegative().SetDisplay("Trailing Step (pips)", "Trailing step distance", "Risk")
        self._sar_step = self.Param("SarStep", 0.05).SetGreaterThanZero().SetDisplay("SAR Step", "Parabolic SAR acceleration step", "Indicators")
        self._sar_max = self.Param("SarMax", 0.5).SetGreaterThanZero().SetDisplay("SAR Maximum", "Parabolic SAR maximum acceleration", "Indicators")
        self._rsi_period = self.Param("RsiPeriod", 14).SetGreaterThanZero().SetDisplay("RSI Period", "Lookback period for RSI", "Indicators")
        self._rsi_neutral = self.Param("RsiNeutralLevel", 50.0).SetDisplay("RSI Neutral", "Neutral RSI threshold", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Candle type", "General")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(sar_rsi_mts_strategy, self).OnReseted()
        self._prev_sar = None
        self._prev_rsi = None
        self._entry_price = 0
        self._long_trailing = None
        self._short_trailing = None

    def OnStarted2(self, time):
        super(sar_rsi_mts_strategy, self).OnStarted2(time)
        self._prev_sar = None
        self._prev_rsi = None
        self._entry_price = 0
        self._long_trailing = None
        self._short_trailing = None
        self._pip_size = 1.0
        if self.Security is not None and self.Security.PriceStep is not None and self.Security.PriceStep > 0:
            self._pip_size = float(self.Security.PriceStep)

        sar = ParabolicSar()
        sar.Acceleration = self._sar_step.Value
        sar.AccelerationStep = self._sar_step.Value
        sar.AccelerationMax = self._sar_max.Value

        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_period.Value

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(sar, rsi, self.OnProcess).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawIndicator(area, sar)
            self.DrawIndicator(area, rsi)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, sar_val, rsi_val):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)

        # Manage risk first
        if self._manage_risk(candle, close):
            self._prev_sar = sar_val
            self._prev_rsi = rsi_val
            return

        if self._prev_sar is None or self._prev_rsi is None:
            self._prev_sar = sar_val
            self._prev_rsi = rsi_val
            return

        prev_sar = self._prev_sar
        prev_rsi = self._prev_rsi

        # Buy signal: SAR below price, SAR rising, RSI above neutral and rising
        buy_signal = (prev_sar < close
            and sar_val > prev_sar
            and rsi_val > self._rsi_neutral.Value
            and rsi_val > prev_rsi)

        if buy_signal:
            if self.Position < 0:
                self.BuyMarket()
            if self.Position <= 0:
                self.BuyMarket()
                self._entry_price = close
                self._long_trailing = None
                self._short_trailing = None
        else:
            # Sell signal: SAR above price, SAR falling, RSI below neutral and falling
            sell_signal = (prev_sar > close
                and sar_val < prev_sar
                and rsi_val < self._rsi_neutral.Value
                and rsi_val < prev_rsi)

            if sell_signal:
                if self.Position > 0:
                    self.SellMarket()
                if self.Position >= 0:
                    self.SellMarket()
                    self._entry_price = close
                    self._long_trailing = None
                    self._short_trailing = None

        self._prev_sar = sar_val
        self._prev_rsi = rsi_val

    def _manage_risk(self, candle, close):
        pip = self._pip_size

        if self.Position > 0 and self._entry_price > 0:
            # Trailing
            trail_dist = self._trailing_pips.Value * pip
            trail_step = self._trailing_step_pips.Value * pip
            if trail_dist > 0:
                profit = close - self._entry_price
                if profit >= trail_dist + trail_step:
                    candidate = close - trail_dist
                    threshold = close - (trail_dist + trail_step)
                    if self._long_trailing is None or self._long_trailing < threshold:
                        self._long_trailing = candidate
                if self._long_trailing is not None and float(candle.LowPrice) <= self._long_trailing:
                    self.SellMarket()
                    self._reset_trailing()
                    return True

            # Stop loss
            sl_dist = self._sl_pips.Value * pip
            if sl_dist > 0:
                if float(candle.LowPrice) <= self._entry_price - sl_dist:
                    self.SellMarket()
                    self._reset_trailing()
                    return True

            # Take profit
            tp_dist = self._tp_pips.Value * pip
            if tp_dist > 0:
                if float(candle.HighPrice) >= self._entry_price + tp_dist:
                    self.SellMarket()
                    self._reset_trailing()
                    return True

        elif self.Position < 0 and self._entry_price > 0:
            # Trailing
            trail_dist = self._trailing_pips.Value * pip
            trail_step = self._trailing_step_pips.Value * pip
            if trail_dist > 0:
                profit = self._entry_price - close
                if profit >= trail_dist + trail_step:
                    candidate = close + trail_dist
                    threshold = close + (trail_dist + trail_step)
                    if self._short_trailing is None or self._short_trailing > threshold:
                        self._short_trailing = candidate
                if self._short_trailing is not None and float(candle.HighPrice) >= self._short_trailing:
                    self.BuyMarket()
                    self._reset_trailing()
                    return True

            # Stop loss
            sl_dist = self._sl_pips.Value * pip
            if sl_dist > 0:
                if float(candle.HighPrice) >= self._entry_price + sl_dist:
                    self.BuyMarket()
                    self._reset_trailing()
                    return True

            # Take profit
            tp_dist = self._tp_pips.Value * pip
            if tp_dist > 0:
                if float(candle.LowPrice) <= self._entry_price - tp_dist:
                    self.BuyMarket()
                    self._reset_trailing()
                    return True
        else:
            self._reset_trailing()

        return False

    def _reset_trailing(self):
        self._long_trailing = None
        self._short_trailing = None
        self._entry_price = 0

    def CreateClone(self):
        return sar_rsi_mts_strategy()
