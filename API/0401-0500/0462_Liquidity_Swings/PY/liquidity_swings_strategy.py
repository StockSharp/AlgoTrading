import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy

REWARD_MULTIPLIER = 2.0


class liquidity_swings_strategy(Strategy):
    """
    Liquidity Swings Strategy.
    The latest pivot high (Lookback bars on each side) is resistance and the latest pivot low is support. A long opens when
    the low crosses above support with the close below resistance; a short opens when the high crosses below resistance with
    the close above support. The stop sits StopLossBuffer beyond the level and the target is twice the risk from the entry.
    """

    def __init__(self):
        super(liquidity_swings_strategy, self).__init__()
        self._lookback = self.Param("Lookback", 5).SetGreaterThanZero().SetDisplay("Lookback", "Bars on each side of a pivot", "Pivots")
        self._stop_loss_buffer = self.Param("StopLossBuffer", 0.5).SetNotNegative().SetDisplay("Stop Loss Buffer", "Price distance of the stop beyond the level", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._window = []
        self._support = None
        self._resistance = None
        self._prev_low = None
        self._prev_high = None
        self._stop_price = None
        self._target_price = None

    def OnReseted(self):
        super(liquidity_swings_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(liquidity_swings_strategy, self).OnStarted2(time)

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

        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        close = float(candle.ClosePrice)

        self._update_pivots(high, low)

        prev_low = self._prev_low
        prev_high = self._prev_high
        self._prev_low = low
        self._prev_high = high

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self.Position > 0:
            if (self._stop_price is not None and low <= self._stop_price) or (self._target_price is not None and high >= self._target_price):
                self.SellMarket(self.Position)
            return

        if self.Position < 0:
            if (self._stop_price is not None and high >= self._stop_price) or (self._target_price is not None and low <= self._target_price):
                self.BuyMarket(-self.Position)
            return

        if self._support is None or self._resistance is None or prev_low is None or prev_high is None:
            return

        support = self._support
        resistance = self._resistance
        buffer = float(self._stop_loss_buffer.Value)

        if prev_low <= support and low > support and close < resistance:
            stop = support - buffer
            risk = close - stop
            if risk <= 0:
                return
            self.BuyMarket(self.Volume)
            self._stop_price = stop
            self._target_price = close + REWARD_MULTIPLIER * risk
        elif prev_high >= resistance and high < resistance and close > support:
            stop = resistance + buffer
            risk = stop - close
            if risk <= 0:
                return
            self.SellMarket(self.Volume)
            self._stop_price = stop
            self._target_price = close - REWARD_MULTIPLIER * risk

    def _update_pivots(self, high, low):
        self._window.append((high, low))

        lookback = self._lookback.Value
        size = lookback * 2 + 1
        if len(self._window) > size:
            self._window.pop(0)

        if len(self._window) < size:
            return

        # The middle candle is a pivot once Lookback candles on each side confirm it.
        center = self._window[lookback]
        others = [c for i, c in enumerate(self._window) if i != lookback]

        if all(c[0] < center[0] for c in others):
            self._resistance = center[0]

        if all(c[1] > center[1] for c in others):
            self._support = center[1]

    def CreateClone(self):
        return liquidity_swings_strategy()
