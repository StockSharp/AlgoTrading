import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SmoothedMovingAverage, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy

class sniper_jaw_strategy(Strategy):
    """Alligator jaw/teeth/lips trend following with SL/TP."""
    def __init__(self):
        super(sniper_jaw_strategy, self).__init__()
        self._order_volume = self.Param("OrderVolume", 0.1).SetGreaterThanZero().SetDisplay("Order Volume", "Trade size", "Trading")
        self._enable_trading = self.Param("EnableTrading", True).SetDisplay("Enable Trading", "Master switch", "Trading")
        self._use_entry_to_exit = self.Param("UseEntryToExit", True).SetDisplay("Use Entry To Exit", "Close opposite before new trade", "Trading")
        self._sl_pips = self.Param("StopLossPips", 20).SetNotNegative().SetDisplay("Stop Loss (pips)", "SL distance", "Risk")
        self._tp_pips = self.Param("TakeProfitPips", 50).SetNotNegative().SetDisplay("Take Profit (pips)", "TP distance", "Risk")
        self._minimum_bars = self.Param("MinimumBars", 1).SetGreaterThanZero().SetDisplay("Minimum Bars", "Required candles before trading", "Filters")
        self._jaw_period = self.Param("JawPeriod", 13).SetGreaterThanZero().SetDisplay("Jaw Period", "Jaw SMA length", "Alligator")
        self._teeth_period = self.Param("TeethPeriod", 8).SetGreaterThanZero().SetDisplay("Teeth Period", "Teeth SMA length", "Alligator")
        self._lips_period = self.Param("LipsPeriod", 5).SetGreaterThanZero().SetDisplay("Lips Period", "Lips SMA length", "Alligator")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))).SetDisplay("Candle Type", "Candle type", "Data")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(sniper_jaw_strategy, self).OnReseted()
        self._long_stop = None
        self._long_take = None
        self._short_stop = None
        self._short_take = None
        self._finished_candles = 0
        self._last_signal_time = None

    def OnStarted(self, time):
        super(sniper_jaw_strategy, self).OnStarted(time)
        self._long_stop = None
        self._long_take = None
        self._short_stop = None
        self._short_take = None
        self._finished_candles = 0
        self._last_signal_time = None

        self._pip_size = self._calculate_pip_size()

        self._jaw = SmoothedMovingAverage()
        self._jaw.Length = self._jaw_period.Value
        self._teeth = SmoothedMovingAverage()
        self._teeth.Length = self._teeth_period.Value
        self._lips = SmoothedMovingAverage()
        self._lips.Length = self._lips_period.Value

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(self.ProcessCandle).Start()

    def OnOwnTradeReceived(self, trade):
        super(sniper_jaw_strategy, self).OnOwnTradeReceived(trade)
        if trade.Order is None or trade.Order.Security != self.Security:
            return
        entry_price = float(trade.Trade.Price)
        pip = self._pip_size
        if self.Position > 0:
            self._long_stop = entry_price - self._sl_pips.Value * pip if self._sl_pips.Value > 0 else None
            self._long_take = entry_price + self._tp_pips.Value * pip if self._tp_pips.Value > 0 else None
            self._short_stop = None
            self._short_take = None
        elif self.Position < 0:
            self._short_stop = entry_price + self._sl_pips.Value * pip if self._sl_pips.Value > 0 else None
            self._short_take = entry_price - self._tp_pips.Value * pip if self._tp_pips.Value > 0 else None
            self._long_stop = None
            self._long_take = None
        else:
            self._long_stop = None
            self._long_take = None
            self._short_stop = None
            self._short_take = None

    def ProcessCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        self._finished_candles += 1

        # Manage existing position (SL/TP) BEFORE indicator processing
        closed = False
        if self.Position > 0:
            if self._long_take is not None and float(candle.HighPrice) >= self._long_take:
                self.SellMarket(float(self.Position))
                self._long_stop = None
                self._long_take = None
                closed = True
            elif self._long_stop is not None and float(candle.LowPrice) <= self._long_stop:
                self.SellMarket(float(self.Position))
                self._long_stop = None
                self._long_take = None
                closed = True
        elif self.Position < 0:
            if self._short_take is not None and float(candle.LowPrice) <= self._short_take:
                self.BuyMarket(abs(float(self.Position)))
                self._short_stop = None
                self._short_take = None
                closed = True
            elif self._short_stop is not None and float(candle.HighPrice) >= self._short_stop:
                self.BuyMarket(abs(float(self.Position)))
                self._short_stop = None
                self._short_take = None
                closed = True

        median = (float(candle.HighPrice) + float(candle.LowPrice)) / 2.0

        jaw_input = DecimalIndicatorValue(self._jaw, median, candle.OpenTime)
        jaw_input.IsFinal = True
        jaw_result = self._jaw.Process(jaw_input)
        teeth_input = DecimalIndicatorValue(self._teeth, median, candle.OpenTime)
        teeth_input.IsFinal = True
        teeth_result = self._teeth.Process(teeth_input)
        lips_input = DecimalIndicatorValue(self._lips, median, candle.OpenTime)
        lips_input.IsFinal = True
        lips_result = self._lips.Process(lips_input)

        if not self._jaw.IsFormed or not self._teeth.IsFormed or not self._lips.IsFormed:
            return

        jaw_val = float(jaw_result)
        teeth_val = float(teeth_result)
        lips_val = float(lips_result)

        if self._finished_candles < self._minimum_bars.Value:
            return

        is_uptrend = jaw_val < teeth_val and teeth_val < lips_val
        is_downtrend = jaw_val > teeth_val and teeth_val > lips_val

        if not self._enable_trading.Value:
            return

        vol = float(self._order_volume.Value)
        close = float(candle.ClosePrice)
        pip = self._pip_size

        if is_uptrend:
            if self.Position < 0 and self._use_entry_to_exit.Value:
                self.BuyMarket(abs(float(self.Position)))
                self._short_stop = None
                self._short_take = None
                return
            if self.Position != 0:
                return
            if self._last_signal_time == candle.OpenTime:
                return
            self.BuyMarket(vol)
            self._last_signal_time = candle.OpenTime
        elif is_downtrend:
            if self.Position > 0 and self._use_entry_to_exit.Value:
                self.SellMarket(float(self.Position))
                self._long_stop = None
                self._long_take = None
                return
            if self.Position != 0:
                return
            if self._last_signal_time == candle.OpenTime:
                return
            self.SellMarket(vol)
            self._last_signal_time = candle.OpenTime

    def _calculate_pip_size(self):
        step = 0.0
        if self.Security is not None and self.Security.PriceStep is not None and self.Security.PriceStep > 0:
            step = float(self.Security.PriceStep)
        if step <= 0:
            return 1.0
        decimals = 0
        if self.Security is not None and self.Security.Decimals is not None:
            decimals = int(self.Security.Decimals)
        if decimals == 3 or decimals == 5:
            return step * 10.0
        return step

    def CreateClone(self):
        return sniper_jaw_strategy()
