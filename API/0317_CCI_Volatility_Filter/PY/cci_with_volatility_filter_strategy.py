import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import CommodityChannelIndex, AverageTrueRange, SimpleMovingAverage, DecimalIndicatorValue, CandleIndicatorValue
from StockSharp.Algo.Strategies import Strategy


class cci_with_volatility_filter_strategy(Strategy):
    """
    Strategy based on CCI with an ATR-based volatility filter.
    """

    def __init__(self):
        super(cci_with_volatility_filter_strategy, self).__init__()

        self._cci_period = self.Param("CciPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("CCI Period", "Period for CCI calculation", "Indicators")

        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR Period", "Period for ATR calculation", "Indicators")

        self._cci_oversold = self.Param("CciOversold", -100.0) \
            .SetDisplay("CCI Oversold", "CCI oversold level", "Indicators")

        self._cci_overbought = self.Param("CciOverbought", 100.0) \
            .SetDisplay("CCI Overbought", "CCI overbought level", "Indicators")

        self._signal_cooldown_bars = self.Param("SignalCooldownBars", 24) \
            .SetGreaterThanZero() \
            .SetDisplay("Signal Cooldown", "Bars to wait between entries", "Trading")

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(cci_with_volatility_filter_strategy, self).OnReseted()
        self._cooldown_remaining = 0

    def OnStarted2(self, time):
        super(cci_with_volatility_filter_strategy, self).OnStarted2(time)

        self._cci = CommodityChannelIndex()
        self._cci.Length = int(self._cci_period.Value)
        self._atr = AverageTrueRange()
        self._atr.Length = int(self._atr_period.Value)
        self._atr_sma = SimpleMovingAverage()
        self._atr_sma.Length = int(self._atr_period.Value)
        self._cooldown_remaining = 0

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

        self.StartProtection(
            Unit(2, UnitTypes.Percent),
            Unit(1, UnitTypes.Percent)
        )

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1

        cci_result = self._cci.Process(CandleIndicatorValue(self._cci, candle))
        atr_result = self._atr.Process(CandleIndicatorValue(self._atr, candle))

        if not cci_result.IsFormed or not atr_result.IsFormed:
            return

        atr_val = float(atr_result)

        atr_avg_input = DecimalIndicatorValue(self._atr_sma, Decimal(atr_val), candle.ServerTime)
        atr_avg_input.IsFinal = True
        atr_avg_result = self._atr_sma.Process(atr_avg_input)

        if not atr_avg_result.IsFormed:
            return

        cci_val = float(cci_result)
        average_atr = float(atr_avg_result)

        is_tradable_volatility = average_atr <= 0.0 or atr_val <= average_atr * 10.0

        if self._cooldown_remaining > 0 or not is_tradable_volatility:
            return

        oversold = float(self._cci_oversold.Value)
        overbought = float(self._cci_overbought.Value)
        cd = int(self._signal_cooldown_bars.Value)

        if self.Position == 0 and cci_val <= oversold:
            self.BuyMarket()
            self._cooldown_remaining = cd
        elif self.Position == 0 and cci_val >= overbought:
            self.SellMarket()
            self._cooldown_remaining = cd

    def CreateClone(self):
        return cci_with_volatility_filter_strategy()
