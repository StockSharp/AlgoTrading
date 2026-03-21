import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import BollingerBands
from StockSharp.Algo.Strategies import Strategy


class scalp_wiz_bollinger_strategy(Strategy):
    def __init__(self):
        super(scalp_wiz_bollinger_strategy, self).__init__()
        self._bands_period = self.Param("BandsPeriod", 30) \
            .SetDisplay("Bands Period", "Number of candles for Bollinger calculation", "Bollinger")
        self._bands_deviation = self.Param("BandsDeviation", 2.0) \
            .SetDisplay("Bands Deviation", "Standard deviation multiplier", "Bollinger")
        self._level1_pips = self.Param("Level1Pips", 1.0) \
            .SetDisplay("Level1 Pips", "Deviation from band for weakest signal", "Levels")
        self._level2_pips = self.Param("Level2Pips", 5.0) \
            .SetDisplay("Level2 Pips", "Deviation from band for level 2", "Levels")
        self._level3_pips = self.Param("Level3Pips", 10.0) \
            .SetDisplay("Level3 Pips", "Deviation from band for level 3", "Levels")
        self._level4_pips = self.Param("Level4Pips", 20.0) \
            .SetDisplay("Level4 Pips", "Deviation from band for strongest signal", "Levels")
        self._multiplier1 = self.Param("StrengthLevel1Multiplier", 1) \
            .SetDisplay("Strength 1 Multiplier", "Volume multiplier for level 1", "Strength")
        self._multiplier2 = self.Param("StrengthLevel2Multiplier", 2) \
            .SetDisplay("Strength 2 Multiplier", "Volume multiplier for level 2", "Strength")
        self._multiplier3 = self.Param("StrengthLevel3Multiplier", 3) \
            .SetDisplay("Strength 3 Multiplier", "Volume multiplier for level 3", "Strength")
        self._multiplier4 = self.Param("StrengthLevel4Multiplier", 4) \
            .SetDisplay("Strength 4 Multiplier", "Volume multiplier for level 4", "Strength")
        self._risk_percent = self.Param("RiskPercent", 2) \
            .SetDisplay("Risk %", "Risk percentage per trade", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

    @property
    def bands_period(self):
        return self._bands_period.Value

    @property
    def bands_deviation(self):
        return self._bands_deviation.Value

    @property
    def level1_pips(self):
        return self._level1_pips.Value

    @property
    def level2_pips(self):
        return self._level2_pips.Value

    @property
    def level3_pips(self):
        return self._level3_pips.Value

    @property
    def level4_pips(self):
        return self._level4_pips.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(scalp_wiz_bollinger_strategy, self).OnReseted()

    def OnStarted(self, time):
        super(scalp_wiz_bollinger_strategy, self).OnStarted(time)

        bollinger = BollingerBands()
        bollinger.Length = self.bands_period
        bollinger.Width = self.bands_deviation

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(bollinger, self.process_candle).Start()

    def process_candle(self, candle, value):
        if candle.State != CandleStates.Finished:
            return

        upper = value.UpBand
        lower = value.LowBand
        if upper is None or lower is None:
            return
        upper = float(upper)
        lower = float(lower)

        step = 0.0001
        if self.Security is not None and self.Security.PriceStep is not None:
            step = float(self.Security.PriceStep)

        close = float(candle.ClosePrice)
        l1 = float(self.level1_pips) * step
        l2 = float(self.level2_pips) * step
        l3 = float(self.level3_pips) * step
        l4 = float(self.level4_pips) * step

        if close - upper > l4:
            self.SellMarket()
        elif close - upper > l3:
            self.SellMarket()
        elif close - upper > l2:
            self.SellMarket()
        elif close - upper > l1:
            self.SellMarket()
        elif lower - close > l4:
            self.BuyMarket()
        elif lower - close > l3:
            self.BuyMarket()
        elif lower - close > l2:
            self.BuyMarket()
        elif lower - close > l1:
            self.BuyMarket()

    def CreateClone(self):
        return scalp_wiz_bollinger_strategy()
