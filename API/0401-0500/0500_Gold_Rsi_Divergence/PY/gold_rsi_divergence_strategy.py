import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy

LONG_RSI_LIMIT = 40
SHORT_RSI_LIMIT = 60


class gold_rsi_divergence_strategy(Strategy):
    """
    Gold RSI Divergence strategy.
    RSI pivots are confirmed LookbackLeft bars before and LookbackRight bars after them. A pivot low whose RSI is higher than the
    previous pivot low while price made a lower low, within RangeLower..RangeUpper bars of it and with RSI below 40, buys; the mirrored
    bearish divergence with RSI above 60 sells. Positions are closed by a fixed stop loss and take profit in pips.
    """

    def __init__(self):
        super(gold_rsi_divergence_strategy, self).__init__()
        self._rsi_length = self.Param("RsiLength", 60).SetGreaterThanZero().SetDisplay("RSI Length", "RSI period", "RSI")
        self._lookback_left = self.Param("LookbackLeft", 5).SetGreaterThanZero().SetDisplay("Lookback Left", "Bars to the left of a pivot", "Divergence")
        self._lookback_right = self.Param("LookbackRight", 5).SetGreaterThanZero().SetDisplay("Lookback Right", "Bars to the right of a pivot", "Divergence")
        self._range_lower = self.Param("RangeLower", 5).SetNotNegative().SetDisplay("Range Lower", "Minimum bars between two pivots", "Divergence")
        self._range_upper = self.Param("RangeUpper", 60).SetGreaterThanZero().SetDisplay("Range Upper", "Maximum bars between two pivots", "Divergence")
        self._stop_loss_pips = self.Param("StopLossPips", 11.0).SetNotNegative().SetDisplay("Stop Loss Pips", "Stop loss in pips (price steps)", "Risk")
        self._take_profit_pips = self.Param("TakeProfitPips", 33.0).SetNotNegative().SetDisplay("Take Profit Pips", "Take profit in pips (price steps)", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._window = []
        self._bar_index = 0
        self._last_pivot_low = None
        self._last_pivot_high = None

    def OnReseted(self):
        super(gold_rsi_divergence_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(gold_rsi_divergence_strategy, self).OnStarted2(time)

        self._reset_state()

        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(rsi, self._process_candle).Start()

        step = self.Security.PriceStep if self.Security.PriceStep is not None else Decimal(1)
        take = Decimal(self._take_profit_pips.Value) * step
        stop = Decimal(self._stop_loss_pips.Value) * step
        self.StartProtection(Unit(take, UnitTypes.Absolute), Unit(stop, UnitTypes.Absolute), useMarketOrders=True)

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)
            oscillators = self.CreateChartArea()
            if oscillators is not None:
                self.DrawIndicator(oscillators, rsi)

    def _in_range(self, bars):
        return self._range_lower.Value <= bars <= self._range_upper.Value

    def _process_candle(self, candle, rsi_value):
        if candle.State != CandleStates.Finished:
            return

        if not rsi_value.IsFormed:
            return

        rsi = rsi_value.GetValue[Decimal](None)
        self._bar_index += 1

        left = self._lookback_left.Value
        right = self._lookback_right.Value
        size = left + right + 1

        self._window.append((rsi, candle.LowPrice, candle.HighPrice))
        if len(self._window) > size:
            self._window.pop(0)

        if len(self._window) < size:
            return

        pivot_rsi, pivot_low, pivot_high = self._window[left]
        pivot_bar = self._bar_index - right
        is_pivot_low = True
        is_pivot_high = True

        for i in range(size):
            if i == left:
                continue
            if self._window[i][0] <= pivot_rsi:
                is_pivot_low = False
            if self._window[i][0] >= pivot_rsi:
                is_pivot_high = False

        can_trade = self.IsFormedAndOnlineAndAllowTrading()

        if is_pivot_low:
            prev = self._last_pivot_low
            bullish = (prev is not None and self._in_range(pivot_bar - prev[2])
                and pivot_rsi > prev[0] and pivot_low < prev[1])

            if can_trade and bullish and rsi < LONG_RSI_LIMIT and self.Position <= 0:
                self.BuyMarket(self.Volume + abs(self.Position))

            self._last_pivot_low = (pivot_rsi, pivot_low, pivot_bar)

        if is_pivot_high:
            prev = self._last_pivot_high
            bearish = (prev is not None and self._in_range(pivot_bar - prev[2])
                and pivot_rsi < prev[0] and pivot_high > prev[1])

            if can_trade and bearish and rsi > SHORT_RSI_LIMIT and self.Position >= 0:
                self.SellMarket(self.Volume + abs(self.Position))

            self._last_pivot_high = (pivot_rsi, pivot_high, pivot_bar)

    def CreateClone(self):
        return gold_rsi_divergence_strategy()
