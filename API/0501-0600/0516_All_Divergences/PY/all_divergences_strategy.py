import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import RelativeStrengthIndex, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

PIVOT_SIDE = 5


class all_divergences_strategy(Strategy):
    """
    All Divergences strategy.
    Confirms price swing lows and highs as pivots with PivotSide bars on each side and compares each new pivot with the previous
    one. A lower price low with a higher RSI low while the close is above the moving average goes long; a higher price high
    with a lower RSI high while the close is below the moving average goes short, reversing an opposite position. A position
    is also closed after MaRiskCandles consecutive closes on the wrong side of the moving average, and optional percent
    stop-loss and take-profit protect it.
    """

    def __init__(self):
        super(all_divergences_strategy, self).__init__()
        self._ma_length = self.Param("MaLength", 50) \
            .SetGreaterThanZero() \
            .SetDisplay("MA Length", "Moving average period", "Indicators")
        self._rsi_length = self.Param("RsiLength", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("RSI Length", "RSI period", "Indicators")
        self._ma_risk_candles = self.Param("MaRiskCandles", 3) \
            .SetGreaterThanZero() \
            .SetDisplay("MA Risk Candles", "Consecutive closes against the MA that close a position", "Risk")
        self._use_protection = self.Param("UseProtection", False) \
            .SetDisplay("Use Protection", "Enable percent stop-loss and take-profit", "Risk")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetNotNegative() \
            .SetDisplay("Stop Loss %", "Stop-loss percentage used when protection is enabled", "Risk")
        self._take_profit_percent = self.Param("TakeProfitPercent", 4.0) \
            .SetNotNegative() \
            .SetDisplay("Take Profit %", "Take-profit percentage used when protection is enabled", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._reset_state()

    @property
    def CandleType(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType)]

    def _reset_state(self):
        self._bars = []
        self._last_pivot_low = None
        self._last_pivot_low_rsi = 0.0
        self._last_pivot_high = None
        self._last_pivot_high_rsi = 0.0
        self._closes_below_ma = 0
        self._closes_above_ma = 0

    def OnReseted(self):
        super(all_divergences_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(all_divergences_strategy, self).OnStarted2(time)

        self._reset_state()

        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_length.Value
        ma = SimpleMovingAverage()
        ma.Length = self._ma_length.Value

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(rsi, ma, self._process_candle).Start()

        if self._use_protection.Value:
            self.StartProtection(
                Unit(Decimal(self._take_profit_percent.Value), UnitTypes.Percent),
                Unit(Decimal(self._stop_loss_percent.Value), UnitTypes.Percent),
                useMarketOrders=True)

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ma)
            self.DrawOwnTrades(area)
            oscillators = self.CreateChartArea()
            if oscillators is not None:
                self.DrawIndicator(oscillators, rsi)

    def _process_candle(self, candle, rsi_value, ma_value):
        if candle.State != CandleStates.Finished:
            return

        rsi = float(rsi_value)
        ma = float(ma_value)

        self._bars.append((float(candle.HighPrice), float(candle.LowPrice), rsi))

        window = 2 * PIVOT_SIDE + 1
        if len(self._bars) > window:
            self._bars.pop(0)

        close = float(candle.ClosePrice)
        self._closes_below_ma = self._closes_below_ma + 1 if close < ma else 0
        self._closes_above_ma = self._closes_above_ma + 1 if close > ma else 0

        if len(self._bars) < window:
            return

        bullish_divergence = False
        bearish_divergence = False

        # The middle bar of the window is a pivot once PivotSide bars on each side have finished.
        pivot_high, pivot_low, pivot_rsi = self._bars[PIVOT_SIDE]
        is_pivot_low = True
        is_pivot_high = True

        for i in range(window):
            if i == PIVOT_SIDE:
                continue
            if self._bars[i][1] <= pivot_low:
                is_pivot_low = False
            if self._bars[i][0] >= pivot_high:
                is_pivot_high = False

        if is_pivot_low:
            if self._last_pivot_low is not None:
                bullish_divergence = pivot_low < self._last_pivot_low and pivot_rsi > self._last_pivot_low_rsi
            self._last_pivot_low = pivot_low
            self._last_pivot_low_rsi = pivot_rsi

        if is_pivot_high:
            if self._last_pivot_high is not None:
                bearish_divergence = pivot_high > self._last_pivot_high and pivot_rsi < self._last_pivot_high_rsi
            self._last_pivot_high = pivot_high
            self._last_pivot_high_rsi = pivot_rsi

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        risk_candles = self._ma_risk_candles.Value

        if bullish_divergence and close > ma and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif bearish_divergence and close < ma and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
        elif self.Position > 0 and self._closes_below_ma >= risk_candles:
            self.SellMarket(self.Position)
        elif self.Position < 0 and self._closes_above_ma >= risk_candles:
            self.BuyMarket(-self.Position)

    def CreateClone(self):
        return all_divergences_strategy()
