import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import BollingerBands, CommodityChannelIndex
from StockSharp.Algo.Strategies import Strategy


class bollinger_cci_strategy(Strategy):
    def __init__(self):
        super(bollinger_cci_strategy, self).__init__()
        self._bollinger_period = self.Param("BollingerPeriod", 20) \
            .SetDisplay("Bollinger Period", "Period for Bollinger Bands", "Bollinger Parameters")
        self._bollinger_deviation = self.Param("BollingerDeviation", 2.0) \
            .SetDisplay("Bollinger Deviation", "Deviation multiplier for Bollinger Bands", "Bollinger Parameters")
        self._cci_period = self.Param("CciPeriod", 20) \
            .SetDisplay("CCI Period", "Period for Commodity Channel Index", "CCI Parameters")
        self._cci_oversold = self.Param("CciOversold", -100.0) \
            .SetDisplay("CCI Oversold", "CCI level to consider market oversold", "CCI Parameters")
        self._cci_overbought = self.Param("CciOverbought", 100.0) \
            .SetDisplay("CCI Overbought", "CCI level to consider market overbought", "CCI Parameters")
        self._cooldown_bars = self.Param("CooldownBars", 80) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Candle type for strategy", "General")
        self._cooldown = 0

    @property
    def bollinger_period(self):
        return self._bollinger_period.Value
    @property
    def bollinger_deviation(self):
        return self._bollinger_deviation.Value
    @property
    def cci_period(self):
        return self._cci_period.Value
    @property
    def cci_oversold(self):
        return self._cci_oversold.Value
    @property
    def cci_overbought(self):
        return self._cci_overbought.Value
    @property
    def cooldown_bars(self):
        return self._cooldown_bars.Value
    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(bollinger_cci_strategy, self).OnReseted()
        self._cooldown = 0

    def OnStarted(self, time):
        super(bollinger_cci_strategy, self).OnStarted(time)
        bollinger = BollingerBands()
        bollinger.Length = self.bollinger_period
        bollinger.Width = self.bollinger_deviation
        cci = CommodityChannelIndex()
        cci.Length = self.cci_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(bollinger, cci, self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, bollinger)
            cci_area = self.CreateChartArea()
            if cci_area is not None:
                self.DrawIndicator(cci_area, cci)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, bollinger_value, cci_value):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        if not bollinger_value.IsFormed or not cci_value.IsFormed:
            return

        bb = bollinger_value
        if bb.UpBand is None or bb.LowBand is None or bb.MovingAverage is None:
            return
        upper_band = float(bb.UpBand)
        lower_band = float(bb.LowBand)
        middle_band = float(bb.MovingAverage)
        cci_val = float(cci_value)
        price = float(candle.ClosePrice)

        if self._cooldown > 0:
            self._cooldown -= 1
            return

        lower_touch = price <= lower_band * 1.002
        upper_touch = price >= upper_band * 0.998

        if lower_touch and cci_val < self.cci_oversold and self.Position == 0:
            self.BuyMarket()
            self._cooldown = self.cooldown_bars
        elif upper_touch and cci_val > self.cci_overbought and self.Position == 0:
            self.SellMarket()
            self._cooldown = self.cooldown_bars
        elif price > middle_band and self.Position > 0:
            self.SellMarket()
            self._cooldown = self.cooldown_bars
        elif price < middle_band and self.Position < 0:
            self.BuyMarket()
            self._cooldown = self.cooldown_bars

    def CreateClone(self):
        return bollinger_cci_strategy()
