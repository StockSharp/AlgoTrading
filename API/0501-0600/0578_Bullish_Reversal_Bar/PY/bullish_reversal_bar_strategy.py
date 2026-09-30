import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.BusinessEntities")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal, Math
from StockSharp.Messages import DataType, CandleStates, Level1Fields
from StockSharp.BusinessEntities import Subscription
from StockSharp.Algo.Indicators import Alligator, AwesomeOscillator, MarketFacilitationIndex, CandleIndicatorValue
from StockSharp.Algo.Strategies import Strategy


class bullish_reversal_bar_strategy(Strategy):
    """Below-Alligator reversal, later high confirmation, optional AO/MFI and a fixed bar-low stop."""

    def __init__(self):
        super(bullish_reversal_bar_strategy, self).__init__()
        self._enable_ao = self.Param("EnableAo", False)
        self._enable_mfi = self.Param("EnableMfi", False)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))
        self._alligator = None
        self._ao = None
        self._mfi = None
        self._reset_state()

    def GetWorkingSecurities(self):
        return [(self.Security, self._candle_type.Value), (self.Security, DataType.Level1)]

    def OnReseted(self):
        super(bullish_reversal_bar_strategy, self).OnReseted()
        self._reset_state()
        self._alligator = None
        self._ao = None
        self._mfi = None

    def _reset_state(self):
        self._previous_low = None
        self._previous_lips = None
        self._previous_ao = None
        self._previous_mfi = None
        self._previous_volume = Decimal.Zero
        self._pending = None
        self._stop_loss = Decimal.Zero
        self._exit_pending = False

    def OnStarted2(self, time):
        super(bullish_reversal_bar_strategy, self).OnStarted2(time)
        self._reset_state()
        self._alligator = Alligator()
        self._ao = AwesomeOscillator() if self._enable_ao.Value else None
        self._mfi = MarketFacilitationIndex() if self._enable_mfi.Value else None
        subscription = self.SubscribeCandles(self._candle_type.Value)
        subscription.BindEx(self._alligator, self._process_candle, True).Start()
        quotes = Subscription(DataType.Level1, self.Security)
        quotes.MarketData.BuildField = Level1Fields.BestBidPrice
        self.SubscribeLevel1(quotes).Bind(self._process_quote).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._alligator)
            if self._ao is not None:
                self.DrawIndicator(area, self._ao)
            if self._mfi is not None:
                self.DrawIndicator(area, self._mfi)
            self.DrawOwnTrades(area)

    @staticmethod
    def _process_filter(indicator, candle):
        if indicator is None:
            return None
        value = indicator.Process(CandleIndicatorValue(indicator, candle))
        return value.GetValue[Decimal](None) if indicator.IsFormed and not value.IsEmpty else None

    def _process_candle(self, candle, value):
        if candle.State != CandleStates.Finished:
            return
        previous_low = self._previous_low
        previous_lips = self._previous_lips
        previous_ao = self._previous_ao
        previous_mfi = self._previous_mfi
        previous_volume = self._previous_volume
        ao = self._process_filter(self._ao, candle)
        mfi = self._process_filter(self._mfi, candle)
        self._previous_low = candle.LowPrice
        self._previous_lips = value.Lips
        self._previous_ao = ao
        self._previous_mfi = mfi
        self._previous_volume = candle.TotalVolume
        jaw, teeth, lips = value.Jaw, value.Teeth, value.Lips
        if jaw is None or teeth is None or lips is None or not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self.Position > 0:
            if candle.LowPrice <= self._stop_loss or previous_lips is not None and lips < previous_lips:
                self._close_long()
            return
        if self.Position != 0:
            return
        self._exit_pending = False

        if self._pending is not None and candle.LowPrice <= self._pending[1]:
            self._pending = None
        if self._pending is not None and candle.ClosePrice > self._pending[0]:
            self._stop_loss = self._pending[1]
            self._pending = None
            self.BuyMarket()
            return

        median = (candle.HighPrice + candle.LowPrice) / Decimal(2)
        reversal = (previous_low is not None and candle.LowPrice < previous_low and
                    candle.ClosePrice > median and
                    candle.HighPrice < Math.Min(jaw, Math.Min(teeth, lips)) and
                    previous_lips is not None and lips > previous_lips)
        ao_filter = (not self._enable_ao.Value or
                     ao is not None and previous_ao is not None and ao > previous_ao)
        squat = (not self._enable_mfi.Value or
                 mfi is not None and previous_mfi is not None and
                 mfi < previous_mfi and candle.TotalVolume > previous_volume)
        if reversal and ao_filter and squat:
            self._pending = (candle.HighPrice, candle.LowPrice)

    def _process_quote(self, message):
        if self.Position == 0:
            self._exit_pending = False
            return
        bid = message.Changes[Level1Fields.BestBidPrice] if message.Changes.ContainsKey(Level1Fields.BestBidPrice) else None
        if self.Position > 0 and self._stop_loss > 0 and bid is not None and bid <= self._stop_loss:
            self._close_long()

    def _close_long(self):
        if self._exit_pending or self.Position <= 0:
            return
        self._exit_pending = True
        self.SellMarket(self.Position)

    def CreateClone(self):
        return bullish_reversal_bar_strategy()
