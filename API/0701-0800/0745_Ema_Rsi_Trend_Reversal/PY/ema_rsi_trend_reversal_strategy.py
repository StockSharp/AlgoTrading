import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import ExponentialMovingAverage, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class ema_rsi_trend_reversal_strategy(Strategy):
    """
    EMA RSI trend reversal strategy.
    Long only: buys when the fast EMA crosses above the slow EMA with RSI above RsiLevel and closes the long when the fast EMA
    crosses below the slow EMA with RSI below RsiLevel. Percent take profit and stop loss protect the position.
    """

    def __init__(self):
        super(ema_rsi_trend_reversal_strategy, self).__init__()
        self._fast_length = self.Param("FastLength", 9).SetGreaterThanZero().SetDisplay("Fast EMA", "Fast EMA length", "Indicators")
        self._slow_length = self.Param("SlowLength", 21).SetGreaterThanZero().SetDisplay("Slow EMA", "Slow EMA length", "Indicators")
        self._rsi_length = self.Param("RsiLength", 14).SetGreaterThanZero().SetDisplay("RSI Length", "RSI length", "Indicators")
        self._rsi_level = self.Param("RsiLevel", 50.0).SetDisplay("RSI Level", "RSI level that confirms entries and exits", "Indicators")
        self._take_profit_percent = self.Param("TakeProfitPercent", 2.0).SetNotNegative().SetDisplay("Take Profit %", "Take profit percentage from entry price", "Risk")
        self._stop_loss_percent = self.Param("StopLossPercent", 1.0).SetNotNegative().SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._prev_fast = None
        self._prev_slow = None

    def OnReseted(self):
        super(ema_rsi_trend_reversal_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(ema_rsi_trend_reversal_strategy, self).OnStarted2(time)

        self._reset_state()

        fast = ExponentialMovingAverage()
        fast.Length = self._fast_length.Value
        slow = ExponentialMovingAverage()
        slow.Length = self._slow_length.Value
        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast, slow, rsi, self._process_candle).Start()

        take = float(self._take_profit_percent.Value)
        stop = float(self._stop_loss_percent.Value)
        self.StartProtection(
            Unit(Decimal(take), UnitTypes.Percent) if take > 0 else Unit(),
            Unit(Decimal(stop), UnitTypes.Percent) if stop > 0 else Unit(),
            useMarketOrders=True)

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast)
            self.DrawIndicator(area, slow)
            self.DrawOwnTrades(area)
            oscillators = self.CreateChartArea()
            if oscillators is not None:
                self.DrawIndicator(oscillators, rsi)

    def _process_candle(self, candle, fast_value, slow_value, rsi_value):
        if candle.State != CandleStates.Finished:
            return

        fast = float(fast_value)
        slow = float(slow_value)
        rsi = float(rsi_value)

        prev_fast = self._prev_fast
        prev_slow = self._prev_slow
        self._prev_fast = fast
        self._prev_slow = slow

        if prev_fast is None or prev_slow is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        level = float(self._rsi_level.Value)

        if self.Position == 0 and prev_fast <= prev_slow and fast > slow and rsi > level:
            self.BuyMarket(self.Volume)
        elif self.Position > 0 and prev_fast >= prev_slow and fast < slow and rsi < level:
            self.SellMarket(self.Position)

    def CreateClone(self):
        return ema_rsi_trend_reversal_strategy()
