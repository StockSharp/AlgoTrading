import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class dkoderweb_repainting_issue_fix_strategy(Strategy):
    """
    Harmonic ABCD pattern strategy with Fibonacci entry, target and stop.
    A zigzag point is set when the candle colour flips: the higher high of the two candles after an up-to-down flip,
    the lower low after a down-to-up flip. The last four points A, B, C, D form an ABCD pattern when BC retraces 0.382-0.886 of AB
    and CD extends 1.13-2.618 of BC; D below C is bullish and D above C bearish.
    Fibonacci levels are measured from D back over the C-D range. A bullish pattern buys when the close is at or below the EntryRate level,
    a bearish one sells when the close is at or above it. The TakeProfitRate and StopLossRate levels at entry close the position.
    """

    def __init__(self):
        super(dkoderweb_repainting_issue_fix_strategy, self).__init__()
        self._trade_size = self.Param("TradeSize", 1.0).SetGreaterThanZero().SetDisplay("Trade Size", "Order volume", "Trading")
        self._entry_rate = self.Param("EntryRate", 0.382).SetDisplay("Entry Rate", "Fibonacci rate of the entry level", "Fibonacci")
        self._take_profit_rate = self.Param("TakeProfitRate", 0.618).SetDisplay("Take Profit Rate", "Fibonacci rate of the take profit level", "Fibonacci")
        self._stop_loss_rate = self.Param("StopLossRate", -0.618).SetDisplay("Stop Loss Rate", "Fibonacci rate of the stop loss level", "Fibonacci")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.candle_type)]

    def _reset_state(self):
        self._points = []
        self._prev_candle = None
        self._direction = 0
        self._take_price = None
        self._stop_price = None

    def OnReseted(self):
        super(dkoderweb_repainting_issue_fix_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(dkoderweb_repainting_issue_fix_strategy, self).OnStarted2(time)

        self._reset_state()
        self.Volume = Decimal(self._trade_size.Value)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        current = (float(candle.OpenPrice), float(candle.HighPrice), float(candle.LowPrice), float(candle.ClosePrice))
        self._update_zigzag(current)
        self._prev_candle = current

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        _, high, low, close = current

        if self.Position > 0:
            if (self._take_price is not None and high >= self._take_price) or (self._stop_price is not None and low <= self._stop_price):
                self.SellMarket(self.Position)
                self._take_price = None
                self._stop_price = None
                return
        elif self.Position < 0:
            if (self._take_price is not None and low <= self._take_price) or (self._stop_price is not None and high >= self._stop_price):
                self.BuyMarket(-self.Position)
                self._take_price = None
                self._stop_price = None
                return

        if len(self._points) < 4:
            return

        a, b, c, d = self._points[-4:]
        ab = abs(a - b)
        bc = abs(b - c)
        cd = abs(c - d)

        if ab == 0 or bc == 0:
            return

        abc = bc / ab
        bcd = cd / bc

        if abc < 0.382 or abc > 0.886 or bcd < 1.13 or bcd > 2.618:
            return

        def fib(rate):
            return d - cd * rate if d > c else d + cd * rate

        entry_rate = float(self._entry_rate.Value)
        tp_rate = float(self._take_profit_rate.Value)
        sl_rate = float(self._stop_loss_rate.Value)

        if d < c and close <= fib(entry_rate) and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
            self._take_price = fib(tp_rate)
            self._stop_price = fib(sl_rate)
        elif d > c and close >= fib(entry_rate) and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
            self._take_price = fib(tp_rate)
            self._stop_price = fib(sl_rate)

    def _update_zigzag(self, current):
        prev = self._prev_candle
        if prev is None:
            return

        open_price, high, low, close = current
        prev_open, prev_high, prev_low, prev_close = prev

        is_up = close >= open_price
        is_down = close <= open_price
        prev_is_up = prev_close >= prev_open
        prev_is_down = prev_close <= prev_open

        prev_direction = self._direction
        if prev_is_up and is_down:
            self._direction = -1
        elif prev_is_down and is_up:
            self._direction = 1

        point = None
        if prev_is_up and is_down and prev_direction != -1:
            point = max(high, prev_high)
        elif prev_is_down and is_up and prev_direction != 1:
            point = min(low, prev_low)

        if point is not None:
            self._points.append(point)
            if len(self._points) > 5:
                self._points.pop(0)

    def CreateClone(self):
        return dkoderweb_repainting_issue_fix_strategy()
