import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes, Level1Fields
from StockSharp.BusinessEntities import Subscription
from StockSharp.Algo.Indicators import SimpleMovingAverage, AverageDirectionalIndex, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy

class ma_adx_strategy(Strategy):
    """
    MA ADX strategy.
    A close crossing above the MaPeriod SMA while ADX is above AdxThreshold goes long and a cross below goes short; the reverse cross
    closes the position and, with ADX still strong, opens the other side. The target lies TakeProfitAtrMultiplier ATR from the entry close
    and is checked on candle closes, and a percent stop limits the loss.
    """

    def __init__(self):
        super(ma_adx_strategy, self).__init__()
        self._ma_period = self.Param("MaPeriod", 20).SetGreaterThanZero().SetDisplay("MA Period", "Period of the SMA", "Indicators")
        self._adx_period = self.Param("AdxPeriod", 14).SetGreaterThanZero().SetDisplay("ADX Period", "Period of ADX", "Indicators")
        self._adx_threshold = self.Param("AdxThreshold", 25.0).SetDisplay("ADX Threshold", "ADX level of a strong trend", "Indicators")
        self._take_profit_atr_multiplier = self.Param("TakeProfitAtrMultiplier", 2.0).SetNotNegative().SetDisplay("Take Profit ATR", "Target distance from the entry in ATRs", "Risk")
        self._atr_period = self.Param("AtrPeriod", 14).SetGreaterThanZero().SetDisplay("ATR Period", "Period of the target ATR", "Risk")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0).SetNotNegative().SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._prev_close = None
        self._prev_ma = Decimal(0)
        self._target_price = Decimal(0)

    def OnReseted(self):
        super(ma_adx_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(ma_adx_strategy, self).OnStarted2(time)

        self._reset_state()

        sma = SimpleMovingAverage()
        sma.Length = self._ma_period.Value
        adx = AverageDirectionalIndex()
        adx.Length = self._adx_period.Value
        atr = AverageTrueRange()
        atr.Length = self._atr_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(sma, adx, atr, self._process_candle).Start()

        self.StartProtection(Unit(), Unit(Decimal(self._stop_loss_percent.Value), UnitTypes.Percent), useMarketOrders=True, isLocalStop=True)

        # The stop has to see prices between candles, not only at their close.
        for field in (Level1Fields.BestBidPrice, Level1Fields.BestAskPrice):
            quotes = Subscription(DataType.Level1, self.Security)
            quotes.MarketData.BuildField = field
            self.SubscribeLevel1(quotes).Bind(self._observe_protection_quote).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sma)
            self.DrawOwnTrades(area)
            oscillators = self.CreateChartArea()
            if oscillators is not None:
                self.DrawIndicator(oscillators, adx)

    def _observe_protection_quote(self, quote):
        # The high-level handler activates native protection before the callback, including between bars.
        pass

    def _process_candle(self, candle, sma_value, adx_value, atr_value):
        if candle.State != CandleStates.Finished:
            return

        if not sma_value.IsFormed or not adx_value.IsFormed or not atr_value.IsFormed:
            return
        if adx_value.MovingAverage is None:
            return

        ma = sma_value.GetValue[Decimal](None)
        close = candle.ClosePrice
        prev_close = self._prev_close
        prev_ma = self._prev_ma
        self._prev_close = close
        self._prev_ma = ma

        if prev_close is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        atr = atr_value.GetValue[Decimal](None)
        cross_up = prev_close <= prev_ma and close > ma
        cross_down = prev_close >= prev_ma and close < ma
        strong = adx_value.MovingAverage > Decimal(self._adx_threshold.Value)

        target_atr = Decimal(self._take_profit_atr_multiplier.Value)
        if cross_up and strong and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
            self._target_price = close + target_atr * atr
        elif cross_down and strong and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
            self._target_price = close - target_atr * atr
        elif self.Position > 0 and (cross_down or (target_atr > 0 and close >= self._target_price)):
            self.SellMarket(self.Position)
        elif self.Position < 0 and (cross_up or (target_atr > 0 and close <= self._target_price)):
            self.BuyMarket(-self.Position)

    def CreateClone(self):
        return ma_adx_strategy()
