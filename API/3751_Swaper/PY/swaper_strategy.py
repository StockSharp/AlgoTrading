import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class swaper_strategy(Strategy):
    """Swap-based mean reversion strategy. Calculates a synthetic fair value using
    realized PnL and adjusts position volume based on the deviation from that value."""

    def __init__(self):
        super(swaper_strategy, self).__init__()

        self._experts = self.Param("Experts", 1.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Experts", "Weighting coefficient applied to the synthetic fair value", "General")
        self._begin_price = self.Param("BeginPrice", 1.8014) \
            .SetGreaterThanZero() \
            .SetDisplay("Begin Price", "Initial price used to recreate the historical balance", "General")
        self._magic_number = self.Param("MagicNumber", 777) \
            .SetDisplay("Magic Number", "Identifier kept for compatibility with the MetaTrader expert", "General")
        self._base_units = self.Param("BaseUnits", 1000.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Base Units", "Synthetic account units used when calculating the fair value denominator", "Money Management")
        self._contract_multiplier = self.Param("ContractMultiplier", 10.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Contract Multiplier", "Value multiplier applied to realized and unrealized profit", "Money Management")
        self._margin_per_lot = self.Param("MarginPerLot", 1000.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Margin Per Lot", "Approximate capital required to keep one lot open", "Money Management")
        self._fallback_spread_steps = self.Param("FallbackSpreadSteps", 1.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Fallback Spread (steps)", "Spread expressed in price steps when level-one data is unavailable", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Primary timeframe that replaces the tick-based loop of the original expert", "Data")

        self._initial_capital = 0.0
        self._realized_pnl = 0.0
        self._position_volume = 0.0
        self._average_price = 0.0
        self._previous_candle = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def Experts(self):
        return self._experts.Value

    @property
    def BeginPrice(self):
        return self._begin_price.Value

    @property
    def BaseUnits(self):
        return self._base_units.Value

    @property
    def ContractMultiplier(self):
        return self._contract_multiplier.Value

    @property
    def MarginPerLot(self):
        return self._margin_per_lot.Value

    @property
    def FallbackSpreadSteps(self):
        return self._fallback_spread_steps.Value

    def OnReseted(self):
        super(swaper_strategy, self).OnReseted()
        self._initial_capital = 0.0
        self._realized_pnl = 0.0
        self._position_volume = 0.0
        self._average_price = 0.0
        self._previous_candle = None

    def OnStarted(self, time):
        super(swaper_strategy, self).OnStarted(time)

        self._initial_capital = float(self.BaseUnits) * float(self.BeginPrice)
        self._realized_pnl = 0.0
        self._position_volume = 0.0
        self._average_price = 0.0
        self._previous_candle = None

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._process_candle).Start()

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        if self._previous_candle is None:
            self._previous_candle = candle
            return

        price_step = 0.0001
        if self.Security is not None and self.Security.PriceStep is not None:
            ps = float(self.Security.PriceStep)
            if ps > 0:
                price_step = ps

        spread = self._get_spread(price_step)
        high = max(float(candle.HighPrice), float(self._previous_candle.HighPrice))
        low = min(float(candle.LowPrice), float(self._previous_candle.LowPrice))

        if high <= 0 or low <= 0:
            self._previous_candle = candle
            return

        denominator = high + spread
        if denominator <= 0:
            self._previous_candle = candle
            return

        com = self._calculate_denominator()
        if com == 0:
            self._previous_candle = candle
            return

        close_price = float(candle.ClosePrice)
        money = self._calculate_synthetic_capital(close_price)
        experts_weight = float(self.Experts)
        dt = (money / denominator - com) * experts_weight / (experts_weight + 1.0)

        if dt < 0:
            alt_denominator = money / low if low > 0 else 0
            if alt_denominator == 0:
                self._previous_candle = candle
                return

            dt_alt = (com - alt_denominator) * experts_weight / (experts_weight + 1.0)

            if dt_alt < 1.0:
                self._close_position_if_exists()
                self._previous_candle = candle
                return

            lots = Math.Floor(dt_alt) / 10.0
            self._adjust_short(lots, close_price)
        else:
            if dt < 1.0:
                self._close_position_if_exists()
                self._previous_candle = candle
                return

            lots = Math.Floor(dt) / 10.0
            self._adjust_long(lots, close_price)

        self._previous_candle = candle

    def _calculate_synthetic_capital(self, current_price):
        multiplier = float(self.ContractMultiplier)
        unrealized = self._position_volume * current_price * multiplier
        return self._initial_capital + self._realized_pnl + unrealized

    def _calculate_denominator(self):
        return float(self.BaseUnits) + float(self.ContractMultiplier) * self._position_volume

    def _get_spread(self, price_step):
        steps = float(self.FallbackSpreadSteps)
        if steps <= 0:
            steps = 1.0
        return steps * price_step

    def _adjust_short(self, target_lots, current_price):
        if target_lots <= 0:
            return

        if self.Position > 0:
            reduce = min(float(self.Position), target_lots)
            if reduce > 0:
                self.SellMarket(reduce)
                self._update_position_tracking(-reduce, current_price)
            return

        current_short = abs(float(self.Position)) if self.Position < 0 else 0.0
        if current_short >= target_lots:
            return

        additional = target_lots - current_short
        tradable = self._get_tradable_volume(additional)
        if tradable > 0:
            self.SellMarket(tradable)
            self._update_position_tracking(-tradable, current_price)

    def _adjust_long(self, target_lots, current_price):
        if target_lots <= 0:
            return

        if self.Position < 0:
            reduce = min(abs(float(self.Position)), target_lots)
            if reduce > 0:
                self.BuyMarket(reduce)
                self._update_position_tracking(reduce, current_price)
            return

        current_long = float(self.Position) if self.Position > 0 else 0.0
        if current_long >= target_lots:
            return

        additional = target_lots - current_long
        tradable = self._get_tradable_volume(additional)
        if tradable > 0:
            self.BuyMarket(tradable)
            self._update_position_tracking(tradable, current_price)

    def _close_position_if_exists(self):
        volume = abs(float(self.Position))
        if volume <= 0:
            return

        if self.Position > 0:
            self.SellMarket(volume)
        else:
            self.BuyMarket(volume)

        self._position_volume = 0.0
        self._average_price = 0.0

    def _get_tradable_volume(self, desired_lots):
        if desired_lots <= 0:
            return 0.0

        margin_per_lot = float(self.MarginPerLot)
        available_capital = self._initial_capital + self._realized_pnl

        if margin_per_lot <= 0 or available_capital <= 0:
            return Math.Floor(desired_lots * 10.0) / 10.0

        max_lots = Math.Floor(available_capital / margin_per_lot * 10.0) / 10.0
        if max_lots <= 0:
            return 0.0

        return min(desired_lots, max_lots)

    def _update_position_tracking(self, signed_volume, price):
        if self._position_volume == 0 or \
                (self._position_volume > 0 and signed_volume > 0) or \
                (self._position_volume < 0 and signed_volume < 0):
            total_volume = self._position_volume + signed_volume
            if total_volume == 0:
                self._position_volume = 0.0
                self._average_price = 0.0
            else:
                weighted = self._average_price * self._position_volume + price * signed_volume
                self._position_volume = total_volume
                self._average_price = weighted / total_volume
            return

        closing_volume = min(abs(signed_volume), abs(self._position_volume))
        sign = 1.0 if self._position_volume > 0 else -1.0
        realized = (price - self._average_price) * closing_volume * sign * float(self.ContractMultiplier)
        self._realized_pnl += realized

        remaining = self._position_volume + signed_volume
        if remaining == 0:
            self._position_volume = 0.0
            self._average_price = 0.0
            return

        if (self._position_volume > 0 and remaining > 0) or (self._position_volume < 0 and remaining < 0):
            self._position_volume = remaining
            return

        self._position_volume = remaining
        self._average_price = price

    def CreateClone(self):
        return swaper_strategy()
