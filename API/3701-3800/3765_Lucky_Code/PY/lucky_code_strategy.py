import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class lucky_code_strategy(Strategy):
    """Momentum strategy that opens trades when candle price jumps reach a configurable distance
    and manages exits with profit and drawdown filters."""

    def __init__(self):
        super(lucky_code_strategy, self).__init__()

        self._shift_points = self.Param("ShiftPoints", 3) \
            .SetGreaterThanZero() \
            .SetDisplay("Shift points", "Minimum price jump required to trigger entries", "Trading") \
            .SetOptimize(1, 20, 1)

        self._limit_points = self.Param("LimitPoints", 18) \
            .SetGreaterThanZero() \
            .SetDisplay("Limit points", "Maximum number of points allowed against the position", "Risk management") \
            .SetOptimize(5, 100, 5)

        self._previous_close = None
        self._shift_threshold = 0.0
        self._limit_threshold = 0.0
        self._entry_price = 0.0
        self._thresholds_ready = False
        self._hold_bars = 0

    @property
    def ShiftPoints(self):
        return self._shift_points.Value

    @ShiftPoints.setter
    def ShiftPoints(self, value):
        self._shift_points.Value = value

    @property
    def LimitPoints(self):
        return self._limit_points.Value

    @LimitPoints.setter
    def LimitPoints(self, value):
        self._limit_points.Value = value

    def OnReseted(self):
        super(lucky_code_strategy, self).OnReseted()
        self._previous_close = None
        self._shift_threshold = 0.0
        self._limit_threshold = 0.0
        self._entry_price = 0.0
        self._thresholds_ready = False
        self._hold_bars = 0

    def OnStarted2(self, time):
        super(lucky_code_strategy, self).OnStarted2(time)

        tf = DataType.TimeFrame(TimeSpan.FromMinutes(5))

        subscription = self.SubscribeCandles(tf)
        subscription.Bind(self._process_candle).Start()

    def _ensure_thresholds(self, price):
        if self._thresholds_ready:
            return

        if price <= 0.0:
            return

        # Use percentage of price. ShiftPoints=3 means 3% shift, LimitPoints=18 means 18% limit.
        self._shift_threshold = price * self.ShiftPoints * 0.01
        self._limit_threshold = price * self.LimitPoints * 0.01
        self._thresholds_ready = True

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)

        self._ensure_thresholds(close)

        if not self._thresholds_ready:
            return

        # Count hold bars for position management.
        if self.Position != 0:
            self._hold_bars += 1

        if self._previous_close is not None:
            prev_close = self._previous_close
            delta = close - prev_close

            # Only enter if flat.
            if self.Position == 0:
                # Price dropped sharply -> buy on expected rebound.
                if (-delta) >= self._shift_threshold:
                    self.BuyMarket()
                    self._entry_price = close
                    self._hold_bars = 0
                    self.LogInfo("Buy triggered by fast price drop. Price=" + str(close))
                # Price rose sharply -> sell on expected reversal.
                elif delta >= self._shift_threshold:
                    self.SellMarket()
                    self._entry_price = close
                    self._hold_bars = 0
                    self.LogInfo("Sell triggered by fast price rise. Price=" + str(close))

        self._previous_close = close

        self._try_close_position(close)

    def _try_close_position(self, current_price):
        if self.Position == 0:
            return

        avg_price = self._entry_price

        if avg_price <= 0.0:
            return

        # Minimum hold of 3 bars before checking exit.
        if self._hold_bars < 3:
            return

        # Use half of shift threshold as profit target.
        profit_target = self._shift_threshold * 0.5

        if self.Position > 0:
            # Close long on profit target or drawdown limit.
            if current_price - avg_price >= profit_target:
                self.SellMarket()
                self._hold_bars = 0
                self.LogInfo("Closed long on profit. Price=" + str(current_price))
            elif self._limit_threshold > 0.0 and avg_price - current_price >= self._limit_threshold:
                self.SellMarket()
                self._hold_bars = 0
                self.LogInfo("Closed long on drawdown limit. Price=" + str(current_price))
        elif self.Position < 0:
            # Close short on profit target or drawdown limit.
            if avg_price - current_price >= profit_target:
                self.BuyMarket()
                self._hold_bars = 0
                self.LogInfo("Closed short on profit. Price=" + str(current_price))
            elif self._limit_threshold > 0.0 and current_price - avg_price >= self._limit_threshold:
                self.BuyMarket()
                self._hold_bars = 0
                self.LogInfo("Closed short on drawdown limit. Price=" + str(current_price))

    def CreateClone(self):
        return lucky_code_strategy()
