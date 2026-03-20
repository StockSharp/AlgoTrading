import clr
import math

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange, StandardDeviation
from StockSharp.Algo.Strategies import Strategy


class _AdaptiveRenkoProcessor(object):
    TREND_NONE = 0
    TREND_UP = 1
    TREND_DOWN = -1

    def __init__(self):
        self._history = []
        self._initialized = False
        self._up = 0.0
        self._down = 0.0
        self._brick = 0.0
        self._trend = self.TREND_NONE

    def reset(self):
        self._history = []
        self._initialized = False
        self._up = 0.0
        self._down = 0.0
        self._brick = 0.0
        self._trend = self.TREND_NONE

    def process(self, candle, volatility, sensitivity, min_brick_points, price_mode_close, signal_offset, step):
        if price_mode_close:
            high = float(candle.ClosePrice)
            low = float(candle.ClosePrice)
        else:
            high = float(candle.HighPrice)
            low = float(candle.LowPrice)

        min_brick = max(min_brick_points * step, 0.0)

        if not self._initialized:
            rng = max(high - low, 0.0)
            initial_brick = max(sensitivity * rng, min_brick)
            self._up = high
            self._down = low
            self._brick = initial_brick if initial_brick > 0.0 else min_brick
            self._trend = self.TREND_NONE
            self._initialized = True
            snapshot = (self.TREND_NONE, None, None)
            self._append_snapshot(snapshot, signal_offset)
            return snapshot

        up = self._up
        down = self._down
        brick = self._brick if self._brick > 0.0 else min_brick
        trend = self._trend

        adjusted_brick = max(sensitivity * abs(volatility), min_brick)
        if adjusted_brick <= 0.0:
            adjusted_brick = min_brick
        if brick <= 0.0:
            brick = adjusted_brick if adjusted_brick > 0.0 else min_brick

        if high > up + brick:
            if brick > 0.0:
                diff = high - up
                bricks = math.floor(diff / brick)
                if bricks < 1.0:
                    bricks = 1.0
                up += bricks * brick
            else:
                up = high
            brick = adjusted_brick
            down = up - brick

        if low < down - brick:
            if brick > 0.0:
                diff = down - low
                bricks = math.floor(diff / brick)
                if bricks < 1.0:
                    bricks = 1.0
                down -= bricks * brick
            else:
                down = low
            brick = adjusted_brick
            up = down + brick

        if self._up < up:
            trend = self.TREND_UP
        if self._down > down:
            trend = self.TREND_DOWN

        self._up = up
        self._down = down
        self._brick = brick
        self._trend = trend

        support = down - brick if trend == self.TREND_UP else None
        resistance = up + brick if trend == self.TREND_DOWN else None

        snapshot = (trend, support, resistance)
        self._append_snapshot(snapshot, signal_offset)
        return snapshot

    def get_snapshot(self, shift):
        if shift < 0:
            shift = 0
        index = len(self._history) - 1 - shift
        if index < 0:
            return None
        return self._history[index]

    def _append_snapshot(self, snapshot, signal_offset):
        self._history.append(snapshot)
        max_history = max(signal_offset + 3, 8)
        overflow = len(self._history) - max_history
        if overflow > 0:
            del self._history[:overflow]


