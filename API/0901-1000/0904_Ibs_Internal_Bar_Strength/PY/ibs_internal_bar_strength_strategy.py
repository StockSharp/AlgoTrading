import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal, Convert, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class ibs_internal_bar_strength_strategy(Strategy):
    """Previous-bar IBS, optional EMA trend filter, spaced additions and full-basket exits."""

    def __init__(self):
        super(ibs_internal_bar_strength_strategy, self).__init__()
        self._ibs_entry_threshold = self.Param("IbsEntryThreshold", 0.09).SetRange(0.0, 1.0)
        self._ibs_exit_threshold = self.Param("IbsExitThreshold", 0.985).SetRange(0.0, 1.0)
        self._ema_period = self.Param("EmaPeriod", 220).SetGreaterThanZero()
        self._use_ema_filter = self.Param("UseEmaFilter", True)
        self._allow_long = self.Param("AllowLong", True)
        self._allow_short = self.Param("AllowShort", True)
        self._min_entry_pct = self.Param("MinEntryPct", 0.0).SetNotNegative()
        self._max_trade_duration = self.Param("MaxTradeDuration", 14).SetGreaterThanZero()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1)))
        self._reset_state()

    def GetWorkingSecurities(self):
        return [(self.Security, self._candle_type.Value)]

    def OnReseted(self):
        super(ibs_internal_bar_strength_strategy, self).OnReseted()
        self._reset_state()

    def _reset_state(self):
        self._previous_ibs = None
        self._previous_close = Decimal.Zero
        self._previous_ema = Decimal.Zero
        self._previous_ema_ready = False
        self._last_entry_price = Decimal.Zero
        self._bar_count = 0
        self._first_entry_bar = 0

    def OnStarted2(self, time):
        super(ibs_internal_bar_strength_strategy, self).OnStarted2(time)
        if self._ibs_entry_threshold.Value >= self._ibs_exit_threshold.Value:
            raise ValueError("IbsEntryThreshold must be below IbsExitThreshold.")
        self._reset_state()
        ema = ExponentialMovingAverage()
        ema.Length = self._ema_period.Value if self._use_ema_filter.Value else 1

        def process(candle, value):
            self._process_candle(candle, value, ema.IsFormed)

        subscription = self.SubscribeCandles(self._candle_type.Value)
        subscription.Bind(ema, process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            if self._use_ema_filter.Value:
                self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, ema_value, ema_ready):
        if candle.State != CandleStates.Finished:
            return

        ibs = self._previous_ibs
        previous_close = self._previous_close
        previous_ema = self._previous_ema
        filter_ready = self._previous_ema_ready
        candle_range = candle.HighPrice - candle.LowPrice
        self._previous_ibs = (Convert.ToDecimal(0.5) if candle_range == 0 else
                              (candle.ClosePrice - candle.LowPrice) / candle_range)
        self._previous_close = candle.ClosePrice
        self._previous_ema = ema_value
        self._previous_ema_ready = ema_ready
        self._bar_count += 1

        if ibs is None or not self.IsFormedAndOnlineAndAllowTrading():
            return

        entry_threshold = Convert.ToDecimal(self._ibs_entry_threshold.Value)
        exit_threshold = Convert.ToDecimal(self._ibs_exit_threshold.Value)
        if self.Position != 0:
            opposite_threshold = ibs > exit_threshold if self.Position > 0 else ibs < entry_threshold
            if (opposite_threshold or
                    self._bar_count - self._first_entry_bar >= self._max_trade_duration.Value):
                if self.Position > 0:
                    self.SellMarket(Math.Abs(self.Position))
                else:
                    self.BuyMarket(Math.Abs(self.Position))
                self._last_entry_price = Decimal.Zero
                self._first_entry_bar = 0
                return

        if self._use_ema_filter.Value and not filter_ready:
            return
        if (self.Position != 0 and self._last_entry_price > 0 and
                Decimal(100) * Math.Abs(candle.ClosePrice - self._last_entry_price) / self._last_entry_price <
                Convert.ToDecimal(self._min_entry_pct.Value)):
            return

        buy = (self._allow_long.Value and self.Position >= 0 and ibs < entry_threshold and
               (not self._use_ema_filter.Value or previous_close > previous_ema))
        sell = (self._allow_short.Value and self.Position <= 0 and ibs > exit_threshold and
                (not self._use_ema_filter.Value or previous_close < previous_ema))
        if not buy and not sell:
            return

        if self.Position == 0:
            self._first_entry_bar = self._bar_count
        self._last_entry_price = candle.ClosePrice
        if buy:
            self.BuyMarket()
        else:
            self.SellMarket()

    def CreateClone(self):
        return ibs_internal_bar_strength_strategy()
