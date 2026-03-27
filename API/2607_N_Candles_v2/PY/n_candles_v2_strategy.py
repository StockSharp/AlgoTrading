import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy

class n_candles_v2_strategy(Strategy):
    """
    N Candles v2: trades after N consecutive same-direction candles with manual SL/TP/trailing.
    """

    def __init__(self):
        super(n_candles_v2_strategy, self).__init__()
        self._candles_count = self.Param("CandlesCount", 3) \
            .SetGreaterThanZero() \
            .SetDisplay("Candles in Row", "Number of identical candles required", "Entry")
        self._lot_size = self.Param("LotSize", 1.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Lot Size", "Position size used for entries", "Risk")
        self._tp_pips = self.Param("TakeProfitPips", 50) \
            .SetDisplay("Take Profit (pips)", "Take-profit distance in price steps", "Risk")
        self._sl_pips = self.Param("StopLossPips", 50) \
            .SetDisplay("Stop Loss (pips)", "Stop-loss distance in price steps", "Risk")
        self._trailing_stop_pips = self.Param("TrailingStopPips", 10) \
            .SetDisplay("Trailing Stop (pips)", "Trailing stop distance in price steps", "Risk")
        self._trailing_step_pips = self.Param("TrailingStepPips", 4) \
            .SetDisplay("Trailing Step (pips)", "Additional move required to tighten trailing stop", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Time frame used for analysis", "General")

        self._streak_len = 0
        self._streak_dir = 0
        self._pos_dir = 0
        self._entry_price = 0.0
        self._stop_price = None
        self._take_price = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def _get_pip_size(self):
        step = 0.0
        if self.Security is not None and self.Security.PriceStep is not None:
            step = float(self.Security.PriceStep)
        return step if step > 0.0 else 1.0

    def _reset_state(self):
        self._streak_len = 0
        self._streak_dir = 0
        self._pos_dir = 0
        self._entry_price = 0.0
        self._stop_price = None
        self._take_price = None

    def OnReseted(self):
        super(n_candles_v2_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted(self, time):
        super(n_candles_v2_strategy, self).OnStarted(time)
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._process_candle).Start()

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        if self._manage_open_position(candle):
            return

        close = float(candle.ClosePrice)
        open_p = float(candle.OpenPrice)
        direction = 1 if close > open_p else (-1 if close < open_p else 0)

        if direction == 0:
            self._streak_len = 0
            self._streak_dir = 0
            return

        if direction == self._streak_dir:
            self._streak_len += 1
        else:
            self._streak_dir = direction
            self._streak_len = 1

        if self._streak_len < int(self._candles_count.Value):
            return

        if direction > 0:
            self._try_open_long(candle)
        else:
            self._try_open_short(candle)

    def _manage_open_position(self, candle):
        if self.Position == 0:
            self._pos_dir = 0
            self._stop_price = None
            self._take_price = None
            self._entry_price = 0.0
            return False

        pip = self._get_pip_size()
        trailing_step = int(self._trailing_step_pips.Value) * pip
        trailing_stop_pips = int(self._trailing_stop_pips.Value)
        close = float(candle.ClosePrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)

        if self._pos_dir > 0:
            if trailing_stop_pips > 0:
                desired = close - trailing_stop_pips * pip
                if self._stop_price is not None and desired - trailing_step > self._stop_price:
                    self._stop_price = desired
            if self._take_price is not None and high >= self._take_price:
                return self._exit_position()
            if self._stop_price is not None and low <= self._stop_price:
                return self._exit_position()
        elif self._pos_dir < 0:
            if trailing_stop_pips > 0:
                desired = close + trailing_stop_pips * pip
                if self._stop_price is not None and desired + trailing_step < self._stop_price:
                    self._stop_price = desired
            if self._take_price is not None and low <= self._take_price:
                return self._exit_position()
            if self._stop_price is not None and high >= self._stop_price:
                return self._exit_position()

        return False

    def _try_open_long(self, candle):
        if self.Position > 0:
            return
        if self.Position < 0:
            self.BuyMarket()
        self.BuyMarket()
        self._set_position_state(float(candle.ClosePrice), 1)

    def _try_open_short(self, candle):
        if self.Position < 0:
            return
        if self.Position > 0:
            self.SellMarket()
        self.SellMarket()
        self._set_position_state(float(candle.ClosePrice), -1)

    def _exit_position(self):
        if self.Position > 0:
            self.SellMarket()
        elif self.Position < 0:
            self.BuyMarket()
        self._reset_state()
        return True

    def _set_position_state(self, price, direction):
        self._pos_dir = direction
        self._entry_price = price
        pip = self._get_pip_size()
        tp_pips = int(self._tp_pips.Value)
        sl_pips = int(self._sl_pips.Value)
        trailing_stop_pips = int(self._trailing_stop_pips.Value)

        if direction > 0:
            self._stop_price = (price - sl_pips * pip) if sl_pips > 0 else (price if trailing_stop_pips > 0 else None)
            self._take_price = (price + tp_pips * pip) if tp_pips > 0 else None
        else:
            self._stop_price = (price + sl_pips * pip) if sl_pips > 0 else (price if trailing_stop_pips > 0 else None)
            self._take_price = (price - tp_pips * pip) if tp_pips > 0 else None

    def CreateClone(self):
        return n_candles_v2_strategy()
