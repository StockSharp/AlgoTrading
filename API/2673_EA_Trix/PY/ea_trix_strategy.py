import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import ExponentialMovingAverage, DecimalIndicatorValue


class ea_trix_strategy(Strategy):
    """TRIX cross strategy: signal line crosses above/below TRIX with SL/TP, break-even and trailing."""

    # Signal directions
    _BUY = 1
    _SELL = -1

    def __init__(self):
        super(ea_trix_strategy, self).__init__()

        self._stop_loss = self.Param("StopLoss", 50.0) \
            .SetDisplay("Stop Loss", "Stop loss distance", "Risk")
        self._take_profit = self.Param("TakeProfit", 150.0) \
            .SetDisplay("Take Profit", "Take profit distance", "Risk")
        self._trailing_stop = self.Param("TrailingStop", 10.0) \
            .SetDisplay("Trailing Stop", "Trailing stop distance", "Risk")
        self._trailing_step = self.Param("TrailingStep", 1.0) \
            .SetDisplay("Trailing Step", "Minimal trailing step", "Risk")
        self._break_even = self.Param("BreakEven", 2.0) \
            .SetDisplay("Break Even", "Break-even trigger distance", "Risk")
        self._trade_on_close_bar = self.Param("TradeOnCloseBar", True) \
            .SetDisplay("Trade On Close", "Confirm signals on closed bars", "General")
        self._ema_period = self.Param("EmaPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("TRIX EMA", "TRIX EMA length", "Indicators")
        self._signal_period = self.Param("SignalPeriod", 8) \
            .SetGreaterThanZero() \
            .SetDisplay("Signal EMA", "Signal EMA length", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe for calculations", "General")

        self._prev_third_trix = None
        self._prev_third_signal = None
        self._prev_trix = None
        self._prev_signal = None
        self._pending_signal = None
        self._entry_price = None
        self._stop_price = None
        self._take_price = None

    @property
    def StopLoss(self):
        return float(self._stop_loss.Value)
    @property
    def TakeProfit(self):
        return float(self._take_profit.Value)
    @property
    def TrailingStop(self):
        return float(self._trailing_stop.Value)
    @property
    def TrailingStep(self):
        return float(self._trailing_step.Value)
    @property
    def BreakEven(self):
        return float(self._break_even.Value)
    @property
    def TradeOnCloseBar(self):
        return self._trade_on_close_bar.Value
    @property
    def EmaPeriod(self):
        return int(self._ema_period.Value)
    @property
    def SignalPeriod(self):
        return int(self._signal_period.Value)
    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnStarted2(self, time):
        super(ea_trix_strategy, self).OnStarted2(time)

        self._prev_third_trix = None
        self._prev_third_signal = None
        self._prev_trix = None
        self._prev_signal = None
        self._pending_signal = None
        self._clear_position_state()

        self._trix_ema1 = ExponentialMovingAverage()
        self._trix_ema1.Length = self.EmaPeriod
        self._trix_ema2 = ExponentialMovingAverage()
        self._trix_ema2.Length = self.EmaPeriod
        self._trix_ema3 = ExponentialMovingAverage()
        self._trix_ema3.Length = self.EmaPeriod

        self._signal_ema1 = ExponentialMovingAverage()
        self._signal_ema1.Length = self.SignalPeriod
        self._signal_ema2 = ExponentialMovingAverage()
        self._signal_ema2.Length = self.SignalPeriod
        self._signal_ema3 = ExponentialMovingAverage()
        self._signal_ema3.Length = self.SignalPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        self._handle_pending_signal(candle)
        self._manage_active_position(candle)

        result = self._try_calculate_indicators(candle)
        if result is None:
            return

        trix, signal = result

        if self._prev_trix is None or self._prev_signal is None:
            self._prev_trix = trix
            self._prev_signal = signal
            return

        if not self._trix_ema3.IsFormed or not self._signal_ema3.IsFormed:
            self._prev_trix = trix
            self._prev_signal = signal
            return

        cross_up = self._prev_signal < self._prev_trix and signal > trix
        cross_down = self._prev_signal > self._prev_trix and signal < trix

        if cross_up:
            if self.TradeOnCloseBar:
                self._pending_signal = self._BUY
            else:
                self._execute_signal(self._BUY, candle, float(candle.ClosePrice))
        elif cross_down:
            if self.TradeOnCloseBar:
                self._pending_signal = self._SELL
            else:
                self._execute_signal(self._SELL, candle, float(candle.ClosePrice))

        self._prev_trix = trix
        self._prev_signal = signal

    def _handle_pending_signal(self, candle):
        if self._pending_signal is None:
            return

        if not self._trix_ema3.IsFormed or not self._signal_ema3.IsFormed:
            return

        self._execute_signal(self._pending_signal, candle, float(candle.OpenPrice))
        self._pending_signal = None

    def _execute_signal(self, direction, candle, fill_price):
        if direction == self._BUY:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._entry_price = fill_price
            self._stop_price = fill_price - self.StopLoss if self.StopLoss > 0 else None
            self._take_price = fill_price + self.TakeProfit if self.TakeProfit > 0 else None
        elif direction == self._SELL:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._entry_price = fill_price
            self._stop_price = fill_price + self.StopLoss if self.StopLoss > 0 else None
            self._take_price = fill_price - self.TakeProfit if self.TakeProfit > 0 else None

    def _manage_active_position(self, candle):
        h = float(candle.HighPrice)
        lo = float(candle.LowPrice)

        if self.Position > 0 and self._entry_price is not None:
            long_entry = self._entry_price

            # Break-even
            if self.BreakEven > 0 and h - long_entry >= self.BreakEven:
                if self._stop_price is None or self._stop_price < long_entry:
                    self._stop_price = long_entry

            # Trailing stop
            if self.TrailingStop > 0:
                move = h - long_entry
                if move >= self.TrailingStop:
                    new_stop = h - self.TrailingStop
                    if self._stop_price is None or new_stop - self._stop_price >= self.TrailingStep:
                        self._stop_price = new_stop

            # Take profit
            if self._take_price is not None and h >= self._take_price:
                self.SellMarket()
                self._clear_position_state()
                return

            # Stop loss
            if self._stop_price is not None and lo <= self._stop_price:
                self.SellMarket()
                self._clear_position_state()

        elif self.Position < 0 and self._entry_price is not None:
            short_entry = self._entry_price

            # Break-even
            if self.BreakEven > 0 and short_entry - lo >= self.BreakEven:
                if self._stop_price is None or self._stop_price > short_entry:
                    self._stop_price = short_entry

            # Trailing stop
            if self.TrailingStop > 0:
                move = short_entry - lo
                if move >= self.TrailingStop:
                    new_stop = lo + self.TrailingStop
                    if self._stop_price is None or self._stop_price - new_stop >= self.TrailingStep:
                        self._stop_price = new_stop

            # Take profit
            if self._take_price is not None and lo <= self._take_price:
                self.BuyMarket()
                self._clear_position_state()
                return

            # Stop loss
            if self._stop_price is not None and h >= self._stop_price:
                self.BuyMarket()
                self._clear_position_state()

        elif self.Position == 0:
            self._clear_position_state()

    def _make_div(self, ind, val, t):
        iv = DecimalIndicatorValue(ind, Decimal(val), t)
        iv.IsFinal = True
        return float(ind.Process(iv).Value)

    def _try_calculate_indicators(self, candle):
        close = float(candle.ClosePrice)
        t = candle.ServerTime

        ema1_val = self._make_div(self._trix_ema1, close, t)
        ema2_val = self._make_div(self._trix_ema2, ema1_val, t)
        ema3_val = self._make_div(self._trix_ema3, ema2_val, t)

        if self._prev_third_trix is None:
            self._prev_third_trix = ema3_val
            return None

        trix = (ema3_val - self._prev_third_trix) / self._prev_third_trix if self._prev_third_trix != 0 else 0.0
        self._prev_third_trix = ema3_val

        sig1_val = self._make_div(self._signal_ema1, close, t)
        sig2_val = self._make_div(self._signal_ema2, sig1_val, t)
        sig_base = self._make_div(self._signal_ema3, sig2_val, t)

        if self._prev_third_signal is None:
            self._prev_third_signal = sig_base
            return None

        signal = (sig_base - self._prev_third_signal) / self._prev_third_signal if self._prev_third_signal != 0 else 0.0
        self._prev_third_signal = sig_base

        return (trix, signal)

    def _clear_position_state(self):
        self._entry_price = None
        self._stop_price = None
        self._take_price = None

    def OnReseted(self):
        super(ea_trix_strategy, self).OnReseted()
        self._prev_third_trix = None
        self._prev_third_signal = None
        self._prev_trix = None
        self._prev_signal = None
        self._pending_signal = None
        self._clear_position_state()

    def CreateClone(self):
        return ea_trix_strategy()
