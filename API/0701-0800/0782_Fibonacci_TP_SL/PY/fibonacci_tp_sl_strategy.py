import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Highest, Lowest, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy

class fibonacci_tp_sl_strategy(Strategy):
    """
    Fibonacci TP SL strategy.
    Retracement levels are measured down from the highest high of the last Lookback candles towards the lowest low. While flat it
    goes long when the close lies between the 78.6% and 38.2% levels and short when it lies between the 61.8% and 23.6% levels,
    provided at least MinBarsBetweenTrades candles passed since the last entry. A position closes at an ATR-based stop set at entry
    or at a percent take profit. No new trades open in a week once the week's return reaches MaxWeeklyReturn.
    """

    def __init__(self):
        super(fibonacci_tp_sl_strategy, self).__init__()
        self._take_profit_percent = self.Param("TakeProfitPercent", 4.0).SetNotNegative().SetDisplay("Take Profit %", "Take profit in percent of the entry price", "Risk")
        self._min_bars_between_trades = self.Param("MinBarsBetweenTrades", 10).SetNotNegative().SetDisplay("Min Bars Between Trades", "Minimum candles between entries", "General")
        self._lookback = self.Param("Lookback", 100).SetGreaterThanZero().SetDisplay("Lookback", "Candles the Fibonacci range spans", "Indicators")
        self._atr_period = self.Param("AtrPeriod", 14).SetGreaterThanZero().SetDisplay("ATR Period", "ATR period", "Indicators")
        self._atr_multiplier = self.Param("AtrMultiplier", 1.5).SetGreaterThanZero().SetDisplay("ATR Multiplier", "ATR multiplier of the stop distance", "Risk")
        self._max_weekly_return = self.Param("MaxWeeklyReturn", 0.15).SetNotNegative().SetDisplay("Max Weekly Return", "Weekly return after which no new trades open", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._bar_index = 0
        self._last_entry_bar = None
        self._stop_price = 0.0
        self._take_price = 0.0
        self._current_week = None
        self._week_start_pnl = 0.0
        self._week_start_value = 0.0

    def OnReseted(self):
        super(fibonacci_tp_sl_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(fibonacci_tp_sl_strategy, self).OnStarted2(time)

        self._reset_state()

        highest = Highest()
        highest.Length = self._lookback.Value
        lowest = Lowest()
        lowest.Length = self._lookback.Value
        atr = AverageTrueRange()
        atr.Length = self._atr_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(highest, lowest, atr, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, highest)
            self.DrawIndicator(area, lowest)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, high_value, low_value, atr_value):
        if candle.State != CandleStates.Finished:
            return

        bar = self._bar_index
        self._bar_index += 1

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        week_cap_reached = self._update_week(candle.OpenTime)

        if self.Position > 0:
            if float(candle.LowPrice) <= self._stop_price or (self._take_price > 0 and float(candle.HighPrice) >= self._take_price):
                self.SellMarket(self.Position)
            return

        if self.Position < 0:
            if float(candle.HighPrice) >= self._stop_price or (self._take_price > 0 and float(candle.LowPrice) <= self._take_price):
                self.BuyMarket(-self.Position)
            return

        if week_cap_reached:
            return

        if self._last_entry_bar is not None and bar - self._last_entry_bar < self._min_bars_between_trades.Value:
            return

        high = float(high_value)
        low = float(low_value)
        rng = high - low
        if rng <= 0:
            return

        fib236 = high - rng * 0.236
        fib382 = high - rng * 0.382
        fib618 = high - rng * 0.618
        fib786 = high - rng * 0.786

        close = float(candle.ClosePrice)
        stop_distance = float(atr_value) * float(self._atr_multiplier.Value)
        take_percent = float(self._take_profit_percent.Value)

        if close <= fib382 and close >= fib786:
            self._stop_price = close - stop_distance
            self._take_price = close * (1.0 + take_percent / 100.0) if take_percent > 0 else 0.0
            self._last_entry_bar = bar
            self.BuyMarket()
        elif close <= fib236 and close >= fib618:
            self._stop_price = close + stop_distance
            self._take_price = close * (1.0 - take_percent / 100.0) if take_percent > 0 else 0.0
            self._last_entry_bar = bar
            self.SellMarket()

    def _update_week(self, time):
        date = time.Date
        week = date.AddDays(-((int(date.DayOfWeek) + 6) % 7))
        pnl = float(self.PnL)

        if self._current_week != week:
            self._current_week = week
            self._week_start_pnl = pnl
            self._week_start_value = float(self.Portfolio.CurrentValue) if self.Portfolio is not None and self.Portfolio.CurrentValue is not None else 0.0

        # Without a known account value there is nothing to measure the return against.
        cap = float(self._max_weekly_return.Value)
        return self._week_start_value > 0 and cap > 0 and (pnl - self._week_start_pnl) / self._week_start_value >= cap

    def CreateClone(self):
        return fibonacci_tp_sl_strategy()
