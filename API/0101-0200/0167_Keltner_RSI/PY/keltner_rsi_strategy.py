import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes, Level1Fields
from StockSharp.BusinessEntities import Subscription
from StockSharp.Algo.Indicators import ExponentialMovingAverage, AverageTrueRange, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy

class keltner_rsi_strategy(Strategy):
    """
    Keltner RSI strategy.
    The Keltner Channel is the EmaPeriod EMA plus and minus AtrMultiplier times the AtrPeriod ATR. A close below the lower band with RSI
    below RsiOversoldLevel goes long and a close above the upper band with RSI above RsiOverboughtLevel goes short, reversing an opposite
    position. The position closes once price returns to the EMA, and a percent stop limits the loss.
    """

    def __init__(self):
        super(keltner_rsi_strategy, self).__init__()
        self._ema_period = self.Param("EmaPeriod", 20).SetGreaterThanZero().SetDisplay("EMA Period", "Period of the channel EMA", "Keltner")
        self._atr_period = self.Param("AtrPeriod", 14).SetGreaterThanZero().SetDisplay("ATR Period", "Period of the channel ATR", "Keltner")
        self._atr_multiplier = self.Param("AtrMultiplier", 2.0).SetGreaterThanZero().SetDisplay("ATR Multiplier", "ATR multiplier of the channel width", "Keltner")
        self._rsi_period = self.Param("RsiPeriod", 14).SetGreaterThanZero().SetDisplay("RSI Period", "Period of RSI", "RSI")
        self._rsi_overbought_level = self.Param("RsiOverboughtLevel", 70.0).SetDisplay("RSI Overbought", "RSI level for shorts", "RSI")
        self._rsi_oversold_level = self.Param("RsiOversoldLevel", 30.0).SetDisplay("RSI Oversold", "RSI level for longs", "RSI")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0).SetNotNegative().SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnStarted2(self, time):
        super(keltner_rsi_strategy, self).OnStarted2(time)

        ema = ExponentialMovingAverage()
        ema.Length = self._ema_period.Value
        atr = AverageTrueRange()
        atr.Length = self._atr_period.Value
        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(ema, atr, rsi, self._process_candle).Start()

        self.StartProtection(Unit(), Unit(Decimal(self._stop_loss_percent.Value), UnitTypes.Percent), useMarketOrders=True, isLocalStop=True)

        # The stop has to see prices between candles, not only at their close.
        for field in (Level1Fields.BestBidPrice, Level1Fields.BestAskPrice):
            quotes = Subscription(DataType.Level1, self.Security)
            quotes.MarketData.BuildField = field
            self.SubscribeLevel1(quotes).Bind(self._observe_protection_quote).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)
            oscillators = self.CreateChartArea()
            if oscillators is not None:
                self.DrawIndicator(oscillators, rsi)

    def _observe_protection_quote(self, quote):
        # The high-level handler activates native protection before the callback, including between bars.
        pass

    def _process_candle(self, candle, ema_value, atr_value, rsi_value):
        if candle.State != CandleStates.Finished:
            return

        if not ema_value.IsFormed or not atr_value.IsFormed or not rsi_value.IsFormed:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        middle = ema_value.GetValue[Decimal](None)
        atr = atr_value.GetValue[Decimal](None)
        rsi = rsi_value.GetValue[Decimal](None)
        multiplier = Decimal(self._atr_multiplier.Value)
        upper = middle + multiplier * atr
        lower = middle - multiplier * atr
        close = candle.ClosePrice

        if close < lower and rsi < Decimal(self._rsi_oversold_level.Value) and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif close > upper and rsi > Decimal(self._rsi_overbought_level.Value) and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
        elif self.Position > 0 and close >= middle:
            self.SellMarket(self.Position)
        elif self.Position < 0 and close <= middle:
            self.BuyMarket(-self.Position)

    def CreateClone(self):
        return keltner_rsi_strategy()
