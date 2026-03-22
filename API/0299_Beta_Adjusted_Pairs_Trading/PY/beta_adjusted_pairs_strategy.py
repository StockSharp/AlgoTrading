import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Math, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import SimpleMovingAverage, StandardDeviation, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy
from StockSharp.BusinessEntities import Security


class beta_adjusted_pairs_strategy(Strategy):
    """
    Mean-reversion strategy that trades the primary instrument when a beta-adjusted
    spread versus the secondary instrument becomes stretched.
    """

    def __init__(self):
        super(beta_adjusted_pairs_strategy, self).__init__()

        self._security2_id = self.Param("Security2Id", "TONUSDT@BNBFT") \
            .SetDisplay("Second Security Id", "Identifier of the secondary security", "General")

        self._beta_asset1 = self.Param("BetaAsset1", 1.0) \
            .SetDisplay("Primary Beta", "Beta coefficient of the primary security", "Spread")

        self._beta_asset2 = self.Param("BetaAsset2", 1.0) \
            .SetDisplay("Secondary Beta", "Beta coefficient of the secondary security", "Spread")

        self._lookback_period = self.Param("LookbackPeriod", 30) \
            .SetDisplay("Lookback Period", "Lookback period for spread statistics", "Indicators")

        self._entry_threshold = self.Param("EntryThreshold", 1.1) \
            .SetDisplay("Entry Threshold", "Entry threshold in spread standard deviations", "Signals")

        self._exit_threshold = self.Param("ExitThreshold", 0.15) \
            .SetDisplay("Exit Threshold", "Exit threshold in spread standard deviations", "Signals")

        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage", "Risk")

        self._cooldown_bars = self.Param("CooldownBars", 120) \
            .SetDisplay("Cooldown Bars", "Bars to wait between orders", "Risk")

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Candle series for both instruments", "General")

        self._security2 = None
        self._spread_average = None
        self._spread_std_dev = None
        self._latest_price1 = Decimal(0)
        self._latest_price2 = Decimal(0)
        self._entry_spread = Decimal(0)
        self._primary_updated = False
        self._secondary_updated = False
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(beta_adjusted_pairs_strategy, self).OnReseted()
        self._security2 = None
        self._spread_average = None
        self._spread_std_dev = None
        self._latest_price1 = Decimal(0)
        self._latest_price2 = Decimal(0)
        self._entry_spread = Decimal(0)
        self._primary_updated = False
        self._secondary_updated = False
        self._cooldown = 0

    def OnStarted(self, time):
        super(beta_adjusted_pairs_strategy, self).OnStarted(time)

        sec2_id = str(self._security2_id.Value)
        if not sec2_id:
            raise Exception("Secondary security identifier is not specified.")

        s = Security()
        s.Id = sec2_id
        self._security2 = s

        self._spread_average = SimpleMovingAverage()
        self._spread_average.Length = int(self._lookback_period.Value)
        self._spread_std_dev = StandardDeviation()
        self._spread_std_dev.Length = int(self._lookback_period.Value)
        self._cooldown = 0

        primary_subscription = self.SubscribeCandles(self.candle_type, False, self.Security)
        secondary_subscription = self.SubscribeCandles(self.candle_type, False, self._security2)

        primary_subscription.Bind(self._process_primary_candle).Start()
        secondary_subscription.Bind(self._process_secondary_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, primary_subscription)
            self.DrawCandles(area, secondary_subscription)
            self.DrawOwnTrades(area)

        self.StartProtection(Unit(0, UnitTypes.Absolute), Unit(self._stop_loss_percent.Value, UnitTypes.Percent), False)

    def _process_primary_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        self._latest_price1 = candle.ClosePrice
        self._primary_updated = True
        self._try_process_spread(candle.OpenTime)

    def _process_secondary_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        self._latest_price2 = candle.ClosePrice
        self._secondary_updated = True
        self._try_process_spread(candle.OpenTime)

    def _try_process_spread(self, time):
        if not self._primary_updated or not self._secondary_updated:
            return

        self._primary_updated = False
        self._secondary_updated = False

        try:
            time = time.UtcDateTime
        except:
            pass

        beta1 = float(self._beta_asset1.Value)
        beta2 = float(self._beta_asset2.Value)

        if float(self._latest_price1) <= 0 or float(self._latest_price2) <= 0 or beta1 <= 0 or beta2 <= 0:
            return

        spread = float(self._latest_price1) / beta1 - float(self._latest_price2) / beta2

        avg_input = DecimalIndicatorValue(self._spread_average, Decimal(spread), time)
        avg_input.IsFinal = True
        avg_result = self._spread_average.Process(avg_input)
        average_spread = float(avg_result)

        std_input = DecimalIndicatorValue(self._spread_std_dev, Decimal(spread), time)
        std_input.IsFinal = True
        std_result = self._spread_std_dev.Process(std_input)
        spread_std_dev = float(std_result)

        if not self._spread_average.IsFormed or not self._spread_std_dev.IsFormed:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            return

        if spread_std_dev <= 0:
            return

        z_score = (spread - average_spread) / spread_std_dev
        entry_thresh = float(self._entry_threshold.Value)
        exit_thresh = float(self._exit_threshold.Value)
        cd = int(self._cooldown_bars.Value)

        if self.Position == 0:
            if z_score <= -entry_thresh:
                self._entry_spread = spread
                self.BuyMarket()
                self._cooldown = cd
            elif z_score >= entry_thresh:
                self._entry_spread = spread
                self.SellMarket()
                self._cooldown = cd
            return

        stop_pct = float(self._stop_loss_percent.Value)
        price_step = float(self.Security.PriceStep) if self.Security.PriceStep is not None else 1.0
        stop_distance = max(abs(self._entry_spread) * stop_pct / 100.0, price_step)

        if self.Position > 0:
            stop_triggered = spread <= self._entry_spread - stop_distance
        else:
            stop_triggered = spread >= self._entry_spread + stop_distance

        if self.Position > 0 and (z_score >= -exit_thresh or stop_triggered):
            self.SellMarket(Math.Abs(self.Position))
            self._cooldown = cd
        elif self.Position < 0 and (z_score <= exit_thresh or stop_triggered):
            self.BuyMarket(Math.Abs(self.Position))
            self._cooldown = cd

    def CreateClone(self):
        return beta_adjusted_pairs_strategy()
