import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, TimeZoneInfo, DateTime, DateTimeKind, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, SimpleMovingAverage, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy

# The README names EMA, VWAP and volume SMA confirmations without giving their lengths.
TREND_LENGTH = 20
VOLUME_LENGTH = 20
STOP_SHARE = 0.33
RANGE_START_MINUTES = 9 * 60 + 30
RANGE_END_MINUTES = 9 * 60 + 45


class ny_orb_cp_strategy(Strategy):
    """
    NY opening range breakout with retest confirmation.
    The range is the high and low of the 9:30-9:45 New York candles and must span at least MinRangePoints price steps. After a close
    above the range high, a candle that dips back to the high and closes above it goes long when the close is above the EMA and the
    session VWAP and its volume is above the volume SMA; the short side mirrors it at the range low. The stop lies 0.33 of the range
    from the entry and the target RiskReward times that. At most MaxTradesPerSession trades are taken per day and trading stops for
    the day once the day's PnL reaches MaxDailyLoss.
    """

    def __init__(self):
        super(ny_orb_cp_strategy, self).__init__()
        self._min_range_points = self.Param("MinRangePoints", 60.0).SetNotNegative().SetDisplay("Min Range Points", "Minimum range size in price steps", "Range")
        self._risk_reward = self.Param("RiskReward", 3.0).SetGreaterThanZero().SetDisplay("Risk Reward", "Target in multiples of the stop distance", "Risk")
        self._max_trades_per_session = self.Param("MaxTradesPerSession", 3).SetGreaterThanZero().SetDisplay("Max Trades", "Maximum trades per session", "Risk")
        self._max_daily_loss = self.Param("MaxDailyLoss", -1000.0).SetDisplay("Max Daily Loss", "Daily PnL at which trading stops for the day", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._new_york = None
        self._volume_average = None
        self._session_date = None
        self._day_start_pnl = 0.0
        self._stop_price = 0.0
        self._take_price = 0.0
        self._reset_session()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_session(self):
        self._range_high = None
        self._range_low = None
        self._broke_up = False
        self._broke_down = False
        self._trades_today = 0
        self._vwap_price_volume = 0.0
        self._vwap_volume = 0.0

    def OnReseted(self):
        super(ny_orb_cp_strategy, self).OnReseted()
        self._session_date = None
        self._day_start_pnl = 0.0
        self._stop_price = 0.0
        self._take_price = 0.0
        self._reset_session()

    def OnStarted2(self, time):
        super(ny_orb_cp_strategy, self).OnStarted2(time)

        self._new_york = TimeZoneInfo.FindSystemTimeZoneById("America/New_York")
        self._session_date = None
        self._reset_session()

        ema = ExponentialMovingAverage()
        ema.Length = TREND_LENGTH
        self._volume_average = SimpleMovingAverage()
        self._volume_average.Length = VOLUME_LENGTH

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ema, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, ema_value):
        if candle.State != CandleStates.Finished:
            return

        ema = float(ema_value)
        ny_time = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(candle.OpenTime, DateTimeKind.Utc), self._new_york)
        ny_date = (ny_time.Year, ny_time.Month, ny_time.Day)

        if self._session_date != ny_date:
            self._session_date = ny_date
            self._reset_session()
            self._day_start_pnl = float(self.PnL)

        volume_input = DecimalIndicatorValue(self._volume_average, candle.TotalVolume, candle.OpenTime)
        volume_input.IsFinal = True
        volume_value = self._volume_average.Process(volume_input)

        high_price = float(candle.HighPrice)
        low_price = float(candle.LowPrice)
        close = float(candle.ClosePrice)
        volume = float(candle.TotalVolume)

        typical = (high_price + low_price + close) / 3.0
        self._vwap_price_volume += typical * volume
        self._vwap_volume += volume
        vwap = self._vwap_price_volume / self._vwap_volume if self._vwap_volume > 0 else close

        minutes = ny_time.Hour * 60 + ny_time.Minute

        if RANGE_START_MINUTES <= minutes < RANGE_END_MINUTES:
            self._range_high = high_price if self._range_high is None else max(self._range_high, high_price)
            self._range_low = low_price if self._range_low is None else min(self._range_low, low_price)
            return

        # A breakout is remembered from the candle that closes beyond the range; the retest comes later.
        broke_up = self._broke_up
        broke_down = self._broke_down

        if minutes >= RANGE_END_MINUTES and self._range_high is not None and self._range_low is not None:
            if close > self._range_high:
                self._broke_up = True
            if close < self._range_low:
                self._broke_down = True

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self.Position > 0:
            if low_price <= self._stop_price or high_price >= self._take_price:
                self.SellMarket(self.Position)
            return

        if self.Position < 0:
            if high_price >= self._stop_price or low_price <= self._take_price:
                self.BuyMarket(-self.Position)
            return

        if minutes < RANGE_END_MINUTES or self._range_high is None or self._range_low is None or not volume_value.IsFormed:
            return

        high = self._range_high
        low = self._range_low
        step = float(self.Security.PriceStep) if self.Security is not None and self.Security.PriceStep is not None else 1.0
        rng = high - low
        if rng <= 0 or rng < float(self._min_range_points.Value) * step:
            return

        if self._trades_today >= self._max_trades_per_session.Value or float(self.PnL) - self._day_start_pnl <= float(self._max_daily_loss.Value):
            return

        volume_ok = volume > float(volume_value.GetValue[Decimal](None))
        stop_distance = rng * STOP_SHARE
        rr = float(self._risk_reward.Value)

        if broke_up and low_price <= high and close > high and close > ema and close > vwap and volume_ok:
            self._stop_price = close - stop_distance
            self._take_price = close + stop_distance * rr
            self._trades_today += 1
            self.BuyMarket(self.Volume)
        elif broke_down and high_price >= low and close < low and close < ema and close < vwap and volume_ok:
            self._stop_price = close + stop_distance
            self._take_price = close - stop_distance * rr
            self._trades_today += 1
            self.SellMarket(self.Volume)

    def CreateClone(self):
        return ny_orb_cp_strategy()
