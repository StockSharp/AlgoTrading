import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import Highest, Lowest, AverageTrueRange, SimpleMovingAverage, StandardDeviation
from StockSharp.Algo.Strategies import Strategy

class donchian_volatility_contraction_strategy(Strategy):
    """
    Breakout strategy that waits for Donchian channel contraction before trading
    a break of the previous channel. Uses ATR for breakout confirmation.
    """

    def __init__(self):
        super(donchian_volatility_contraction_strategy, self).__init__()
        self._donchian_period = self.Param("DonchianPeriod", 20) \
            .SetDisplay("Donchian Period", "Period for the Donchian channel", "Indicators")
        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetDisplay("ATR Period", "Period for the ATR", "Indicators")
        self._volatility_factor = self.Param("VolatilityFactor", 0.8) \
            .SetDisplay("Volatility Factor", "Std dev multiplier for contraction detection", "Signals")
        self._cooldown_bars = self.Param("CooldownBars", 72) \
            .SetDisplay("Cooldown Bars", "Bars to wait after each order", "Risk")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._previous_high = 0.0
        self._previous_low = 0.0
        self._previous_width = 0.0
        self._width_average_value = 0.0
        self._width_std_dev_value = 0.0
        self._is_initialized = False
        self._cooldown = 0
        self._width_average = None
        self._width_std_dev = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(donchian_volatility_contraction_strategy, self).OnReseted()
        self._previous_high = 0.0
        self._previous_low = 0.0
        self._previous_width = 0.0
        self._width_average_value = 0.0
        self._width_std_dev_value = 0.0
        self._is_initialized = False
        self._cooldown = 0

    def OnStarted(self, time):
        super(donchian_volatility_contraction_strategy, self).OnStarted(time)

        donchian_high = Highest()
        donchian_high.Length = self._donchian_period.Value
        donchian_low = Lowest()
        donchian_low.Length = self._donchian_period.Value
        atr = AverageTrueRange()
        atr.Length = self._atr_period.Value
        self._width_average = SimpleMovingAverage()
        self._width_average.Length = self._donchian_period.Value
        self._width_std_dev = StandardDeviation()
        self._width_std_dev.Length = self._donchian_period.Value
        self._is_initialized = False
        self._cooldown = 0

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(donchian_high, donchian_low, atr, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, atr)
            self.DrawOwnTrades(area)

        sl_pct = self._stop_loss_percent.Value
        self.StartProtection(
            Unit(0.0, UnitTypes.Absolute),
            Unit(float(sl_pct), UnitTypes.Percent)
        )

    def _process_candle(self, candle, donchian_high, donchian_low, atr_value):
        if candle.State != CandleStates.Finished:
            return

        dh = float(donchian_high)
        dl = float(donchian_low)
        atr_val = float(atr_value)

        if not self._is_initialized:
            self._previous_high = dh
            self._previous_low = dl
            self._previous_width = dh - dl
            self._is_initialized = True
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            self._update_channel(candle, dh, dl)
            return

        price = float(candle.ClosePrice)
        channel_middle = (self._previous_high + self._previous_low) / 2.0

        step = 1.0
        if self.Security is not None and self.Security.PriceStep is not None:
            step = float(self.Security.PriceStep)
        if step <= 0:
            step = 1.0

        vol_threshold = max(self._width_average_value - self._volatility_factor.Value * self._width_std_dev_value, step)
        is_contracted = self._previous_width <= vol_threshold

        if self.Position == 0:
            if is_contracted and price >= self._previous_high + atr_val * 0.05:
                self.BuyMarket()
                self._cooldown = self._cooldown_bars.Value
            elif is_contracted and price <= self._previous_low - atr_val * 0.05:
                self.SellMarket()
                self._cooldown = self._cooldown_bars.Value
        elif self.Position > 0:
            if price <= channel_middle:
                self.SellMarket()
                self._cooldown = self._cooldown_bars.Value
        elif self.Position < 0:
            if price >= channel_middle:
                self.BuyMarket()
                self._cooldown = self._cooldown_bars.Value

        self._update_channel(candle, dh, dl)

    def _update_channel(self, candle, dh, dl):
        self._previous_high = dh
        self._previous_low = dl
        self._previous_width = dh - dl

    def CreateClone(self):
        return donchian_volatility_contraction_strategy()
