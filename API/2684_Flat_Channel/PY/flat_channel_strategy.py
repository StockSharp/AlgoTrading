import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import StandardDeviation, DonchianChannels, DecimalIndicatorValue


class flat_channel_strategy(Strategy):
    """Flat channel breakout: detects consolidation via falling StdDev, then trades channel breakouts."""

    def __init__(self):
        super(flat_channel_strategy, self).__init__()

        self._std_dev_period = self.Param("StdDevPeriod", 37) \
            .SetGreaterThanZero() \
            .SetDisplay("StdDev Period", "Standard deviation indicator period", "Indicators")
        self._flat_bars = self.Param("FlatBars", 2) \
            .SetGreaterThanZero() \
            .SetDisplay("Flat Bars", "Minimum bars in flat state", "Indicators")
        self._channel_min_pips = self.Param("ChannelMinPips", 10.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Channel Min Pips", "Minimum channel width in pips", "Indicators")
        self._channel_max_pips = self.Param("ChannelMaxPips", 100000.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Channel Max Pips", "Maximum channel width in pips", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Primary candle type", "General")

        self._previous_std_dev = 0.0
        self._flat_bar_count = 0
        self._channel_high = 0.0
        self._channel_low = 0.0
        self._pending_buy_price = None
        self._pending_sell_price = None
        self._entry_price = 0.0
        self._long_stop = 0.0
        self._long_take = 0.0
        self._short_stop = 0.0
        self._short_take = 0.0

    @property
    def StdDevPeriod(self):
        return int(self._std_dev_period.Value)
    @property
    def FlatBars(self):
        return int(self._flat_bars.Value)
    @property
    def ChannelMinPips(self):
        return float(self._channel_min_pips.Value)
    @property
    def ChannelMaxPips(self):
        return float(self._channel_max_pips.Value)
    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnStarted(self, time):
        super(flat_channel_strategy, self).OnStarted(time)

        self._previous_std_dev = 0.0
        self._flat_bar_count = 0
        self._channel_high = 0.0
        self._channel_low = 0.0
        self._pending_buy_price = None
        self._pending_sell_price = None
        self._entry_price = 0.0
        self._long_stop = 0.0
        self._long_take = 0.0
        self._short_stop = 0.0
        self._short_take = 0.0

        self._std_dev = StandardDeviation()
        self._std_dev.Length = self.StdDevPeriod
        self._donchian = DonchianChannels()
        self._donchian.Length = self.FlatBars

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(self._donchian, self.process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._donchian)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, channel_value):
        if candle.State != CandleStates.Finished:
            return

        h = float(candle.HighPrice)
        lo = float(candle.LowPrice)
        median_price = (h + lo) / 2.0

        sd_iv = DecimalIndicatorValue(self._std_dev, Decimal(median_price), candle.ServerTime)
        sd_iv.IsFinal = True
        std_dev_value = float(self._std_dev.Process(sd_iv).Value)

        if not self._std_dev.IsFormed:
            self._previous_std_dev = std_dev_value
            return

        upper_val = channel_value.UpperBand
        lower_val = channel_value.LowerBand
        if upper_val is None or lower_val is None:
            self._previous_std_dev = std_dev_value
            return

        upper = float(upper_val)
        lower = float(lower_val)

        # Update flat state
        self._update_std_dev_state(std_dev_value, upper, lower, candle)

        # Check pending entries
        self._check_pending_entries(candle)

        # Manage position
        self._manage_position(candle)

        # If flat and no position, setup pending breakout entries
        if self.Position == 0 and self._flat_bar_count >= self.FlatBars and self._channel_high > self._channel_low:
            channel_width = self._channel_high - self._channel_low
            sec = self.Security
            price_step = float(sec.PriceStep) if sec is not None and sec.PriceStep is not None and float(sec.PriceStep) > 0 else 0.01
            min_width = self.ChannelMinPips * price_step
            max_width = self.ChannelMaxPips * price_step

            if channel_width >= min_width and channel_width <= max_width:
                self._pending_buy_price = self._channel_high
                self._pending_sell_price = self._channel_low
                self._long_stop = self._channel_high - channel_width * 2.0
                self._long_take = self._channel_high + channel_width
                self._short_stop = self._channel_low + channel_width * 2.0
                self._short_take = self._channel_low - channel_width

        self._previous_std_dev = std_dev_value

    def _update_std_dev_state(self, std_dev_value, upper, lower, candle):
        if self._previous_std_dev == 0:
            self._previous_std_dev = std_dev_value
            return

        h = float(candle.HighPrice)
        lo = float(candle.LowPrice)

        if std_dev_value < self._previous_std_dev:
            self._flat_bar_count += 1

            if self._flat_bar_count == self.FlatBars:
                self._channel_high = upper
                self._channel_low = lower
            elif self._flat_bar_count > self.FlatBars:
                if h > self._channel_high:
                    self._channel_high = h
                if lo < self._channel_low:
                    self._channel_low = lo
        elif std_dev_value > self._previous_std_dev:
            self._flat_bar_count = 0
            self._channel_high = 0.0
            self._channel_low = 0.0
            self._pending_buy_price = None
            self._pending_sell_price = None
        elif self._flat_bar_count >= self.FlatBars and self._channel_high <= self._channel_low:
            self._channel_high = upper
            self._channel_low = lower

    def _check_pending_entries(self, candle):
        if self.Position != 0:
            return

        h = float(candle.HighPrice)
        lo = float(candle.LowPrice)

        if self._pending_buy_price is not None and h >= self._pending_buy_price:
            self.BuyMarket()
            self._entry_price = self._pending_buy_price
            self._pending_buy_price = None
            self._pending_sell_price = None
            return

        if self._pending_sell_price is not None and lo <= self._pending_sell_price:
            self.SellMarket()
            self._entry_price = self._pending_sell_price
            self._pending_buy_price = None
            self._pending_sell_price = None

    def _manage_position(self, candle):
        h = float(candle.HighPrice)
        lo = float(candle.LowPrice)

        if self.Position > 0:
            if self._long_stop > 0 and lo <= self._long_stop:
                self.SellMarket()
                self._reset_position_state()
                return
            if self._long_take > 0 and h >= self._long_take:
                self.SellMarket()
                self._reset_position_state()
        elif self.Position < 0:
            if self._short_stop > 0 and h >= self._short_stop:
                self.BuyMarket()
                self._reset_position_state()
                return
            if self._short_take > 0 and lo <= self._short_take:
                self.BuyMarket()
                self._reset_position_state()

    def _reset_position_state(self):
        self._entry_price = 0.0
        self._long_stop = 0.0
        self._long_take = 0.0
        self._short_stop = 0.0
        self._short_take = 0.0
        self._pending_buy_price = None
        self._pending_sell_price = None

    def OnReseted(self):
        super(flat_channel_strategy, self).OnReseted()
        self._previous_std_dev = 0.0
        self._flat_bar_count = 0
        self._channel_high = 0.0
        self._channel_low = 0.0
        self._pending_buy_price = None
        self._pending_sell_price = None
        self._entry_price = 0.0
        self._long_stop = 0.0
        self._long_take = 0.0
        self._short_stop = 0.0
        self._short_take = 0.0

    def CreateClone(self):
        return flat_channel_strategy()
