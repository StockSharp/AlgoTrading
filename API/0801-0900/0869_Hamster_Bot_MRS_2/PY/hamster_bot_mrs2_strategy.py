import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from collections import deque
from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class hamster_bot_mrs2_strategy(Strategy):
    """
    Hamster Bot MRS 2 strategy.
    The level is the simple moving average shifted Shift candles back. A close crossing above the level buys and a close crossing
    below it sells; the reverse crossing of the same level closes the position and opens the opposite one.
    """

    def __init__(self):
        super(hamster_bot_mrs2_strategy, self).__init__()
        self._ma_length = self.Param("MaLength", 3).SetGreaterThanZero().SetDisplay("MA Length", "Moving average length", "Indicators")
        self._shift = self.Param("Shift", 1).SetNotNegative().SetDisplay("Shift", "Candles the moving average level is shifted back", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._ma_history = deque()
        self._prev_close = None
        self._prev_level = None

    def OnReseted(self):
        super(hamster_bot_mrs2_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(hamster_bot_mrs2_strategy, self).OnStarted2(time)

        self._reset_state()

        sma = SimpleMovingAverage()
        sma.Length = self._ma_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(sma, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sma)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, ma_value):
        if candle.State != CandleStates.Finished:
            return

        shift = self._shift.Value
        self._ma_history.append(float(ma_value))
        while len(self._ma_history) > shift + 1:
            self._ma_history.popleft()

        if len(self._ma_history) < shift + 1:
            return

        # The oldest kept value is the average Shift candles ago.
        level = self._ma_history[0]
        close = float(candle.ClosePrice)

        prev_close = self._prev_close
        prev_level = self._prev_level

        self._prev_close = close
        self._prev_level = level

        if prev_close is None or prev_level is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if prev_close <= prev_level and close > level and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif prev_close >= prev_level and close < level and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))

    def CreateClone(self):
        return hamster_bot_mrs2_strategy()
