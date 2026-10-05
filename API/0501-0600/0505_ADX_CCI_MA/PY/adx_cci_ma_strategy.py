import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import (AverageDirectionalIndex, CommodityChannelIndex, SimpleMovingAverage, ExponentialMovingAverage,
    WeightedMovingAverage, SmoothedMovingAverage)
from StockSharp.Algo.Strategies import Strategy

CCI_LEVEL = 100


class MovingAverageTypeEnum:
    """Moving average types."""
    Simple = 0
    Exponential = 1
    Weighted = 2
    Smoothed = 3


class adx_cci_ma_strategy(Strategy):
    """
    ADX CCI MA strategy.
    A long opens when +DI crosses above -DI, CCI is above 100 and ADX exceeds AdxThreshold (and the close is above the moving average
    when UseMaTrend is set); a short mirrors this with -DI crossing above +DI and CCI below -100. Percent take profit and stop loss
    protect positions, and the optional MA risk management exits after MaRiskExitCandles consecutive closes on the wrong side of the
    moving average.
    """

    def __init__(self):
        super(adx_cci_ma_strategy, self).__init__()
        self._enable_long = self.Param("EnableLong", True).SetDisplay("Enable Long", "Allow long trades", "General")
        self._enable_short = self.Param("EnableShort", True).SetDisplay("Enable Short", "Allow short trades", "General")
        self._take_profit_percent = self.Param("TakeProfitPercent", 2.0).SetNotNegative().SetDisplay("Take Profit %", "Take profit percentage", "Risk")
        self._stop_loss_percent = self.Param("StopLossPercent", 1.0).SetNotNegative().SetDisplay("Stop Loss %", "Stop loss percentage", "Risk")
        self._cci_period = self.Param("CciPeriod", 15).SetGreaterThanZero().SetDisplay("CCI Period", "CCI period", "Indicators")
        self._adx_length = self.Param("AdxLength", 10).SetGreaterThanZero().SetDisplay("ADX Length", "ADX period", "Indicators")
        self._adx_threshold = self.Param("AdxThreshold", 20.0).SetDisplay("ADX Threshold", "ADX level a trend must exceed", "Indicators")
        self._use_ma_trend = self.Param("UseMaTrend", True).SetDisplay("Use MA Trend", "Require the close on the trade side of the moving average", "MA")
        self._ma_type = self.Param("MaType", MovingAverageTypeEnum.Simple).SetDisplay("MA Type", "Moving average type", "MA")
        self._ma_length = self.Param("MaLength", 200).SetGreaterThanZero().SetDisplay("MA Length", "Moving average period", "MA")
        self._use_ma_risk_management = self.Param("UseMaRiskManagement", False).SetDisplay("Use MA Risk Management", "Exit after several closes against the moving average", "Risk")
        self._ma_risk_exit_candles = self.Param("MaRiskExitCandles", 2).SetGreaterThanZero().SetDisplay("MA Risk Exit Candles", "Consecutive closes against the moving average that trigger the exit", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._prev_plus_di = None
        self._prev_minus_di = None
        self._against_ma_count = 0

    def OnReseted(self):
        super(adx_cci_ma_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(adx_cci_ma_strategy, self).OnStarted2(time)

        self._reset_state()

        adx = AverageDirectionalIndex()
        adx.Length = self._adx_length.Value
        cci = CommodityChannelIndex()
        cci.Length = self._cci_period.Value
        ma_type = self._ma_type.Value
        if ma_type == MovingAverageTypeEnum.Exponential:
            ma = ExponentialMovingAverage()
        elif ma_type == MovingAverageTypeEnum.Weighted:
            ma = WeightedMovingAverage()
        elif ma_type == MovingAverageTypeEnum.Smoothed:
            ma = SmoothedMovingAverage()
        else:
            ma = SimpleMovingAverage()
        ma.Length = self._ma_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(adx, cci, ma, self._process_candle).Start()

        self.StartProtection(Unit(Decimal(self._take_profit_percent.Value), UnitTypes.Percent), Unit(Decimal(self._stop_loss_percent.Value), UnitTypes.Percent), useMarketOrders=True)

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ma)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, adx_value, cci_value, ma_value):
        if candle.State != CandleStates.Finished:
            return

        if not adx_value.IsFormed or not cci_value.IsFormed or not ma_value.IsFormed:
            return

        adx = adx_value.MovingAverage
        plus_di = adx_value.Dx.Plus
        minus_di = adx_value.Dx.Minus
        if adx is None or plus_di is None or minus_di is None:
            return

        prev_plus = self._prev_plus_di
        prev_minus = self._prev_minus_di
        self._prev_plus_di = plus_di
        self._prev_minus_di = minus_di

        if prev_plus is None or prev_minus is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        cci = cci_value.GetValue[Decimal](None)
        ma = ma_value.GetValue[Decimal](None)
        close = candle.ClosePrice

        if (self.Position > 0 and close < ma) or (self.Position < 0 and close > ma):
            self._against_ma_count += 1
        else:
            self._against_ma_count = 0

        if self._use_ma_risk_management.Value and self.Position != 0 and self._against_ma_count >= self._ma_risk_exit_candles.Value:
            if self.Position > 0:
                self.SellMarket(self.Position)
            else:
                self.BuyMarket(-self.Position)
            self._against_ma_count = 0
            return

        threshold = Decimal(self._adx_threshold.Value)
        use_ma = self._use_ma_trend.Value

        long_signal = (self._enable_long.Value and prev_plus <= prev_minus and plus_di > minus_di and cci > CCI_LEVEL
            and adx > threshold and (not use_ma or close > ma))
        short_signal = (self._enable_short.Value and prev_minus <= prev_plus and minus_di > plus_di and cci < -CCI_LEVEL
            and adx > threshold and (not use_ma or close < ma))

        if long_signal and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
            self._against_ma_count = 0
        elif short_signal and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
            self._against_ma_count = 0

    def CreateClone(self):
        return adx_cci_ma_strategy()
