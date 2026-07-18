import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class lucky_strategy(Strategy):
    """Breakout strategy that reacts to fast price shifts (candle-to-candle high/low jumps)
    and closes trades on profit target or adverse move (stop loss)."""

    def __init__(self):
        super(lucky_strategy, self).__init__()

        self._shift_pct = self.Param("ShiftPct", 1.5) \
            .SetDisplay("Shift %", "Minimum percentage shift in high/low to trigger entry", "Trading") \
            .SetOptimize(0.5, 3.0, 0.5)

        self._profit_pct = self.Param("ProfitPct", 2.0) \
            .SetDisplay("Profit %", "Profit target as percentage of entry price", "Risk management") \
            .SetOptimize(1.0, 5.0, 0.5)

        self._stop_pct = self.Param("StopPct", 3.0) \
            .SetDisplay("Stop %", "Stop loss as percentage of entry price", "Risk management") \
            .SetOptimize(1.0, 5.0, 0.5)

        self._reverse = self.Param("Reverse", False) \
            .SetDisplay("Reverse mode", "Invert the direction of new trades", "Trading")

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle type", "Candle timeframe", "General")

        self._entry_price = 0.0
        self._previous_high = None
        self._previous_low = None
        self._is_ready = False

    @property
    def ShiftPct(self):
        return self._shift_pct.Value

    @property
    def ProfitPct(self):
        return self._profit_pct.Value

    @property
    def StopPct(self):
        return self._stop_pct.Value

    @property
    def Reverse(self):
        return self._reverse.Value

    @property
    def CandleType(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType)]

    def OnReseted(self):
        super(lucky_strategy, self).OnReseted()
        self._previous_high = None
        self._previous_low = None
        self._entry_price = 0.0
        self._is_ready = False

    def OnStarted2(self, time):
        super(lucky_strategy, self).OnStarted2(time)

        self.SubscribeCandles(self.CandleType) \
            .Bind(self.process_candle) \
            .Start()

    def process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        close = float(candle.ClosePrice)

        if not self._is_ready:
            self._previous_high = high
            self._previous_low = low
            self._is_ready = True
            return

        # Try to close existing position first
        self._try_close_position(close)

        # Only open new positions if flat
        if self.Position == 0 and self._previous_high is not None and self._previous_low is not None:
            prev_h = self._previous_high
            prev_l = self._previous_low

            # Check for upward breakout: high moved up sharply relative to previous high
            if prev_h > 0 and (high - prev_h) / prev_h * 100.0 >= float(self.ShiftPct):
                if self.Reverse:
                    self._open_short(close)
                else:
                    self._open_long(close)
            # Check for downward breakdown: low moved down sharply relative to previous low
            elif prev_l > 0 and (prev_l - low) / prev_l * 100.0 >= float(self.ShiftPct):
                if self.Reverse:
                    self._open_long(close)
                else:
                    self._open_short(close)

        self._previous_high = high
        self._previous_low = low

    def _open_long(self, price):
        self.BuyMarket(self.Volume)
        self._entry_price = price

    def _open_short(self, price):
        self.SellMarket(self.Volume)
        self._entry_price = price

    def _try_close_position(self, current_price):
        if self.Position == 0 or self._entry_price <= 0:
            return

        if self.Position > 0:
            pct_change = (current_price - self._entry_price) / self._entry_price * 100.0

            if pct_change >= float(self.ProfitPct) or pct_change <= -float(self.StopPct):
                self.SellMarket(self.Position)
        elif self.Position < 0:
            pct_change = (self._entry_price - current_price) / self._entry_price * 100.0

            if pct_change >= float(self.ProfitPct) or pct_change <= -float(self.StopPct):
                self.BuyMarket(Math.Abs(self.Position))

    def CreateClone(self):
        return lucky_strategy()
