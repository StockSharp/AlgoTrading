import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes, Level1Fields
from StockSharp.BusinessEntities import Subscription
from StockSharp.Algo.Indicators import SimpleMovingAverage, ExponentialMovingAverage, WeightedMovingAverage, SmoothedMovingAverage, HullMovingAverage, WilliamsR
from StockSharp.Algo.Strategies import Strategy

class MovingAverageTypeEnum:
    """Moving average types."""
    Simple = 0
    Exponential = 1
    Weighted = 2
    Smoothed = 3
    HullMA = 4

class ma_williams_r_strategy(Strategy):
    """
    MA Williams %R strategy.
    A close above the MaPeriod moving average of type MaType with Williams %R below WilliamsROversold goes long and a close below it with
    %R above WilliamsROverbought goes short, reversing an opposite position. A long closes once %R returns to the -50 middle from below
    and a short once it returns from above, and a percent stop limits the loss.
    """

    def __init__(self):
        super(ma_williams_r_strategy, self).__init__()
        self._ma_period = self.Param("MaPeriod", 20).SetGreaterThanZero().SetDisplay("MA Period", "Period of the moving average", "Indicators")
        self._ma_type = self.Param("MaType", MovingAverageTypeEnum.Simple).SetDisplay("MA Type", "Type of the moving average", "Indicators")
        self._williams_r_period = self.Param("WilliamsRPeriod", 14).SetGreaterThanZero().SetDisplay("Williams %R Period", "Period of Williams %R", "Indicators")
        self._williams_r_oversold = self.Param("WilliamsROversold", -80.0).SetDisplay("Williams %R Oversold", "Williams %R level for longs", "Indicators")
        self._williams_r_overbought = self.Param("WilliamsROverbought", -20.0).SetDisplay("Williams %R Overbought", "Williams %R level for shorts", "Indicators")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0).SetNotNegative().SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnStarted2(self, time):
        super(ma_williams_r_strategy, self).OnStarted2(time)

        ma = self._create_moving_average()
        williams = WilliamsR()
        williams.Length = self._williams_r_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(ma, williams, self._process_candle).Start()

        self.StartProtection(Unit(), Unit(Decimal(self._stop_loss_percent.Value), UnitTypes.Percent), useMarketOrders=True, isLocalStop=True)

        # The stop has to see prices between candles, not only at their close.
        for field in (Level1Fields.BestBidPrice, Level1Fields.BestAskPrice):
            quotes = Subscription(DataType.Level1, self.Security)
            quotes.MarketData.BuildField = field
            self.SubscribeLevel1(quotes).Bind(self._observe_protection_quote).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ma)
            self.DrawOwnTrades(area)
            oscillators = self.CreateChartArea()
            if oscillators is not None:
                self.DrawIndicator(oscillators, williams)

    def _observe_protection_quote(self, quote):
        # The high-level handler activates native protection before the callback, including between bars.
        pass

    def _create_moving_average(self):
        ma_type = self._ma_type.Value
        if ma_type == MovingAverageTypeEnum.Exponential:
            ma = ExponentialMovingAverage()
        elif ma_type == MovingAverageTypeEnum.Weighted:
            ma = WeightedMovingAverage()
        elif ma_type == MovingAverageTypeEnum.Smoothed:
            ma = SmoothedMovingAverage()
        elif ma_type == MovingAverageTypeEnum.HullMA:
            ma = HullMovingAverage()
        else:
            ma = SimpleMovingAverage()
        ma.Length = self._ma_period.Value
        return ma

    def _process_candle(self, candle, ma_value, williams_value):
        if candle.State != CandleStates.Finished:
            return

        if not ma_value.IsFormed or not williams_value.IsFormed:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        ma = ma_value.GetValue[Decimal](None)
        williams = williams_value.GetValue[Decimal](None)
        close = candle.ClosePrice

        middle = Decimal(-50)
        if close > ma and williams < Decimal(self._williams_r_oversold.Value) and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif close < ma and williams > Decimal(self._williams_r_overbought.Value) and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
        elif self.Position > 0 and williams >= middle:
            self.SellMarket(self.Position)
        elif self.Position < 0 and williams <= middle:
            self.BuyMarket(-self.Position)

    def CreateClone(self):
        return ma_williams_r_strategy()
