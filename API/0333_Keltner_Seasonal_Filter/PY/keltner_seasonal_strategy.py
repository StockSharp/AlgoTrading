import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import ExponentialMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class keltner_seasonal_strategy(Strategy):
    """
    Strategy that trades based on Keltner Channel breakouts with seasonal bias filter.
    """

    def __init__(self):
        super(keltner_seasonal_strategy, self).__init__()

        self._ema_period = self.Param("EmaPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("EMA Period", "Period for EMA in Keltner Channel", "Indicator Settings")

        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR Period", "Period for ATR in Keltner Channel", "Indicator Settings")

        self._atr_multiplier = self.Param("AtrMultiplier", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR Multiplier", "Multiplier for ATR to set channel width", "Indicator Settings")

        self._seasonal_threshold = self.Param("SeasonalThreshold", 0.5) \
            .SetDisplay("Seasonal Threshold", "Minimum seasonal strength to consider for trading", "Seasonal Settings")

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._monthly_returns = {}
        self._current_seasonal_strength = 0.0
        self._initialize_seasonal_data()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.candle_type)]

    def OnReseted(self):
        super(keltner_seasonal_strategy, self).OnReseted()
        self._current_seasonal_strength = 0.0

    def OnStarted(self, time):
        super(keltner_seasonal_strategy, self).OnStarted(time)

        self._update_seasonal_strength(time)

        ema = ExponentialMovingAverage()
        ema.Length = int(self._ema_period.Value)

        atr = AverageTrueRange()
        atr.Length = int(self._atr_period.Value)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ema, atr, self._process_keltner).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

        self.StartProtection(
            takeProfit=Unit(0),
            stopLoss=Unit(float(self._atr_multiplier.Value), UnitTypes.Absolute)
        )

    def _process_keltner(self, candle, ema_value, atr_value):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        candle_month = candle.OpenTime.Month
        current_month = self.CurrentTime.Month
        if candle_month != current_month:
            self._update_seasonal_strength(self.CurrentTime)

        multiplier = float(self._atr_multiplier.Value)
        threshold = float(self._seasonal_threshold.Value)
        ema_val = float(ema_value)
        atr_val = float(atr_value)

        upper_band = ema_val + atr_val * multiplier
        lower_band = ema_val - atr_val * multiplier
        close_price = float(candle.ClosePrice)

        if self._current_seasonal_strength > threshold:
            if close_price > upper_band and self.Position <= 0:
                self.BuyMarket(self.Volume + Math.Abs(self.Position))
        elif self._current_seasonal_strength < -threshold:
            if close_price < lower_band and self.Position >= 0:
                self.SellMarket(self.Volume + Math.Abs(self.Position))

        if (self.Position > 0 and close_price < ema_val) or \
           (self.Position < 0 and close_price > ema_val):
            if self.Position > 0:
                self.SellMarket(self.Position)
            elif self.Position < 0:
                self.BuyMarket(Math.Abs(self.Position))

    def _initialize_seasonal_data(self):
        self._monthly_returns[1] = 0.8
        self._monthly_returns[2] = 0.3
        self._monthly_returns[3] = 0.6
        self._monthly_returns[4] = 0.7
        self._monthly_returns[5] = 0.2
        self._monthly_returns[6] = -0.3
        self._monthly_returns[7] = -0.1
        self._monthly_returns[8] = -0.4
        self._monthly_returns[9] = -0.8
        self._monthly_returns[10] = 0.1
        self._monthly_returns[11] = 0.9
        self._monthly_returns[12] = 0.7

    def _update_seasonal_strength(self, time):
        month = time.Month
        if month in self._monthly_returns:
            self._current_seasonal_strength = self._monthly_returns[month]
        else:
            self._current_seasonal_strength = 0.0

    def CreateClone(self):
        return keltner_seasonal_strategy()
