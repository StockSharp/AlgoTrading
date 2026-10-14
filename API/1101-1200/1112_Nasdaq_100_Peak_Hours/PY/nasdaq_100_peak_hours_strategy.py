import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, RelativeStrengthIndex, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy

# Regular NASDAQ session (09:30-16:00 New York) expressed in UTC minutes.
SESSION_START = 13 * 60 + 30
SESSION_END = 20 * 60


class nasdaq_100_peak_hours_strategy(Strategy):
    """
    NASDAQ 100 peak hours strategy.
    Trades only in the first two hours and the last hour of the cash session. A long opens when the close is above the short EMA,
    the short EMA is above the long EMA, both EMAs are rising, RSI is above 50 and the close is above the session VWAP; a short uses
    the opposite conditions. The position is protected by an initial ATR stop that moves to break-even and then trails by an ATR
    multiple, and it is closed after TimeExitBars bars or when the EMA trend reverses.
    """

    def __init__(self):
        super(nasdaq_100_peak_hours_strategy, self).__init__()
        self._long_ema_length = self.Param("LongEmaLength", 21).SetGreaterThanZero().SetDisplay("Long EMA", "Long EMA period", "Indicators")
        self._short_ema_length = self.Param("ShortEmaLength", 9).SetGreaterThanZero().SetDisplay("Short EMA", "Short EMA period", "Indicators")
        self._rsi_length = self.Param("RsiLength", 14).SetGreaterThanZero().SetDisplay("RSI", "RSI period", "Indicators")
        self._atr_length = self.Param("AtrLength", 14).SetGreaterThanZero().SetDisplay("ATR", "ATR period", "Indicators")
        self._trail_atr_mult = self.Param("TrailAtrMult", 1.5).SetNotNegative().SetDisplay("Trail ATR Mult", "ATR multiple of the trailing stop", "Risk")
        self._initial_sl_mult = self.Param("InitialSlMult", 0.5).SetNotNegative().SetDisplay("Initial SL Mult", "ATR multiple of the initial stop", "Risk")
        self._break_even_atr_mult = self.Param("BreakEvenAtrMult", 1.5).SetNotNegative().SetDisplay("Break-even ATR Mult", "ATR profit that moves the stop to break-even", "Risk")
        self._time_exit_bars = self.Param("TimeExitBars", 20).SetGreaterThanZero().SetDisplay("Time Exit Bars", "Bars after which an open position is closed", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._prev_short = None
        self._prev_long = None
        self._vwap_date = None
        self._vwap_price_volume = 0.0
        self._vwap_volume = 0.0
        self._entry_price = 0.0
        self._entry_atr = 0.0
        self._stop_price = 0.0
        self._best_price = 0.0
        self._bars_in_position = 0

    def OnReseted(self):
        super(nasdaq_100_peak_hours_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(nasdaq_100_peak_hours_strategy, self).OnStarted2(time)

        self._reset_state()

        short_ema = ExponentialMovingAverage()
        short_ema.Length = self._short_ema_length.Value
        long_ema = ExponentialMovingAverage()
        long_ema.Length = self._long_ema_length.Value
        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_length.Value
        atr = AverageTrueRange()
        atr.Length = self._atr_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(short_ema, long_ema, rsi, atr, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, short_ema)
            self.DrawIndicator(area, long_ema)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, short_value, long_value, rsi_value, atr_value):
        if candle.State != CandleStates.Finished:
            return

        # Session VWAP restarts every UTC day.
        open_time = candle.OpenTime
        date = open_time.Date
        if self._vwap_date is None or date != self._vwap_date:
            self._vwap_date = date
            self._vwap_price_volume = 0.0
            self._vwap_volume = 0.0

        volume = float(candle.TotalVolume)
        typical = (float(candle.HighPrice) + float(candle.LowPrice) + float(candle.ClosePrice)) / 3.0
        self._vwap_price_volume += typical * volume
        self._vwap_volume += volume

        short_ema = float(short_value)
        long_ema = float(long_value)
        last_short = self._prev_short
        last_long = self._prev_long
        self._prev_short = short_ema
        self._prev_long = long_ema

        if last_short is None or last_long is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        atr = float(atr_value)
        if self._manage_position(candle, short_ema, long_ema, atr):
            return

        if self._vwap_volume <= 0 or atr <= 0 or not self._is_peak_hour(open_time):
            return

        close = float(candle.ClosePrice)
        rsi = float(rsi_value)
        vwap = self._vwap_price_volume / self._vwap_volume
        rising = short_ema > last_short and long_ema > last_long
        falling = short_ema < last_short and long_ema < last_long

        if close > short_ema and short_ema > long_ema and rising and rsi > 50 and close > vwap and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
            self._on_entry(close, atr, True)
        elif close < short_ema and short_ema < long_ema and falling and rsi < 50 and close < vwap and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
            self._on_entry(close, atr, False)

    def _is_peak_hour(self, open_time):
        minutes = open_time.Hour * 60 + open_time.Minute
        opening_window = SESSION_START <= minutes < SESSION_START + 120
        closing_window = SESSION_END - 60 <= minutes < SESSION_END
        return opening_window or closing_window

    def _on_entry(self, price, atr, is_long):
        self._entry_price = price
        self._entry_atr = atr
        self._best_price = price
        self._bars_in_position = 0
        offset = atr * float(self._initial_sl_mult.Value)
        self._stop_price = price - offset if is_long else price + offset

    def _manage_position(self, candle, short_ema, long_ema, atr):
        if self.Position == 0:
            return False

        self._bars_in_position += 1
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        trail = atr * float(self._trail_atr_mult.Value)
        break_even = self._entry_atr * float(self._break_even_atr_mult.Value)
        time_exit = self._bars_in_position >= self._time_exit_bars.Value

        if self.Position > 0:
            if low <= self._stop_price or time_exit or short_ema < long_ema:
                self.SellMarket(self.Position)
                return True
            self._best_price = max(self._best_price, high)
            if self._best_price - self._entry_price >= break_even:
                self._stop_price = max(self._stop_price, self._entry_price)
            self._stop_price = max(self._stop_price, self._best_price - trail)
        else:
            if high >= self._stop_price or time_exit or short_ema > long_ema:
                self.BuyMarket(-self.Position)
                return True
            self._best_price = min(self._best_price, low)
            if self._entry_price - self._best_price >= break_even:
                self._stop_price = min(self._stop_price, self._entry_price)
            self._stop_price = min(self._stop_price, self._best_price + trail)

        return False

    def CreateClone(self):
        return nasdaq_100_peak_hours_strategy()
