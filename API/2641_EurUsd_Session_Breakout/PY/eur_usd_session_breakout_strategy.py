import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Highest, Lowest, CandleIndicatorValue
from StockSharp.Algo.Strategies import Strategy

class eur_usd_session_breakout_strategy(Strategy):
    """
    Session breakout strategy using rolling highest/lowest channels.
    Buys on breakout above rolling highest, sells on breakout below rolling lowest.
    Manual SL/TP management.
    """

    def __init__(self):
        super(eur_usd_session_breakout_strategy, self).__init__()
        self._eu_session_length = self.Param("EuSessionLengthBars", 10) \
            .SetDisplay("Range Session Length", "Number of bars for range", "Schedule")
        self._stop_loss_dist = self.Param("StopLossDistance", 5.0) \
            .SetDisplay("Stop Loss Distance", "Stop loss in price units", "Risk")
        self._take_profit_dist = self.Param("TakeProfitDistance", 8.0) \
            .SetDisplay("Take Profit Distance", "Take profit in price units", "Risk")
        self._breakout_buffer = self.Param("BreakoutBuffer", 0.0) \
            .SetDisplay("Breakout Buffer", "Extra buffer for breakout trigger", "Entries")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candle type", "General")

        self._highest = None
        self._lowest = None
        self._current_highest = 0.0
        self._current_lowest = 0.0
        self._entry_price = 0.0
        self._stop_price = 0.0
        self._take_price = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(eur_usd_session_breakout_strategy, self).OnReseted()
        self._highest = None
        self._lowest = None
        self._current_highest = 0.0
        self._current_lowest = 0.0
        self._entry_price = 0.0
        self._stop_price = 0.0
        self._take_price = 0.0

    def OnStarted2(self, time):
        super(eur_usd_session_breakout_strategy, self).OnStarted2(time)

        self._highest = Highest()
        self._highest.Length = self._eu_session_length.Value
        self._lowest = Lowest()
        self._lowest.Length = self._eu_session_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        self._manage_position(candle)

        prev_highest = self._current_highest
        prev_lowest = self._current_lowest

        h_result = self._highest.Process(CandleIndicatorValue(self._highest, candle))
        l_result = self._lowest.Process(CandleIndicatorValue(self._lowest, candle))
        self._current_highest = float(h_result) if not h_result.IsEmpty else self._current_highest
        self._current_lowest = float(l_result) if not l_result.IsEmpty else self._current_lowest

        if not self._highest.IsFormed or not self._lowest.IsFormed:
            return

        if prev_highest <= 0 or prev_lowest <= 0:
            return

        if self.Position != 0:
            return

        close = float(candle.ClosePrice)
        buf = float(self._breakout_buffer.Value)

        if close > prev_highest + buf:
            self.BuyMarket()
            self._set_long_targets(close)
        elif close < prev_lowest - buf:
            self.SellMarket()
            self._set_short_targets(close)

    def _manage_position(self, candle):
        if self.Position == 0:
            return

        low = float(candle.LowPrice)
        high = float(candle.HighPrice)
        sl_dist = float(self._stop_loss_dist.Value)
        tp_dist = float(self._take_profit_dist.Value)

        if self.Position > 0:
            exit_stop = sl_dist > 0 and low <= self._stop_price
            exit_take = tp_dist > 0 and high >= self._take_price
            if exit_stop or exit_take:
                self.SellMarket()
                self._clear_targets()
        elif self.Position < 0:
            exit_stop = sl_dist > 0 and high >= self._stop_price
            exit_take = tp_dist > 0 and low <= self._take_price
            if exit_stop or exit_take:
                self.BuyMarket()
                self._clear_targets()

    def _set_long_targets(self, price):
        self._entry_price = price
        self._stop_price = price - float(self._stop_loss_dist.Value)
        self._take_price = price + float(self._take_profit_dist.Value)

    def _set_short_targets(self, price):
        self._entry_price = price
        self._stop_price = price + float(self._stop_loss_dist.Value)
        self._take_price = price - float(self._take_profit_dist.Value)

    def _clear_targets(self):
        self._entry_price = 0.0
        self._stop_price = 0.0
        self._take_price = 0.0

    def CreateClone(self):
        return eur_usd_session_breakout_strategy()
