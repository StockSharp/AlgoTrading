import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import BollingerBands, AverageTrueRange, DonchianChannels
from StockSharp.Algo.Strategies import Strategy


class bands_strategy(Strategy):
    """Bollinger Bands breakout confirmed by Donchian channel slope and ATR-based stops."""

    def __init__(self):
        super(bands_strategy, self).__init__()

        self._trade_volume = self.Param("TradeVolume", 0.1) \
            .SetGreaterThanZero() \
            .SetDisplay("Volume", "Net volume in lots sent with every order", "Trading")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Time frame used for indicator calculations", "Market Data")
        self._bollinger_period = self.Param("BollingerPeriod", 100) \
            .SetGreaterThanZero() \
            .SetDisplay("Bollinger Period", "Number of candles used for Bollinger Bands", "Indicators")
        self._bollinger_deviation = self.Param("BollingerDeviation", 1.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Bollinger Deviation", "Standard deviation multiplier", "Indicators")
        self._donchian_period = self.Param("DonchianPeriod", 100) \
            .SetGreaterThanZero() \
            .SetDisplay("Donchian Period", "Donchian Channel length", "Indicators")
        self._confirmation_period = self.Param("ConfirmationPeriod", 100) \
            .SetGreaterThanZero() \
            .SetDisplay("Slope Confirmation", "Min bars for Donchian slope confirmation", "Indicators")
        self._atr_period = self.Param("AtrPeriod", 21) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR Period", "Length of Average True Range", "Indicators")
        self._stop_atr_multiplier = self.Param("StopAtrMultiplier", 4.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Stop ATR Multiplier", "ATRs for stop placement", "Risk")
        self._take_atr_multiplier = self.Param("TakeAtrMultiplier", 4.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Take ATR Multiplier", "ATRs for target placement", "Risk")

        self._prev_open = None
        self._prev_close = None
        self._prev_lower_band = None
        self._prev_upper_band = None
        self._prev_donch_lower = None
        self._prev_donch_upper = None
        self._prev_atr = None
        self._lower_trend_length = 0
        self._upper_trend_length = 0
        self._stop_loss_price = None
        self._take_profit_price = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def TradeVolume(self):
        return self._trade_volume.Value

    @property
    def BollingerPeriod(self):
        return self._bollinger_period.Value

    @property
    def BollingerDeviation(self):
        return self._bollinger_deviation.Value

    @property
    def DonchianPeriod(self):
        return self._donchian_period.Value

    @property
    def ConfirmationPeriod(self):
        return self._confirmation_period.Value

    @property
    def AtrPeriod(self):
        return self._atr_period.Value

    @property
    def StopAtrMultiplier(self):
        return self._stop_atr_multiplier.Value

    @property
    def TakeAtrMultiplier(self):
        return self._take_atr_multiplier.Value

    def OnReseted(self):
        super(bands_strategy, self).OnReseted()
        self._prev_open = None
        self._prev_close = None
        self._prev_lower_band = None
        self._prev_upper_band = None
        self._prev_donch_lower = None
        self._prev_donch_upper = None
        self._prev_atr = None
        self._lower_trend_length = 0
        self._upper_trend_length = 0
        self._stop_loss_price = None
        self._take_profit_price = None

    def OnStarted(self, time):
        super(bands_strategy, self).OnStarted(time)

        bollinger = BollingerBands()
        bollinger.Length = self.BollingerPeriod
        bollinger.Width = self.BollingerDeviation

        atr = AverageTrueRange()
        atr.Length = self.AtrPeriod

        donchian = DonchianChannels()
        donchian.Length = self.DonchianPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(bollinger, atr, donchian, self._process_candle).Start()

    def _process_candle(self, candle, bb_val, atr_val, donch_val):
        if candle.State != CandleStates.Finished:
            return

        if not bb_val.IsFormed or not atr_val.IsFormed or not donch_val.IsFormed:
            return

        upper_bb = bb_val.UpBand
        lower_bb = bb_val.LowBand
        atr_v = float(atr_val)
        donch_upper = donch_val.UpperBand
        donch_lower = donch_val.LowerBand

        if upper_bb is None or lower_bb is None or donch_upper is None or donch_lower is None:
            return

        upper = float(upper_bb)
        lower = float(lower_bb)
        d_upper = float(donch_upper)
        d_lower = float(donch_lower)

        lower_trend = self._calc_lower_trend(d_lower)
        upper_trend = self._calc_upper_trend(d_upper)

        if self._prev_open is None:
            self._cache_values(candle, lower, upper, d_lower, d_upper, atr_v, lower_trend, upper_trend)
            return

        prev_open = self._prev_open
        prev_close = self._prev_close
        prev_lower_band = self._prev_lower_band
        prev_upper_band = self._prev_upper_band
        prev_donch_lower = self._prev_donch_lower
        prev_donch_upper = self._prev_donch_upper
        atr_for_stops = self._prev_atr if self._prev_atr is not None else atr_v

        if self.Position == 0:
            if prev_open < prev_lower_band and prev_close > prev_lower_band and lower_trend > self.ConfirmationPeriod:
                self._open_long(float(candle.ClosePrice), atr_for_stops)
            elif prev_open > prev_upper_band and prev_close < prev_upper_band and upper_trend > self.ConfirmationPeriod:
                self._open_short(float(candle.ClosePrice), atr_for_stops)
        elif self.Position > 0:
            stop_hit = self._stop_loss_price is not None and float(candle.LowPrice) <= self._stop_loss_price
            take_hit = self._take_profit_price is not None and float(candle.HighPrice) >= self._take_profit_price
            if stop_hit or take_hit or prev_close > prev_donch_upper or prev_close < prev_donch_lower:
                self.SellMarket(self.Position)
                self._clear_protection()
        elif self.Position < 0:
            stop_hit = self._stop_loss_price is not None and float(candle.HighPrice) >= self._stop_loss_price
            take_hit = self._take_profit_price is not None and float(candle.LowPrice) <= self._take_profit_price
            if stop_hit or take_hit or prev_close < prev_donch_lower or prev_close > prev_donch_upper:
                self.BuyMarket(abs(self.Position))
                self._clear_protection()

        self._cache_values(candle, lower, upper, d_lower, d_upper, atr_v, lower_trend, upper_trend)

    def _calc_lower_trend(self, current_lower):
        if self._prev_donch_lower is not None:
            return self._lower_trend_length + 1 if current_lower >= self._prev_donch_lower else 1
        return 1

    def _calc_upper_trend(self, current_upper):
        if self._prev_donch_upper is not None:
            return self._upper_trend_length + 1 if current_upper <= self._prev_donch_upper else 1
        return 1

    def _cache_values(self, candle, lower, upper, d_lower, d_upper, atr_v, lower_trend, upper_trend):
        self._prev_open = float(candle.OpenPrice)
        self._prev_close = float(candle.ClosePrice)
        self._prev_lower_band = lower
        self._prev_upper_band = upper
        self._prev_donch_lower = d_lower
        self._prev_donch_upper = d_upper
        self._prev_atr = atr_v
        self._lower_trend_length = lower_trend
        self._upper_trend_length = upper_trend

    def _open_long(self, entry_price, atr_v):
        vol = float(self.TradeVolume)
        if vol <= 0:
            return
        self.BuyMarket(vol)
        self._assign_protection(entry_price, atr_v, True)

    def _open_short(self, entry_price, atr_v):
        vol = float(self.TradeVolume)
        if vol <= 0:
            return
        self.SellMarket(vol)
        self._assign_protection(entry_price, atr_v, False)

    def _assign_protection(self, entry_price, atr_v, is_long):
        if atr_v <= 0:
            self._clear_protection()
            return
        stop_dist = atr_v * float(self.StopAtrMultiplier)
        take_dist = atr_v * float(self.TakeAtrMultiplier)
        if is_long:
            self._stop_loss_price = entry_price - stop_dist
            self._take_profit_price = entry_price + take_dist
        else:
            self._stop_loss_price = entry_price + stop_dist
            self._take_profit_price = entry_price - take_dist

    def _clear_protection(self):
        self._stop_loss_price = None
        self._take_profit_price = None

    def CreateClone(self):
        return bands_strategy()
