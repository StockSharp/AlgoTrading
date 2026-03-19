import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Sides
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergence, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy

class dealers_trade_macd_mql4_strategy(Strategy):
    """
    Dealers Trade MACD strategy (v7.74 port from MQL4).
    Uses MACD slope direction for trade signals with martingale grid,
    trailing stops, account protection, and configurable money management.
    """

    def __init__(self):
        super(dealers_trade_macd_mql4_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Primary timeframe for signals", "General")
        self._fixed_volume = self.Param("FixedVolume", 0.1) \
            .SetDisplay("Fixed Volume", "Lot size when risk sizing is disabled", "Risk")
        self._use_risk_sizing = self.Param("UseRiskSizing", True) \
            .SetDisplay("Use Risk Sizing", "Enable balance based money management", "Risk")
        self._risk_percent = self.Param("RiskPercent", 2.0) \
            .SetDisplay("Risk Percent", "Percentage of equity used when sizing dynamically", "Risk")
        self._is_standard_account = self.Param("IsStandardAccount", True) \
            .SetDisplay("Standard Account", "True for standard accounts, false for mini", "Risk")
        self._max_volume = self.Param("MaxVolume", 5.0) \
            .SetDisplay("Max Volume", "Upper cap for any single order", "Risk")
        self._lot_multiplier = self.Param("LotMultiplier", 1.5) \
            .SetDisplay("Lot Multiplier", "Multiplier applied to subsequent entries", "Money Management")
        self._max_trades = self.Param("MaxTrades", 1) \
            .SetDisplay("Max Trades", "Maximum simultaneous positions", "Money Management")
        self._spacing_pips = self.Param("SpacingPips", 200) \
            .SetDisplay("Spacing (pips)", "Minimum price movement before adding", "Money Management")
        self._orders_to_protect = self.Param("OrdersToProtect", 3) \
            .SetDisplay("Orders To Protect", "Number of trades kept when protection triggers", "Money Management")
        self._account_protection = self.Param("AccountProtection", True) \
            .SetDisplay("Account Protection", "Close last trade once secure profit is reached", "Money Management")
        self._secure_profit = self.Param("SecureProfit", 50.0) \
            .SetDisplay("Secure Profit", "Currency profit required to lock gains", "Money Management")
        self._take_profit_pips = self.Param("TakeProfitPips", 200) \
            .SetDisplay("Take Profit (pips)", "Take profit distance from entry", "Risk")
        self._stop_loss_pips = self.Param("StopLossPips", 500) \
            .SetDisplay("Stop Loss (pips)", "Initial stop loss distance", "Risk")
        self._trailing_stop_pips = self.Param("TrailingStopPips", 100) \
            .SetDisplay("Trailing Stop (pips)", "Trailing distance after activation", "Risk")
        self._reverse_condition = self.Param("ReverseCondition", False) \
            .SetDisplay("Reverse Condition", "Invert MACD slope interpretation", "General")
        self._macd_fast = self.Param("MacdFast", 14) \
            .SetDisplay("MACD Fast", "Fast EMA length", "Indicators")
        self._macd_slow = self.Param("MacdSlow", 26) \
            .SetDisplay("MACD Slow", "Slow EMA length", "Indicators")
        self._macd_signal = self.Param("MacdSignal", 1) \
            .SetDisplay("MACD Signal", "Signal EMA length", "Indicators")

        self._positions = []
        self._previous_macd = None
        self._pip_size = 0.0
        self._step_value = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(dealers_trade_macd_mql4_strategy, self).OnReseted()
        self._positions = []
        self._previous_macd = None

    def OnStarted(self, time):
        super(dealers_trade_macd_mql4_strategy, self).OnStarted(time)

        self._positions = []

        slow_ema = ExponentialMovingAverage()
        slow_ema.Length = self._macd_slow.Value
        fast_ema = ExponentialMovingAverage()
        fast_ema.Length = self._macd_fast.Value
        macd = MovingAverageConvergenceDivergence(slow_ema, fast_ema)

        step = 1.0
        if self.Security is not None and self.Security.PriceStep is not None:
            step = float(self.Security.PriceStep)
        if step <= 0:
            step = 0.0001
        self._pip_size = step
        self._step_value = step

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(macd, self._process_candle).Start()

    def _process_candle(self, candle, macd_result):
        if candle.State != CandleStates.Finished:
            return
        if not macd_result.IsFinal:
            return

        macd_value = float(macd_result.GetValue[float]())

        self._update_trailing_and_stops(candle)

        open_trades = len(self._positions)
        allow_new = open_trades < self._max_trades.Value

        if self._previous_macd is None:
            self._previous_macd = macd_value
            return

        direction = 0
        if macd_value > self._previous_macd:
            direction = 1
        elif macd_value < self._previous_macd:
            direction = -1

        if self._reverse_condition.Value:
            direction = -direction

        if self._account_protection.Value and open_trades >= max(1, self._max_trades.Value - self._orders_to_protect.Value):
            total_profit = self._calculate_total_profit(float(candle.ClosePrice))
            if total_profit >= self._secure_profit.Value:
                self._close_last_position()
                self._previous_macd = macd_value
                return

        if allow_new and direction > 0:
            self._try_open(True, candle)
        elif allow_new and direction < 0:
            self._try_open(False, candle)

        self._previous_macd = macd_value

    def _try_open(self, is_buy, candle):
        price = float(candle.ClosePrice)
        spacing = self._spacing_pips.Value * self._pip_size
        side = Sides.Buy if is_buy else Sides.Sell

        if is_buy:
            ref = self._get_reference_price(Sides.Buy)
            if ref != 0 and ref - price < spacing:
                return
        else:
            ref = self._get_reference_price(Sides.Sell)
            if ref != 0 and price - ref < spacing:
                return

        same_count = self._count_positions(side)

        stop_distance = self._stop_loss_pips.Value * self._pip_size
        take_distance = self._take_profit_pips.Value * self._pip_size

        if is_buy:
            self.BuyMarket()
        else:
            self.SellMarket()

        state = {
            "side": side,
            "entry_price": price,
            "stop_price": (price - stop_distance) if (is_buy and stop_distance > 0) else ((price + stop_distance) if (not is_buy and stop_distance > 0) else None),
            "tp_price": (price + take_distance) if (is_buy and take_distance > 0) else ((price - take_distance) if (not is_buy and take_distance > 0) else None),
        }
        self._positions.append(state)

    def _update_trailing_and_stops(self, candle):
        trailing_distance = self._trailing_stop_pips.Value * self._pip_size
        activation_distance = (self._trailing_stop_pips.Value + self._spacing_pips.Value) * self._pip_size

        i = len(self._positions) - 1
        while i >= 0:
            state = self._positions[i]
            if state["side"] == Sides.Buy:
                if state["tp_price"] is not None and float(candle.HighPrice) >= state["tp_price"]:
                    self.SellMarket()
                    self._positions.pop(i)
                    i -= 1
                    continue
                if state["stop_price"] is not None and float(candle.LowPrice) <= state["stop_price"]:
                    self.SellMarket()
                    self._positions.pop(i)
                    i -= 1
                    continue
                if self._trailing_stop_pips.Value > 0 and float(candle.ClosePrice) - state["entry_price"] >= activation_distance:
                    candidate = float(candle.ClosePrice) - trailing_distance
                    if state.get("trailing") is None or state["trailing"] < candidate:
                        state["trailing"] = candidate
                if state.get("trailing") is not None and float(candle.LowPrice) <= state["trailing"]:
                    self.SellMarket()
                    self._positions.pop(i)
                    i -= 1
                    continue
            else:
                if state["tp_price"] is not None and float(candle.LowPrice) <= state["tp_price"]:
                    self.BuyMarket()
                    self._positions.pop(i)
                    i -= 1
                    continue
                if state["stop_price"] is not None and float(candle.HighPrice) >= state["stop_price"]:
                    self.BuyMarket()
                    self._positions.pop(i)
                    i -= 1
                    continue
                if self._trailing_stop_pips.Value > 0 and state["entry_price"] - float(candle.ClosePrice) >= activation_distance:
                    candidate = float(candle.ClosePrice) + trailing_distance
                    if state.get("trailing") is None or state["trailing"] > candidate:
                        state["trailing"] = candidate
                if state.get("trailing") is not None and float(candle.HighPrice) >= state["trailing"]:
                    self.BuyMarket()
                    self._positions.pop(i)
                    i -= 1
                    continue
            i -= 1

    def _calculate_total_profit(self, current_price):
        profit = 0.0
        for state in self._positions:
            if state["side"] == Sides.Buy:
                diff = current_price - state["entry_price"]
            else:
                diff = state["entry_price"] - current_price
            steps = diff / self._pip_size if self._pip_size > 0 else diff
            profit += steps * self._step_value
        return profit

    def _close_last_position(self):
        if len(self._positions) == 0:
            return
        state = self._positions[-1]
        if state["side"] == Sides.Buy:
            self.SellMarket()
        else:
            self.BuyMarket()
        self._positions.pop()

    def _get_reference_price(self, side):
        for i in range(len(self._positions) - 1, -1, -1):
            if self._positions[i]["side"] == side:
                return self._positions[i]["entry_price"]
        return 0.0

    def _count_positions(self, side):
        count = 0
        for p in self._positions:
            if p["side"] == side:
                count += 1
        return count

    def CreateClone(self):
        return dealers_trade_macd_mql4_strategy()
