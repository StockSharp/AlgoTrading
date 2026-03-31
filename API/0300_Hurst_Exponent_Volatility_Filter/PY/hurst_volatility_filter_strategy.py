import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math, Decimal
from StockSharp.Messages import DataType, Unit, UnitTypes, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, AverageTrueRange, HurstExponent, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy


class hurst_volatility_filter_strategy(Strategy):
    """
    Mean-reversion strategy that enters only when Hurst indicates anti-persistent
    behavior and ATR confirms a quiet regime.
    """

    def __init__(self):
        super(hurst_volatility_filter_strategy, self).__init__()

        self._hurst_period = self.Param("HurstPeriod", 80) \
            .SetDisplay("Hurst Period", "Period for the Hurst exponent", "Indicators")

        self._ma_period = self.Param("MAPeriod", 20) \
            .SetDisplay("MA Period", "Period for the moving average", "Indicators")

        self._atr_period = self.Param("ATRPeriod", 14) \
            .SetDisplay("ATR Period", "Period for the ATR", "Indicators")

        self._hurst_threshold = self.Param("HurstThreshold", 0.7) \
            .SetDisplay("Hurst Threshold", "Maximum Hurst value allowed for entries", "Signals")

        self._deviation_atr_multiplier = self.Param("DeviationAtrMultiplier", 0.5) \
            .SetDisplay("Deviation ATR", "Minimum ATR multiple required for entry", "Signals")

        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage", "Risk")

        self._cooldown_bars = self.Param("CooldownBars", 90) \
            .SetDisplay("Cooldown Bars", "Bars to wait after each order", "Risk")

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._sma = None
        self._atr = None
        self._hurst_exponent = None
        self._atr_average = None
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(hurst_volatility_filter_strategy, self).OnReseted()
        self._sma = None
        self._atr = None
        self._hurst_exponent = None
        self._atr_average = None
        self._cooldown = 0

    def OnStarted2(self, time):
        super(hurst_volatility_filter_strategy, self).OnStarted2(time)

        atr_period = int(self._atr_period.Value)

        self._sma = SimpleMovingAverage()
        self._sma.Length = int(self._ma_period.Value)
        self._atr = AverageTrueRange()
        self._atr.Length = atr_period
        self._hurst_exponent = HurstExponent()
        self._hurst_exponent.Length = int(self._hurst_period.Value)
        self._atr_average = SimpleMovingAverage()
        self._atr_average.Length = max(atr_period * 2, 10)
        self._cooldown = 0

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._sma, self._atr, self._hurst_exponent, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._sma)
            self.DrawIndicator(area, self._atr)
            self.DrawIndicator(area, self._hurst_exponent)
            self.DrawOwnTrades(area)

        self.StartProtection(Unit(0, UnitTypes.Absolute), Unit(self._stop_loss_percent.Value, UnitTypes.Percent), False)

    def _process_candle(self, candle, sma_value, atr_value, hurst_value):
        if candle.State != CandleStates.Finished:
            return

        sma_val = float(sma_value)
        atr_val = float(atr_value)
        hurst_val = float(hurst_value)

        atr_avg_input = DecimalIndicatorValue(self._atr_average, Decimal(atr_val), candle.OpenTime)
        atr_avg_input.IsFinal = True
        atr_average_value = float(self._atr_average.Process(atr_avg_input))

        if not self._sma.IsFormed or not self._atr.IsFormed or not self._hurst_exponent.IsFormed or not self._atr_average.IsFormed:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            return

        price = float(candle.ClosePrice)
        deviation = price - sma_val
        dev_mult = float(self._deviation_atr_multiplier.Value)
        required_deviation = atr_val * dev_mult
        hurst_thresh = float(self._hurst_threshold.Value)
        is_mean_reversion_regime = hurst_val <= hurst_thresh
        is_quiet_volatility = atr_val <= atr_average_value * 1.5
        cd = int(self._cooldown_bars.Value)

        if self.Position == 0:
            if not is_mean_reversion_regime or not is_quiet_volatility:
                return

            if deviation <= -required_deviation:
                self.BuyMarket()
                self._cooldown = cd
            elif deviation >= required_deviation:
                self.SellMarket()
                self._cooldown = cd
            return

        if self.Position > 0 and (price >= sma_val or deviation >= -atr_val * 0.2 or not is_mean_reversion_regime):
            self.SellMarket(Math.Abs(self.Position))
            self._cooldown = cd
        elif self.Position < 0 and (price <= sma_val or deviation <= atr_val * 0.2 or not is_mean_reversion_regime):
            self.BuyMarket(Math.Abs(self.Position))
            self._cooldown = cd

    def CreateClone(self):
        return hurst_volatility_filter_strategy()
