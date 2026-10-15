import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class ema_rsi_trail_stop_strategy(Strategy):
    """
    EMA RSI trail stop strategy.
    EMA A crossing above EMA B with the close above EMA C on a bullish candle goes long, the mirrored conditions go short.
    RSI above ExitLongRsi closes a long and RSI below ExitShortRsi closes a short. A fixed percent stop protects the position
    and turns into a trailing stop TrailPoints price steps behind the best price once price has moved TrailOffset steps in favour.
    Optionally a profitable position is closed after XBars candles.
    """

    def __init__(self):
        super(ema_rsi_trail_stop_strategy, self).__init__()
        self._ema_a_length = self.Param("EmaALength", 10).SetGreaterThanZero().SetDisplay("EMA A", "Fast EMA length", "Indicators")
        self._ema_b_length = self.Param("EmaBLength", 20).SetGreaterThanZero().SetDisplay("EMA B", "Medium EMA length", "Indicators")
        self._ema_c_length = self.Param("EmaCLength", 100).SetGreaterThanZero().SetDisplay("EMA C", "Trend EMA length", "Indicators")
        self._rsi_length = self.Param("RsiLength", 14).SetGreaterThanZero().SetDisplay("RSI Length", "RSI length", "Indicators")
        self._exit_long_rsi = self.Param("ExitLongRsi", 70.0).SetDisplay("Exit Long RSI", "RSI level that closes a long", "Exits")
        self._exit_short_rsi = self.Param("ExitShortRsi", 30.0).SetDisplay("Exit Short RSI", "RSI level that closes a short", "Exits")
        self._trail_points = self.Param("TrailPoints", 50.0).SetNotNegative().SetDisplay("Trail Points", "Trailing distance in price steps", "Risk")
        self._trail_offset = self.Param("TrailOffset", 10.0).SetNotNegative().SetDisplay("Trail Offset", "Favourable move in price steps that activates trailing", "Risk")
        self._fix_stop_loss_percent = self.Param("FixStopLossPercent", 5.0).SetNotNegative().SetDisplay("Fixed Stop %", "Fixed stop loss percentage from entry price", "Risk")
        self._close_after_x_bars = self.Param("CloseAfterXBars", True).SetDisplay("Close After X Bars", "Close a profitable position after XBars candles", "Exits")
        self._x_bars = self.Param("XBars", 24).SetGreaterThanZero().SetDisplay("X Bars", "Candles after which a profitable position is closed", "Exits")
        self._show_long = self.Param("ShowLong", True).SetDisplay("Long Trades", "Allow long trades", "General")
        self._show_short = self.Param("ShowShort", False).SetDisplay("Short Trades", "Allow short trades", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._prev_a = None
        self._prev_b = None
        self._entry_price = 0.0
        self._best_price = 0.0
        self._stop_price = None
        self._bars_in_position = 0

    def OnReseted(self):
        super(ema_rsi_trail_stop_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(ema_rsi_trail_stop_strategy, self).OnStarted2(time)

        self._reset_state()

        ema_a = ExponentialMovingAverage()
        ema_a.Length = self._ema_a_length.Value
        ema_b = ExponentialMovingAverage()
        ema_b.Length = self._ema_b_length.Value
        ema_c = ExponentialMovingAverage()
        ema_c.Length = self._ema_c_length.Value
        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ema_a, ema_b, ema_c, rsi, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema_a)
            self.DrawIndicator(area, ema_b)
            self.DrawIndicator(area, ema_c)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, a_value, b_value, c_value, rsi_value):
        if candle.State != CandleStates.Finished:
            return

        a = float(a_value)
        b = float(b_value)
        c = float(c_value)
        rsi = float(rsi_value)

        prev_a = self._prev_a
        prev_b = self._prev_b
        self._prev_a = a
        self._prev_b = b

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self.Position != 0 and self._manage_position(candle, rsi):
            return

        if prev_a is None or prev_b is None:
            return

        cross_up = prev_a <= prev_b and a > b
        cross_down = prev_a >= prev_b and a < b

        close = float(candle.ClosePrice)
        open_price = float(candle.OpenPrice)

        if self._show_long.Value and cross_up and close > c and close > open_price and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
            self._open_position(close, True)
        elif self._show_short.Value and cross_down and close < c and close < open_price and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
            self._open_position(close, False)

    def _open_position(self, price, is_long):
        self._entry_price = price
        self._best_price = price
        self._bars_in_position = 0
        percent = float(self._fix_stop_loss_percent.Value)
        if percent > 0:
            self._stop_price = price * (1.0 - percent / 100.0) if is_long else price * (1.0 + percent / 100.0)
        else:
            self._stop_price = None

    def _manage_position(self, candle, rsi):
        step = float(self.Security.PriceStep) if self.Security is not None and self.Security.PriceStep is not None else 1.0
        self._bars_in_position += 1

        close = float(candle.ClosePrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        trail_points = float(self._trail_points.Value) * step
        trail_offset = float(self._trail_offset.Value) * step
        time_exit = self._close_after_x_bars.Value and self._bars_in_position >= self._x_bars.Value

        if self.Position > 0:
            if self._stop_price is not None and low <= self._stop_price:
                self.SellMarket(self.Position)
                return True

            if rsi > float(self._exit_long_rsi.Value) or (time_exit and close > self._entry_price):
                self.SellMarket(self.Position)
                return True

            self._best_price = max(self._best_price, high)

            if self._best_price - self._entry_price >= trail_offset:
                trail = self._best_price - trail_points
                self._stop_price = trail if self._stop_price is None else max(self._stop_price, trail)
        else:
            if self._stop_price is not None and high >= self._stop_price:
                self.BuyMarket(-self.Position)
                return True

            if rsi < float(self._exit_short_rsi.Value) or (time_exit and close < self._entry_price):
                self.BuyMarket(-self.Position)
                return True

            self._best_price = min(self._best_price, low)

            if self._entry_price - self._best_price >= trail_offset:
                trail = self._best_price + trail_points
                self._stop_price = trail if self._stop_price is None else min(self._stop_price, trail)

        return False

    def CreateClone(self):
        return ema_rsi_trail_stop_strategy()
