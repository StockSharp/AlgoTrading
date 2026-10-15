import clr
import math

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import BollingerBands, ExponentialMovingAverage, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy


class ema_trend_heikin_ashi_entry_strategy(Strategy):
    """
    EMA trend Heikin Ashi entry strategy.
    Bollinger Bands are calculated on Heikin Ashi closes. After at least two bearish Heikin Ashi candles touching the lower band,
    a bullish candle closing above the lower band goes long when the fast EMA is above the slow EMA on the higher timeframe; the
    short side mirrors it at the upper band. The stop is the signal candle's low (high). Half of the position is taken at 1R,
    after which the stop trails the previous candle's low (high).
    """

    def __init__(self):
        super(ema_trend_heikin_ashi_entry_strategy, self).__init__()
        self._bollinger_period = self.Param("BollingerPeriod", 20).SetGreaterThanZero().SetDisplay("Bollinger Period", "Bollinger Bands period", "Indicators")
        self._bollinger_deviation = self.Param("BollingerDeviation", 2.0).SetGreaterThanZero().SetDisplay("Bollinger Deviation", "Bollinger Bands deviation", "Indicators")
        self._fast_ema_period = self.Param("FastEmaPeriod", 9).SetGreaterThanZero().SetDisplay("Fast EMA", "Fast EMA period on the higher timeframe", "Trend")
        self._slow_ema_period = self.Param("SlowEmaPeriod", 21).SetGreaterThanZero().SetDisplay("Slow EMA", "Slow EMA period on the higher timeframe", "Trend")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._higher_timeframe = self.Param("HigherTimeframe", DataType.TimeFrame(TimeSpan.FromMinutes(180))).SetDisplay("Higher Timeframe", "Timeframe of the EMA trend filter", "Trend")
        self._bollinger = None
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.candle_type), (self.Security, self._higher_timeframe.Value)]

    def _reset_state(self):
        self._ha_open = None
        self._ha_close = None
        self._bear_touch_count = 0
        self._bull_touch_count = 0
        self._higher_fast = None
        self._higher_slow = None
        self._stop_price = 0.0
        self._target_price = 0.0
        self._target_done = False

    def OnReseted(self):
        super(ema_trend_heikin_ashi_entry_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(ema_trend_heikin_ashi_entry_strategy, self).OnStarted2(time)

        self._reset_state()

        self._bollinger = BollingerBands()
        self._bollinger.Length = self._bollinger_period.Value
        self._bollinger.Width = Decimal(float(self._bollinger_deviation.Value))
        fast_ema = ExponentialMovingAverage()
        fast_ema.Length = self._fast_ema_period.Value
        slow_ema = ExponentialMovingAverage()
        slow_ema.Length = self._slow_ema_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()

        self.SubscribeCandles(self._higher_timeframe.Value).Bind(fast_ema, slow_ema, self._process_higher_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_higher_candle(self, candle, fast_value, slow_value):
        if candle.State != CandleStates.Finished:
            return

        self._higher_fast = float(fast_value)
        self._higher_slow = float(slow_value)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        open_price = float(candle.OpenPrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        close = float(candle.ClosePrice)

        ha_close = (open_price + high + low + close) / 4.0
        if self._ha_open is not None and self._ha_close is not None:
            ha_open = (self._ha_open + self._ha_close) / 2.0
        else:
            ha_open = (open_price + close) / 2.0
        ha_high = max(high, ha_open, ha_close)
        ha_low = min(low, ha_open, ha_close)
        self._ha_open = ha_open
        self._ha_close = ha_close

        bands_input = DecimalIndicatorValue(self._bollinger, Decimal(ha_close), candle.OpenTime)
        bands_input.IsFinal = True
        bands_value = self._bollinger.Process(bands_input)

        if not self._bollinger.IsFormed or bands_value.UpBand is None or bands_value.LowBand is None:
            return

        upper = float(bands_value.UpBand)
        lower = float(bands_value.LowBand)

        bear_touches = self._bear_touch_count
        bull_touches = self._bull_touch_count
        bullish = ha_close > ha_open
        bearish = ha_close < ha_open
        self._bear_touch_count = self._bear_touch_count + 1 if bearish and ha_low <= lower else 0
        self._bull_touch_count = self._bull_touch_count + 1 if bullish and ha_high >= upper else 0

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self.Position != 0:
            self._manage_position(high, low)
            return

        if self._higher_fast is None or self._higher_slow is None:
            return

        fast = self._higher_fast
        slow = self._higher_slow

        if bear_touches >= 2 and bullish and ha_close > lower and fast > slow:
            risk = close - low
            if risk <= 0:
                return
            self._stop_price = low
            self._target_price = close + risk
            self._target_done = False
            self.BuyMarket(self.Volume)
        elif bull_touches >= 2 and bearish and ha_close < upper and fast < slow:
            risk = high - close
            if risk <= 0:
                return
            self._stop_price = high
            self._target_price = close - risk
            self._target_done = False
            self.SellMarket(self.Volume)

    def _half_volume(self):
        step = float(self.Security.VolumeStep) if self.Security is not None and self.Security.VolumeStep is not None else 0.0
        half = abs(float(self.Position)) / 2.0
        if step > 0:
            half = math.floor(half / step + 1e-9) * step
        return half

    def _manage_position(self, high, low):
        if self.Position > 0:
            if low <= self._stop_price:
                self.SellMarket(self.Position)
                return

            if not self._target_done and high >= self._target_price:
                self._target_done = True
                half = self._half_volume()
                self.SellMarket(Decimal(half) if half > 0 else self.Position)

            if self._target_done:
                self._stop_price = max(self._stop_price, low)
        else:
            if high >= self._stop_price:
                self.BuyMarket(-self.Position)
                return

            if not self._target_done and low <= self._target_price:
                self._target_done = True
                half = self._half_volume()
                self.BuyMarket(Decimal(half) if half > 0 else -self.Position)

            if self._target_done:
                self._stop_price = min(self._stop_price, high)

    def CreateClone(self):
        return ema_trend_heikin_ashi_entry_strategy()
