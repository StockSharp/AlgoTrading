import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ParabolicSar, CommodityChannelIndex
from StockSharp.Algo.Strategies import Strategy

class parabolic_sar_cci_strategy(Strategy):
    """
    Parabolic SAR CCI strategy.
    A close above the Parabolic SAR with CCI below CciOversold buys the dip in the uptrend and a close below the SAR with CCI above
    CciOverbought sells the rally in the downtrend,
    reversing an opposite position. The SAR is the trailing stop: a long closes when price closes below it and a short when price closes above it.
    """

    def __init__(self):
        super(parabolic_sar_cci_strategy, self).__init__()
        self._sar_acceleration_factor = self.Param("SarAccelerationFactor", 0.02).SetGreaterThanZero().SetDisplay("SAR Acceleration", "Initial acceleration factor of the SAR", "SAR")
        self._sar_max_acceleration_factor = self.Param("SarMaxAccelerationFactor", 0.2).SetGreaterThanZero().SetDisplay("SAR Max Acceleration", "Maximum acceleration factor of the SAR", "SAR")
        self._cci_period = self.Param("CciPeriod", 20).SetGreaterThanZero().SetDisplay("CCI Period", "Period of CCI", "CCI")
        self._cci_oversold = self.Param("CciOversold", -100.0).SetDisplay("CCI Oversold", "CCI level for longs", "CCI")
        self._cci_overbought = self.Param("CciOverbought", 100.0).SetDisplay("CCI Overbought", "CCI level for shorts", "CCI")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnStarted2(self, time):
        super(parabolic_sar_cci_strategy, self).OnStarted2(time)

        sar = ParabolicSar()
        sar.Acceleration = Decimal(self._sar_acceleration_factor.Value)
        sar.AccelerationMax = Decimal(self._sar_max_acceleration_factor.Value)
        cci = CommodityChannelIndex()
        cci.Length = self._cci_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(sar, cci, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sar)
            self.DrawOwnTrades(area)
            oscillators = self.CreateChartArea()
            if oscillators is not None:
                self.DrawIndicator(oscillators, cci)

    def _process_candle(self, candle, sar_value, cci_value):
        if candle.State != CandleStates.Finished:
            return

        # The first SAR value is formed but empty.
        if not sar_value.IsFormed or sar_value.IsEmpty or not cci_value.IsFormed:
            return

        sar = sar_value.GetValue[Decimal](None)
        cci = cci_value.GetValue[Decimal](None)
        close = candle.ClosePrice

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if close > sar and cci < Decimal(self._cci_oversold.Value) and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif close < sar and cci > Decimal(self._cci_overbought.Value) and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
        elif self.Position > 0 and close < sar:
            self.SellMarket(self.Position)
        elif self.Position < 0 and close > sar:
            self.BuyMarket(-self.Position)

    def CreateClone(self):
        return parabolic_sar_cci_strategy()
