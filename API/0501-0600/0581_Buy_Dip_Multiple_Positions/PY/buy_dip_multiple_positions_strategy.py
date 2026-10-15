import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal, Math
from StockSharp.Messages import DataType, CandleStates, Sides
from StockSharp.Algo.Strategies import Strategy

DIP_PERCENT = 0.2
VOLUME_RATIO = 1.2
RISK_PERCENT = 2


class buy_dip_multiple_positions_strategy(Strategy):
    """
    Buy Dip Multiple Positions strategy.
    Long only: a candle that closes 0.2% below the previous low, with volume above 120% of the average of the two previous bars and
    a close below PriceSurgePercent percent of the close SurgeLookbackBars bars ago, adds a long (up to MaxPositions entries) while
    the last closed trade was profitable. Each entry risks 2% of the portfolio value against the stop. Every entry resets the
    shared levels: the stop at InitialStopPercent percent of the entry bar low, which then rises by TrailRatePercent percent each
    bar, and the target TargetPricePercent percent above the entry bar low. Touching either level closes the whole position.
    """

    def __init__(self):
        super(buy_dip_multiple_positions_strategy, self).__init__()
        self._max_positions = self.Param("MaxPositions", 20).SetGreaterThanZero().SetDisplay("Max Positions", "Maximum number of stacked entries", "Trading")
        self._trail_rate_percent = self.Param("TrailRatePercent", 1.0).SetNotNegative().SetDisplay("Trail Rate %", "Percent the trailing stop rises each bar", "Risk")
        self._initial_stop_percent = self.Param("InitialStopPercent", 85.0).SetRange(0.0, 100.0).SetDisplay("Initial Stop %", "Initial stop as a percent of the entry bar low", "Risk")
        self._target_price_percent = self.Param("TargetPricePercent", 60.0).SetNotNegative().SetDisplay("Target Price %", "Target distance above the entry bar low", "Risk")
        self._price_surge_percent = self.Param("PriceSurgePercent", 89.0).SetGreaterThanZero().SetDisplay("Price Surge %", "Close must be below this percent of the older close", "Entry")
        self._surge_lookback_bars = self.Param("SurgeLookbackBars", 14).SetGreaterThanZero().SetDisplay("Surge Lookback Bars", "Bars back of the comparison close", "Entry")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(12))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._closes = []
        self._prev_low = None
        self._prev_volume1 = None
        self._prev_volume2 = None
        self._entries = 0
        self._stop_price = None
        self._target_price = None
        self._last_trade_profitable = True
        self._pnl_at_open = Decimal(0)

    def OnReseted(self):
        super(buy_dip_multiple_positions_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(buy_dip_multiple_positions_strategy, self).OnStarted2(time)

        self._reset_state()

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        lookback = self._surge_lookback_bars.Value
        close = candle.ClosePrice
        prev_low = self._prev_low
        prev_volume1 = self._prev_volume1
        prev_volume2 = self._prev_volume2

        old_close = self._closes[len(self._closes) - lookback] if len(self._closes) >= lookback else None

        self._closes.append(close)
        while len(self._closes) > lookback:
            self._closes.pop(0)

        self._prev_low = candle.LowPrice
        self._prev_volume2 = prev_volume1
        self._prev_volume1 = candle.TotalVolume

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self.Position > 0 and self._check_exit(candle):
            return

        if prev_low is None or prev_volume1 is None or prev_volume2 is None or old_close is None:
            return

        hundred = Decimal(100)
        dip = close < prev_low * (Decimal(1) - Decimal(DIP_PERCENT) / hundred)
        high_volume = candle.TotalVolume > Decimal(VOLUME_RATIO) * (prev_volume1 + prev_volume2) / Decimal(2)
        surge = close < old_close * Decimal(self._price_surge_percent.Value) / hundred

        if not dip or not high_volume or not surge or not self._last_trade_profitable or self._entries >= self._max_positions.Value:
            return

        stop = candle.LowPrice * Decimal(self._initial_stop_percent.Value) / hundred
        volume = self._get_entry_volume(close, stop)
        if volume <= 0:
            return

        self.BuyMarket(volume)
        self._entries += 1
        self._stop_price = stop
        self._target_price = candle.LowPrice * (Decimal(1) + Decimal(self._target_price_percent.Value) / hundred)

    def _check_exit(self, candle):
        stop_hit = self._stop_price is not None and candle.LowPrice <= self._stop_price
        target_hit = self._target_price is not None and candle.HighPrice >= self._target_price

        if stop_hit or target_hit:
            self.SellMarket(self.Position)
            self._entries = 0
            self._stop_price = None
            self._target_price = None
            return True

        if self._stop_price is not None:
            self._stop_price = self._stop_price * (Decimal(1) + Decimal(self._trail_rate_percent.Value) / Decimal(100))

        return False

    def _get_entry_volume(self, price, stop):
        risk = price - stop
        portfolio = self.Portfolio
        equity = None
        if portfolio is not None:
            equity = portfolio.CurrentValue if portfolio.CurrentValue is not None else portfolio.BeginValue
        if equity is None or equity <= 0 or risk <= 0:
            return self.Volume

        volume = equity * Decimal(RISK_PERCENT) / Decimal(100) / risk

        security = self.Security
        if security is not None:
            step = security.VolumeStep
            if step is not None and step > 0:
                volume = Math.Floor(volume / step) * step
            max_volume = security.MaxVolume
            if max_volume is not None and max_volume > 0 and volume > max_volume:
                volume = max_volume

        return volume

    def OnOwnTradeReceived(self, trade):
        super(buy_dip_multiple_positions_strategy, self).OnOwnTradeReceived(trade)

        if trade.Order is not None and trade.Order.Side == Sides.Sell and self.Position <= 0:
            self._last_trade_profitable = self.PnL - self._pnl_at_open > 0
            self._pnl_at_open = self.PnL

    def CreateClone(self):
        return buy_dip_multiple_positions_strategy()
