import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes, OrderStates
from StockSharp.Algo.Indicators import BollingerBands
from StockSharp.Algo.Strategies import Strategy


class enhanced_bollinger_bands_sl_tp_strategy(Strategy):
    """
    Enhanced Bollinger Bands SL TP strategy.
    A close back above the lower band after the previous close was at or below the previous lower band places a buy limit order at
    the close; a close back below the upper band after the previous close was at or above the previous upper band places a sell
    limit order. An unfilled entry order is cancelled on the next candle. Stop loss and take profit are StopLossPips and
    TakeProfitPips times PipValue from the entry.
    """

    def __init__(self):
        super(enhanced_bollinger_bands_sl_tp_strategy, self).__init__()
        self._bollinger_length = self.Param("BollingerLength", 20).SetGreaterThanZero().SetDisplay("Bollinger Length", "Bollinger Bands length", "Indicators")
        self._bollinger_multiplier = self.Param("BollingerMultiplier", 2.0).SetGreaterThanZero().SetDisplay("Bollinger Multiplier", "Bollinger Bands deviation multiplier", "Indicators")
        self._enable_long = self.Param("EnableLong", True).SetDisplay("Enable Long", "Allow long trades", "General")
        self._enable_short = self.Param("EnableShort", True).SetDisplay("Enable Short", "Allow short trades", "General")
        self._pip_value = self.Param("PipValue", 0.0001).SetGreaterThanZero().SetDisplay("Pip Value", "Price of one pip", "Risk")
        self._stop_loss_pips = self.Param("StopLossPips", 10.0).SetNotNegative().SetDisplay("Stop Loss Pips", "Stop loss in pips", "Risk")
        self._take_profit_pips = self.Param("TakeProfitPips", 20.0).SetNotNegative().SetDisplay("Take Profit Pips", "Take profit in pips", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._prev_close = None
        self._prev_upper = None
        self._prev_lower = None
        self._entry_order = None

    def OnReseted(self):
        super(enhanced_bollinger_bands_sl_tp_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(enhanced_bollinger_bands_sl_tp_strategy, self).OnStarted2(time)

        self._reset_state()

        bollinger = BollingerBands()
        bollinger.Length = self._bollinger_length.Value
        bollinger.Width = Decimal(float(self._bollinger_multiplier.Value))

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(bollinger, self._process_candle).Start()

        pip = float(self._pip_value.Value)
        take = float(self._take_profit_pips.Value)
        stop = float(self._stop_loss_pips.Value)
        self.StartProtection(
            Unit(Decimal(take * pip), UnitTypes.Absolute) if take > 0 else Unit(),
            Unit(Decimal(stop * pip), UnitTypes.Absolute) if stop > 0 else Unit(),
            useMarketOrders=True)

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, bollinger)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, bollinger_value):
        if candle.State != CandleStates.Finished:
            return

        if self._entry_order is not None and self._entry_order.State == OrderStates.Active:
            self.CancelOrder(self._entry_order)

        self._entry_order = None

        if not bollinger_value.IsFormed or bollinger_value.UpBand is None or bollinger_value.LowBand is None:
            return

        upper = float(bollinger_value.UpBand)
        lower = float(bollinger_value.LowBand)
        close = float(candle.ClosePrice)

        prev_close = self._prev_close
        prev_upper = self._prev_upper
        prev_lower = self._prev_lower
        self._prev_close = close
        self._prev_upper = upper
        self._prev_lower = lower

        if prev_close is None or prev_upper is None or prev_lower is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self._enable_long.Value and prev_close <= prev_lower and close > lower and self.Position <= 0:
            self._entry_order = self.BuyLimit(candle.ClosePrice, self.Volume + abs(self.Position))
        elif self._enable_short.Value and prev_close >= prev_upper and close < upper and self.Position >= 0:
            self._entry_order = self.SellLimit(candle.ClosePrice, self.Volume + abs(self.Position))

    def CreateClone(self):
        return enhanced_bollinger_bands_sl_tp_strategy()
