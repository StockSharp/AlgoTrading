import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import StochasticOscillator, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class the_puncher_strategy(Strategy):
    """Stochastic + RSI extreme oversold/overbought with SL/TP/trailing/break-even."""
    def __init__(self):
        super(the_puncher_strategy, self).__init__()
        self._stoch_length = self.Param("StochasticLength", 100).SetGreaterThanZero().SetDisplay("Stochastic Length", "Lookback for %K", "Indicators")
        self._stoch_d = self.Param("StochasticSmoothingPeriod", 3).SetGreaterThanZero().SetDisplay("Stochastic %D", "Smoothing for %D", "Indicators")
        self._rsi_period = self.Param("RsiPeriod", 14).SetGreaterThanZero().SetDisplay("RSI Period", "RSI period", "Indicators")
        self._oversold = self.Param("OversoldLevel", 30.0).SetDisplay("Oversold Level", "Shared oversold threshold", "Indicators")
        self._overbought = self.Param("OverboughtLevel", 70.0).SetDisplay("Overbought Level", "Shared overbought threshold", "Indicators")
        self._sl_pips = self.Param("StopLossPips", 2000).SetDisplay("Stop-Loss (pips)", "Protective stop distance", "Risk")
        self._tp_pips = self.Param("TakeProfitPips", 0).SetDisplay("Take-Profit (pips)", "Profit target distance", "Risk")
        self._trail_pips = self.Param("TrailingStopPips", 0).SetDisplay("Trailing Stop (pips)", "Trailing stop distance", "Risk")
        self._trail_step = self.Param("TrailingStepPips", 1).SetNotNegative().SetDisplay("Trailing Step (pips)", "Min move before trailing", "Risk")
        self._be_pips = self.Param("BreakEvenPips", 0).SetNotNegative().SetDisplay("Break-Even (pips)", "Profit to move stop to entry", "Risk")
        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(15).TimeFrame()).SetDisplay("Candle Type", "Primary timeframe", "General")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(the_puncher_strategy, self).OnReseted()
        self._entry_price = 0
        self._stop_price = 0
        self._tp_price = 0
        self._be_activated = False

    def OnStarted(self, time):
        super(the_puncher_strategy, self).OnStarted(time)
        self._entry_price = 0
        self._stop_price = 0
        self._tp_price = 0
        self._be_activated = False

        self._stoch = StochasticOscillator()
        self._stoch.K.Length = self._stoch_length.Value
        self._stoch.D.Length = self._stoch_d.Value
        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_period.Value

        sub = self.SubscribeCandles(self.CandleType)
        sub.BindEx(self._stoch, rsi, self.OnProcess).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawIndicator(area, self._stoch)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, stoch_val, rsi_val):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Extract %D from stochastic
        stoch_signal = None
        inner = stoch_val.InnerValues
        if inner is not None:
            for iv in inner:
                stoch_signal = float(iv.Value.ToDecimal())
                break
        if stoch_signal is None:
            return

        rsi = float(rsi_val.ToDecimal())
        close = float(candle.ClosePrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        oversold = self._oversold.Value
        overbought = self._overbought.Value

        buy_signal = stoch_signal < oversold and rsi < oversold
        sell_signal = stoch_signal > overbought and rsi > overbought

        # Manage position
        if self.Position > 0:
            if self._stop_price > 0 and low <= self._stop_price:
                self.SellMarket()
                self._reset_state()
                return
            if self._tp_price > 0 and high >= self._tp_price:
                self.SellMarket()
                self._reset_state()
                return
            self._apply_long_risk(close)
            if sell_signal:
                self.SellMarket()
                self._reset_state()
                return
        elif self.Position < 0:
            if self._stop_price > 0 and high >= self._stop_price:
                self.BuyMarket()
                self._reset_state()
                return
            if self._tp_price > 0 and low <= self._tp_price:
                self.BuyMarket()
                self._reset_state()
                return
            self._apply_short_risk(close)
            if buy_signal:
                self.BuyMarket()
                self._reset_state()
                return

        # Entry
        if self.Position == 0:
            if buy_signal:
                self.BuyMarket()
                self._entry_price = close
                self._init_protection(True)
            elif sell_signal:
                self.SellMarket()
                self._entry_price = close
                self._init_protection(False)

    def _init_protection(self, is_long):
        self._stop_price = 0
        self._tp_price = 0
        self._be_activated = False
        sl = self._sl_pips.Value
        tp = self._tp_pips.Value
        if is_long:
            if sl > 0:
                self._stop_price = self._entry_price - sl
            if tp > 0:
                self._tp_price = self._entry_price + tp
        else:
            if sl > 0:
                self._stop_price = self._entry_price + sl
            if tp > 0:
                self._tp_price = self._entry_price - tp

    def _apply_long_risk(self, close):
        be = self._be_pips.Value
        if be > 0 and not self._be_activated and self._entry_price > 0:
            if close - self._entry_price >= be:
                if self._stop_price == 0 or self._stop_price < self._entry_price:
                    self._stop_price = self._entry_price
                self._be_activated = True
        trail = self._trail_pips.Value
        step = self._trail_step.Value
        if trail > 0 and close - self._entry_price > trail:
            new_stop = close - trail
            if self._stop_price == 0 or new_stop > self._stop_price:
                self._stop_price = new_stop

    def _apply_short_risk(self, close):
        be = self._be_pips.Value
        if be > 0 and not self._be_activated and self._entry_price > 0:
            if self._entry_price - close >= be:
                if self._stop_price == 0 or self._stop_price > self._entry_price:
                    self._stop_price = self._entry_price
                self._be_activated = True
        trail = self._trail_pips.Value
        step = self._trail_step.Value
        if trail > 0 and self._entry_price - close > trail:
            new_stop = close + trail
            if self._stop_price == 0 or new_stop < self._stop_price:
                self._stop_price = new_stop

    def _reset_state(self):
        self._entry_price = 0
        self._stop_price = 0
        self._tp_price = 0
        self._be_activated = False

    def CreateClone(self):
        return the_puncher_strategy()
