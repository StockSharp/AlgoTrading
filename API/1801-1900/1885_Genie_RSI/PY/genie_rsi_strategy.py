import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy

class genie_rsi_strategy(Strategy):
    """
    RSI-based reversal strategy.
    Sells when RSI crosses above 80 (overbought), buys when RSI crosses below 20 (oversold).
    Includes trailing stop, take profit, and RSI exit signals.
    """

    def __init__(self):
        super(genie_rsi_strategy, self).__init__()
        self._take_profit = self.Param("TakeProfit", 500.0) \
            .SetDisplay("Take Profit", "Take profit distance in price units", "Risk Management")
        self._trailing_stop = self.Param("TrailingStop", 200.0) \
            .SetDisplay("Trailing Stop", "Trailing stop distance in price units", "Risk Management")
        self._rsi_period = self.Param("RsiPeriod", 15) \
            .SetDisplay("RSI Period", "Period for RSI indicator", "Indicators")
        self._cooldown_bars = self.Param("CooldownBars", 4) \
            .SetDisplay("Cooldown Bars", "Completed candles to wait after position change", "Risk Management")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")

        self._entry_price = 0.0
        self._trailing_level = 0.0
        self._is_long = False
        self._prev_rsi = None
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(genie_rsi_strategy, self).OnReseted()
        self._entry_price = 0.0
        self._trailing_level = 0.0
        self._is_long = False
        self._prev_rsi = None
        self._cooldown_remaining = 0

    def OnStarted2(self, time):
        super(genie_rsi_strategy, self).OnStarted2(time)

        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(rsi, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, rsi)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, rsi_val):
        if candle.State != CandleStates.Finished:
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1

        price = float(candle.ClosePrice)
        rsi = float(rsi_val)

        crossed_down = self._prev_rsi is not None and self._prev_rsi >= 20.0 and rsi < 20.0
        crossed_up = self._prev_rsi is not None and self._prev_rsi <= 80.0 and rsi > 80.0
        self._prev_rsi = rsi

        if self.Position == 0 and self._cooldown_remaining == 0:
            if crossed_up:
                self.SellMarket()
                self._entry_price = price
                self._trailing_level = price + self._trailing_stop.Value
                self._is_long = False
                self._cooldown_remaining = self._cooldown_bars.Value
            elif crossed_down:
                self.BuyMarket()
                self._entry_price = price
                self._trailing_level = price - self._trailing_stop.Value
                self._is_long = True
                self._cooldown_remaining = self._cooldown_bars.Value
            return

        if self._is_long:
            trailing = self._trailing_stop.Value
            if trailing > 0:
                new_level = price - trailing
                if new_level > self._trailing_level:
                    self._trailing_level = new_level
                if price <= self._trailing_level:
                    self.SellMarket()
                    self._entry_price = 0.0
                    self._cooldown_remaining = self._cooldown_bars.Value
                    return

            tp = self._take_profit.Value
            if tp > 0 and price - self._entry_price >= tp:
                self.SellMarket()
                self._entry_price = 0.0
                self._cooldown_remaining = self._cooldown_bars.Value
                return

            if crossed_up:
                self.SellMarket()
                self._entry_price = 0.0
                self._cooldown_remaining = self._cooldown_bars.Value
        else:
            trailing = self._trailing_stop.Value
            if trailing > 0:
                new_level = price + trailing
                if self._trailing_level == 0.0 or new_level < self._trailing_level:
                    self._trailing_level = new_level
                if price >= self._trailing_level:
                    self.BuyMarket()
                    self._entry_price = 0.0
                    self._cooldown_remaining = self._cooldown_bars.Value
                    return

            tp = self._take_profit.Value
            if tp > 0 and self._entry_price - price >= tp:
                self.BuyMarket()
                self._entry_price = 0.0
                self._cooldown_remaining = self._cooldown_bars.Value
                return

            if crossed_down:
                self.BuyMarket()
                self._entry_price = 0.0
                self._cooldown_remaining = self._cooldown_bars.Value

    def CreateClone(self):
        return genie_rsi_strategy()
