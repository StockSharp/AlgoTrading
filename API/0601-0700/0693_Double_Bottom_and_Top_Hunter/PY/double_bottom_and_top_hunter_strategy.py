import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class double_bottom_and_top_hunter_strategy(Strategy):
    """
    Double Bottom and Top Hunter strategy.
    The recent window is the previous Length candles and the wider window adds the Lookback candles before them. A double bottom is
    a candle whose low reaches the lowest low of the older part of the wider window again while the recent candles stayed above it,
    and that closes back above it; a double top mirrors this with highs. A double bottom goes long and a double top goes short,
    reversing an opposite position. A long closes once price has made a new high above the recent high and then closes below the
    recent low; a short closes once price has made a new low below the recent low and then closes above the recent high.
    """

    def __init__(self):
        super(double_bottom_and_top_hunter_strategy, self).__init__()
        self._length = self.Param("Length", 100).SetGreaterThanZero().SetDisplay("Length", "Candles in the recent window", "Pattern")
        self._lookback = self.Param("Lookback", 100).SetGreaterThanZero().SetDisplay("Lookback", "Older candles that widen the window", "Pattern")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._highs = []
        self._lows = []
        self._new_extreme_since_entry = False

    def OnReseted(self):
        super(double_bottom_and_top_hunter_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(double_bottom_and_top_hunter_strategy, self).OnStarted2(time)

        self._reset_state()

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _add_candle(self, candle, total):
        self._highs.append(candle.HighPrice)
        self._lows.append(candle.LowPrice)
        while len(self._highs) > total:
            self._highs.pop(0)
            self._lows.pop(0)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        lookback = self._lookback.Value
        total = self._length.Value + lookback

        if len(self._highs) < total:
            self._add_candle(candle, total)
            return

        # Oldest first: the first Lookback entries are the older part, the last Length entries the recent one.
        older_high = max(self._highs[:lookback])
        older_low = min(self._lows[:lookback])
        recent_high = max(self._highs[lookback:])
        recent_low = min(self._lows[lookback:])

        self._add_candle(candle, total)

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        close = candle.ClosePrice

        double_bottom = older_low < recent_low and candle.LowPrice <= older_low and close > older_low
        double_top = older_high > recent_high and candle.HighPrice >= older_high and close < older_high

        if double_bottom and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
            self._new_extreme_since_entry = False
            return

        if double_top and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
            self._new_extreme_since_entry = False
            return

        if self.Position > 0:
            if candle.HighPrice > recent_high:
                self._new_extreme_since_entry = True
            elif self._new_extreme_since_entry and close < recent_low:
                self.SellMarket(self.Position)
                self._new_extreme_since_entry = False
        elif self.Position < 0:
            if candle.LowPrice < recent_low:
                self._new_extreme_since_entry = True
            elif self._new_extreme_since_entry and close > recent_high:
                self.BuyMarket(-self.Position)
                self._new_extreme_since_entry = False

    def CreateClone(self):
        return double_bottom_and_top_hunter_strategy()
