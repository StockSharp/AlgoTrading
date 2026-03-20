import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import Highest, Lowest

class hans123_trader_range_breakout_strategy(Strategy):
    def __init__(self):
        super(hans123_trader_range_breakout_strategy, self).__init__()

        self._range_length = self.Param("RangeLength", 20) \
            .SetDisplay("Range Length", "Number of candles used to compute the breakout range", "Breakout")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Candle series for range detection", "General")

        self._entry_price = 0.0
        self._stop_price = 0.0
        self._prev_highest = 0.0
        self._prev_lowest = 0.0

    @property
    def RangeLength(self):
        return self._range_length.Value

    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnStarted(self, time):
        super(hans123_trader_range_breakout_strategy, self).OnStarted(time)

        self._entry_price = 0.0
        self._stop_price = 0.0
        self._prev_highest = 0.0
        self._prev_lowest = 0.0

        self._highest = Highest()
        self._highest.Length = self.RangeLength
        self._lowest = Lowest()
        self._lowest.Length = self.RangeLength

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._highest, self._lowest, self.ProcessCandle).Start()

        self.StartProtection(
            Unit(2, UnitTypes.Percent),
            Unit(1, UnitTypes.Percent)
        )

    def ProcessCandle(self, candle, highest_value, lowest_value):
        if candle.State != CandleStates.Finished:
            return

        hv = float(highest_value)
        lv = float(lowest_value)

        if hv <= 0 or lv <= 0:
            self._prev_highest = hv
            self._prev_lowest = lv
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            self._prev_highest = hv
            self._prev_lowest = lv
            return

        # Entry on breakout using previous levels
        if self.Position == 0 and self._prev_highest > 0 and self._prev_lowest > 0:
            close = float(candle.ClosePrice)
            if close > self._prev_highest:
                self.BuyMarket()
                self._entry_price = close
            elif close < self._prev_lowest:
                self.SellMarket()
                self._entry_price = close

        self._prev_highest = hv
        self._prev_lowest = lv

    def OnReseted(self):
        super(hans123_trader_range_breakout_strategy, self).OnReseted()
        self._entry_price = 0.0
        self._stop_price = 0.0
        self._prev_highest = 0.0
        self._prev_lowest = 0.0

    def CreateClone(self):
        return hans123_trader_range_breakout_strategy()
