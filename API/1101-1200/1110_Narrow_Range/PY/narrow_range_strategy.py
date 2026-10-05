import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class narrow_range_strategy(Strategy):
    """
    Narrow range breakout strategy.
    A setup appears on an inside bar whose range is narrower than the range of the reference bar Length periods ago.
    The reference bar's high and low become breakout levels: a close above the high buys, a close below the low sells.
    The take profit is the reference range away from the entry level and the stop loss is StopLossPercent of that range.
    """

    def __init__(self):
        super(narrow_range_strategy, self).__init__()
        self._length = self.Param("Length", 4).SetGreaterThanZero().SetDisplay("Length", "Bars back to the reference bar", "General")
        self._stop_loss_percent = self.Param("StopLossPercent", 0.35).SetNotNegative().SetDisplay("Stop Loss", "Stop loss as a fraction of the reference range", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.candle_type)]

    def _reset_state(self):
        self._history = []
        self._breakout_high = None
        self._breakout_low = None
        self._setup_range = 0.0
        self._stop_price = 0.0
        self._take_price = 0.0

    def OnReseted(self):
        super(narrow_range_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(narrow_range_strategy, self).OnStarted2(time)

        self._reset_state()

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        if self.IsFormedAndOnlineAndAllowTrading():
            self._trade(candle)

        length = self._length.Value
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        self._history.append((high, low))
        if len(self._history) > length + 1:
            self._history.pop(0)

        if self.Position != 0 or len(self._history) <= length:
            return

        prev_high, prev_low = self._history[-2]
        ref_high, ref_low = self._history[0]
        rng = high - low
        ref_range = ref_high - ref_low
        inside_bar = high < prev_high and low > prev_low

        if inside_bar and rng < ref_range and ref_range > 0:
            self._breakout_high = ref_high
            self._breakout_low = ref_low
            self._setup_range = ref_range

    def _trade(self, candle):
        sl = float(self._stop_loss_percent.Value)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)

        if self.Position > 0:
            if (sl > 0 and low <= self._stop_price) or high >= self._take_price:
                self.SellMarket(self.Position)
            return

        if self.Position < 0:
            if (sl > 0 and high >= self._stop_price) or low <= self._take_price:
                self.BuyMarket(-self.Position)
            return

        if self._breakout_high is None or self._breakout_low is None:
            return

        close = float(candle.ClosePrice)
        if close > self._breakout_high:
            self.BuyMarket(self.Volume)
            self._take_price = self._breakout_high + self._setup_range
            self._stop_price = self._breakout_high - self._setup_range * sl
            self._breakout_high = None
            self._breakout_low = None
        elif close < self._breakout_low:
            self.SellMarket(self.Volume)
            self._take_price = self._breakout_low - self._setup_range
            self._stop_price = self._breakout_low + self._setup_range * sl
            self._breakout_high = None
            self._breakout_low = None

    def CreateClone(self):
        return narrow_range_strategy()
