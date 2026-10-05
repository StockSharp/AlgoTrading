import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import BollingerBands, ExponentialMovingAverage, AverageTrueRange, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy


class channels_with_nvi_strategy(Strategy):
    """
    Channels with NVI strategy.
    The channel is a Bollinger band (ChannelType "BB") or a Keltner channel (ChannelType "KC": EMA plus and minus ChannelMultiplier
    ATRs) of ChannelLength. The Negative Volume Index starts at 1000 and moves with the close only on candles whose volume is below the
    previous one. Long only: buys when the close is below the lower channel line and NVI is above EMA(NviEmaLength) of NVI, and closes the
    long when NVI falls below that EMA. Optional percent stop loss and take profit protect the position.
    """

    def __init__(self):
        super(channels_with_nvi_strategy, self).__init__()
        self._channel_type = self.Param("ChannelType", "BB").SetDisplay("Channel Type", "Channel kind: BB for Bollinger Bands, KC for Keltner Channels", "Channel")
        self._channel_length = self.Param("ChannelLength", 20).SetGreaterThanZero().SetDisplay("Channel Length", "Channel period", "Channel")
        self._channel_multiplier = self.Param("ChannelMultiplier", 2.0).SetGreaterThanZero().SetDisplay("Channel Multiplier", "Channel width multiplier", "Channel")
        self._nvi_ema_length = self.Param("NviEmaLength", 200).SetGreaterThanZero().SetDisplay("NVI EMA Length", "EMA period of NVI", "NVI")
        self._enable_stop_loss = self.Param("EnableStopLoss", False).SetDisplay("Enable Stop Loss", "Enable the stop loss", "Risk")
        self._stop_loss_percent = self.Param("StopLossPercent", 0.0).SetNotNegative().SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk")
        self._enable_take_profit = self.Param("EnableTakeProfit", False).SetDisplay("Enable Take Profit", "Enable the take profit", "Risk")
        self._take_profit_percent = self.Param("TakeProfitPercent", 0.0).SetNotNegative().SetDisplay("Take Profit %", "Take profit percentage from entry price", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._nvi_ema = None
        self._nvi = Decimal(1000)
        self._prev_close = None
        self._prev_volume = None

    def OnReseted(self):
        super(channels_with_nvi_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(channels_with_nvi_strategy, self).OnStarted2(time)

        self._reset_state()
        self._nvi_ema = ExponentialMovingAverage()
        self._nvi_ema.Length = self._nvi_ema_length.Value

        length = self._channel_length.Value
        bollinger = BollingerBands()
        bollinger.Length = length
        bollinger.Width = Decimal(self._channel_multiplier.Value)
        ema = ExponentialMovingAverage()
        ema.Length = length
        atr = AverageTrueRange()
        atr.Length = length

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(bollinger, ema, atr, self._process_candle).Start()

        take_percent = Decimal(self._take_profit_percent.Value)
        stop_percent = Decimal(self._stop_loss_percent.Value)
        take_profit = Unit(take_percent, UnitTypes.Percent) if self._enable_take_profit.Value and take_percent > 0 else Unit()
        stop_loss = Unit(stop_percent, UnitTypes.Percent) if self._enable_stop_loss.Value and stop_percent > 0 else Unit()
        self.StartProtection(take_profit, stop_loss, useMarketOrders=True)

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, bollinger)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, bollinger_value, ema_value, atr_value):
        if candle.State != CandleStates.Finished:
            return

        close = candle.ClosePrice
        volume = candle.TotalVolume
        pc = self._prev_close
        pv = self._prev_volume

        if pc is not None and pv is not None and pc != 0 and volume < pv:
            self._nvi += self._nvi * (close - pc) / pc

        self._prev_close = close
        self._prev_volume = volume

        nvi_input = DecimalIndicatorValue(self._nvi_ema, self._nvi, candle.OpenTime)
        nvi_input.IsFinal = True
        nvi_ema_value = self._nvi_ema.Process(nvi_input)

        if not nvi_ema_value.IsFormed:
            return

        if str(self._channel_type.Value).upper() == "KC":
            if not ema_value.IsFormed or not atr_value.IsFormed:
                return
            lower = ema_value.GetValue[Decimal](None) - Decimal(self._channel_multiplier.Value) * atr_value.GetValue[Decimal](None)
        else:
            if not bollinger_value.IsFormed or bollinger_value.LowBand is None:
                return
            lower = bollinger_value.LowBand

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        nvi_ema = nvi_ema_value.GetValue[Decimal](None)

        if self.Position > 0:
            if self._nvi < nvi_ema:
                self.SellMarket(self.Position)
        elif self.Position == 0 and close < lower and self._nvi > nvi_ema:
            self.BuyMarket(self.Volume)

    def CreateClone(self):
        return channels_with_nvi_strategy()
