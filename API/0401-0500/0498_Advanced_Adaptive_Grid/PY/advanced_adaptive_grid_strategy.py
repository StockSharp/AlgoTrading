import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import (AverageTrueRange, IndicatorHelper,
                                        MovingAverageConvergenceDivergenceSignal,
                                        RelativeStrengthIndex, SimpleMovingAverage)
from StockSharp.Algo.Strategies import Strategy


class advanced_adaptive_grid_strategy(Strategy):
    """Advanced Adaptive Grid Trading Strategy.

    Trend direction comes from three moving averages together with MACD, the distance between grid
    levels adapts to volatility through ATR, and the open grid is guarded by stop-loss, take-profit,
    trailing stop, a maximum holding period and a daily loss limit.
    """

    def __init__(self):
        super(advanced_adaptive_grid_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._base_grid_size = self.Param("BaseGridSize", 1.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Base Grid Size", "Distance between grid levels in percent of price", "Grid")
        self._max_positions = self.Param("MaxPositions", 5) \
            .SetGreaterThanZero() \
            .SetDisplay("Max Positions", "Maximum number of grid levels open at the same time", "Grid")
        self._use_volatility_grid = self.Param("UseVolatilityGrid", True) \
            .SetDisplay("Use Volatility Grid", "Size the grid step from ATR instead of Base Grid Size", "Grid")
        self._atr_length = self.Param("AtrLength", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR Length", "ATR period for the volatility grid", "Grid")
        self._atr_multiplier = self.Param("AtrMultiplier", 1.5) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR Multiplier", "ATR multiplier for the grid step", "Grid")
        self._rsi_length = self.Param("RsiLength", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("RSI Length", "RSI period", "Indicators")
        self._rsi_overbought = self.Param("RsiOverbought", 70) \
            .SetDisplay("RSI Overbought", "Overbought level", "Indicators")
        self._rsi_oversold = self.Param("RsiOversold", 30) \
            .SetDisplay("RSI Oversold", "Oversold level", "Indicators")
        self._short_ma_length = self.Param("ShortMaLength", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Short MA", "Short moving average length", "Trend")
        self._long_ma_length = self.Param("LongMaLength", 50) \
            .SetGreaterThanZero() \
            .SetDisplay("Long MA", "Long moving average length", "Trend")
        self._super_long_ma_length = self.Param("SuperLongMaLength", 200) \
            .SetGreaterThanZero() \
            .SetDisplay("Super Long MA", "Super long moving average length", "Trend")
        self._macd_fast_length = self.Param("MacdFastLength", 12) \
            .SetGreaterThanZero() \
            .SetDisplay("MACD Fast", "MACD fast moving average length", "Trend")
        self._macd_slow_length = self.Param("MacdSlowLength", 26) \
            .SetGreaterThanZero() \
            .SetDisplay("MACD Slow", "MACD slow moving average length", "Trend")
        self._macd_signal_length = self.Param("MacdSignalLength", 9) \
            .SetGreaterThanZero() \
            .SetDisplay("MACD Signal", "MACD signal line length", "Trend")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Stop Loss %", "Stop loss percentage from the average entry", "Risk")
        self._take_profit_percent = self.Param("TakeProfitPercent", 3.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Take Profit %", "Take profit percentage from the average entry", "Risk")
        self._use_trailing_stop = self.Param("UseTrailingStop", True) \
            .SetDisplay("Use Trailing Stop", "Protect an open grid with a trailing stop", "Risk")
        self._trailing_stop_percent = self.Param("TrailingStopPercent", 1.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Trailing Stop %", "Trailing stop distance from the best price", "Risk")
        self._max_loss_per_day = self.Param("MaxLossPerDay", 5.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Max Loss Per Day %", "Daily loss limit in percent of the account value", "Risk")
        self._time_based_exit = self.Param("TimeBasedExit", True) \
            .SetDisplay("Time Based Exit", "Close the grid after Max Holding Period bars", "Risk")
        self._max_holding_period = self.Param("MaxHoldingPeriod", 48) \
            .SetGreaterThanZero() \
            .SetDisplay("Max Holding Period", "Maximum number of bars a grid stays open", "Risk")

        self._grid_direction = 0
        self._open_levels = 0
        self._average_price = 0.0
        self._last_level_price = 0.0
        self._best_price = 0.0
        self._bars_in_position = 0
        self._current_day = None
        self._day_start_pnl = 0.0
        self._day_start_value = 0.0
        self._day_loss_reached = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(advanced_adaptive_grid_strategy, self).OnReseted()

        self._reset_grid()

        self._current_day = None
        self._day_start_pnl = 0.0
        self._day_start_value = 0.0
        self._day_loss_reached = False

    def OnStarted2(self, time):
        super(advanced_adaptive_grid_strategy, self).OnStarted2(time)

        atr = AverageTrueRange()
        atr.Length = int(self._atr_length.Value)

        rsi = RelativeStrengthIndex()
        rsi.Length = int(self._rsi_length.Value)

        short_ma = SimpleMovingAverage()
        short_ma.Length = int(self._short_ma_length.Value)

        long_ma = SimpleMovingAverage()
        long_ma.Length = int(self._long_ma_length.Value)

        super_long_ma = SimpleMovingAverage()
        super_long_ma.Length = int(self._super_long_ma_length.Value)

        macd = MovingAverageConvergenceDivergenceSignal()
        macd.Macd.ShortMa.Length = int(self._macd_fast_length.Value)
        macd.Macd.LongMa.Length = int(self._macd_slow_length.Value)
        macd.SignalMa.Length = int(self._macd_signal_length.Value)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(atr, rsi, short_ma, long_ma, super_long_ma, macd, self._on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, short_ma)
            self.DrawIndicator(area, long_ma)
            self.DrawIndicator(area, super_long_ma)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, atr_val, rsi_val, short_ma_val, long_ma_val, super_long_ma_val, macd_val):
        if candle.State != CandleStates.Finished:
            return

        if atr_val.IsEmpty or rsi_val.IsEmpty or short_ma_val.IsEmpty or long_ma_val.IsEmpty \
                or super_long_ma_val.IsEmpty or macd_val.IsEmpty:
            return

        if macd_val.Macd is None or macd_val.Signal is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        macd = float(macd_val.Macd)
        macd_signal = float(macd_val.Signal)
        atr = float(IndicatorHelper.ToDecimal(atr_val))
        rsi = float(IndicatorHelper.ToDecimal(rsi_val))
        short_ma = float(IndicatorHelper.ToDecimal(short_ma_val))
        long_ma = float(IndicatorHelper.ToDecimal(long_ma_val))
        super_long_ma = float(IndicatorHelper.ToDecimal(super_long_ma_val))
        price = float(candle.ClosePrice)

        self._update_daily_loss(candle.OpenTime.Date)

        # ATR keeps the distance between levels proportional to current volatility, otherwise the
        # levels sit a fixed percentage of price apart.
        if bool(self._use_volatility_grid.Value):
            grid_step = atr * float(self._atr_multiplier.Value)
        else:
            grid_step = price * float(self._base_grid_size.Value) / 100.0

        if grid_step <= 0:
            return

        trend_up = short_ma > long_ma and price > super_long_ma and macd > macd_signal
        trend_down = short_ma < long_ma and price < super_long_ma and macd < macd_signal

        if self._grid_direction != 0:
            self._bars_in_position += 1
            if self._grid_direction > 0:
                self._best_price = max(self._best_price, float(candle.HighPrice))
            else:
                self._best_price = min(self._best_price, float(candle.LowPrice))

            if self._try_close_grid(price, trend_up, trend_down):
                return

        if self._day_loss_reached:
            return

        if self._grid_direction == 0:
            self._try_open_grid(price, rsi, trend_up, trend_down)
        else:
            self._try_add_level(price, rsi, grid_step)

    def _update_daily_loss(self, day):
        if self._current_day != day:
            self._current_day = day
            self._day_start_pnl = float(self.PnL)
            account_value = self.Portfolio.CurrentValue if self.Portfolio is not None else None
            self._day_start_value = float(account_value) if account_value is not None else 0.0
            self._day_loss_reached = False

        # Without a known account value there is nothing to take the percentage of.
        if self._day_start_value <= 0:
            return

        limit = self._day_start_value * float(self._max_loss_per_day.Value) / 100.0
        if float(self.PnL) - self._day_start_pnl <= -limit:
            self._day_loss_reached = True

    def _try_close_grid(self, price, trend_up, trend_down):
        if self.Position == 0:
            return False

        is_long = self._grid_direction > 0
        sl_pct = float(self._stop_loss_percent.Value)
        tp_pct = float(self._take_profit_percent.Value)
        trail_pct = float(self._trailing_stop_percent.Value)

        if is_long:
            stop_price = self._average_price * (1.0 - sl_pct / 100.0)
            take_price = self._average_price * (1.0 + tp_pct / 100.0)
            exit_now = price <= stop_price or price >= take_price
        else:
            stop_price = self._average_price * (1.0 + sl_pct / 100.0)
            take_price = self._average_price * (1.0 - tp_pct / 100.0)
            exit_now = price >= stop_price or price <= take_price

        # The trailing stop follows the best price reached by the grid and engages only once the
        # grid is at least one trailing distance in profit.
        if not exit_now and bool(self._use_trailing_stop.Value):
            if is_long:
                exit_now = self._best_price >= self._average_price * (1.0 + trail_pct / 100.0) \
                    and price <= self._best_price * (1.0 - trail_pct / 100.0)
            else:
                exit_now = self._best_price <= self._average_price * (1.0 - trail_pct / 100.0) \
                    and price >= self._best_price * (1.0 + trail_pct / 100.0)

        if not exit_now and bool(self._time_based_exit.Value) \
                and self._bars_in_position >= int(self._max_holding_period.Value):
            exit_now = True

        # The trend turned against the whole grid.
        if not exit_now and (trend_down if is_long else trend_up):
            exit_now = True

        if not exit_now and self._day_loss_reached:
            exit_now = True

        if not exit_now:
            return False

        if is_long:
            self.SellMarket(Math.Abs(self.Position))
        else:
            self.BuyMarket(Math.Abs(self.Position))

        self._reset_grid()

        return True

    def _try_open_grid(self, price, rsi, trend_up, trend_down):
        if self.Position != 0:
            return

        overbought = float(self._rsi_overbought.Value)
        oversold = float(self._rsi_oversold.Value)

        # In a trending market the first level follows the trend while RSI is not at the opposite
        # extreme; in a sideways market the grid fades the RSI extremes instead.
        go_long = rsi < overbought if trend_up else (not trend_down and rsi < oversold)
        go_short = rsi > oversold if trend_down else (not trend_up and rsi > overbought)

        if go_long:
            self.BuyMarket(self.Volume)
            self._start_grid(1, price)
        elif go_short:
            self.SellMarket(self.Volume)
            self._start_grid(-1, price)

    def _try_add_level(self, price, rsi, grid_step):
        if self._open_levels >= int(self._max_positions.Value):
            return

        overbought = float(self._rsi_overbought.Value)
        oversold = float(self._rsi_oversold.Value)

        # The next level is filled once price has travelled a full grid step against the grid.
        if self._grid_direction > 0:
            if price > self._last_level_price - grid_step or rsi >= overbought:
                return

            self.BuyMarket(self.Volume)
        else:
            if price < self._last_level_price + grid_step or rsi <= oversold:
                return

            self.SellMarket(self.Volume)

        self._add_level(price)

    def _start_grid(self, direction, price):
        self._grid_direction = direction
        self._open_levels = 1
        self._average_price = price
        self._last_level_price = price
        self._best_price = price
        self._bars_in_position = 0

    def _add_level(self, price):
        # Every level has the same volume, so the average entry is the plain mean of the level prices.
        self._average_price = (self._average_price * self._open_levels + price) / (self._open_levels + 1)
        self._open_levels += 1
        self._last_level_price = price

    def _reset_grid(self):
        self._grid_direction = 0
        self._open_levels = 0
        self._average_price = 0.0
        self._last_level_price = 0.0
        self._best_price = 0.0
        self._bars_in_position = 0

    def CreateClone(self):
        return advanced_adaptive_grid_strategy()
