import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal, Math
from StockSharp.Messages import DataType, CandleStates, OrderStates
from StockSharp.Algo.Indicators import AverageTrueRange, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class adaptive_fractal_grid_scalping_strategy(Strategy):
    """
    Adaptive Fractal Grid Scalping strategy.
    Five-bar fractals mark the grid. While ATR is above VolatilityThreshold the strategy keeps a buy limit at the last fractal low minus
    ATR * GridMultiplierLow when the close is above the SMA, or a sell limit at the last fractal high plus the same distance when it is
    below. A filled long exits at the opposite grid level (fractal high plus ATR * GridMultiplierHigh) or by a trailing stop
    ATR * TrailStopMultiplier behind the best price; shorts mirror this.
    """

    def __init__(self):
        super(adaptive_fractal_grid_scalping_strategy, self).__init__()
        self._atr_length = self.Param("AtrLength", 14).SetGreaterThanZero().SetDisplay("ATR Length", "ATR period", "Indicators")
        self._sma_length = self.Param("SmaLength", 50).SetGreaterThanZero().SetDisplay("SMA Length", "Period of the trend SMA", "Indicators")
        self._grid_multiplier_high = self.Param("GridMultiplierHigh", 2.0).SetNotNegative().SetDisplay("Grid Multiplier High", "ATR multiplier of the opposite grid level used as the target", "Grid")
        self._grid_multiplier_low = self.Param("GridMultiplierLow", 0.5).SetNotNegative().SetDisplay("Grid Multiplier Low", "ATR multiplier of the entry grid level", "Grid")
        self._trail_stop_multiplier = self.Param("TrailStopMultiplier", 0.5).SetNotNegative().SetDisplay("Trail Stop Multiplier", "ATR multiplier of the trailing stop distance", "Risk")
        self._volatility_threshold = self.Param("VolatilityThreshold", 1.0).SetNotNegative().SetDisplay("Volatility Threshold", "ATR level above which the grid is active", "Grid")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._highs = []
        self._lows = []
        self._fractal_high = None
        self._fractal_low = None
        self._entry_order = None
        self._target = None
        self._trail_stop = None

    def OnReseted(self):
        super(adaptive_fractal_grid_scalping_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(adaptive_fractal_grid_scalping_strategy, self).OnStarted2(time)

        self._reset_state()

        atr = AverageTrueRange()
        atr.Length = self._atr_length.Value
        sma = SimpleMovingAverage()
        sma.Length = self._sma_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(atr, sma, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sma)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, atr_value, sma_value):
        if candle.State != CandleStates.Finished:
            return

        self._update_fractals(candle)

        if not atr_value.IsFormed or not sma_value.IsFormed:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        atr = atr_value.GetValue[Decimal](None)
        sma = sma_value.GetValue[Decimal](None)
        close = candle.ClosePrice

        if self.Position != 0:
            self._cancel_entry()
            self._manage_position(candle, atr)
            return

        self._target = None
        self._trail_stop = None

        # A pending order that is still being registered is left alone until its state is known.
        if self._entry_order is not None and self._entry_order.State != OrderStates.Active and self._entry_order.State != OrderStates.Done and self._entry_order.State != OrderStates.Failed:
            return

        self._cancel_entry()

        if atr <= Decimal(self._volatility_threshold.Value):
            return

        offset = atr * Decimal(self._grid_multiplier_low.Value)

        if close > sma and self._fractal_low is not None:
            self._entry_order = self.BuyLimit(self._round_price(self._fractal_low - offset), self.Volume)
        elif close < sma and self._fractal_high is not None:
            self._entry_order = self.SellLimit(self._round_price(self._fractal_high + offset), self.Volume)

    def _manage_position(self, candle, atr):
        trail_distance = atr * Decimal(self._trail_stop_multiplier.Value)
        grid_high = Decimal(self._grid_multiplier_high.Value)

        if self.Position > 0:
            if self._target is None and self._fractal_high is not None:
                self._target = self._fractal_high + atr * grid_high
            new_stop = candle.HighPrice - trail_distance
            self._trail_stop = new_stop if self._trail_stop is None else max(self._trail_stop, new_stop)

            if (self._target is not None and candle.HighPrice >= self._target) or candle.ClosePrice <= self._trail_stop:
                self.SellMarket(self.Position)
        else:
            if self._target is None and self._fractal_low is not None:
                self._target = self._fractal_low - atr * grid_high
            new_stop = candle.LowPrice + trail_distance
            self._trail_stop = new_stop if self._trail_stop is None else min(self._trail_stop, new_stop)

            if (self._target is not None and candle.LowPrice <= self._target) or candle.ClosePrice >= self._trail_stop:
                self.BuyMarket(-self.Position)

    def _update_fractals(self, candle):
        self._highs.append(candle.HighPrice)
        self._lows.append(candle.LowPrice)

        if len(self._highs) > 5:
            self._highs.pop(0)
            self._lows.pop(0)

        if len(self._highs) < 5:
            return

        # The middle bar is a fractal when it beats the two bars on each side.
        h = self._highs
        if h[2] > h[0] and h[2] > h[1] and h[2] > h[3] and h[2] > h[4]:
            self._fractal_high = h[2]

        l = self._lows
        if l[2] < l[0] and l[2] < l[1] and l[2] < l[3] and l[2] < l[4]:
            self._fractal_low = l[2]

    def _cancel_entry(self):
        if self._entry_order is not None and self._entry_order.State == OrderStates.Active:
            self.CancelOrder(self._entry_order)
        self._entry_order = None

    def _round_price(self, price):
        step = self.Security.PriceStep
        if step is None or step <= 0:
            return price
        return Math.Round(price / step) * step

    def CreateClone(self):
        return adaptive_fractal_grid_scalping_strategy()
