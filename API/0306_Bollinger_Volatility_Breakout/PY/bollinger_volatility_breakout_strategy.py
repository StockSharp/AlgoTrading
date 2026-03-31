import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import BollingerBands, AverageTrueRange, SimpleMovingAverage, StandardDeviation, DecimalIndicatorValue, CandleIndicatorValue
from StockSharp.Algo.Strategies import Strategy


class bollinger_volatility_breakout_strategy(Strategy):
    """
    Breakout strategy that trades Bollinger band breaks only when ATR expands
    beyond its recent regime.
    """

    def __init__(self):
        super(bollinger_volatility_breakout_strategy, self).__init__()

        self._bollinger_period = self.Param("BollingerPeriod", 20) \
            .SetDisplay("Bollinger Period", "Period for Bollinger band calculation", "Indicators")

        self._bollinger_deviation = self.Param("BollingerDeviation", 2.0) \
            .SetDisplay("Bollinger Deviation", "Standard deviation multiplier for Bollinger bands", "Indicators")

        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetDisplay("ATR Period", "Period for ATR calculation", "Indicators")

        self._atr_deviation_multiplier = self.Param("AtrDeviationMultiplier", 1.6) \
            .SetDisplay("ATR Deviation Multiplier", "ATR regime threshold multiplier", "Signals")

        self._stop_loss_multiplier = self.Param("StopLossMultiplier", 1.8) \
            .SetDisplay("Stop Loss Multiplier", "ATR multiplier used for stop distance", "Risk")

        self._cooldown_bars = self.Param("CooldownBars", 84) \
            .SetDisplay("Cooldown Bars", "Bars to wait after each order", "Risk")

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles for the strategy", "General")

        self._bollinger_bands = None
        self._atr = None
        self._atr_sma = None
        self._atr_std_dev = None
        self._entry_price = 0.0
        self._entry_atr = 0.0
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(bollinger_volatility_breakout_strategy, self).OnReseted()
        self._bollinger_bands = None
        self._atr = None
        self._atr_sma = None
        self._atr_std_dev = None
        self._entry_price = 0.0
        self._entry_atr = 0.0
        self._cooldown = 0

    def OnStarted2(self, time):
        super(bollinger_volatility_breakout_strategy, self).OnStarted2(time)

        atr_period = int(self._atr_period.Value)

        self._bollinger_bands = BollingerBands()
        self._bollinger_bands.Length = int(self._bollinger_period.Value)
        self._bollinger_bands.Width = Decimal(self._bollinger_deviation.Value)

        self._atr = AverageTrueRange()
        self._atr.Length = atr_period
        self._atr_sma = SimpleMovingAverage()
        self._atr_sma.Length = atr_period
        self._atr_std_dev = StandardDeviation()
        self._atr_std_dev.Length = atr_period
        self._cooldown = 0

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(self._bollinger_bands, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._bollinger_bands)
            self.DrawIndicator(area, self._atr)
            self.DrawOwnTrades(area)

        self.StartProtection(Unit(0, UnitTypes.Absolute), Unit(self._stop_loss_multiplier.Value, UnitTypes.Percent), False)

    def _process_candle(self, candle, bollinger_value):
        if candle.State != CandleStates.Finished:
            return

        up_band = bollinger_value.UpBand
        low_band = bollinger_value.LowBand
        moving_avg = bollinger_value.MovingAverage

        if up_band is None or low_band is None or moving_avg is None:
            return

        upper_band = float(up_band)
        lower_band = float(low_band)
        middle_band = float(moving_avg)

        atr_result = self._atr.Process(CandleIndicatorValue(self._atr, candle))
        atr_value = float(atr_result)

        atr_avg_input = DecimalIndicatorValue(self._atr_sma, Decimal(atr_value), candle.OpenTime)
        atr_avg_input.IsFinal = True
        atr_average_value = float(self._atr_sma.Process(atr_avg_input))

        atr_std_input = DecimalIndicatorValue(self._atr_std_dev, Decimal(atr_value), candle.OpenTime)
        atr_std_input.IsFinal = True
        atr_std_dev_value = float(self._atr_std_dev.Process(atr_std_input))

        if not self._bollinger_bands.IsFormed or not self._atr.IsFormed or not self._atr_sma.IsFormed or not self._atr_std_dev.IsFormed:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            return

        adm = float(self._atr_deviation_multiplier.Value)
        volatility_threshold = atr_average_value + adm * atr_std_dev_value
        is_high_volatility = atr_value >= volatility_threshold
        price = float(candle.ClosePrice)
        cd = int(self._cooldown_bars.Value)
        slm = float(self._stop_loss_multiplier.Value)

        if self.Position == 0:
            if not is_high_volatility:
                return

            if price >= upper_band:
                self._entry_price = price
                self._entry_atr = atr_value
                self.BuyMarket()
                self._cooldown = cd
            elif price <= lower_band:
                self._entry_price = price
                self._entry_atr = atr_value
                self.SellMarket()
                self._cooldown = cd
            return

        stop_distance = self._entry_atr * slm

        if self.Position > 0:
            if price <= middle_band or not is_high_volatility or price <= self._entry_price - stop_distance:
                self.SellMarket(Math.Abs(self.Position))
                self._cooldown = cd
        elif self.Position < 0:
            if price >= middle_band or not is_high_volatility or price >= self._entry_price + stop_distance:
                self.BuyMarket(Math.Abs(self.Position))
                self._cooldown = cd

    def CreateClone(self):
        return bollinger_volatility_breakout_strategy()
