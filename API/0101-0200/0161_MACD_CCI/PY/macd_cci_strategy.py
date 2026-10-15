import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes, Level1Fields
from StockSharp.BusinessEntities import Subscription
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergenceSignal, CommodityChannelIndex
from StockSharp.Algo.Strategies import Strategy

class macd_cci_strategy(Strategy):
    """
    MACD CCI strategy.
    MACD above its signal line with CCI below CciOversold goes long and MACD below the signal line with CCI above CciOverbought goes short,
    reversing an opposite position. A long closes when MACD crosses below the signal line and a short when it crosses above it,
    and a percent stop limits the loss.
    """

    def __init__(self):
        super(macd_cci_strategy, self).__init__()
        self._fast_period = self.Param("FastPeriod", 12).SetGreaterThanZero().SetDisplay("Fast Period", "Fast EMA period of MACD", "MACD")
        self._slow_period = self.Param("SlowPeriod", 26).SetGreaterThanZero().SetDisplay("Slow Period", "Slow EMA period of MACD", "MACD")
        self._signal_period = self.Param("SignalPeriod", 9).SetGreaterThanZero().SetDisplay("Signal Period", "Signal line period of MACD", "MACD")
        self._cci_period = self.Param("CciPeriod", 20).SetGreaterThanZero().SetDisplay("CCI Period", "Period of CCI", "CCI")
        self._cci_oversold = self.Param("CciOversold", -100.0).SetDisplay("CCI Oversold", "CCI level for longs", "CCI")
        self._cci_overbought = self.Param("CciOverbought", 100.0).SetDisplay("CCI Overbought", "CCI level for shorts", "CCI")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0).SetNotNegative().SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnStarted2(self, time):
        super(macd_cci_strategy, self).OnStarted2(time)

        macd = MovingAverageConvergenceDivergenceSignal()
        macd.Macd.ShortMa.Length = self._fast_period.Value
        macd.Macd.LongMa.Length = self._slow_period.Value
        macd.SignalMa.Length = self._signal_period.Value
        cci = CommodityChannelIndex()
        cci.Length = self._cci_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(macd, cci, self._process_candle).Start()

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
                self.DrawIndicator(oscillators, macd)
                self.DrawIndicator(oscillators, cci)

    def _observe_protection_quote(self, quote):
        # The high-level handler activates native protection before the callback, including between bars.
        pass

    def _process_candle(self, candle, macd_value, cci_value):
        if candle.State != CandleStates.Finished:
            return

        if not macd_value.IsFormed or not cci_value.IsFormed:
            return
        if macd_value.Macd is None or macd_value.Signal is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        macd = macd_value.Macd
        signal = macd_value.Signal
        cci = cci_value.GetValue[Decimal](None)

        if macd > signal and cci < Decimal(self._cci_oversold.Value) and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif macd < signal and cci > Decimal(self._cci_overbought.Value) and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
        elif self.Position > 0 and macd < signal:
            self.SellMarket(self.Position)
        elif self.Position < 0 and macd > signal:
            self.BuyMarket(-self.Position)

    def CreateClone(self):
        return macd_cci_strategy()
