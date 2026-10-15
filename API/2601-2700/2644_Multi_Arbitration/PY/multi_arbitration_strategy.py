import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class multi_arbitration_strategy(Strategy):
    """Multi-direction arbitration strategy with profit-based flattening."""

    def __init__(self):
        super(multi_arbitration_strategy, self).__init__()

        self._profit_for_close = self.Param("ProfitForClose", 300.0) \
            .SetDisplay("Profit Threshold", "Profit required before flattening all positions.", "Risk")
        self._max_open_positions = self.Param("MaxOpenPositions", 15) \
            .SetGreaterThanZero() \
            .SetDisplay("Max Open Positions", "Maximum simultaneous positions allowed before closing everything.", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candle type used to synchronize trading decisions.", "Data")

        self._initial_order_placed = False
        self._entry_price = 0.0
        self._current_side = 0  # 0=none, 1=buy, -1=sell

    @property
    def ProfitForClose(self):
        return self._profit_for_close.Value
    @property
    def MaxOpenPositions(self):
        return self._max_open_positions.Value
    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnStarted2(self, time):
        super(multi_arbitration_strategy, self).OnStarted2(time)

        self._initial_order_placed = False
        self._entry_price = 0.0
        self._current_side = 0

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.process_candle).Start()

    def process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        vol = float(self.Volume) if self.Volume > 0 else 1.0

        if not self._initial_order_placed:
            self._open_long(close)
            self._initial_order_placed = True

        long_count = 1 if self._current_side == 1 else 0
        short_count = 1 if self._current_side == -1 else 0

        long_profit = (close - self._entry_price) * vol if self._current_side == 1 else 0.0
        short_profit = (self._entry_price - close) * vol if self._current_side == -1 else 0.0

        if long_count + short_count < self.MaxOpenPositions:
            if long_profit < short_profit and self._current_side != 1:
                self._open_long(close)
            elif short_profit < long_profit and self._current_side != -1:
                self._open_short(close)
            elif long_profit == 0.0 and short_profit == 0.0 and self.Position == 0 and self._current_side == 0:
                self._open_long(close)
        elif float(self.PnL) > 0.0 and self.Position != 0:
            self._flatten(close)

        if float(self.PnL) > float(self.ProfitForClose) and self.Position != 0:
            self._flatten(close)

    def _open_long(self, close):
        if self.Position > 0:
            self._entry_price = close
            self._current_side = 1
            return
        self.BuyMarket()
        self._entry_price = close
        self._current_side = 1

    def _open_short(self, close):
        if self.Position < 0:
            self._entry_price = close
            self._current_side = -1
            return
        self.SellMarket()
        self._entry_price = close
        self._current_side = -1

    def _flatten(self, close):
        if self._current_side == 0:
            return
        if self.Position > 0:
            self.SellMarket()
        elif self.Position < 0:
            self.BuyMarket()
        self._current_side = 0
        self._entry_price = 0.0

    def OnReseted(self):
        super(multi_arbitration_strategy, self).OnReseted()
        self._initial_order_placed = False
        self._entry_price = 0.0
        self._current_side = 0

    def CreateClone(self):
        return multi_arbitration_strategy()
