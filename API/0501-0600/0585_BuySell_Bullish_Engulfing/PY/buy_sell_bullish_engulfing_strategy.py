import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.BusinessEntities")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal, Convert, Math
from StockSharp.Messages import DataType, CandleStates, Level1Fields, Unit, UnitTypes
from StockSharp.BusinessEntities import Subscription
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class buy_sell_bullish_engulfing_strategy(Strategy):
    """Long-only body engulfing, optional prior-bar SMA50 trend, equity sizing and local SL/TP."""

    def __init__(self):
        super(buy_sell_bullish_engulfing_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15)))
        self._take_profit_percent = self.Param("TakeProfitPercent", 2.0).SetRange(0.0, 100.0)
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0).SetRange(0.0, 100.0)
        self._order_percent = self.Param("OrderPercent", 30.0).SetRange(0.0, 100.0).SetGreaterThanZero()
        self._trend_mode = self.Param("TrendMode", "SMA50")
        self._previous_bar = None

    def GetWorkingSecurities(self):
        return [(self.Security, self._candle_type.Value), (self.Security, DataType.Level1)]

    def OnReseted(self):
        super(buy_sell_bullish_engulfing_strategy, self).OnReseted()
        self._previous_bar = None

    def OnStarted2(self, time):
        super(buy_sell_bullish_engulfing_strategy, self).OnStarted2(time)
        if self._trend_mode.Value not in ("None", "SMA50"):
            raise ValueError("TrendMode must be None or SMA50.")
        self._previous_bar = None
        self.StartProtection(Unit(float(self._take_profit_percent.Value), UnitTypes.Percent),
                             Unit(float(self._stop_loss_percent.Value), UnitTypes.Percent),
                             useMarketOrders=True, isLocalStop=True)
        sma = SimpleMovingAverage()
        sma.Length = 50 if self._trend_mode.Value == "SMA50" else 1

        def process(candle, value):
            self._process_candle(candle, value, sma.IsFormed)

        subscription = self.SubscribeCandles(self._candle_type.Value)
        subscription.Bind(sma, process).Start()
        # Bid updates drive native long-position protection between finished candles.
        quotes = Subscription(DataType.Level1, self.Security)
        quotes.MarketData.BuildField = Level1Fields.BestBidPrice
        self.SubscribeLevel1(quotes).Bind(lambda message: None).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            if self._trend_mode.Value == "SMA50":
                self.DrawIndicator(area, sma)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, sma_value, sma_ready):
        if candle.State != CandleStates.Finished:
            return
        previous = self._previous_bar
        self._previous_bar = (candle.OpenPrice, candle.ClosePrice, sma_value, sma_ready)
        if previous is None or self.Position != 0 or not self.IsFormedAndOnlineAndAllowTrading():
            return

        engulfing = (previous[0] > previous[1] and candle.ClosePrice > candle.OpenPrice and
                     candle.OpenPrice <= previous[1] and candle.ClosePrice >= previous[0])
        trend = (self._trend_mode.Value == "None" or
                 previous[3] and previous[1] < previous[2])
        if not engulfing or not trend or candle.ClosePrice <= 0:
            return

        equity = self.Portfolio.CurrentValue
        if equity is None:
            equity = self.Portfolio.BeginValue
        step = self.Security.VolumeStep
        if step is None:
            step = Decimal.One
        if equity is None or equity <= 0 or step <= 0:
            return
        volume = equity * Convert.ToDecimal(self._order_percent.Value) / Decimal(100) / candle.ClosePrice
        maximum = self.Security.MaxVolume
        if maximum is not None:
            volume = Math.Min(volume, maximum)
        volume = Math.Floor(volume / step) * step
        minimum = self.Security.MinVolume
        if minimum is None:
            minimum = step
        if volume > 0 and volume >= minimum:
            self.BuyMarket(volume)

    def CreateClone(self):
        return buy_sell_bullish_engulfing_strategy()
