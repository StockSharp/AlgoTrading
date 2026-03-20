import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy

class raymond_cloudy_day_strategy(Strategy):
    def __init__(self):
        super(raymond_cloudy_day_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))
        self._protective_offset_pct = self.Param("ProtectiveOffsetPct", 1.0)

        self._take_profit_sell_level = None
        self._entry_price = None
        self._take_price = None
        self._stop_price = None
        self._direction = 0

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def ProtectiveOffsetPct(self):
        return self._protective_offset_pct.Value

    @ProtectiveOffsetPct.setter
    def ProtectiveOffsetPct(self, value):
        self._protective_offset_pct.Value = value

    def OnReseted(self):
        super(raymond_cloudy_day_strategy, self).OnReseted()
        self._take_profit_sell_level = None
        self._entry_price = None
        self._take_price = None
        self._stop_price = None
        self._direction = 0

    def OnStarted(self, time):
        super(raymond_cloudy_day_strategy, self).OnStarted(time)
        self._take_profit_sell_level = None
        self._entry_price = None
        self._take_price = None
        self._stop_price = None
        self._direction = 0

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._process_candle).Start()

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        open_p = float(candle.OpenPrice)
        close = float(candle.ClosePrice)

        # Compute Raymond levels from this candle (pivot-style)
        trade_session = (high + low + open_p + close) / 4.0
        pivot_range = high - low
        self._take_profit_sell_level = trade_session - 0.618 * pivot_range

        # Manage open position
        if self.Position != 0 and self._entry_price is not None and self._take_price is not None and self._stop_price is not None:
            if self.Position > 0:
                if low <= self._stop_price or high >= self._take_price:
                    self.SellMarket()
                    self._entry_price = None
                    self._take_price = None
                    self._stop_price = None
                    self._direction = 0
                    return
            elif self.Position < 0:
                if high >= self._stop_price or low <= self._take_price:
                    self.BuyMarket()
                    self._entry_price = None
                    self._take_price = None
                    self._stop_price = None
                    self._direction = 0
                    return

        if self._take_profit_sell_level is None:
            return

        trigger_level = self._take_profit_sell_level
        offset_pct = float(self.ProtectiveOffsetPct)

        # Long entry: price dips below TPS1 and closes above it
        if self.Position <= 0 and low < trigger_level and close > trigger_level:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            offset = close * offset_pct / 100.0
            self._entry_price = close
            self._take_price = close + offset
            self._stop_price = close - offset
            self._direction = 1
        # Short entry: price above TPS1 and closes below it
        elif self.Position >= 0 and low > trigger_level and close < trigger_level:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            offset = close * offset_pct / 100.0
            self._entry_price = close
            self._take_price = close - offset
            self._stop_price = close + offset
            self._direction = -1

    def CreateClone(self):
        return raymond_cloudy_day_strategy()
