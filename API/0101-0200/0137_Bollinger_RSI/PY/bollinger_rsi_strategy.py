import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes, Level1Fields
from StockSharp.BusinessEntities import Subscription
from StockSharp.Algo.Indicators import BollingerBands, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy

class bollinger_rsi_strategy(Strategy):
    """
    Bollinger RSI strategy.
    A close above the upper band that is higher than the previous close above that band while RSI is lower than it was then
    is a bearish divergence and goes short; the mirror below the lower band goes long, reversing an opposite position.
    The position closes once price closes back inside the bands or RSI crosses back over 50, and a percent stop limits the loss.
    """

    def __init__(self):
        super(bollinger_rsi_strategy, self).__init__()
        self._bollinger_period = self.Param("BollingerPeriod", 20).SetGreaterThanZero().SetDisplay("Bollinger Period", "Period of the Bollinger Bands", "Indicators")
        self._bollinger_deviation = self.Param("BollingerDeviation", 2.0).SetGreaterThanZero().SetDisplay("Bollinger Deviation", "Standard deviation multiplier of the bands", "Indicators")
        self._rsi_period = self.Param("RsiPeriod", 14).SetGreaterThanZero().SetDisplay("RSI Period", "Period of RSI", "Indicators")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0).SetNotNegative().SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        # The previous close outside each band and RSI at that close.
        self._upper_close = None
        self._upper_rsi = Decimal(0)
        self._lower_close = None
        self._lower_rsi = Decimal(0)

    def OnReseted(self):
        super(bollinger_rsi_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(bollinger_rsi_strategy, self).OnStarted2(time)

        self._reset_state()

        bollinger = BollingerBands()
        bollinger.Length = self._bollinger_period.Value
        bollinger.Width = Decimal(self._bollinger_deviation.Value)
        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(bollinger, rsi, self._process_candle).Start()

        self.StartProtection(Unit(), Unit(Decimal(self._stop_loss_percent.Value), UnitTypes.Percent), useMarketOrders=True, isLocalStop=True)

        # The stop has to see prices between candles, not only at their close.
        for field in (Level1Fields.BestBidPrice, Level1Fields.BestAskPrice):
            quotes = Subscription(DataType.Level1, self.Security)
            quotes.MarketData.BuildField = field
            self.SubscribeLevel1(quotes).Bind(self._observe_protection_quote).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, bollinger)
            self.DrawOwnTrades(area)
            oscillators = self.CreateChartArea()
            if oscillators is not None:
                self.DrawIndicator(oscillators, rsi)

    def _observe_protection_quote(self, quote):
        # The high-level handler activates native protection before the callback, including between bars.
        pass

    def _process_candle(self, candle, bollinger_value, rsi_value):
        if candle.State != CandleStates.Finished:
            return

        if not bollinger_value.IsFormed or not rsi_value.IsFormed:
            return
        if bollinger_value.UpBand is None or bollinger_value.LowBand is None:
            return

        upper = bollinger_value.UpBand
        lower = bollinger_value.LowBand
        rsi = rsi_value.GetValue[Decimal](None)
        close = candle.ClosePrice
        signal = 0

        if close > upper:
            if self._upper_close is not None and close > self._upper_close and rsi < self._upper_rsi:
                signal = -1
            self._upper_close = close
            self._upper_rsi = rsi
        elif close < lower:
            if self._lower_close is not None and close < self._lower_close and rsi > self._lower_rsi:
                signal = 1
            self._lower_close = close
            self._lower_rsi = rsi

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        middle = Decimal(50)
        if signal > 0:
            if self.Position <= 0:
                self.BuyMarket(self.Volume + abs(self.Position))
        elif signal < 0:
            if self.Position >= 0:
                self.SellMarket(self.Volume + abs(self.Position))
        elif self.Position > 0:
            if close >= lower or rsi > middle:
                self.SellMarket(self.Position)
        elif self.Position < 0:
            if close <= upper or rsi < middle:
                self.BuyMarket(-self.Position)

    def CreateClone(self):
        return bollinger_rsi_strategy()
