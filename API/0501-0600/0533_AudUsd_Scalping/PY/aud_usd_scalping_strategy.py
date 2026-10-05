import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import ExponentialMovingAverage, RelativeStrengthIndex, BollingerBands
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *


class aud_usd_scalping_strategy(Strategy):
    """
    AUD/USD scalping strategy.
    In an uptrend (fast EMA above slow EMA) a candle touching the lower Bollinger Band with RSI above RsiOversold goes long; in a
    downtrend a candle touching the upper band with RSI below RsiOverbought goes short, reversing an opposite position. Fixed
    price distances TakeProfit and StopLoss close the position.
    """

    def __init__(self):
        super(aud_usd_scalping_strategy, self).__init__()
        self._ema_short = self.Param("EmaShort", 13) \
            .SetGreaterThanZero() \
            .SetDisplay("EMA Short", "Fast EMA period", "Trend")
        self._ema_long = self.Param("EmaLong", 26) \
            .SetGreaterThanZero() \
            .SetDisplay("EMA Long", "Slow EMA period", "Trend")
        self._rsi_period = self.Param("RsiPeriod", 4) \
            .SetGreaterThanZero() \
            .SetDisplay("RSI Period", "RSI period", "RSI")
        self._rsi_overbought = self.Param("RsiOverbought", 70.0) \
            .SetDisplay("RSI Overbought", "RSI level shorts must stay below", "RSI")
        self._rsi_oversold = self.Param("RsiOversold", 30.0) \
            .SetDisplay("RSI Oversold", "RSI level longs must stay above", "RSI")
        self._bb_length = self.Param("BbLength", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("BB Length", "Bollinger Bands period", "Bollinger")
        self._bb_multiplier = self.Param("BbMultiplier", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("BB Multiplier", "Bollinger Bands width multiplier", "Bollinger")
        self._take_profit = self.Param("TakeProfit", 0.0005) \
            .SetNotNegative() \
            .SetDisplay("Take Profit", "Take-profit distance in price", "Risk")
        self._stop_loss = self.Param("StopLoss", 0.0004) \
            .SetNotNegative() \
            .SetDisplay("Stop Loss", "Stop-loss distance in price", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

    @property
    def CandleType(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType)]

    def OnStarted2(self, time):
        super(aud_usd_scalping_strategy, self).OnStarted2(time)

        ema_short = ExponentialMovingAverage()
        ema_short.Length = self._ema_short.Value
        ema_long = ExponentialMovingAverage()
        ema_long.Length = self._ema_long.Value
        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_period.Value
        bollinger = BollingerBands()
        bollinger.Length = self._bb_length.Value
        bollinger.Width = Decimal(self._bb_multiplier.Value)

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(ema_short, ema_long, rsi, bollinger, self._process_candle).Start()

        take = float(self._take_profit.Value)
        stop = float(self._stop_loss.Value)
        self.StartProtection(
            Unit(Decimal(take), UnitTypes.Absolute) if take > 0 else Unit(),
            Unit(Decimal(stop), UnitTypes.Absolute) if stop > 0 else Unit(),
            useMarketOrders=True)

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema_short)
            self.DrawIndicator(area, ema_long)
            self.DrawIndicator(area, bollinger)
            self.DrawOwnTrades(area)
            oscillators = self.CreateChartArea()
            if oscillators is not None:
                self.DrawIndicator(oscillators, rsi)

    def _process_candle(self, candle, ema_short_value, ema_long_value, rsi_value, bollinger_value):
        if candle.State != CandleStates.Finished:
            return

        if not ema_short_value.IsFormed or not ema_long_value.IsFormed or not rsi_value.IsFormed or not bollinger_value.IsFormed:
            return

        if bollinger_value.UpBand is None or bollinger_value.LowBand is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        upper = float(bollinger_value.UpBand)
        lower = float(bollinger_value.LowBand)
        fast = float(to_decimal(ema_short_value))
        slow = float(to_decimal(ema_long_value))
        rsi = float(to_decimal(rsi_value))

        if fast > slow and float(candle.LowPrice) <= lower and rsi > float(self._rsi_oversold.Value) and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif fast < slow and float(candle.HighPrice) >= upper and rsi < float(self._rsi_overbought.Value) and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))

    def CreateClone(self):
        return aud_usd_scalping_strategy()
