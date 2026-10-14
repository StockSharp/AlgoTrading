import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SuperTrend
from StockSharp.Algo.Strategies import Strategy


class vietnamese_3x_supertrend_strategy(Strategy):
    """
    Vietnamese 3x SuperTrend strategy.
    Three SuperTrends (fast, medium, slow) drive a long-only scaling scheme. While the slow SuperTrend is down the strategy adds up to
    three entries: Long 1 when the medium trend is up and the fast one down, Long 2 when the medium trend is down and the close is above
    the fast line, Long 3 when the fast trend is down and the close breaks the highest high of that fast downtrend (or of the last two
    red candles). Positions close when all trends are up on a bearish candle, when the average entry price is above the close, or by a
    break-even stop once price has moved above the average entry price.
    """

    def __init__(self):
        super(vietnamese_3x_supertrend_strategy, self).__init__()
        self._fast_atr_length = self.Param("FastAtrLength", 10).SetGreaterThanZero().SetDisplay("Fast ATR Length", "ATR length of the fast SuperTrend", "SuperTrend")
        self._fast_multiplier = self.Param("FastMultiplier", 1.0).SetGreaterThanZero().SetDisplay("Fast Multiplier", "ATR multiplier of the fast SuperTrend", "SuperTrend")
        self._medium_atr_length = self.Param("MediumAtrLength", 11).SetGreaterThanZero().SetDisplay("Medium ATR Length", "ATR length of the medium SuperTrend", "SuperTrend")
        self._medium_multiplier = self.Param("MediumMultiplier", 2.0).SetGreaterThanZero().SetDisplay("Medium Multiplier", "ATR multiplier of the medium SuperTrend", "SuperTrend")
        self._slow_atr_length = self.Param("SlowAtrLength", 12).SetGreaterThanZero().SetDisplay("Slow ATR Length", "ATR length of the slow SuperTrend", "SuperTrend")
        self._slow_multiplier = self.Param("SlowMultiplier", 3.0).SetGreaterThanZero().SetDisplay("Slow Multiplier", "ATR multiplier of the slow SuperTrend", "SuperTrend")
        self._use_highest_of_two_red_candles = self.Param("UseHighestOfTwoRedCandles", False).SetDisplay("Highest Of Two Red Candles", "Long 3 breaks the highest high of the last two red candles", "Entries")
        self._use_entry_stop_loss = self.Param("UseEntryStopLoss", True).SetDisplay("Break-Even Stop", "Enable the break-even stop", "Exits")
        self._use_all_downtrend_exit = self.Param("UseAllDowntrendExit", True).SetDisplay("All Trends Exit", "Exit when all SuperTrends are up and the candle closes bearish", "Exits")
        self._use_avg_price_in_loss = self.Param("UseAvgPriceInLoss", True).SetDisplay("Average Price In Loss", "Exit when the average entry price is above the close", "Exits")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._downtrend_high = None
        self._last_red_high = None
        self._prev_red_high = None
        self._reset_entries()

    def _reset_entries(self):
        self._long1_done = False
        self._long2_done = False
        self._long3_done = False
        self._entry_count = 0
        self._avg_entry_price = Decimal(0)
        self._break_even_armed = False

    def OnReseted(self):
        super(vietnamese_3x_supertrend_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(vietnamese_3x_supertrend_strategy, self).OnStarted2(time)

        self._reset_state()

        fast = SuperTrend()
        fast.Length = self._fast_atr_length.Value
        fast.Multiplier = Decimal(self._fast_multiplier.Value)
        medium = SuperTrend()
        medium.Length = self._medium_atr_length.Value
        medium.Multiplier = Decimal(self._medium_multiplier.Value)
        slow = SuperTrend()
        slow.Length = self._slow_atr_length.Value
        slow.Multiplier = Decimal(self._slow_multiplier.Value)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(fast, medium, slow, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast)
            self.DrawIndicator(area, medium)
            self.DrawIndicator(area, slow)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, fast_value, medium_value, slow_value):
        if candle.State != CandleStates.Finished:
            return

        if not fast_value.IsFormed or not medium_value.IsFormed or not slow_value.IsFormed:
            return

        fast_up = fast_value.IsUpTrend
        medium_up = medium_value.IsUpTrend
        slow_up = slow_value.IsUpTrend
        fast_line = fast_value.GetValue[Decimal](None)
        close = candle.ClosePrice
        bearish = close < candle.OpenPrice

        # Breakout references come from earlier candles only.
        if self._use_highest_of_two_red_candles.Value:
            if self._last_red_high is not None and self._prev_red_high is not None:
                breakout_level = max(self._last_red_high, self._prev_red_high)
            else:
                breakout_level = None
        else:
            breakout_level = self._downtrend_high

        if fast_up:
            self._downtrend_high = None
        elif self._downtrend_high is None or candle.HighPrice > self._downtrend_high:
            self._downtrend_high = candle.HighPrice

        if bearish:
            self._prev_red_high = self._last_red_high
            self._last_red_high = candle.HighPrice

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self.Position > 0:
            exit_position = False

            if self._use_entry_stop_loss.Value:
                if self._break_even_armed and candle.LowPrice <= self._avg_entry_price:
                    exit_position = True
                elif candle.LowPrice > self._avg_entry_price:
                    self._break_even_armed = True

            if self._use_all_downtrend_exit.Value and fast_up and medium_up and slow_up and bearish:
                exit_position = True

            if self._use_avg_price_in_loss.Value and self._avg_entry_price > close:
                exit_position = True

            if exit_position:
                self.SellMarket(self.Position)
                self._reset_entries()
                return
        elif self._entry_count > 0:
            self._reset_entries()

        if slow_up:
            return

        if not self._long1_done and medium_up and not fast_up:
            self._long1_done = True
            self._enter(close)

        if not self._long2_done and not medium_up and close > fast_line:
            self._long2_done = True
            self._enter(close)

        if not self._long3_done and not fast_up and breakout_level is not None and close > breakout_level:
            self._long3_done = True
            self._enter(close)

    def _enter(self, price):
        self.BuyMarket(self.Volume)
        self._avg_entry_price = (self._avg_entry_price * self._entry_count + price) / Decimal(self._entry_count + 1)
        self._entry_count += 1

    def CreateClone(self):
        return vietnamese_3x_supertrend_strategy()
