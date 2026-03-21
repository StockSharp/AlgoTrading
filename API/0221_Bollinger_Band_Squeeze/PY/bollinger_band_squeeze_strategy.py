import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, UnitTypes, Unit, CandleStates
from StockSharp.Algo.Indicators import BollingerBands, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class bollinger_band_squeeze_strategy(Strategy):
    def __init__(self):
        super(bollinger_band_squeeze_strategy, self).__init__()
        self._bollinger_period = self.Param("BollingerPeriod", 20) \
            .SetDisplay("Bollinger Period", "Period for Bollinger Bands calculation", "Parameters")
        self._bollinger_multiplier = self.Param("BollingerMultiplier", 2.0) \
            .SetDisplay("Bollinger Multiplier", "Standard deviation multiplier for Bollinger Bands", "Parameters")
        self._lookback_period = self.Param("LookbackPeriod", 20) \
            .SetDisplay("Lookback Period", "Period for averaging Bollinger width", "Parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Candle type for strategy", "Common")
        self._prev_bollinger_width = 0.0
        self._avg_bollinger_width = 0.0
        self._bollinger_width_sum = 0.0
        self._bollinger_widths = []

    @property
    def bollinger_period(self):
        return self._bollinger_period.Value
    @property
    def bollinger_multiplier(self):
        return self._bollinger_multiplier.Value
    @property
    def lookback_period(self):
        return self._lookback_period.Value
    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(bollinger_band_squeeze_strategy, self).OnReseted()
        self._prev_bollinger_width = 0.0
        self._avg_bollinger_width = 0.0
        self._bollinger_width_sum = 0.0
        self._bollinger_widths = []

    def OnStarted(self, time):
        super(bollinger_band_squeeze_strategy, self).OnStarted(time)
        bollinger = BollingerBands()
        bollinger.Length = self.bollinger_period
        bollinger.Width = self.bollinger_multiplier
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(bollinger, self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, bollinger)
            self.DrawOwnTrades(area)
        self.StartProtection(
            takeProfit=Unit(0, UnitTypes.Absolute),
            stopLoss=Unit(2, UnitTypes.Absolute)
        )

    def OnProcess(self, candle, bollinger_value):
        if candle.State != CandleStates.Finished:
            return

        if bollinger_value.UpBand is None:
            return
        upper_band = float(bollinger_value.UpBand)

        if bollinger_value.LowBand is None:
            return
        lower_band = float(bollinger_value.LowBand)

        bollinger_width = upper_band - lower_band

        self._bollinger_widths.append(bollinger_width)
        self._bollinger_width_sum += bollinger_width

        if len(self._bollinger_widths) > self.lookback_period:
            old_value = self._bollinger_widths.pop(0)
            self._bollinger_width_sum -= old_value

        if len(self._bollinger_widths) == self.lookback_period:
            self._avg_bollinger_width = self._bollinger_width_sum / self.lookback_period
            is_squeeze = bollinger_width < self._avg_bollinger_width

            if is_squeeze:
                if candle.ClosePrice > upper_band and self.Position <= 0:
                    self.BuyMarket()
                elif candle.ClosePrice < lower_band and self.Position >= 0:
                    self.SellMarket()

        self._prev_bollinger_width = bollinger_width

    def CreateClone(self):
        return bollinger_band_squeeze_strategy()
