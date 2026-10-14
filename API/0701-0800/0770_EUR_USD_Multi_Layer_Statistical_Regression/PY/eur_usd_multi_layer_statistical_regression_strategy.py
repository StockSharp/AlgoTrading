import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import LinearRegression
from StockSharp.Algo.Strategies import Strategy

MIN_RELIABILITY = 0.5

class eur_usd_multi_layer_statistical_regression_strategy(Strategy):
    """
    EUR/USD multi-layer statistical regression strategy.
    Short, medium and long linear regressions each yield a slope and an R². A layer counts only when its R² reaches MinRSquared
    and its absolute slope reaches SlopeThreshold. The weighted slope of the valid layers gives the direction and the weighted R²
    of the valid layers relative to all weights gives the reliability; with reliability above 0.5 the strategy goes long on a
    positive slope and short on a negative one, reversing an opposite position. Trading stops for the rest of the day once the
    day's loss reaches MaxDailyLossPct of the account value.
    """

    def __init__(self):
        super(eur_usd_multi_layer_statistical_regression_strategy, self).__init__()
        self._short_length = self.Param("ShortLength", 20).SetGreaterThanZero().SetDisplay("Short Length", "Length of the short regression", "Regression")
        self._medium_length = self.Param("MediumLength", 50).SetGreaterThanZero().SetDisplay("Medium Length", "Length of the medium regression", "Regression")
        self._long_length = self.Param("LongLength", 100).SetGreaterThanZero().SetDisplay("Long Length", "Length of the long regression", "Regression")
        self._min_r_squared = self.Param("MinRSquared", 0.45).SetDisplay("Min R²", "Minimum R² for a layer to count", "Validation")
        self._slope_threshold = self.Param("SlopeThreshold", 0.00005).SetNotNegative().SetDisplay("Slope Threshold", "Minimum absolute slope for a layer to count", "Validation")
        self._weight_short = self.Param("WeightShort", 0.4).SetNotNegative().SetDisplay("Short Weight", "Weight of the short regression", "Ensemble")
        self._weight_medium = self.Param("WeightMedium", 0.35).SetNotNegative().SetDisplay("Medium Weight", "Weight of the medium regression", "Ensemble")
        self._weight_long = self.Param("WeightLong", 0.25).SetNotNegative().SetDisplay("Long Weight", "Weight of the long regression", "Ensemble")
        self._position_size_pct = self.Param("PositionSizePct", 50.0).SetNotNegative().SetDisplay("Position Size %", "Position size in percent of equity", "Risk")
        self._max_daily_loss_pct = self.Param("MaxDailyLossPct", 12.0).SetNotNegative().SetDisplay("Max Daily Loss %", "Daily loss limit in percent of the account value", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_day()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_day(self):
        self._current_day = None
        self._day_start_pnl = 0.0
        self._day_start_value = 0.0
        self._day_loss_reached = False

    def OnReseted(self):
        super(eur_usd_multi_layer_statistical_regression_strategy, self).OnReseted()
        self._reset_day()

    def OnStarted2(self, time):
        super(eur_usd_multi_layer_statistical_regression_strategy, self).OnStarted2(time)

        self._reset_day()

        short_reg = LinearRegression()
        short_reg.Length = self._short_length.Value
        medium_reg = LinearRegression()
        medium_reg.Length = self._medium_length.Value
        long_reg = LinearRegression()
        long_reg.Length = self._long_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(short_reg, medium_reg, long_reg, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, short_reg)
            self.DrawIndicator(area, medium_reg)
            self.DrawIndicator(area, long_reg)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, short_value, medium_value, long_value):
        if candle.State != CandleStates.Finished:
            return

        if not short_value.IsFormed or not medium_value.IsFormed or not long_value.IsFormed:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self._update_daily_loss(candle.OpenTime.Date):
            if self.Position > 0:
                self.SellMarket(self.Position)
            elif self.Position < 0:
                self.BuyMarket(-self.Position)
            return

        weights = (float(self._weight_short.Value), float(self._weight_medium.Value), float(self._weight_long.Value))
        total_weight = sum(weights)
        if total_weight <= 0:
            return

        valid_weight = 0.0
        weighted_slope = 0.0
        weighted_r2 = 0.0

        for value, weight in zip((short_value, medium_value, long_value), weights):
            slope = value.LinearRegSlope
            r2 = value.RSquared
            if slope is None or r2 is None:
                continue
            slope = float(slope)
            r2 = float(r2)
            if r2 < float(self._min_r_squared.Value) or abs(slope) < float(self._slope_threshold.Value):
                continue
            valid_weight += weight
            weighted_slope += weight * slope
            weighted_r2 += weight * r2

        if valid_weight <= 0:
            return

        slope = weighted_slope / valid_weight
        reliability = weighted_r2 / total_weight

        if reliability <= MIN_RELIABILITY:
            return

        if slope > 0 and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif slope < 0 and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))

    def _update_daily_loss(self, day):
        pnl = float(self.PnL)
        if self._current_day != day:
            self._current_day = day
            self._day_start_pnl = pnl
            self._day_start_value = float(self.Portfolio.CurrentValue) if self.Portfolio is not None and self.Portfolio.CurrentValue is not None else 0.0
            self._day_loss_reached = False

        # Without a known account value there is nothing to take the percentage of.
        limit = float(self._max_daily_loss_pct.Value)
        if self._day_start_value > 0 and limit > 0 and pnl - self._day_start_pnl <= -self._day_start_value * limit / 100.0:
            self._day_loss_reached = True

        return self._day_loss_reached

    def CreateClone(self):
        return eur_usd_multi_layer_statistical_regression_strategy()
