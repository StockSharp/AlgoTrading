import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes, Level1Fields
from StockSharp.BusinessEntities import Subscription
from StockSharp.Algo.Indicators import ExponentialMovingAverage, AverageTrueRange, WilliamsR
from StockSharp.Algo.Strategies import Strategy

class keltner_williams_r_strategy(Strategy):
    """
    Keltner Williams R strategy.
    The Keltner Channel is the EmaPeriod EMA plus and minus KeltnerMultiplier times the AtrPeriod ATR. A close below the lower band with
    Williams %R below WilliamsROversold goes long and a close above the upper band with %R above WilliamsROverbought goes short, reversing
    an opposite position. A long closes once price returns to the middle band and a short likewise, and a percent stop limits the loss.
    """

    def __init__(self):
        super(keltner_williams_r_strategy, self).__init__()
        self._ema_period = self.Param("EmaPeriod", 20).SetGreaterThanZero().SetDisplay("EMA Period", "Period of the channel EMA", "Keltner")
        self._keltner_multiplier = self.Param("KeltnerMultiplier", 2.0).SetGreaterThanZero().SetDisplay("Keltner Multiplier", "ATR multiplier of the channel width", "Keltner")
        self._atr_period = self.Param("AtrPeriod", 14).SetGreaterThanZero().SetDisplay("ATR Period", "Period of the channel ATR", "Keltner")
        self._williams_r_period = self.Param("WilliamsRPeriod", 14).SetGreaterThanZero().SetDisplay("Williams %R Period", "Period of Williams %R", "Williams %R")
        self._williams_r_oversold = self.Param("WilliamsROversold", -80.0).SetDisplay("Williams %R Oversold", "Williams %R level for longs", "Williams %R")
        self._williams_r_overbought = self.Param("WilliamsROverbought", -20.0).SetDisplay("Williams %R Overbought", "Williams %R level for shorts", "Williams %R")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0).SetNotNegative().SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnStarted2(self, time):
        super(keltner_williams_r_strategy, self).OnStarted2(time)

        ema = ExponentialMovingAverage()
        ema.Length = self._ema_period.Value
        atr = AverageTrueRange()
        atr.Length = self._atr_period.Value
        williams = WilliamsR()
        williams.Length = self._williams_r_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(ema, atr, williams, self._process_candle).Start()

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
                self.DrawIndicator(oscillators, williams)

    def _observe_protection_quote(self, quote):
        # The high-level handler activates native protection before the callback, including between bars.
        pass

    def _process_candle(self, candle, ema_value, atr_value, williams_value):
        if candle.State != CandleStates.Finished:
            return

        if not ema_value.IsFormed or not atr_value.IsFormed or not williams_value.IsFormed:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        middle = ema_value.GetValue[Decimal](None)
        atr = atr_value.GetValue[Decimal](None)
        williams = williams_value.GetValue[Decimal](None)
        multiplier = Decimal(self._keltner_multiplier.Value)
        upper = middle + multiplier * atr
        lower = middle - multiplier * atr
        close = candle.ClosePrice

        if close < lower and williams < Decimal(self._williams_r_oversold.Value) and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif close > upper and williams > Decimal(self._williams_r_overbought.Value) and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
        elif self.Position > 0 and close >= middle:
            self.SellMarket(self.Position)
        elif self.Position < 0 and close <= middle:
            self.BuyMarket(-self.Position)

    def CreateClone(self):
        return keltner_williams_r_strategy()
