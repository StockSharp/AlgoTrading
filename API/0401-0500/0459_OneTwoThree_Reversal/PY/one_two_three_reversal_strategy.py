import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class one_two_three_reversal_strategy(Strategy):
    """
    One-Two-Three Reversal Strategy.
    A long opens on a bullish 1-2-3 pattern: the current low is below the previous low, the previous low is below the low
    three bars ago, the low two bars ago is below the low four bars ago and the high two bars ago is below the high three
    bars ago. The long closes after DaysToHold bars or when the close crosses above the MaLength SMA.
    """

    def __init__(self):
        super(one_two_three_reversal_strategy, self).__init__()
        self._days_to_hold = self.Param("DaysToHold", 7).SetGreaterThanZero().SetDisplay("Days To Hold", "Bars to hold the position", "Trading")
        self._ma_length = self.Param("MaLength", 200).SetGreaterThanZero().SetDisplay("MA Length", "SMA period of the exit", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._history = []
        self._prev_close = None
        self._prev_ma = None
        self._bars_in_position = 0

    def OnReseted(self):
        super(one_two_three_reversal_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(one_two_three_reversal_strategy, self).OnStarted2(time)

        self._reset_state()

        sma = SimpleMovingAverage()
        sma.Length = self._ma_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(sma, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sma)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, sma_value):
        if candle.State != CandleStates.Finished:
            return

        self._history.append((float(candle.LowPrice), float(candle.HighPrice)))
        if len(self._history) > 5:
            self._history.pop(0)

        ma = float(sma_value.GetValue[Decimal](None)) if sma_value.IsFormed else None
        close = float(candle.ClosePrice)
        prev_close = self._prev_close
        prev_ma = self._prev_ma
        self._prev_close = close
        self._prev_ma = ma

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self.Position > 0:
            self._bars_in_position += 1

            cross_above_ma = ma is not None and prev_ma is not None and prev_close is not None and prev_close <= prev_ma and close > ma

            if self._bars_in_position >= self._days_to_hold.Value or cross_above_ma:
                self.SellMarket(self.Position)
                self._bars_in_position = 0

            return

        if len(self._history) < 5:
            return

        # history[4] is the current bar, history[0] is the bar four bars ago.
        current = self._history[4]
        bar1 = self._history[3]
        bar2 = self._history[2]
        bar3 = self._history[1]
        bar4 = self._history[0]

        pattern = (current[0] < bar1[0]
                   and bar1[0] < bar3[0]
                   and bar2[0] < bar4[0]
                   and bar2[1] < bar3[1])

        if pattern and self.Position == 0:
            self.BuyMarket(self.Volume)
            self._bars_in_position = 0

    def CreateClone(self):
        return one_two_three_reversal_strategy()
