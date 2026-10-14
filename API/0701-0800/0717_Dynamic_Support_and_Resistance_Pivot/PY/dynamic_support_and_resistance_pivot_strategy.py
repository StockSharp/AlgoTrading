import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Strategies import Strategy


class dynamic_support_and_resistance_pivot_strategy(Strategy):
    """
    Dynamic Support and Resistance Pivot strategy.
    Support is the last pivot low and resistance the last pivot high, a pivot being a candle whose low (high) is beyond those of the
    PivotLength candles on each side. A close crossing above support goes long and a close crossing below resistance goes short,
    reversing an opposite position, when the close is within SupportResistanceDistance percent of that level. Percent stop loss and
    take profit manage the position.
    """

    def __init__(self):
        super(dynamic_support_and_resistance_pivot_strategy, self).__init__()
        self._pivot_length = self.Param("PivotLength", 2).SetGreaterThanZero().SetDisplay("Pivot Length", "Candles on each side of a pivot", "Pivots")
        self._support_resistance_distance = self.Param("SupportResistanceDistance", 0.4).SetNotNegative().SetDisplay("S/R Distance %", "Maximum distance of the close from the level, in percent", "Pivots")
        self._stop_loss_percent = self.Param("StopLossPercent", 10.0).SetNotNegative().SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk")
        self._take_profit_percent = self.Param("TakeProfitPercent", 26.0).SetNotNegative().SetDisplay("Take Profit %", "Take profit percentage from entry price", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._highs = []
        self._lows = []
        self._support = None
        self._resistance = None
        self._prev_close = None

    def OnReseted(self):
        super(dynamic_support_and_resistance_pivot_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(dynamic_support_and_resistance_pivot_strategy, self).OnStarted2(time)

        self._reset_state()

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()

        take_percent = Decimal(self._take_profit_percent.Value)
        stop_percent = Decimal(self._stop_loss_percent.Value)
        take = Unit(take_percent, UnitTypes.Percent) if take_percent > 0 else Unit()
        stop = Unit(stop_percent, UnitTypes.Percent) if stop_percent > 0 else Unit()
        self.StartProtection(take, stop, useMarketOrders=True)

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        length = self._pivot_length.Value
        size = length * 2 + 1

        self._highs.append(candle.HighPrice)
        self._lows.append(candle.LowPrice)

        if len(self._highs) > size:
            self._highs.pop(0)
            self._lows.pop(0)

        if len(self._highs) == size:
            center_high = self._highs[length]
            center_low = self._lows[length]
            is_pivot_high = True
            is_pivot_low = True
            for i in range(size):
                if i == length:
                    continue
                if self._highs[i] >= center_high:
                    is_pivot_high = False
                if self._lows[i] <= center_low:
                    is_pivot_low = False
            if is_pivot_high:
                self._resistance = center_high
            if is_pivot_low:
                self._support = center_low

        close = candle.ClosePrice
        pc = self._prev_close
        self._prev_close = close

        if pc is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        max_distance = Decimal(self._support_resistance_distance.Value) / Decimal(100)

        support = self._support
        resistance = self._resistance

        long_signal = support is not None and support > 0 and pc <= support and close > support and \
            abs(close - support) / support <= max_distance
        short_signal = resistance is not None and resistance > 0 and pc >= resistance and close < resistance and \
            abs(close - resistance) / resistance <= max_distance

        if long_signal and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif short_signal and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))

    def CreateClone(self):
        return dynamic_support_and_resistance_pivot_strategy()
