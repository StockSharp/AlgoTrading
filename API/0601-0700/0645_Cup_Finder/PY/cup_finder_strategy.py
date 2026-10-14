import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")

from collections import deque
from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Strategies import Strategy


class cup_finder_strategy(Strategy):
    """
    Cup Finder strategy.
    The previous Lookback candles are split into thirds. A cup has rims (highest highs of the outer thirds) within WidthPercent
    of each other and a middle third that stays below the lower rim; a close above the higher rim buys. An inverted cup has
    troughs (lowest lows of the outer thirds) within WidthPercent of each other and a middle third that stays above the higher
    trough; a close below the lower trough sells short. An opposite breakout reverses the position and a percent stop limits the loss.
    """

    def __init__(self):
        super(cup_finder_strategy, self).__init__()
        self._lookback = self.Param("Lookback", 150).SetRange(3, 10000).SetDisplay("Lookback", "Candles the cup is searched in", "Pattern")
        self._width_percent = self.Param("WidthPercent", 5.0).SetNotNegative().SetDisplay("Width %", "Maximum difference between the two rims in percent", "Pattern")
        self._stop_loss_percent = self.Param("StopLossPercent", 1.0).SetNotNegative().SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._bars = deque()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(cup_finder_strategy, self).OnReseted()
        self._bars = deque()

    def OnStarted2(self, time):
        super(cup_finder_strategy, self).OnStarted2(time)

        self._bars = deque()

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()

        stop = float(self._stop_loss_percent.Value)
        self.StartProtection(Unit(), Unit(Decimal(stop), UnitTypes.Percent) if stop > 0 else Unit(), useMarketOrders=True)

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        lookback = self._lookback.Value
        width = float(self._width_percent.Value)
        close = float(candle.ClosePrice)
        ready = len(self._bars) >= lookback
        bullish = False
        bearish = False

        if ready:
            bars = list(self._bars)
            third = len(bars) // 3
            left = bars[:third]
            middle = bars[third:len(bars) - third]
            right = bars[len(bars) - third:]

            left_rim = max(b[0] for b in left)
            right_rim = max(b[0] for b in right)
            middle_high = max(b[0] for b in middle)
            rim = max(left_rim, right_rim)
            is_cup = rim > 0 and (rim - min(left_rim, right_rim)) / rim * 100.0 <= width and middle_high < min(left_rim, right_rim)
            bullish = is_cup and close > rim

            left_trough = min(b[1] for b in left)
            right_trough = min(b[1] for b in right)
            middle_low = min(b[1] for b in middle)
            trough = min(left_trough, right_trough)
            higher_trough = max(left_trough, right_trough)
            is_inverted = higher_trough > 0 and (higher_trough - trough) / higher_trough * 100.0 <= width and middle_low > higher_trough
            bearish = is_inverted and close < trough

        self._bars.append((float(candle.HighPrice), float(candle.LowPrice)))
        while len(self._bars) > lookback:
            self._bars.popleft()

        if not ready or not self.IsFormedAndOnlineAndAllowTrading():
            return

        if bullish and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif bearish and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))

    def CreateClone(self):
        return cup_finder_strategy()
