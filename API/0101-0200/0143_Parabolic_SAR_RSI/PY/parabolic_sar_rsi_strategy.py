import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ParabolicSar, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy

class parabolic_sar_rsi_strategy(Strategy):
    """
    Parabolic SAR RSI strategy.
    A close above the Parabolic SAR with RSI below RsiOversold goes long and a close below the SAR with RSI above RsiOverbought goes short,
    reversing an opposite position. The SAR is the trailing stop: a long closes when price closes below it and a short when price closes above it.
    """

    def __init__(self):
        super(parabolic_sar_rsi_strategy, self).__init__()
        self._sar_af = self.Param("SarAf", 0.02).SetGreaterThanZero().SetDisplay("SAR Acceleration", "Initial acceleration factor of the SAR", "SAR")
        self._sar_max_af = self.Param("SarMaxAf", 0.2).SetGreaterThanZero().SetDisplay("SAR Max Acceleration", "Maximum acceleration factor of the SAR", "SAR")
        self._rsi_period = self.Param("RsiPeriod", 14).SetGreaterThanZero().SetDisplay("RSI Period", "Period of RSI", "RSI")
        self._rsi_oversold = self.Param("RsiOversold", 30.0).SetDisplay("RSI Oversold", "RSI level for longs", "RSI")
        self._rsi_overbought = self.Param("RsiOverbought", 70.0).SetDisplay("RSI Overbought", "RSI level for shorts", "RSI")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnStarted2(self, time):
        super(parabolic_sar_rsi_strategy, self).OnStarted2(time)

        sar = ParabolicSar()
        sar.Acceleration = Decimal(self._sar_af.Value)
        sar.AccelerationMax = Decimal(self._sar_max_af.Value)
        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(sar, rsi, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sar)
            self.DrawOwnTrades(area)
            oscillators = self.CreateChartArea()
            if oscillators is not None:
                self.DrawIndicator(oscillators, rsi)

    def _process_candle(self, candle, sar_value, rsi_value):
        if candle.State != CandleStates.Finished:
            return

        # The first SAR value is formed but empty.
        if not sar_value.IsFormed or sar_value.IsEmpty or not rsi_value.IsFormed:
            return

        sar = sar_value.GetValue[Decimal](None)
        rsi = rsi_value.GetValue[Decimal](None)
        close = candle.ClosePrice

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if close > sar and rsi < Decimal(self._rsi_oversold.Value) and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif close < sar and rsi > Decimal(self._rsi_overbought.Value) and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
        elif self.Position > 0 and close < sar:
            self.SellMarket(self.Position)
        elif self.Position < 0 and close > sar:
            self.BuyMarket(-self.Position)

    def CreateClone(self):
        return parabolic_sar_rsi_strategy()
