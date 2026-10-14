import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes, Level1Fields
from StockSharp.BusinessEntities import Subscription
from StockSharp.Algo.Indicators import DonchianChannels, CommodityChannelIndex
from StockSharp.Algo.Strategies import Strategy

class donchian_cci_strategy(Strategy):
    """
    Donchian CCI strategy.
    The channel spans the highest high and lowest low of the previous DonchianPeriod candles. A close above it with CCI above CciOverbought
    confirms momentum and goes long, a close below it with CCI below CciOversold goes short, reversing an opposite position. A long closes
    when price falls below the channel middle and a short when it rises above it, and a percent stop limits the loss.
    """

    def __init__(self):
        super(donchian_cci_strategy, self).__init__()
        self._donchian_period = self.Param("DonchianPeriod", 20).SetGreaterThanZero().SetDisplay("Donchian Period", "Previous candles the channel spans", "Indicators")
        self._cci_period = self.Param("CciPeriod", 20).SetGreaterThanZero().SetDisplay("CCI Period", "Period of CCI", "Indicators")
        self._cci_overbought = self.Param("CciOverbought", 100.0).SetDisplay("CCI Overbought", "CCI level that confirms an upside breakout", "Indicators")
        self._cci_oversold = self.Param("CciOversold", -100.0).SetDisplay("CCI Oversold", "CCI level that confirms a downside breakout", "Indicators")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0).SetNotNegative().SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._prev_upper = None
        self._prev_lower = None

    def OnReseted(self):
        super(donchian_cci_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(donchian_cci_strategy, self).OnStarted2(time)

        self._reset_state()

        donchian = DonchianChannels()
        donchian.Length = self._donchian_period.Value
        cci = CommodityChannelIndex()
        cci.Length = self._cci_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(donchian, cci, self._process_candle).Start()

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
                self.DrawIndicator(oscillators, cci)

    def _observe_protection_quote(self, quote):
        # The high-level handler activates native protection before the callback, including between bars.
        pass

    def _process_candle(self, candle, donchian_value, cci_value):
        if candle.State != CandleStates.Finished:
            return

        # The channel is measured on the candles before this one.
        upper = self._prev_upper
        lower = self._prev_lower

        if donchian_value.IsFormed and donchian_value.UpperBand is not None and donchian_value.LowerBand is not None:
            self._prev_upper = donchian_value.UpperBand
            self._prev_lower = donchian_value.LowerBand

        if not cci_value.IsFormed or upper is None or lower is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        cci = cci_value.GetValue[Decimal](None)
        close = candle.ClosePrice
        middle = (upper + lower) / Decimal(2)

        if close > upper and cci > Decimal(self._cci_overbought.Value) and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif close < lower and cci < Decimal(self._cci_oversold.Value) and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
        elif self.Position > 0 and close < middle:
            self.SellMarket(self.Position)
        elif self.Position < 0 and close > middle:
            self.BuyMarket(-self.Position)

    def CreateClone(self):
        return donchian_cci_strategy()
