import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes, Level1Fields
from StockSharp.BusinessEntities import Subscription
from StockSharp.Algo.Indicators import DonchianChannels, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy

class rsi_donchian_strategy(Strategy):
    """
    RSI Donchian strategy.
    The channel spans the highest high and lowest low of the previous DonchianPeriod candles. A close above it with RSI above RsiOverbought
    confirms momentum and goes long, a close below it with RSI below RsiOversold goes short, reversing an opposite position. A long closes
    when price falls below the channel middle and a short when it rises above it, and a percent stop limits the loss.
    """

    def __init__(self):
        super(rsi_donchian_strategy, self).__init__()
        self._rsi_period = self.Param("RsiPeriod", 14).SetGreaterThanZero().SetDisplay("RSI Period", "Period of RSI", "Indicators")
        self._donchian_period = self.Param("DonchianPeriod", 20).SetGreaterThanZero().SetDisplay("Donchian Period", "Previous candles the channel spans", "Indicators")
        self._rsi_overbought = self.Param("RsiOverbought", 70.0).SetDisplay("RSI Overbought", "RSI level that confirms an upside breakout", "Indicators")
        self._rsi_oversold = self.Param("RsiOversold", 30.0).SetDisplay("RSI Oversold", "RSI level that confirms a downside breakout", "Indicators")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0).SetNotNegative().SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._prev_upper = None
        self._prev_lower = None

    def OnReseted(self):
        super(rsi_donchian_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(rsi_donchian_strategy, self).OnStarted2(time)

        self._reset_state()

        donchian = DonchianChannels()
        donchian.Length = self._donchian_period.Value
        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(donchian, rsi, self._process_candle).Start()

        self.StartProtection(Unit(), Unit(Decimal(self._stop_loss_percent.Value), UnitTypes.Percent), useMarketOrders=True, isLocalStop=True)

        # The stop has to see prices between candles, not only at their close.
        for field in (Level1Fields.BestBidPrice, Level1Fields.BestAskPrice):
            quotes = Subscription(DataType.Level1, self.Security)
            quotes.MarketData.BuildField = field
            self.SubscribeLevel1(quotes).Bind(self._observe_protection_quote).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, donchian)
            self.DrawOwnTrades(area)
            oscillators = self.CreateChartArea()
            if oscillators is not None:
                self.DrawIndicator(oscillators, rsi)

    def _observe_protection_quote(self, quote):
        # The high-level handler activates native protection before the callback, including between bars.
        pass

    def _process_candle(self, candle, donchian_value, rsi_value):
        if candle.State != CandleStates.Finished:
            return

        # The channel is measured on the candles before this one.
        upper = self._prev_upper
        lower = self._prev_lower

        if donchian_value.IsFormed and donchian_value.UpperBand is not None and donchian_value.LowerBand is not None:
            self._prev_upper = donchian_value.UpperBand
            self._prev_lower = donchian_value.LowerBand

        if not rsi_value.IsFormed or upper is None or lower is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        rsi = rsi_value.GetValue[Decimal](None)
        close = candle.ClosePrice
        middle = (upper + lower) / Decimal(2)

        if close > upper and rsi > Decimal(self._rsi_overbought.Value) and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif close < lower and rsi < Decimal(self._rsi_oversold.Value) and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
        elif self.Position > 0 and close < middle:
            self.SellMarket(self.Position)
        elif self.Position < 0 and close > middle:
            self.BuyMarket(-self.Position)

    def CreateClone(self):
        return rsi_donchian_strategy()
