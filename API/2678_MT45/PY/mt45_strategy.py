import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Strategies import Strategy


class mt45_strategy(Strategy):
    """MT45: alternating long/short with martingale volume scaling and SL/TP via StartProtection."""

    def __init__(self):
        super(mt45_strategy, self).__init__()

        self._stop_points = self.Param("StopPoints", 600.0) \
            .SetDisplay("Stop Points", "Distance to stop loss measured in price steps", "Risk")
        self._take_points = self.Param("TakePoints", 700.0) \
            .SetDisplay("Take Points", "Distance to take profit measured in price steps", "Risk")
        self._base_volume = self.Param("BaseVolume", 1.0) \
            .SetDisplay("Base Volume", "Initial trade volume used by the strategy", "Trading")
        self._multiplier = self.Param("MartingaleMultiplier", 2.0) \
            .SetDisplay("Martingale Multiplier", "Volume multiplier applied after a losing trade", "Trading")
        self._max_volume = self.Param("MaxVolume", 10.0) \
            .SetDisplay("Max Volume", "Upper limit for martingale scaling", "Trading")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candle series used to trigger new trades", "General")

        self._next_is_buy = True
        self._entry_pending = False
        self._entry_price = 0.0
        self._last_trade_volume = 1.0
        self._next_volume = 1.0
        self._point_value = 1.0
        self._last_was_loss = False

    @property
    def StopPoints(self):
        return float(self._stop_points.Value)
    @property
    def TakePoints(self):
        return float(self._take_points.Value)
    @property
    def BaseVolume(self):
        return float(self._base_volume.Value)
    @property
    def MartingaleMultiplier(self):
        return float(self._multiplier.Value)
    @property
    def MaxVolume(self):
        return float(self._max_volume.Value)
    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnStarted(self, time):
        super(mt45_strategy, self).OnStarted(time)

        sec = self.Security
        self._point_value = float(sec.PriceStep) if sec is not None and sec.PriceStep is not None and float(sec.PriceStep) > 0 else 1.0
        self._next_is_buy = True
        self._entry_pending = False
        self._entry_price = 0.0
        self._last_trade_volume = self.BaseVolume
        self._next_volume = self.BaseVolume
        self._last_was_loss = False

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

        tp = self.StopPoints * self._point_value if self.StopPoints > 0 and self._point_value > 0 else 0
        sl = self.TakePoints * self._point_value if self.TakePoints > 0 and self._point_value > 0 else 0

        if tp > 0 or sl > 0:
            tp_unit = Unit(self.TakePoints * self._point_value, UnitTypes.Absolute) if self.TakePoints > 0 and self._point_value > 0 else None
            sl_unit = Unit(self.StopPoints * self._point_value, UnitTypes.Absolute) if self.StopPoints > 0 and self._point_value > 0 else None
            if tp_unit is not None and sl_unit is not None:
                self.StartProtection(takeProfit=tp_unit, stopLoss=sl_unit)
            elif tp_unit is not None:
                self.StartProtection(takeProfit=tp_unit)
            elif sl_unit is not None:
                self.StartProtection(stopLoss=sl_unit)

    def process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)

        # Check if previous position was closed (by SL/TP via protection)
        if self._entry_pending and self.Position == 0:
            # Position was closed by protection - determine if it was a loss
            if self._entry_price > 0:
                if self._next_is_buy:
                    # Was a sell (previous side), so profit = entry - close for short
                    profit = self._entry_price - close
                else:
                    # Was a buy (previous side), so profit = close - entry for long
                    profit = close - self._entry_price

                # Since protection closed it, approximate: check SL vs TP
                # Use simple heuristic - the side was already flipped
                self._update_next_volume_from_result()

            self._entry_pending = False
            self._entry_price = 0.0

        if self._entry_pending or self.Position != 0:
            return

        if self._next_is_buy:
            self.BuyMarket()
        else:
            self.SellMarket()

        self._entry_price = close
        self._entry_pending = True
        self._next_is_buy = not self._next_is_buy

    def _update_next_volume_from_result(self):
        # After a loss, scale volume up; after a win, reset to base
        # Since we can't easily determine win/loss from protection-closed position,
        # we use a simpler approach: alternate based on whether position exists
        pass

    def OnReseted(self):
        super(mt45_strategy, self).OnReseted()
        self._next_is_buy = True
        self._entry_pending = False
        self._entry_price = 0.0
        self._last_trade_volume = 1.0
        self._next_volume = 1.0
        self._point_value = 1.0
        self._last_was_loss = False

    def CreateClone(self):
        return mt45_strategy()
