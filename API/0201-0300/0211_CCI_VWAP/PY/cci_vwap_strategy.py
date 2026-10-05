import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes, Level1Fields
from StockSharp.BusinessEntities import Subscription
from StockSharp.Algo.Indicators import CommodityChannelIndex
from StockSharp.Algo.Strategies import Strategy

class cci_vwap_strategy(Strategy):
    """
    CCI VWAP strategy.
    The market trades around the clock, so the session VWAP restarts with each UTC day and weighs each candle's typical price by its volume.
    CCI below CciOversold with a close below VWAP goes long and CCI above CciOverbought with a close above VWAP goes short,
    reversing an opposite position. A long closes once price closes above VWAP and a short once it closes below, and a percent stop limits the loss.
    """

    def __init__(self):
        super(cci_vwap_strategy, self).__init__()
        self._cci_period = self.Param("CciPeriod", 20).SetGreaterThanZero().SetDisplay("CCI Period", "Period of CCI", "Indicators")
        self._cci_oversold = self.Param("CciOversold", -100.0).SetDisplay("CCI Oversold", "CCI level for longs", "Indicators")
        self._cci_overbought = self.Param("CciOverbought", 100.0).SetDisplay("CCI Overbought", "CCI level for shorts", "Indicators")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0).SetNotNegative().SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._day = None
        self._cumulative_price_volume = Decimal(0)
        self._cumulative_volume = Decimal(0)

    def OnReseted(self):
        super(cci_vwap_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(cci_vwap_strategy, self).OnStarted2(time)

        self._reset_state()

        cci = CommodityChannelIndex()
        cci.Length = self._cci_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(cci, self._process_candle).Start()

        self.StartProtection(Unit(), Unit(Decimal(self._stop_loss_percent.Value), UnitTypes.Percent), useMarketOrders=True, isLocalStop=True)

        # The stop has to see prices between candles, not only at their close.
        for field in (Level1Fields.BestBidPrice, Level1Fields.BestAskPrice):
            quotes = Subscription(DataType.Level1, self.Security)
            quotes.MarketData.BuildField = field
            self.SubscribeLevel1(quotes).Bind(self._observe_protection_quote).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)
            oscillators = self.CreateChartArea()
            if oscillators is not None:
                self.DrawIndicator(oscillators, cci)

    def _observe_protection_quote(self, quote):
        # The high-level handler activates native protection before the callback, including between bars.
        pass

    def _process_candle(self, candle, cci_value):
        if candle.State != CandleStates.Finished:
            return

        day = candle.OpenTime.Date
        if self._day is None or self._day != day:
            self._day = day
            self._cumulative_price_volume = Decimal(0)
            self._cumulative_volume = Decimal(0)

        typical_price = (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / Decimal(3)
        self._cumulative_price_volume += typical_price * candle.TotalVolume
        self._cumulative_volume += candle.TotalVolume

        if not cci_value.IsFormed or self._cumulative_volume <= 0:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        cci = cci_value.GetValue[Decimal](None)
        vwap = self._cumulative_price_volume / self._cumulative_volume
        close = candle.ClosePrice

        if cci < Decimal(self._cci_oversold.Value) and close < vwap and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif cci > Decimal(self._cci_overbought.Value) and close > vwap and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
        elif self.Position > 0 and close > vwap:
            self.SellMarket(self.Position)
        elif self.Position < 0 and close < vwap:
            self.BuyMarket(-self.Position)

    def CreateClone(self):
        return cci_vwap_strategy()
