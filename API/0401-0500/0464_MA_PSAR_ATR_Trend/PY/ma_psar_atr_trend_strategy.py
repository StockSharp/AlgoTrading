import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, AverageTrueRange, ParabolicSar
from StockSharp.Algo.Strategies import Strategy


class ma_psar_atr_trend_strategy(Strategy):
    """
    MA PSAR ATR Trend Strategy.
    A long opens when Fast MA > Slow MA, the close is above the fast MA and the low is above the Parabolic SAR of the last
    finished daily candle; a short mirrors this. UsePsarFilter switches the daily SAR condition off. Each entry gets a stop
    AtrMultiplierLong or AtrMultiplierShort ATRs away; a position closes when the stop is hit or the fast MA crosses back
    over the slow MA.
    """

    def __init__(self):
        super(ma_psar_atr_trend_strategy, self).__init__()
        self._fast_ma_period = self.Param("FastMaPeriod", 40).SetGreaterThanZero().SetDisplay("Fast MA", "Fast MA period", "Indicators")
        self._slow_ma_period = self.Param("SlowMaPeriod", 160).SetGreaterThanZero().SetDisplay("Slow MA", "Slow MA period", "Indicators")
        self._sar_step = self.Param("SarStep", 0.02).SetGreaterThanZero().SetDisplay("SAR Step", "Parabolic SAR acceleration step", "PSAR")
        self._sar_max_step = self.Param("SarMaxStep", 0.2).SetGreaterThanZero().SetDisplay("SAR Max Step", "Parabolic SAR maximum acceleration", "PSAR")
        self._atr_period = self.Param("AtrPeriod", 14).SetGreaterThanZero().SetDisplay("ATR Period", "ATR period", "Risk")
        self._atr_multiplier_long = self.Param("AtrMultiplierLong", 2.0).SetGreaterThanZero().SetDisplay("ATR Multiplier Long", "ATR multiplier of the long stop", "Risk")
        self._atr_multiplier_short = self.Param("AtrMultiplierShort", 2.0).SetGreaterThanZero().SetDisplay("ATR Multiplier Short", "ATR multiplier of the short stop", "Risk")
        self._use_psar_filter = self.Param("UsePsarFilter", True).SetDisplay("Use PSAR Filter", "Require the daily Parabolic SAR to agree", "PSAR")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Candle type of the signals", "General")
        self._daily_sar = None
        self._stop_price = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.candle_type), (self.Security, DataType.TimeFrame(TimeSpan.FromDays(1)))]

    def OnReseted(self):
        super(ma_psar_atr_trend_strategy, self).OnReseted()
        self._daily_sar = None
        self._stop_price = None

    def OnStarted2(self, time):
        super(ma_psar_atr_trend_strategy, self).OnStarted2(time)

        self._daily_sar = None
        self._stop_price = None

        fast_ma = ExponentialMovingAverage()
        fast_ma.Length = self._fast_ma_period.Value
        slow_ma = ExponentialMovingAverage()
        slow_ma.Length = self._slow_ma_period.Value
        atr = AverageTrueRange()
        atr.Length = self._atr_period.Value
        psar = ParabolicSar()
        psar.AccelerationStep = Decimal(float(self._sar_step.Value))
        psar.AccelerationMax = Decimal(float(self._sar_max_step.Value))

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(fast_ma, slow_ma, atr, self._process_candle).Start()

        self.SubscribeCandles(DataType.TimeFrame(TimeSpan.FromDays(1))).BindEx(psar, self._process_daily_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast_ma)
            self.DrawIndicator(area, slow_ma)
            self.DrawOwnTrades(area)

    def _process_daily_candle(self, candle, psar_value):
        if candle.State != CandleStates.Finished or not psar_value.IsFormed:
            return

        self._daily_sar = float(psar_value.GetValue[Decimal](None))

    def _process_candle(self, candle, fast_value, slow_value, atr_value):
        if candle.State != CandleStates.Finished:
            return

        if not fast_value.IsFormed or not slow_value.IsFormed or not atr_value.IsFormed:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        fast = float(fast_value.GetValue[Decimal](None))
        slow = float(slow_value.GetValue[Decimal](None))
        atr = float(atr_value.GetValue[Decimal](None))
        close = float(candle.ClosePrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)

        if self.Position > 0:
            if fast < slow or (self._stop_price is not None and low <= self._stop_price):
                self.SellMarket(self.Position)
                self._stop_price = None
            return

        if self.Position < 0:
            if fast > slow or (self._stop_price is not None and high >= self._stop_price):
                self.BuyMarket(-self.Position)
                self._stop_price = None
            return

        use_psar = self._use_psar_filter.Value
        if use_psar and self._daily_sar is None:
            return

        long_sar = not use_psar or low > self._daily_sar
        short_sar = not use_psar or high < self._daily_sar

        if fast > slow and close > fast and long_sar:
            self.BuyMarket(self.Volume)
            self._stop_price = close - atr * float(self._atr_multiplier_long.Value)
        elif fast < slow and close < fast and short_sar:
            self.SellMarket(self.Volume)
            self._stop_price = close + atr * float(self._atr_multiplier_short.Value)

    def CreateClone(self):
        return ma_psar_atr_trend_strategy()
