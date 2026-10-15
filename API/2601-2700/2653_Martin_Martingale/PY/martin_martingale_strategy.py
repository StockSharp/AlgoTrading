import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class martin_martingale_strategy(Strategy):
    """Martingale grid that alternates long/short entries while doubling volume."""

    def __init__(self):
        super(martin_martingale_strategy, self).__init__()

        self._step_points = self.Param("StepPoints", 10) \
            .SetGreaterThanZero() \
            .SetDisplay("Step (points)", "Distance multiplier for reversals", "General")
        self._entry_offset_points = self.Param("EntryOffsetPoints", 10) \
            .SetGreaterThanZero() \
            .SetDisplay("Entry Offset (points)", "Offset for initial breakout entry", "General")
        self._profit_target = self.Param("ProfitTarget", 5.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Profit Target", "Total profit to close all positions", "Risk")
        self._max_level = self.Param("MaxLevel", 5) \
            .SetGreaterThanZero() \
            .SetDisplay("Max Level", "Maximum martingale levels", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candles for price monitoring", "Data")

        self._step_size = 0.0
        self._entry_offset = 0.0
        self._last_trade_price = 0.0
        self._last_trade_volume = 0.0
        self._martingale_level = 0
        self._last_trade_side = 0  # 0=none, 1=buy, -1=sell
        self._is_closing = False
        self._initial_price = None

    @property
    def StepPoints(self):
        return int(self._step_points.Value)
    @property
    def EntryOffsetPoints(self):
        return int(self._entry_offset_points.Value)
    @property
    def ProfitTarget(self):
        return self._profit_target.Value
    @property
    def MaxLevel(self):
        return int(self._max_level.Value)
    @property
    def CandleType(self):
        return self._candle_type.Value

    def _update_step_settings(self):
        sec = self.Security
        step = float(sec.PriceStep) if sec is not None and sec.PriceStep is not None and float(sec.PriceStep) > 0 else 1.0
        self._step_size = self.StepPoints * step
        self._entry_offset = self.EntryOffsetPoints * step

    def _reset_cycle(self):
        self._martingale_level = 0
        self._last_trade_price = 0.0
        self._last_trade_volume = 0.0
        self._last_trade_side = 0

    def OnStarted2(self, time):
        super(martin_martingale_strategy, self).OnStarted2(time)

        self._update_step_settings()
        self._reset_cycle()
        self._is_closing = False
        self._initial_price = None

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        self._update_step_settings()

        if self._step_size <= 0 or float(self.Volume) <= 0:
            return

        price = float(candle.ClosePrice)

        # If closing, flatten and wait
        if self._is_closing:
            if self.Position == 0:
                self._is_closing = False
                self._reset_cycle()
            return

        # If flat after a cycle, reset
        if self.Position == 0 and self._martingale_level > 0:
            self._reset_cycle()

        # Check profit target
        if float(self.ProfitTarget) > 0 and float(self.PnL) >= float(self.ProfitTarget) and self.Position != 0:
            self._is_closing = True
            if self.Position > 0:
                self.SellMarket()
            elif self.Position < 0:
                self.BuyMarket()
            return

        # Max level reached
        if self._martingale_level >= self.MaxLevel and self.Position != 0:
            self._is_closing = True
            if self.Position > 0:
                self.SellMarket()
            elif self.Position < 0:
                self.BuyMarket()
            return

        # Initial entry: wait for breakout from first candle
        if self._martingale_level == 0 and self.Position == 0:
            if self._initial_price is None:
                self._initial_price = price
                return

            if self._entry_offset <= 0:
                return

            if price >= self._initial_price + self._entry_offset:
                self.BuyMarket()
                self._last_trade_price = price
                self._last_trade_volume = float(self.Volume)
                self._last_trade_side = 1
                self._martingale_level = 1
                self._initial_price = None
            elif price <= self._initial_price - self._entry_offset:
                self.SellMarket()
                self._last_trade_price = price
                self._last_trade_volume = float(self.Volume)
                self._last_trade_side = -1
                self._martingale_level = 1
                self._initial_price = None
            return

        if self._last_trade_side == 0 or self._martingale_level == 0:
            return

        threshold = self._step_size

        if self._last_trade_side == 1:
            if price <= self._last_trade_price - threshold:
                self.SellMarket()
                self._last_trade_price = price
                self._last_trade_volume *= 2.0
                self._last_trade_side = -1
                self._martingale_level += 1
        else:
            if price >= self._last_trade_price + threshold:
                self.BuyMarket()
                self._last_trade_price = price
                self._last_trade_volume *= 2.0
                self._last_trade_side = 1
                self._martingale_level += 1

    def OnReseted(self):
        super(martin_martingale_strategy, self).OnReseted()
        self._reset_cycle()
        self._is_closing = False
        self._initial_price = None
        self._step_size = 0.0
        self._entry_offset = 0.0

    def CreateClone(self):
        return martin_martingale_strategy()
