import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import SuperTrend
from StockSharp.Algo.Strategies import Strategy

STOP_NONE = 0
STOP_SUPERTREND = 1
STOP_FIXED_PERCENTAGE = 2

DIRECTION_BOTH = 0
DIRECTION_LONG = 1
DIRECTION_SHORT = 2


class liquidity_breakout_strategy(Strategy):
    """
    Liquidity Breakout strategy.
    The range is bounded by the latest pivot high and pivot low, each confirmed by PivotLength candles on both sides. A close crossing
    above the range high goes long and a close crossing below the range low goes short, reversing an opposite position. Direction limits
    the allowed side: a breakout of the disabled side only closes the position. The stop is either the SuperTrend line (exit on a close
    beyond it) or a fixed percentage from the entry price.
    """

    def __init__(self):
        super(liquidity_breakout_strategy, self).__init__()
        self._pivot_length = self.Param("PivotLength", 12).SetGreaterThanZero().SetDisplay("Pivot Length", "Candles on each side that confirm a pivot", "Range")
        self._stop_loss = self.Param("StopLoss", STOP_SUPERTREND).SetDisplay("Stop Loss", "0 None, 1 SuperTrend, 2 FixedPercentage", "Risk")
        self._fixed_percentage = self.Param("FixedPercentage", 0.1).SetNotNegative().SetDisplay("Fixed Percentage", "Fixed stop loss percentage from the entry price", "Risk")
        self._super_trend_period = self.Param("SuperTrendPeriod", 10).SetGreaterThanZero().SetDisplay("SuperTrend Period", "ATR period of SuperTrend", "Risk")
        self._super_trend_multiplier = self.Param("SuperTrendMultiplier", 3.0).SetGreaterThanZero().SetDisplay("SuperTrend Multiplier", "ATR multiplier of SuperTrend", "Risk")
        self._direction = self.Param("Direction", DIRECTION_BOTH).SetDisplay("Direction", "0 Both, 1 Long, 2 Short", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._highs = []
        self._lows = []
        self._range_high = None
        self._range_low = None
        self._prev_close = None

    def OnReseted(self):
        super(liquidity_breakout_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(liquidity_breakout_strategy, self).OnStarted2(time)

        self._reset_state()

        super_trend = SuperTrend()
        super_trend.Length = self._super_trend_period.Value
        super_trend.Multiplier = Decimal(self._super_trend_multiplier.Value)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(super_trend, self._process_candle).Start()

        if self._stop_loss.Value == STOP_FIXED_PERCENTAGE and self._fixed_percentage.Value > 0:
            self.StartProtection(Unit(), Unit(Decimal(self._fixed_percentage.Value), UnitTypes.Percent), useMarketOrders=True)

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, super_trend)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, super_trend_value):
        if candle.State != CandleStates.Finished:
            return

        # Breakouts are measured against the range known before this candle.
        range_high = self._range_high
        range_low = self._range_low
        prev_close = self._prev_close
        self._prev_close = candle.ClosePrice

        self._update_pivots(candle)

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        close = candle.ClosePrice

        if self._stop_loss.Value == STOP_SUPERTREND and super_trend_value.IsFormed:
            line = super_trend_value.GetValue[Decimal](None)

            if self.Position > 0 and close < line:
                self.SellMarket(self.Position)
                return

            if self.Position < 0 and close > line:
                self.BuyMarket(-self.Position)
                return

        if prev_close is None:
            return

        long_breakout = range_high is not None and prev_close <= range_high and close > range_high
        short_breakout = range_low is not None and prev_close >= range_low and close < range_low
        direction = self._direction.Value

        if long_breakout:
            if direction != DIRECTION_SHORT and self.Position <= 0:
                self.BuyMarket(self.Volume + abs(self.Position))
            elif direction == DIRECTION_SHORT and self.Position < 0:
                self.BuyMarket(-self.Position)
        elif short_breakout:
            if direction != DIRECTION_LONG and self.Position >= 0:
                self.SellMarket(self.Volume + abs(self.Position))
            elif direction == DIRECTION_LONG and self.Position > 0:
                self.SellMarket(self.Position)

    def _update_pivots(self, candle):
        length = self._pivot_length.Value
        size = length * 2 + 1

        self._highs.append(candle.HighPrice)
        self._lows.append(candle.LowPrice)

        if len(self._highs) > size:
            self._highs.pop(0)
            self._lows.pop(0)

        if len(self._highs) < size:
            return

        # The middle candle is a pivot when no candle within PivotLength on either side exceeds it.
        center_high = self._highs[length]
        if center_high >= max(self._highs):
            self._range_high = center_high

        center_low = self._lows[length]
        if center_low <= min(self._lows):
            self._range_low = center_low

    def CreateClone(self):
        return liquidity_breakout_strategy()
