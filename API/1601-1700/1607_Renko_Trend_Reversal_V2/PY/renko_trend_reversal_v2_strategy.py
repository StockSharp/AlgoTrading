import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class renko_trend_reversal_v2_strategy(Strategy):
    def __init__(self):
        super(renko_trend_reversal_v2_strategy, self).__init__()

        self._renko_atr_length = self.Param("RenkoAtrLength", 10).SetGreaterThanZero()
        self._stop_loss_pct = self.Param("StopLossPct", 3.0).SetNotNegative()
        self._take_profit_pct = self.Param("TakeProfitPct", 20.0).SetNotNegative()
        self._allow_shorts = self.Param("AllowShorts", True)
        self._trade_start = self.Param("TradeStart", TimeSpan.Zero)
        self._trade_end = self.Param("TradeEnd", TimeSpan(23, 59, 59))
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))

        self._brick_close = None
        self._last_brick_direction = 0

    def GetWorkingSecurities(self):
        return [(self.Security, self._candle_type.Value)]

    def OnReseted(self):
        super(renko_trend_reversal_v2_strategy, self).OnReseted()
        self._brick_close = None
        self._last_brick_direction = 0

    def OnStarted2(self, time):
        super(renko_trend_reversal_v2_strategy, self).OnStarted2(time)

        tp = float(self._take_profit_pct.Value)
        sl = float(self._stop_loss_pct.Value)
        if tp > 0 or sl > 0:
            self.StartProtection(
                Unit(tp, UnitTypes.Percent) if tp > 0 else None,
                Unit(sl, UnitTypes.Percent) if sl > 0 else None)

        atr = AverageTrueRange()
        atr.Length = int(self._renko_atr_length.Value)

        def on_candle(candle, atr_value):
            if candle.State != CandleStates.Finished or not atr.IsFormed or float(atr_value) <= 0:
                return
            self._process_adaptive_renko(candle, float(atr_value))

        subscription = self.SubscribeCandles(self._candle_type.Value)
        subscription.Bind(atr, on_candle).Start()

    def _process_adaptive_renko(self, candle, brick_size):
        close_price = float(candle.ClosePrice)
        if self._brick_close is None:
            self._brick_close = close_price
            return

        while close_price >= self._brick_close + brick_size:
            open_price = self._brick_close
            brick_close = open_price + brick_size
            self._process_brick(candle.OpenTime, open_price, brick_close)
            self._brick_close = brick_close

        while close_price <= self._brick_close - brick_size:
            open_price = self._brick_close
            brick_close = open_price - brick_size
            self._process_brick(candle.OpenTime, open_price, brick_close)
            self._brick_close = brick_close

    def _process_brick(self, time, open_price, close_price):
        direction = 1 if close_price > open_price else -1
        reversal = self._last_brick_direction != 0 and direction != self._last_brick_direction
        self._last_brick_direction = direction

        if not reversal or not self._in_window(time.TimeOfDay):
            return

        if direction > 0 and self.Position <= 0:
            self.BuyMarket(self.Volume + Math.Abs(self.Position))
        elif direction < 0 and bool(self._allow_shorts.Value) and self.Position >= 0:
            self.SellMarket(self.Volume + Math.Abs(self.Position))
        elif direction < 0 and not bool(self._allow_shorts.Value) and self.Position > 0:
            self.SellMarket(Math.Abs(self.Position))

    def _in_window(self, value):
        start = self._trade_start.Value
        end = self._trade_end.Value
        if start <= end:
            return value >= start and value <= end
        return value >= start or value <= end

    def CreateClone(self):
        return renko_trend_reversal_v2_strategy()