class adaptive_renko_duplex_strategy(Strategy):
    def __init__(self):
        super(adaptive_renko_duplex_strategy, self).__init__()

        self._long_candle_type = self.Param("LongCandleType", DataType.TimeFrame(TimeSpan.FromDays(1))) \
            .SetDisplay("Long Candle Type", "Timeframe used to derive long-side signals", "Long Side")
        self._short_candle_type = self.Param("ShortCandleType", DataType.TimeFrame(TimeSpan.FromDays(1))) \
            .SetDisplay("Short Candle Type", "Timeframe used to derive short-side signals", "Short Side")
        self._long_volatility_period = self.Param("LongVolatilityPeriod", 10) \
            .SetDisplay("Long Volatility Period", "Lookback period for volatility calculation", "Long Side")
        self._short_volatility_period = self.Param("ShortVolatilityPeriod", 10) \
            .SetDisplay("Short Volatility Period", "Lookback period for volatility calculation", "Short Side")
        self._long_sensitivity = self.Param("LongSensitivity", 1.0) \
            .SetDisplay("Long Sensitivity", "Multiplier applied to volatility for long bricks", "Long Side")
        self._short_sensitivity = self.Param("ShortSensitivity", 1.0) \
            .SetDisplay("Short Sensitivity", "Multiplier applied to volatility for short bricks", "Short Side")
        self._long_price_mode_close = self.Param("LongPriceModeClose", True) \
            .SetDisplay("Long Price Mode Close", "True=Close, False=HighLow for long bricks", "Long Side")
        self._short_price_mode_close = self.Param("ShortPriceModeClose", True) \
            .SetDisplay("Short Price Mode Close", "True=Close, False=HighLow for short bricks", "Short Side")
        self._long_minimum_brick_points = self.Param("LongMinimumBrickPoints", 5.0) \
            .SetDisplay("Long Minimum Brick", "Minimal brick height in points for long bricks", "Long Side")
        self._short_minimum_brick_points = self.Param("ShortMinimumBrickPoints", 5.0) \
            .SetDisplay("Short Minimum Brick", "Minimal brick height in points for short bricks", "Short Side")
        self._long_signal_bar_offset = self.Param("LongSignalBarOffset", 2) \
            .SetDisplay("Long Signal Offset", "Number of closed bars to delay long signals", "Long Side")
        self._short_signal_bar_offset = self.Param("ShortSignalBarOffset", 2) \
            .SetDisplay("Short Signal Offset", "Number of closed bars to delay short signals", "Short Side")
        self._long_stop_loss_points = self.Param("LongStopLossPoints", 1000.0) \
            .SetDisplay("Long Stop Loss", "Protective stop distance in points for long trades", "Risk")
        self._long_take_profit_points = self.Param("LongTakeProfitPoints", 2000.0) \
            .SetDisplay("Long Take Profit", "Profit target distance in points for long trades", "Risk")
        self._short_stop_loss_points = self.Param("ShortStopLossPoints", 1000.0) \
            .SetDisplay("Short Stop Loss", "Protective stop distance in points for short trades", "Risk")
        self._short_take_profit_points = self.Param("ShortTakeProfitPoints", 2000.0) \
            .SetDisplay("Short Take Profit", "Profit target distance in points for short trades", "Risk")
        self._use_atr_long = self.Param("UseAtrLong", True) \
            .SetDisplay("Use ATR Long", "True=ATR, False=StdDev for long volatility", "Long Side")
        self._use_atr_short = self.Param("UseAtrShort", True) \
            .SetDisplay("Use ATR Short", "True=ATR, False=StdDev for short volatility", "Short Side")

        self._long_processor = _AdaptiveRenkoProcessor()
        self._short_processor = _AdaptiveRenkoProcessor()
        self._long_entry_price = None
        self._short_entry_price = None
        self._long_vol_indicator = None
        self._short_vol_indicator = None

    @property
    def LongCandleType(self):
        return self._long_candle_type.Value
    @property
    def ShortCandleType(self):
        return self._short_candle_type.Value
    @property
    def LongVolatilityPeriod(self):
        return self._long_volatility_period.Value
    @property
    def ShortVolatilityPeriod(self):
        return self._short_volatility_period.Value
    @property
    def LongSensitivity(self):
        return self._long_sensitivity.Value
    @property
    def ShortSensitivity(self):
        return self._short_sensitivity.Value
    @property
    def LongPriceModeClose(self):
        return self._long_price_mode_close.Value
    @property
    def ShortPriceModeClose(self):
        return self._short_price_mode_close.Value
    @property
    def LongMinimumBrickPoints(self):
        return self._long_minimum_brick_points.Value
    @property
    def ShortMinimumBrickPoints(self):
        return self._short_minimum_brick_points.Value
    @property
    def LongSignalBarOffset(self):
        return self._long_signal_bar_offset.Value
    @property
    def ShortSignalBarOffset(self):
        return self._short_signal_bar_offset.Value
    @property
    def LongStopLossPoints(self):
        return self._long_stop_loss_points.Value
    @property
    def LongTakeProfitPoints(self):
        return self._long_take_profit_points.Value
    @property
    def ShortStopLossPoints(self):
        return self._short_stop_loss_points.Value
    @property
    def ShortTakeProfitPoints(self):
        return self._short_take_profit_points.Value
    @property
    def UseAtrLong(self):
        return self._use_atr_long.Value
    @property
    def UseAtrShort(self):
        return self._use_atr_short.Value

    def OnReseted(self):
        super(adaptive_renko_duplex_strategy, self).OnReseted()
        self._long_processor.reset()
        self._short_processor.reset()
        self._long_entry_price = None
        self._short_entry_price = None

    def OnStarted(self, time):
        super(adaptive_renko_duplex_strategy, self).OnStarted(time)
        self._long_processor.reset()
        self._short_processor.reset()
        self._long_entry_price = None
        self._short_entry_price = None

        if self.UseAtrLong:
            self._long_vol_indicator = AverageTrueRange()
        else:
            self._long_vol_indicator = StandardDeviation()
        self._long_vol_indicator.Length = self.LongVolatilityPeriod

        if self.UseAtrShort:
            self._short_vol_indicator = AverageTrueRange()
        else:
            self._short_vol_indicator = StandardDeviation()
        self._short_vol_indicator.Length = self.ShortVolatilityPeriod

        long_subscription = self.SubscribeCandles(self.LongCandleType)
        long_subscription.Bind(self._on_long_candle).Start()

        short_subscription = self.SubscribeCandles(self.ShortCandleType)
        short_subscription.Bind(self._on_short_candle).Start()

    def _on_long_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        self._manage_long_risk(candle)

        vol_result = self._long_vol_indicator.Process(candle)
        if not vol_result.IsFinal:
            return

        volatility = float(vol_result.ToDecimal())
        step = self._get_price_step()
        self._long_processor.process(
            candle, volatility,
            float(self.LongSensitivity),
            float(self.LongMinimumBrickPoints),
            self.LongPriceModeClose,
            self.LongSignalBarOffset,
            step
        )

        signal = self._long_processor.get_snapshot(self.LongSignalBarOffset)
        if signal is None:
            return

        trend, support, resistance = signal

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self.Position > 0 and trend == _AdaptiveRenkoProcessor.TREND_DOWN:
            self.SellMarket()
            self._long_entry_price = None

        if trend == _AdaptiveRenkoProcessor.TREND_UP and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._long_entry_price = float(candle.ClosePrice)
            self._short_entry_price = None

    def _on_short_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        self._manage_short_risk(candle)

        vol_result = self._short_vol_indicator.Process(candle)
        if not vol_result.IsFinal:
            return

        volatility = float(vol_result.ToDecimal())
        step = self._get_price_step()
        self._short_processor.process(
            candle, volatility,
            float(self.ShortSensitivity),
            float(self.ShortMinimumBrickPoints),
            self.ShortPriceModeClose,
            self.ShortSignalBarOffset,
            step
        )

        signal = self._short_processor.get_snapshot(self.ShortSignalBarOffset)
        if signal is None:
            return

        trend, support, resistance = signal

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self.Position < 0 and trend == _AdaptiveRenkoProcessor.TREND_UP:
            self.BuyMarket()
            self._short_entry_price = None

        if trend == _AdaptiveRenkoProcessor.TREND_DOWN and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._short_entry_price = float(candle.ClosePrice)
            self._long_entry_price = None

    def _manage_long_risk(self, candle):
        if self.Position <= 0:
            self._long_entry_price = None
            return

        if self._long_entry_price is None:
            self._long_entry_price = float(candle.ClosePrice)

        step = self._get_price_step()

        sl = float(self.LongStopLossPoints)
        if sl > 0.0:
            stop_dist = sl * step
            if stop_dist > 0.0 and float(candle.LowPrice) <= self._long_entry_price - stop_dist:
                self.SellMarket()
                self._long_entry_price = None
                return

        tp = float(self.LongTakeProfitPoints)
        if tp > 0.0:
            target_dist = tp * step
            if target_dist > 0.0 and float(candle.HighPrice) >= self._long_entry_price + target_dist:
                self.SellMarket()
                self._long_entry_price = None

    def _manage_short_risk(self, candle):
        if self.Position >= 0:
            self._short_entry_price = None
            return

        if self._short_entry_price is None:
            self._short_entry_price = float(candle.ClosePrice)

        step = self._get_price_step()

        sl = float(self.ShortStopLossPoints)
        if sl > 0.0:
            stop_dist = sl * step
            if stop_dist > 0.0 and float(candle.HighPrice) >= self._short_entry_price + stop_dist:
                self.BuyMarket()
                self._short_entry_price = None
                return

        tp = float(self.ShortTakeProfitPoints)
        if tp > 0.0:
            target_dist = tp * step
            if target_dist > 0.0 and float(candle.LowPrice) <= self._short_entry_price - target_dist:
                self.BuyMarket()
                self._short_entry_price = None

    def _get_price_step(self):
        sec = self.Security
        if sec is not None and sec.PriceStep is not None:
            step = float(sec.PriceStep)
            if step > 0.0:
                return step
        return 1.0

    def CreateClone(self):
        return adaptive_renko_duplex_strategy()
