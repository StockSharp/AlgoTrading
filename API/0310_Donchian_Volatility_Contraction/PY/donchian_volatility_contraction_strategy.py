import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import Highest, Lowest, AverageTrueRange, SimpleMovingAverage, StandardDeviation, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy


class donchian_volatility_contraction_strategy(Strategy):
    """
    Breakout strategy that waits for Donchian channel contraction before trading a break of the previous channel.
    """

    def __init__(self):
        super(donchian_volatility_contraction_strategy, self).__init__()

        self._donchian_period = self.Param("DonchianPeriod", 20) \
            .SetRange(2, 100) \
            .SetDisplay("Donchian Period", "Period for the Donchian channel", "Indicators")

        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetRange(2, 100) \
            .SetDisplay("ATR Period", "Period for the ATR", "Indicators")

        self._volatility_factor = self.Param("VolatilityFactor", 0.8) \
            .SetRange(0.1, 5.0) \
            .SetDisplay("Volatility Factor", "Standard deviation multiplier for contraction detection", "Signals")

        self._cooldown_bars = self.Param("CooldownBars", 72) \
            .SetRange(1, 500) \
            .SetDisplay("Cooldown Bars", "Bars to wait after each order", "Risk")

        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetRange(0.5, 10.0) \
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
        donchian_high.Length = int(self._donchian_period.Value)
        donchian_low = Lowest()
        donchian_low.Length = int(self._donchian_period.Value)
        atr = AverageTrueRange()
        atr.Length = int(self._atr_period.Value)

        self._width_average = SimpleMovingAverage()
        self._width_average.Length = int(self._donchian_period.Value)
        self._width_std_dev = StandardDeviation()
        self._width_std_dev.Length = int(self._donchian_period.Value)
        self._is_initialized = False
        self._cooldown = 0

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(donchian_high, donchian_low, atr, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, atr)
            self.DrawOwnTrades(area)

        self.StartProtection(
            Unit(0, UnitTypes.Absolute),
            Unit(self._stop_loss_percent.Value, UnitTypes.Percent),
            False
        )

    def _process_width(self, width, open_time):
        avg_input = DecimalIndicatorValue(self._width_average, Decimal(width), open_time)
        avg_input.IsFinal = True
        self._width_average_value = float(self._width_average.Process(avg_input))

        std_input = DecimalIndicatorValue(self._width_std_dev, Decimal(width), open_time)
        std_input.IsFinal = True
        self._width_std_dev_value = float(self._width_std_dev.Process(std_input))

    def _process_candle(self, candle, donchian_high_val, donchian_low_val, atr_value):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            dh = float(donchian_high_val)
            dl = float(donchian_low_val)
            width = dh - dl
            self._previous_high = dh
            self._previous_low = dl
            self._previous_width = width
            self._process_width(width, candle.OpenTime)
            if not self._is_initialized:
                self._is_initialized = True
            return

        dh = float(donchian_high_val)
        dl = float(donchian_low_val)
        atr_val = float(atr_value)

        if not self._is_initialized:
            self._previous_high = dh
            self._previous_low = dl
            self._previous_width = dh - dl
            self._process_width(self._previous_width, candle.OpenTime)
            self._is_initialized = True
            return

        if not self._width_average.IsFormed or not self._width_std_dev.IsFormed:
            self._previous_high = dh
            self._previous_low = dl
            self._previous_width = dh - dl
            self._process_width(self._previous_width, candle.OpenTime)
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            self._update_channel_stats(candle, dh, dl)
            return

        price = float(candle.ClosePrice)
        channel_middle = (self._previous_high + self._previous_low) / 2.0

        step = 1.0
        if self.Security is not None and self.Security.PriceStep is not None:
            step = float(self.Security.PriceStep)
        if step <= 0:
            step = 1.0

        vf = float(self._volatility_factor.Value)
        vol_threshold = max(self._width_average_value - vf * self._width_std_dev_value, step)
        is_contracted = self._previous_width <= vol_threshold

        cd = int(self._cooldown_bars.Value)

        if self.Position == 0:
            if is_contracted and price >= self._previous_high + atr_val * 0.05:
                self.BuyMarket()
                self._cooldown = cd
            elif is_contracted and price <= self._previous_low - atr_val * 0.05:
                self.SellMarket()
                self._cooldown = cd
        elif self.Position > 0:
            if price <= channel_middle:
                self.SellMarket(Math.Abs(self.Position))
                self._cooldown = cd
        elif self.Position < 0:
            if price >= channel_middle:
                self.BuyMarket(Math.Abs(self.Position))
                self._cooldown = cd

        self._update_channel_stats(candle, dh, dl)

    def _update_channel_stats(self, candle, dh, dl):
        self._previous_high = dh
        self._previous_low = dl
        self._previous_width = dh - dl
        self._process_width(self._previous_width, candle.OpenTime)

    def CreateClone(self):
        return donchian_volatility_contraction_strategy()
