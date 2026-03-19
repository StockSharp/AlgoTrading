import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import BollingerBands, WilliamsR, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class bollinger_williams_r_strategy(Strategy):
    def __init__(self):
        super(bollinger_williams_r_strategy, self).__init__()
        self._bollinger_period = self.Param("BollingerPeriod", 20) \
            .SetDisplay("Bollinger Period", "Period for Bollinger Bands", "Indicators")
        self._bollinger_deviation = self.Param("BollingerDeviation", 2.0) \
            .SetDisplay("Bollinger Deviation", "Deviation multiplier for Bollinger Bands", "Indicators")
        self._williams_r_period = self.Param("WilliamsRPeriod", 14) \
            .SetDisplay("Williams %R Period", "Period for Williams %R indicator", "Indicators")
        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetDisplay("ATR Period", "Period for ATR indicator for stop-loss", "Risk Management")
        self._atr_multiplier = self.Param("AtrMultiplier", 2.0) \
            .SetDisplay("ATR Multiplier", "Multiplier for ATR-based stop-loss", "Risk Management")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Timeframe for strategy", "General")
        self._cooldown = 0
        self._was_below_lower = False
        self._was_above_upper = False

    @property
    def bollinger_period(self):
        return self._bollinger_period.Value
    @property
    def bollinger_deviation(self):
        return self._bollinger_deviation.Value
    @property
    def williams_r_period(self):
        return self._williams_r_period.Value
    @property
    def atr_period(self):
        return self._atr_period.Value
    @property
    def atr_multiplier(self):
        return self._atr_multiplier.Value
    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(bollinger_williams_r_strategy, self).OnReseted()
        self._cooldown = 0
        self._was_below_lower = False
        self._was_above_upper = False

    def OnStarted(self, time):
        super(bollinger_williams_r_strategy, self).OnStarted(time)
        bollinger = BollingerBands()
        bollinger.Length = self.bollinger_period
        bollinger.Width = self.bollinger_deviation
        williams_r = WilliamsR()
        williams_r.Length = self.williams_r_period
        atr = AverageTrueRange()
        atr.Length = self.atr_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(bollinger, williams_r, atr, self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, bollinger)
            williams_area = self.CreateChartArea()
            if williams_area is not None:
                self.DrawIndicator(williams_area, williams_r)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, bollinger_value, williams_r_value, atr_value):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        bb = bollinger_value
        if bb.UpBand is None or bb.LowBand is None or bb.MovingAverage is None:
            return
        upper_band = float(bb.UpBand)
        lower_band = float(bb.LowBand)
        middle_band = float(bb.MovingAverage)
        price = float(candle.ClosePrice)
        williams_val = float(williams_r_value)

        is_below_lower = price <= lower_band * 1.001
        is_above_upper = price >= upper_band * 0.999

        if self._cooldown > 0:
            self._cooldown -= 1
            self._was_below_lower = is_below_lower
            self._was_above_upper = is_above_upper
            return

        if not self._was_below_lower and is_below_lower and williams_val < -45 and self.Position <= 0:
            self.BuyMarket()
            self._cooldown = 6
        elif not self._was_above_upper and is_above_upper and williams_val > -55 and self.Position >= 0:
            self.SellMarket()
            self._cooldown = 6
        elif price >= middle_band and self.Position < 0:
            self.BuyMarket()
        elif price <= middle_band and self.Position > 0:
            self.SellMarket()

        self._was_below_lower = is_below_lower
        self._was_above_upper = is_above_upper

    def CreateClone(self):
        return bollinger_williams_r_strategy()
