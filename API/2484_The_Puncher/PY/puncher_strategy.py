import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import RelativeStrengthIndex


class puncher_strategy(Strategy):
    def __init__(self):
        super(puncher_strategy, self).__init__()

        self._stochastic_period = self.Param("StochasticPeriod", 100) \
            .SetGreaterThanZero() \
            .SetDisplay("Stochastic Period", "Base period for the Stochastic oscillator", "Indicators")

        self._stochastic_signal_period = self.Param("StochasticSignalPeriod", 3) \
            .SetGreaterThanZero() \
            .SetDisplay("Stochastic Signal", "Smoothing period for the K line", "Indicators")

        self._stochastic_smoothing_period = self.Param("StochasticSmoothingPeriod", 3) \
            .SetGreaterThanZero() \
            .SetDisplay("Stochastic Smoothing", "Smoothing period for the D line", "Indicators")

        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("RSI Period", "RSI calculation length", "Indicators")

        self._oversold_level = self.Param("OversoldLevel", 20.0) \
            .SetDisplay("Oversold Level", "Threshold for oversold detection", "Signals")

        self._overbought_level = self.Param("OverboughtLevel", 80.0) \
            .SetDisplay("Overbought Level", "Threshold for overbought detection", "Signals")

        self._stop_loss_pips = self.Param("StopLossPips", 20) \
            .SetNotNegative() \
            .SetDisplay("Stop Loss (pips)", "Distance of the protective stop-loss", "Risk")

        self._take_profit_pips = self.Param("TakeProfitPips", 50) \
            .SetNotNegative() \
            .SetDisplay("Take Profit (pips)", "Distance of the profit target", "Risk")

        self._trailing_stop_pips = self.Param("TrailingStopPips", 10) \
            .SetNotNegative() \
            .SetDisplay("Trailing Stop (pips)", "Trailing stop distance", "Risk")

        self._trailing_step_pips = self.Param("TrailingStepPips", 5) \
            .SetNotNegative() \
            .SetDisplay("Trailing Step (pips)", "Minimum improvement before trailing stop updates", "Risk")

        self._break_even_pips = self.Param("BreakEvenPips", 21) \
            .SetNotNegative() \
            .SetDisplay("Break-Even (pips)", "Profit needed to move the stop to entry", "Risk")

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Type of candles for processing", "General")

        self._entry_price = 0.0
        self._stop_price = None
        self._take_profit_price = None
        self._break_even_activated = False
        self._last_trailing_price = None

    @property
    def StochasticPeriod(self):
        return self._stochastic_period.Value

    @property
    def StochasticSignalPeriod(self):
        return self._stochastic_signal_period.Value

    @property
    def StochasticSmoothingPeriod(self):
        return self._stochastic_smoothing_period.Value

    @property
    def RsiPeriod(self):
        return self._rsi_period.Value

    @property
    def OversoldLevel(self):
        return self._oversold_level.Value

    @property
    def OverboughtLevel(self):
        return self._overbought_level.Value

    @property
    def StopLossPips(self):
        return self._stop_loss_pips.Value

    @property
    def TakeProfitPips(self):
        return self._take_profit_pips.Value

    @property
    def TrailingStopPips(self):
        return self._trailing_stop_pips.Value

    @property
    def TrailingStepPips(self):
        return self._trailing_step_pips.Value

    @property
    def BreakEvenPips(self):
        return self._break_even_pips.Value

    @property
    def CandleType(self):
        return self._candle_type.Value

    def _get_price_step(self):
        sec = self.Security
        if sec is not None and sec.PriceStep is not None and float(sec.PriceStep) > 0:
            return float(sec.PriceStep)
        return 1.0

    def _get_pip_value(self, pips, price_step):
        return price_step * pips

    def OnStarted(self, time):
        super(puncher_strategy, self).OnStarted(time)

        rsi = RelativeStrengthIndex()
        rsi.Length = self.RsiPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription \
            .Bind(rsi, self.process_candle) \
            .Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, rsi)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, rsi_val):
        if candle.State != CandleStates.Finished:
            return

        rsi = float(rsi_val)

        if self._manage_position(candle):
            return

        is_buy = rsi < float(self.OversoldLevel)
        is_sell = rsi > float(self.OverboughtLevel)

        if self.Position > 0 and is_sell:
            self._close_long()
            return

        if self.Position < 0 and is_buy:
            self._close_short()
            return

        if is_buy and self.Position <= 0:
            self._enter_long(candle)
            return

        if is_sell and self.Position >= 0:
            self._enter_short(candle)

    def _manage_position(self, candle):
        if self.Position > 0:
            return self._handle_long(candle)
        if self.Position < 0:
            return self._handle_short(candle)
        if self._stop_price is not None or self._take_profit_price is not None or self._entry_price != 0.0:
            self._reset_protection()
        return False

    def _handle_long(self, candle):
        if self._entry_price == 0.0:
            self._entry_price = float(candle.ClosePrice)

        ps = self._get_price_step()

        if self.BreakEvenPips > 0 and not self._break_even_activated:
            be = self._entry_price + self._get_pip_value(self.BreakEvenPips, ps)
            if float(candle.HighPrice) >= be:
                if self._stop_price is None or self._stop_price < self._entry_price:
                    self._stop_price = self._entry_price
                    self._break_even_activated = True

        if self.TrailingStopPips > 0:
            td = self._get_pip_value(self.TrailingStopPips, ps)
            ts = self._get_pip_value(self.TrailingStepPips, ps) if self.TrailingStepPips > 0 else 0.0
            if self._last_trailing_price is None:
                self._last_trailing_price = self._entry_price

            high = float(candle.HighPrice)
            if high >= self._entry_price + td:
                ref = self._last_trailing_price
                do_update = (ref == self._entry_price) or (ts == 0.0) or (high - ref >= ts)
                if do_update:
                    ns = high - td
                    if self._stop_price is None or ns > self._stop_price:
                        self._stop_price = ns
                    self._last_trailing_price = high

        if self._take_profit_price is not None and float(candle.HighPrice) >= self._take_profit_price:
            self._close_long()
            return True

        if self._stop_price is not None and float(candle.LowPrice) <= self._stop_price:
            self._close_long()
            return True

        return False

    def _handle_short(self, candle):
        if self._entry_price == 0.0:
            self._entry_price = float(candle.ClosePrice)

        ps = self._get_price_step()

        if self.BreakEvenPips > 0 and not self._break_even_activated:
            be = self._entry_price - self._get_pip_value(self.BreakEvenPips, ps)
            if float(candle.LowPrice) <= be:
                if self._stop_price is None or self._stop_price > self._entry_price:
                    self._stop_price = self._entry_price
                    self._break_even_activated = True

        if self.TrailingStopPips > 0:
            td = self._get_pip_value(self.TrailingStopPips, ps)
            ts = self._get_pip_value(self.TrailingStepPips, ps) if self.TrailingStepPips > 0 else 0.0
            if self._last_trailing_price is None:
                self._last_trailing_price = self._entry_price

            low = float(candle.LowPrice)
            if low <= self._entry_price - td:
                ref = self._last_trailing_price
                do_update = (ref == self._entry_price) or (ts == 0.0) or (ref - low >= ts)
                if do_update:
                    ns = low + td
                    if self._stop_price is None or ns < self._stop_price:
                        self._stop_price = ns
                    self._last_trailing_price = low

        if self._take_profit_price is not None and float(candle.LowPrice) <= self._take_profit_price:
            self._close_short()
            return True

        if self._stop_price is not None and float(candle.HighPrice) >= self._stop_price:
            self._close_short()
            return True

        return False

    def _enter_long(self, candle):
        vol = self.Volume + (abs(self.Position) if self.Position < 0 else 0)
        if vol <= 0:
            return
        self.BuyMarket(vol)
        self._entry_price = float(candle.ClosePrice)
        self._init_protection(True)

    def _enter_short(self, candle):
        vol = self.Volume + (self.Position if self.Position > 0 else 0)
        if vol <= 0:
            return
        self.SellMarket(vol)
        self._entry_price = float(candle.ClosePrice)
        self._init_protection(False)

    def _close_long(self):
        if self.Position > 0:
            self.SellMarket(self.Position)
        self._reset_protection()

    def _close_short(self):
        if self.Position < 0:
            self.BuyMarket(abs(self.Position))
        self._reset_protection()

    def _init_protection(self, is_long):
        ps = self._get_price_step()
        so = self._get_pip_value(self.StopLossPips, ps) if self.StopLossPips > 0 else None
        to = self._get_pip_value(self.TakeProfitPips, ps) if self.TakeProfitPips > 0 else None

        if is_long:
            self._stop_price = (self._entry_price - so) if so is not None else None
            self._take_profit_price = (self._entry_price + to) if to is not None else None
        else:
            self._stop_price = (self._entry_price + so) if so is not None else None
            self._take_profit_price = (self._entry_price - to) if to is not None else None

        self._break_even_activated = False
        self._last_trailing_price = self._entry_price

    def _reset_protection(self):
        self._entry_price = 0.0
        self._stop_price = None
        self._take_profit_price = None
        self._break_even_activated = False
        self._last_trailing_price = None

    def OnReseted(self):
        super(puncher_strategy, self).OnReseted()
        self._reset_protection()

    def CreateClone(self):
        return puncher_strategy()
