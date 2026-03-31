import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import (
    SimpleMovingAverage, RelativeStrengthIndex,
    CommodityChannelIndex, DecimalIndicatorValue
)
from collections import deque


class yen_trader051_strategy(Strategy):
    """Multi-security JPY cross strategy with RSI/CCI/MA confirmation (simplified to single security)."""

    def __init__(self):
        super(yen_trader051_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Signal Candles", "Primary timeframe for signals", "Data")

        self._loop_back_bars = self.Param("LoopBackBars", 2) \
            .SetDisplay("Loop Back Bars", "Number of historical bars for breakout logic", "Filters")

        self._use_rsi_filter = self.Param("UseRsiFilter", True) \
            .SetDisplay("Use RSI", "Enable RSI confirmation filter", "Indicators")

        self._use_cci_filter = self.Param("UseCciFilter", True) \
            .SetDisplay("Use CCI", "Enable CCI confirmation filter", "Indicators")

        self._use_ma_filter = self.Param("UseMovingAverageFilter", True) \
            .SetDisplay("Use Moving Average", "Enable moving average confirmation filter", "Indicators")

        self._ma_period = self.Param("MaPeriod", 34) \
            .SetGreaterThanZero() \
            .SetDisplay("MA Period", "Moving average period", "Indicators")

        self._stop_loss_pips = self.Param("StopLossPips", 1000) \
            .SetDisplay("Stop Loss (pips)", "Stop loss distance in pips", "Risk")

        self._take_profit_pips = self.Param("TakeProfitPips", 5000) \
            .SetDisplay("Take Profit (pips)", "Take profit distance in pips", "Risk")

        self._break_even_pips = self.Param("BreakEvenPips", 200) \
            .SetDisplay("Break Even (pips)", "Distance before moving stop to break even", "Risk")

        self._trailing_stop_pips = self.Param("TrailingStopPips", 200) \
            .SetDisplay("Trailing Stop (pips)", "Trailing stop distance in pips", "Risk")

        self._trailing_step_pips = self.Param("TrailingStepPips", 10) \
            .SetDisplay("Trailing Step (pips)", "Minimum trailing stop step in pips", "Risk")

        self._close_on_opposite = self.Param("CloseOnOpposite", False) \
            .SetDisplay("Close On Opposite", "Close current position when opposite signal appears", "Risk")

        self._allow_hedging = self.Param("AllowHedging", True) \
            .SetDisplay("Allow Hedging", "Allow simultaneous trades", "Risk")

        self._entry_price = None
        self._stop_price = None
        self._take_profit_price = None
        self._break_even_activated = False
        self._highest_since_entry = 0.0
        self._lowest_since_entry = 0.0
        self._last_close = None
        self._closes = deque()
        self._rsi_value = None
        self._cci_value = None
        self._ma_value = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    @property
    def LoopBackBars(self):
        return self._loop_back_bars.Value

    @property
    def UseRsiFilter(self):
        return self._use_rsi_filter.Value

    @property
    def UseCciFilter(self):
        return self._use_cci_filter.Value

    @property
    def UseMovingAverageFilter(self):
        return self._use_ma_filter.Value

    @property
    def MaPeriod(self):
        return self._ma_period.Value

    @property
    def StopLossPips(self):
        return self._stop_loss_pips.Value

    @property
    def TakeProfitPips(self):
        return self._take_profit_pips.Value

    @property
    def BreakEvenPips(self):
        return self._break_even_pips.Value

    @property
    def TrailingStopPips(self):
        return self._trailing_stop_pips.Value

    @property
    def TrailingStepPips(self):
        return self._trailing_step_pips.Value

    @property
    def CloseOnOpposite(self):
        return self._close_on_opposite.Value

    @property
    def AllowHedging(self):
        return self._allow_hedging.Value

    def OnStarted2(self, time):
        super(yen_trader051_strategy, self).OnStarted2(time)

        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = 14

        self._cci = CommodityChannelIndex()
        self._cci.Length = 14

        self._ma = SimpleMovingAverage()
        self._ma.Length = max(1, self.MaPeriod)

        subscription = self.SubscribeCandles(self.CandleType)
        subscription \
            .Bind(self.process_candle) \
            .Start()

        self.StartProtection(None, None)

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)

        rsi_out = self._rsi.Process(DecimalIndicatorValue(self._rsi, candle.ClosePrice, candle.CloseTime))
        if rsi_out.IsFinal:
            self._rsi_value = float(rsi_out)

        cci_out = self._cci.Process(candle)
        if cci_out.IsFinal:
            self._cci_value = float(cci_out)

        ma_out = self._ma.Process(DecimalIndicatorValue(self._ma, candle.ClosePrice, candle.CloseTime))
        if ma_out.IsFinal:
            self._ma_value = float(ma_out)

        self._last_close = close

        self._closes.append(close)
        max_count = max(self.LoopBackBars + 1, 2)
        while len(self._closes) > max_count:
            self._closes.popleft()

        self._update_risk(candle)

        if not self.IsFormed:
            return

        if self._rsi_value is None or self._cci_value is None or self._ma_value is None:
            return

        long_sig = self._breakout_check(True)
        short_sig = self._breakout_check(False)

        if long_sig:
            if self.UseRsiFilter and self._rsi_value <= 50:
                long_sig = False
            if self.UseCciFilter and self._cci_value <= 0:
                long_sig = False
            if self.UseMovingAverageFilter and self._last_close <= self._ma_value:
                long_sig = False

        if short_sig:
            if self.UseRsiFilter and self._rsi_value >= 50:
                short_sig = False
            if self.UseCciFilter and self._cci_value >= 0:
                short_sig = False
            if self.UseMovingAverageFilter and self._last_close >= self._ma_value:
                short_sig = False

        if long_sig:
            self._try_long(candle)

        if short_sig:
            self._try_short(candle)

    def _breakout_check(self, is_long):
        if self._last_close is None:
            return False
        if self.LoopBackBars <= 1:
            return True
        if len(self._closes) <= self.LoopBackBars:
            return False
        vals = list(self._closes)
        idx = len(vals) - 1 - self.LoopBackBars
        if idx < 0:
            return False
        lb = vals[idx]
        return self._last_close > lb if is_long else self._last_close < lb

    def _try_long(self, candle):
        if not self.AllowHedging and self.Position < 0:
            return
        vol = self.Volume
        if self.CloseOnOpposite and self.Position < 0:
            vol += abs(self.Position)
        if vol <= 0:
            return
        self.BuyMarket(vol)
        self._init_pos(candle, True)

    def _try_short(self, candle):
        if not self.AllowHedging and self.Position > 0:
            return
        vol = self.Volume
        if self.CloseOnOpposite and self.Position > 0:
            vol += abs(self.Position)
        if vol <= 0:
            return
        self.SellMarket(vol)
        self._init_pos(candle, False)

    def _init_pos(self, candle, is_long):
        close = float(candle.ClosePrice)
        self._entry_price = close
        self._stop_price = None
        self._take_profit_price = None
        self._break_even_activated = False
        self._highest_since_entry = float(candle.HighPrice)
        self._lowest_since_entry = float(candle.LowPrice)

        sd = self._pips_to_price(self.StopLossPips)
        td = self._pips_to_price(self.TakeProfitPips)

        if is_long:
            if sd > 0:
                self._stop_price = close - sd
            if td > 0:
                self._take_profit_price = close + td
        else:
            if sd > 0:
                self._stop_price = close + sd
            if td > 0:
                self._take_profit_price = close - td

    def _update_risk(self, candle):
        if self.Position == 0:
            self._reset_pos()
            return

        if self._entry_price is None:
            return

        high = float(candle.HighPrice)
        low = float(candle.LowPrice)

        if self.Position > 0:
            self._highest_since_entry = max(self._highest_since_entry, high)

            if self._take_profit_price is not None and high >= self._take_profit_price:
                self.SellMarket(self.Position)
                self._reset_pos()
                return

            if self._stop_price is not None and low <= self._stop_price:
                self.SellMarket(self.Position)
                self._reset_pos()
                return

            self._trail(candle, True)
        else:
            self._lowest_since_entry = min(self._lowest_since_entry, low)

            if self._take_profit_price is not None and low <= self._take_profit_price:
                self.BuyMarket(abs(self.Position))
                self._reset_pos()
                return

            if self._stop_price is not None and high >= self._stop_price:
                self.BuyMarket(abs(self.Position))
                self._reset_pos()
                return

            self._trail(candle, False)

    def _trail(self, candle, is_long):
        if self._entry_price is None:
            return

        be = self._pips_to_price(self.BreakEvenPips)
        td = self._pips_to_price(self.TrailingStopPips)
        ts = self._pips_to_price(self.TrailingStepPips)

        high = float(candle.HighPrice)
        low = float(candle.LowPrice)

        if is_long:
            if not self._break_even_activated and be > 0 and high >= self._entry_price + be:
                ns = self._entry_price + be
                self._stop_price = max(self._stop_price, ns) if self._stop_price is not None else ns
                self._break_even_activated = True

            if td > 0:
                desired = max(self._entry_price, high - td)
                if self._stop_price is None or desired > self._stop_price + ts:
                    self._stop_price = desired
        else:
            if not self._break_even_activated and be > 0 and low <= self._entry_price - be:
                ns = self._entry_price - be
                self._stop_price = min(self._stop_price, ns) if self._stop_price is not None else ns
                self._break_even_activated = True

            if td > 0:
                desired = min(self._entry_price, low + td)
                if self._stop_price is None or desired < self._stop_price - ts:
                    self._stop_price = desired

    def _pips_to_price(self, pips):
        if pips <= 0:
            return 0.0
        sec = self.Security
        step = float(sec.PriceStep) if sec is not None and sec.PriceStep is not None and float(sec.PriceStep) > 0 else 1.0
        return pips * step

    def _reset_pos(self):
        self._entry_price = None
        self._stop_price = None
        self._take_profit_price = None
        self._break_even_activated = False
        self._highest_since_entry = 0.0
        self._lowest_since_entry = 0.0

    def OnReseted(self):
        super(yen_trader051_strategy, self).OnReseted()
        self._last_close = None
        self._closes.clear()
        self._rsi_value = None
        self._cci_value = None
        self._ma_value = None
        self._reset_pos()

    def CreateClone(self):
        return yen_trader051_strategy()
