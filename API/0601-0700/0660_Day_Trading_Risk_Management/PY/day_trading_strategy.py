import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import ExponentialMovingAverage, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class day_trading_strategy(Strategy):
    """EMA cross with RSI confirmation and native TP/SL percent protection."""

    def __init__(self):
        super(day_trading_strategy, self).__init__()

        self._fast_ema_period = self.Param("FastEmaPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Fast EMA Length", "Fast EMA signal length", "Indicators")
        self._slow_ema_period = self.Param("SlowEmaPeriod", 50) \
            .SetGreaterThanZero() \
            .SetDisplay("Slow EMA Length", "Slow EMA signal length", "Indicators")
        self._rsi_length = self.Param("RsiLength", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("RSI Length", "RSI confirmation length", "Indicators")
        self._rsi_overbought = self.Param("RsiOverbought", 70.0) \
            .SetRange(0.0, 100.0) \
            .SetDisplay("RSI Overbought", "Strict long-side RSI upper bound", "Signals")
        self._rsi_oversold = self.Param("RsiOversold", 30.0) \
            .SetRange(0.0, 100.0) \
            .SetDisplay("RSI Oversold", "Strict short-side RSI lower bound", "Signals")
        self._take_profit_percent = self.Param("TakeProfitPercent", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Take Profit %", "Native percentage take-profit", "Risk")
        self._stop_loss_percent = self.Param("StopLossPercent", 1.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Stop Loss %", "Native percentage stop-loss", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))) \
            .SetDisplay("Candle Type", "Finished signal candles", "General")

        self._fast = None
        self._slow = None
        self._rsi = None
        self._previous_fast = None
        self._previous_slow = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(day_trading_strategy, self).OnReseted()
        self._fast = None
        self._slow = None
        self._rsi = None
        self._previous_fast = None
        self._previous_slow = None

    def OnStarted2(self, time):
        super(day_trading_strategy, self).OnStarted2(time)

        if int(self._fast_ema_period.Value) >= int(self._slow_ema_period.Value):
            raise ValueError("FastEmaPeriod must be less than SlowEmaPeriod")
        if float(self._rsi_oversold.Value) >= float(self._rsi_overbought.Value):
            raise ValueError("RsiOversold must be less than RsiOverbought")

        self._fast = ExponentialMovingAverage()
        self._fast.Length = int(self._fast_ema_period.Value)
        self._slow = ExponentialMovingAverage()
        self._slow.Length = int(self._slow_ema_period.Value)
        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = int(self._rsi_length.Value)
        self._previous_fast = None
        self._previous_slow = None

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._fast, self._slow, self._rsi, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._fast)
            self.DrawIndicator(area, self._slow)
            self.DrawIndicator(area, self._rsi)
            self.DrawOwnTrades(area)

        self.StartProtection(
            Unit(float(self._take_profit_percent.Value), UnitTypes.Percent),
            Unit(float(self._stop_loss_percent.Value), UnitTypes.Percent),
            False
        )

    def _process_candle(self, candle, fast_val, slow_val, rsi_val):
        if candle.State != CandleStates.Finished:
            return

        fast = float(fast_val)
        slow = float(slow_val)
        rsi = float(rsi_val)

        cross_up = (self._previous_fast is not None and
                    self._previous_fast <= self._previous_slow and fast > slow)
        cross_down = (self._previous_fast is not None and
                      self._previous_fast >= self._previous_slow and fast < slow)

        self._previous_fast = fast
        self._previous_slow = slow

        if not self._fast.IsFormed or not self._slow.IsFormed or not self._rsi.IsFormed:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if cross_up and rsi < float(self._rsi_overbought.Value) and self.Position <= 0:
            self.BuyMarket(self.Volume + Math.Abs(self.Position))
        elif cross_down and rsi > float(self._rsi_oversold.Value) and self.Position >= 0:
            self.SellMarket(self.Volume + Math.Abs(self.Position))

    def CreateClone(self):
        return day_trading_strategy()
