import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal, Array
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import IIndicator, ExponentialMovingAverage, ArnaudLegouxMovingAverage, AverageTrueRange, RelativeStrengthIndex, AverageDirectionalIndex, BollingerBands, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *

ALMA_LENGTH = 9
ALMA_OFFSET = 0.85
ALMA_SIGMA = 6
BB_LENGTH = 20
RSI_LONG_LEVEL = 30.0
RSI_SHORT_LEVEL = 70.0
ADX_LEVEL = 30.0


class alma_ut_bot_confluence_strategy(Strategy):
    """
    ALMA and UT Bot confluence strategy.
    A long opens on a UT Bot buy signal when the close is above the long EMA and ALMA, volume is above its average, RSI is above
    30, ADX is above 30, the close is below the upper Bollinger Band and ATR is at least MinAtr. A short opens when the close
    crosses below the fast EMA while the UT Bot is bearish under the mirrored filters. Entries are spaced by BaseCooldownBars.
    A position closes when the UT Bot trailing stop flips against it or when price reaches the ATR stop-loss or take-profit
    fixed at entry.
    """

    def __init__(self):
        super(alma_ut_bot_confluence_strategy, self).__init__()
        self._fast_ema_length = self.Param("FastEmaLength", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Fast EMA Length", "Fast EMA period", "Trend")
        self._ema_length = self.Param("EmaLength", 72) \
            .SetGreaterThanZero() \
            .SetDisplay("EMA Length", "Long-term EMA period", "Trend")
        self._atr_length = self.Param("AtrLength", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR Length", "ATR period of the stop-loss and take-profit", "Risk")
        self._adx_length = self.Param("AdxLength", 10) \
            .SetGreaterThanZero() \
            .SetDisplay("ADX Length", "ADX period", "Filters")
        self._rsi_length = self.Param("RsiLength", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("RSI Length", "RSI period", "Filters")
        self._bb_multiplier = self.Param("BbMultiplier", 3.0) \
            .SetGreaterThanZero() \
            .SetDisplay("BB Multiplier", "Bollinger Bands width multiplier", "Filters")
        self._stop_loss_atr_multiplier = self.Param("StopLossAtrMultiplier", 5.0) \
            .SetNotNegative() \
            .SetDisplay("Stop Loss ATR", "Stop-loss distance in ATR multiples", "Risk")
        self._take_profit_atr_multiplier = self.Param("TakeProfitAtrMultiplier", 4.0) \
            .SetNotNegative() \
            .SetDisplay("Take Profit ATR", "Take-profit distance in ATR multiples", "Risk")
        self._ut_atr_period = self.Param("UtAtrPeriod", 10) \
            .SetGreaterThanZero() \
            .SetDisplay("UT ATR Period", "ATR period of the UT Bot", "UT Bot")
        self._ut_key_value = self.Param("UtKeyValue", 1.0) \
            .SetGreaterThanZero() \
            .SetDisplay("UT Key Value", "ATR multiplier of the UT Bot trailing stop", "UT Bot")
        self._volume_ma_length = self.Param("VolumeMaLength", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Volume MA Length", "Period of the volume average", "Filters")
        self._base_cooldown_bars = self.Param("BaseCooldownBars", 7) \
            .SetNotNegative() \
            .SetDisplay("Cooldown Bars", "Minimum bars between entries", "Filters")
        self._min_atr = self.Param("MinAtr", 0.005) \
            .SetNotNegative() \
            .SetDisplay("Min ATR", "Minimum ATR required for entries", "Filters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._volume_ma = None
        self._reset_state()

    @property
    def CandleType(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType)]

    def _reset_state(self):
        self._ut_stop = None
        self._prev_close = 0.0
        self._prev_fast_ema = None
        self._bar_index = 0
        self._last_entry_bar = None
        self._stop_price = 0.0
        self._take_price = 0.0

    def OnReseted(self):
        super(alma_ut_bot_confluence_strategy, self).OnReseted()
        self._volume_ma = None
        self._reset_state()

    def OnStarted2(self, time):
        super(alma_ut_bot_confluence_strategy, self).OnStarted2(time)

        self._reset_state()

        ema = ExponentialMovingAverage()
        ema.Length = self._ema_length.Value
        fast_ema = ExponentialMovingAverage()
        fast_ema.Length = self._fast_ema_length.Value
        alma = ArnaudLegouxMovingAverage()
        alma.Length = ALMA_LENGTH
        alma.Offset = Decimal(ALMA_OFFSET)
        alma.Sigma = ALMA_SIGMA
        atr = AverageTrueRange()
        atr.Length = self._atr_length.Value
        ut_atr = AverageTrueRange()
        ut_atr.Length = self._ut_atr_period.Value
        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_length.Value
        adx = AverageDirectionalIndex()
        adx.Length = self._adx_length.Value
        bollinger = BollingerBands()
        bollinger.Length = BB_LENGTH
        bollinger.Width = Decimal(self._bb_multiplier.Value)
        self._volume_ma = SimpleMovingAverage()
        self._volume_ma.Length = self._volume_ma_length.Value

        indicators = Array[IIndicator]([ema, fast_ema, alma, atr, ut_atr, rsi, adx, bollinger])
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(indicators, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawIndicator(area, fast_ema)
            self.DrawIndicator(area, alma)
            self.DrawIndicator(area, bollinger)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, values):
        if candle.State != CandleStates.Finished:
            return

        volume_ma = float(to_decimal(process_float(self._volume_ma, candle.TotalVolume, candle.ServerTime, True)))
        close = float(candle.ClosePrice)
        ut_atr_value = values[4]

        if not ut_atr_value.IsFormed:
            self._prev_close = close
            return

        # UT Bot: an ATR trailing stop that ratchets with price and flips when price closes through it.
        n_loss = float(self._ut_key_value.Value) * float(to_decimal(ut_atr_value))
        prev_stop = self._ut_stop if self._ut_stop is not None else close - n_loss
        prev_close = self._prev_close

        if close > prev_stop and prev_close > prev_stop:
            ut_stop = max(prev_stop, close - n_loss)
        elif close < prev_stop and prev_close < prev_stop:
            ut_stop = min(prev_stop, close + n_loss)
        else:
            ut_stop = close - n_loss if close > prev_stop else close + n_loss

        ut_buy = self._ut_stop is not None and prev_close <= prev_stop and close > ut_stop
        ut_sell = self._ut_stop is not None and prev_close >= prev_stop and close < ut_stop
        ut_bearish = close < ut_stop

        self._ut_stop = ut_stop
        self._prev_close = close
        self._bar_index += 1

        fast_ema_value = values[1]
        prev_fast = self._prev_fast_ema
        if fast_ema_value.IsFormed:
            self._prev_fast_ema = float(to_decimal(fast_ema_value))

        for value in values:
            if not value.IsFormed:
                return

        if not self._volume_ma.IsFormed or prev_fast is None:
            return

        adx_value = values[6].MovingAverage
        upper_band = values[7].UpBand
        lower_band = values[7].LowBand
        if adx_value is None or upper_band is None or lower_band is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        adx = float(adx_value)
        upper_band = float(upper_band)
        lower_band = float(lower_band)
        ema = float(to_decimal(values[0]))
        fast_ema = float(to_decimal(values[1]))
        alma = float(to_decimal(values[2]))
        atr = float(to_decimal(values[3]))
        rsi = float(to_decimal(values[5]))

        cooldown_ok = self._last_entry_bar is None or self._bar_index - self._last_entry_bar >= self._base_cooldown_bars.Value
        common_filters = cooldown_ok and float(candle.TotalVolume) > volume_ma and adx > ADX_LEVEL and atr >= float(self._min_atr.Value)

        long_signal = common_filters and ut_buy and close > ema and close > alma and rsi > RSI_LONG_LEVEL and close < upper_band
        cross_below_fast = prev_close >= prev_fast and close < fast_ema
        short_signal = common_filters and ut_bearish and cross_below_fast and close < ema and close < alma \
            and rsi < RSI_SHORT_LEVEL and close > lower_band

        sl_mult = float(self._stop_loss_atr_multiplier.Value)
        tp_mult = float(self._take_profit_atr_multiplier.Value)

        if long_signal and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
            self._last_entry_bar = self._bar_index
            self._stop_price = close - sl_mult * atr
            self._take_price = close + tp_mult * atr
        elif short_signal and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
            self._last_entry_bar = self._bar_index
            self._stop_price = close + sl_mult * atr
            self._take_price = close - tp_mult * atr
        elif self.Position > 0:
            hit_stop = sl_mult > 0 and float(candle.LowPrice) <= self._stop_price
            hit_take = tp_mult > 0 and float(candle.HighPrice) >= self._take_price
            if ut_sell or hit_stop or hit_take:
                self.SellMarket(self.Position)
        elif self.Position < 0:
            hit_stop = sl_mult > 0 and float(candle.HighPrice) >= self._stop_price
            hit_take = tp_mult > 0 and float(candle.LowPrice) <= self._take_price
            if ut_buy or hit_stop or hit_take:
                self.BuyMarket(-self.Position)

    def CreateClone(self):
        return alma_ut_bot_confluence_strategy()
