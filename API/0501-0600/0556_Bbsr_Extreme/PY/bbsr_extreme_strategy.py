import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.BusinessEntities")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.BusinessEntities import Subscription
from StockSharp.Messages import DataType, CandleStates, Level1Fields, OrderStates, OrderTypes, Sides, Unit, UnitTypes
from StockSharp.Algo.Indicators import BollingerBands, ExponentialMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class bbsr_extreme_strategy(Strategy):
    def __init__(self):
        super(bbsr_extreme_strategy, self).__init__()
        self._bb_period = self.Param("BollingerPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Bollinger Period", "Bollinger Bands length", "Indicators")
        self._bb_mult = self.Param("BollingerMultiplier", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Bollinger Multiplier", "Standard deviation multiplier", "Indicators")
        self._ma_length = self.Param("MaLength", 7) \
            .SetGreaterThanZero() \
            .SetDisplay("MA Length", "Length for moving average", "Indicators")
        self._atr_length = self.Param("AtrLength", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR Length", "ATR indicator period", "Risk Management")
        self._atr_stop_mult = self.Param("AtrStopMultiplier", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR Stop Multiplier", "ATR multiplier for stop", "Risk Management")
        self._atr_profit_mult = self.Param("AtrProfitMultiplier", 3.0) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR Profit Multiplier", "ATR multiplier for take profit", "Risk Management")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._clear_state()

    def _clear_state(self):
        self._previous = None
        self._pending = None

    def OnReseted(self):
        super(bbsr_extreme_strategy, self).OnReseted()
        self._clear_state()

    def OnStarted2(self, time):
        super(bbsr_extreme_strategy, self).OnStarted2(time)
        for field in (Level1Fields.BestBidPrice, Level1Fields.BestAskPrice):
            quotes = Subscription(DataType.Level1, self.Security)
            quotes.MarketData.BuildField = field
            self.SubscribeLevel1(quotes).Start()
        self._bb = BollingerBands()
        self._bb.Length = self._bb_period.Value
        self._bb.Width = self._bb_mult.Value
        self._ma = ExponentialMovingAverage()
        self._ma.Length = self._ma_length.Value
        self._atr = AverageTrueRange()
        self._atr.Length = self._atr_length.Value
        candles = self.SubscribeCandles(self._candle_type.Value)
        candles.BindEx(self._bb, self._ma, self._atr, self._process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, candles)
            self.DrawIndicator(area, self._ma)
            self.DrawIndicator(area, self._bb)
            self.DrawOwnTrades(area)

    def _process(self, candle, bb, ma, atr):
        if candle.State != CandleStates.Finished or not self._bb.IsFormed or not self._ma.IsFormed or not self._atr.IsFormed or bb.UpBand is None or bb.LowBand is None:
            return
        average = ma.GetValue[Decimal](None)
        distance = atr.GetValue[Decimal](None)
        close = candle.ClosePrice
        bull = self._previous is not None and self._previous[0] < self._previous[1] and close > bb.LowBand and average > self._previous[3]
        bear = self._previous is not None and self._previous[0] > self._previous[2] and close < bb.UpBand and average < self._previous[3]
        self._previous = (close, bb.LowBand, bb.UpBand, average)
        pending = self._pending is not None and self._pending.State not in (OrderStates.Done, OrderStates.Failed)
        if self.Position != 0 or pending or distance <= 0 or not self.IsFormedAndOnlineAndAllowTrading() or not (bull or bear):
            return
        self.StartProtection(Unit(distance * Decimal(self._atr_profit_mult.Value), UnitTypes.Absolute),
                             Unit(distance * Decimal(self._atr_stop_mult.Value), UnitTypes.Absolute),
                             useMarketOrders=True, isLocalStop=True)
        self._pending = self.CreateOrder(Sides.Buy if bull else Sides.Sell, Decimal(0), self.Volume)
        self._pending.Type = OrderTypes.Market
        self._pending.Comment = "0556Entry"
        self.RegisterOrder(self._pending)

    def CreateClone(self):
        return bbsr_extreme_strategy()
