import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class big_runner_strategy(Strategy):
    """
    Big Runner strategy.
    Buys when on the same candle the close crosses above the fast SMA and the fast SMA crosses above the slow SMA, and sells on the
    mirrored crosses; an opposite signal reverses the position. The order size is PercentOfPortfolio of the portfolio value times
    Leverage divided by the price. Separate long and short percent stop losses and take profits are measured from the entry close
    (0 disables a level).
    """

    def __init__(self):
        super(big_runner_strategy, self).__init__()
        self._fast_length = self.Param("FastLength", 5).SetGreaterThanZero().SetDisplay("Fast Length", "Fast SMA period", "Indicators")
        self._slow_length = self.Param("SlowLength", 20).SetGreaterThanZero().SetDisplay("Slow Length", "Slow SMA period", "Indicators")
        self._take_profit_long_percent = self.Param("TakeProfitLongPercent", 4.0).SetNotNegative().SetDisplay("Take Profit Long %", "Long take profit in percent", "Risk")
        self._take_profit_short_percent = self.Param("TakeProfitShortPercent", 7.0).SetNotNegative().SetDisplay("Take Profit Short %", "Short take profit in percent", "Risk")
        self._stop_loss_long_percent = self.Param("StopLossLongPercent", 2.0).SetNotNegative().SetDisplay("Stop Loss Long %", "Long stop loss in percent", "Risk")
        self._stop_loss_short_percent = self.Param("StopLossShortPercent", 2.0).SetNotNegative().SetDisplay("Stop Loss Short %", "Short stop loss in percent", "Risk")
        self._percent_of_portfolio = self.Param("PercentOfPortfolio", 10.0).SetGreaterThanZero().SetDisplay("Percent Of Portfolio", "Share of the portfolio value used per trade", "Money Management")
        self._leverage = self.Param("Leverage", 1.0).SetGreaterThanZero().SetDisplay("Leverage", "Leverage applied to the position value", "Money Management")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._prev_close = None
        self._prev_fast = None
        self._prev_slow = None
        self._entry_price = Decimal(0)

    def OnReseted(self):
        super(big_runner_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(big_runner_strategy, self).OnStarted2(time)

        self._reset_state()

        fast = SimpleMovingAverage()
        fast.Length = self._fast_length.Value
        slow = SimpleMovingAverage()
        slow.Length = self._slow_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast, slow, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast)
            self.DrawIndicator(area, slow)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, fast, slow):
        if candle.State != CandleStates.Finished:
            return

        close = candle.ClosePrice
        prev_close = self._prev_close
        prev_fast = self._prev_fast
        prev_slow = self._prev_slow

        self._prev_close = close
        self._prev_fast = fast
        self._prev_slow = slow

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self._check_stops(candle):
            return

        if prev_close is None or prev_fast is None or prev_slow is None:
            return

        long_signal = prev_close <= prev_fast and close > fast and prev_fast <= prev_slow and fast > slow
        short_signal = prev_close >= prev_fast and close < fast and prev_fast >= prev_slow and fast < slow

        if long_signal and self.Position <= 0:
            self.BuyMarket(self._get_order_volume(close) + abs(self.Position))
            self._entry_price = close
        elif short_signal and self.Position >= 0:
            self.SellMarket(self._get_order_volume(close) + abs(self.Position))
            self._entry_price = close

    def _check_stops(self, candle):
        if self._entry_price <= 0:
            return False

        hundred = Decimal(100)
        one = Decimal(1)

        if self.Position > 0:
            sl = Decimal(self._stop_loss_long_percent.Value)
            tp = Decimal(self._take_profit_long_percent.Value)
            stop_hit = sl > 0 and candle.LowPrice <= self._entry_price * (one - sl / hundred)
            take_hit = tp > 0 and candle.HighPrice >= self._entry_price * (one + tp / hundred)
            if stop_hit or take_hit:
                self.SellMarket(self.Position)
                self._entry_price = Decimal(0)
                return True
        elif self.Position < 0:
            sl = Decimal(self._stop_loss_short_percent.Value)
            tp = Decimal(self._take_profit_short_percent.Value)
            stop_hit = sl > 0 and candle.HighPrice >= self._entry_price * (one + sl / hundred)
            take_hit = tp > 0 and candle.LowPrice <= self._entry_price * (one - tp / hundred)
            if stop_hit or take_hit:
                self.BuyMarket(-self.Position)
                self._entry_price = Decimal(0)
                return True

        return False

    def _get_order_volume(self, price):
        portfolio = self.Portfolio
        equity = None
        if portfolio is not None:
            equity = portfolio.CurrentValue if portfolio.CurrentValue is not None else portfolio.BeginValue
        if equity is None or equity <= 0 or price <= 0:
            return self.Volume

        volume = equity * Decimal(self._percent_of_portfolio.Value) / Decimal(100) * Decimal(self._leverage.Value) / price

        security = self.Security
        if security is not None:
            step = security.VolumeStep
            if step is not None and step > 0:
                volume = Math.Floor(volume / step) * step
            max_volume = security.MaxVolume
            if max_volume is not None and max_volume > 0 and volume > max_volume:
                volume = max_volume

        return volume if volume > 0 else self.Volume

    def CreateClone(self):
        return big_runner_strategy()
