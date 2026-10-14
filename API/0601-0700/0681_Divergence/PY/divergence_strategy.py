import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class divergence_strategy(Strategy):
    """
    Price and RSI divergence strategy.
    A pivot is a candle whose low (high) is below (above) the lows (highs) of its neighbours on both sides.
    A pivot low below the previous pivot low with a higher RSI is a bullish divergence and buys;
    a pivot high above the previous pivot high with a lower RSI is a bearish divergence and sells.
    TradeDirection ("Long", "Short" or "Both") limits the entries; an opposite divergence reverses or closes the position.
    A percent stop loss and a take profit of RiskReward times the stop protect every position.
    """

    def __init__(self):
        super(divergence_strategy, self).__init__()
        self._trade_direction = self.Param("TradeDirection", "Both").SetDisplay("Trade Direction", "Allowed trade direction: Long, Short or Both", "Trading")
        self._rsi_period = self.Param("RsiPeriod", 14).SetGreaterThanZero().SetDisplay("RSI Period", "RSI period", "Indicators")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0).SetNotNegative().SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk")
        self._risk_reward = self.Param("RiskReward", 2.0).SetNotNegative().SetDisplay("Risk Reward", "Take profit as a multiple of the stop loss", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.candle_type)]

    def _reset_state(self):
        self._bars = []
        self._last_pivot_low = None
        self._last_pivot_high = None

    def OnReseted(self):
        super(divergence_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(divergence_strategy, self).OnStarted2(time)

        self._reset_state()

        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(rsi, self._process_candle).Start()

        stop = float(self._stop_loss_percent.Value)
        take = stop * float(self._risk_reward.Value)
        self.StartProtection(
            Unit(Decimal(take), UnitTypes.Percent) if take > 0 else Unit(),
            Unit(Decimal(stop), UnitTypes.Percent) if stop > 0 else Unit(),
            useMarketOrders=True)

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)
            oscillators = self.CreateChartArea()
            if oscillators is not None:
                self.DrawIndicator(oscillators, rsi)

    def _process_candle(self, candle, rsi_value):
        if candle.State != CandleStates.Finished:
            return

        if not rsi_value.IsFormed:
            return

        self._bars.append((float(candle.HighPrice), float(candle.LowPrice), float(rsi_value.GetValue[Decimal](None))))
        if len(self._bars) > 3:
            self._bars.pop(0)

        if len(self._bars) < 3:
            return

        # The middle bar is confirmed as a pivot once the bar after it has closed.
        left, mid, right = self._bars
        bullish = False
        bearish = False

        if mid[1] < left[1] and mid[1] < right[1]:
            prev_low = self._last_pivot_low
            if prev_low is not None and mid[1] < prev_low[0] and mid[2] > prev_low[1]:
                bullish = True
            self._last_pivot_low = (mid[1], mid[2])

        if mid[0] > left[0] and mid[0] > right[0]:
            prev_high = self._last_pivot_high
            if prev_high is not None and mid[0] > prev_high[0] and mid[2] < prev_high[1]:
                bearish = True
            self._last_pivot_high = (mid[0], mid[2])

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        direction = str(self._trade_direction.Value).lower()
        allow_long = direction != "short"
        allow_short = direction != "long"

        if bullish and not bearish:
            if allow_long and self.Position <= 0:
                self.BuyMarket(self.Volume + abs(self.Position))
            elif not allow_long and self.Position < 0:
                self.BuyMarket(-self.Position)
        elif bearish and not bullish:
            if allow_short and self.Position >= 0:
                self.SellMarket(self.Volume + abs(self.Position))
            elif not allow_short and self.Position > 0:
                self.SellMarket(self.Position)

    def CreateClone(self):
        return divergence_strategy()
