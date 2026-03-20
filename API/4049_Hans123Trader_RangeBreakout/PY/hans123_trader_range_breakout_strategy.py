import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Highest, Lowest
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Messages import Unit, UnitTypes


class hans123_trader_range_breakout_strategy(Strategy):
    def __init__(self):
        super(hans123_trader_range_breakout_strategy, self).__init__()

        self._range_length = self.Param("RangeLength", 20) \
            .SetDisplay("Range Length", "Number of candles used to compute the breakout range.", "Breakout")
        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(5) \
            .SetDisplay("Range Length", "Number of candles used to compute the breakout range.", "Breakout")

        self._entry_price = 0.0
        self._stop_price = 0.0
        self._prev_highest = 0.0
        self._prev_lowest = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(hans123_trader_range_breakout_strategy, self).OnReseted()
        self._entry_price = 0.0
        self._stop_price = 0.0
        self._prev_highest = 0.0
        self._prev_lowest = 0.0

    def OnStarted(self, time):
        super(hans123_trader_range_breakout_strategy, self).OnStarted(time)

        self._highest = Highest()
        self._highest.Length = self.range_length
        self._lowest = Lowest()
        self._lowest.Length = self.range_length

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._highest, self._lowest, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return hans123_trader_range_breakout_strategy()
