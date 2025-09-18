namespace StockSharp.Samples.Strategies;

using System;
using System.Drawing;
using System.Text;
using System.Threading;

using StockSharp.Algo.Strategies;

/// <summary>
/// Portfolio risk helper converted from the MQL5 script "RiskManager_with_InfoPanel_and_Support".
/// Produces periodic account snapshots and calculates recommended order volume based on the configured risk.
/// </summary>
public class RiskManagerWithInfoPanelAndSupportStrategy : Strategy
{
private readonly StrategyParam<decimal> _riskPercent;
private readonly StrategyParam<decimal> _entryPrice;
private readonly StrategyParam<decimal> _stopLossPercent;
private readonly StrategyParam<decimal> _takeProfitPercent;
private readonly StrategyParam<decimal> _maxDailyRiskPercent;

private readonly StrategyParam<int> _updateIntervalSeconds;
private readonly StrategyParam<int> _infoPanelXDistance;
private readonly StrategyParam<int> _infoPanelYDistance;
private readonly StrategyParam<int> _infoPanelWidth;
private readonly StrategyParam<int> _infoPanelHeight;
private readonly StrategyParam<int> _infoPanelFontSize;
private readonly StrategyParam<string> _infoPanelFontName;
private readonly StrategyParam<Color> _infoPanelFontColor;
private readonly StrategyParam<Color> _infoPanelBackColor;

private readonly StrategyParam<bool> _useSupportPanel;
private readonly StrategyParam<string> _supportPanelText;
private readonly StrategyParam<Color> _supportPanelFontColor;
private readonly StrategyParam<int> _supportPanelFontSize;
private readonly StrategyParam<string> _supportPanelFontName;
private readonly StrategyParam<int> _supportPanelXDistance;
private readonly StrategyParam<int> _supportPanelYDistance;
private readonly StrategyParam<int> _supportPanelXSize;
private readonly StrategyParam<int> _supportPanelYSize;

private Timer? _timer;
private int _isProcessing;
private DateTime _dailyBaseDate;
private decimal _dailyPnLBase;

/// <summary>
/// Initializes a new instance of the <see cref="RiskManagerWithInfoPanelAndSupportStrategy"/> class.
/// </summary>
public RiskManagerWithInfoPanelAndSupportStrategy()
{
_riskPercent = Param(nameof(RiskPercent), 1m)
.SetRange(0m, 100m)
.SetDisplay("Risk Percent", "Risk per trade expressed as account percentage.", "Risk");

_entryPrice = Param(nameof(EntryPrice), 1.1000m)
.SetGreaterThanZero()
.SetDisplay("Entry Price", "Reference entry price used for the risk estimate.", "Risk");

_stopLossPercent = Param(nameof(StopLossPercent), 0.2m)
.SetRange(0m, 100m)
.SetDisplay("Stop Loss %", "Stop loss distance as percentage of the entry price.", "Risk");

_takeProfitPercent = Param(nameof(TakeProfitPercent), 0.5m)
.SetRange(0m, 100m)
.SetDisplay("Take Profit %", "Take profit distance as percentage of the entry price.", "Risk");

_maxDailyRiskPercent = Param(nameof(MaxDailyRiskPercent), 2m)
.SetRange(0m, 100m)
.SetDisplay("Max Daily Risk %", "Daily loss threshold that suspends trading when exceeded.", "Risk");

_updateIntervalSeconds = Param(nameof(UpdateIntervalSeconds), 10)
.SetGreaterThanZero()
.SetDisplay("Update Interval (sec)", "How often the snapshot is logged.", "Display");

_infoPanelXDistance = Param(nameof(InfoPanelXDistance), 10)
.SetDisplay("Panel X", "Virtual horizontal offset of the info panel.", "Display");

_infoPanelYDistance = Param(nameof(InfoPanelYDistance), 10)
.SetDisplay("Panel Y", "Virtual vertical offset of the info panel.", "Display");

_infoPanelWidth = Param(nameof(InfoPanelWidth), 350)
.SetDisplay("Panel Width", "Width of the info panel in pixels.", "Display");

_infoPanelHeight = Param(nameof(InfoPanelHeight), 300)
.SetDisplay("Panel Height", "Height of the info panel in pixels.", "Display");

_infoPanelFontSize = Param(nameof(InfoPanelFontSize), 12)
.SetDisplay("Panel Font Size", "Font size used in the info panel.", "Display");

_infoPanelFontName = Param(nameof(InfoPanelFontName), "Arial")
.SetDisplay("Panel Font", "Font family used in the info panel.", "Display");

_infoPanelFontColor = Param(nameof(InfoPanelFontColor), Color.White)
.SetDisplay("Panel Font Color", "Font color of the info panel.", "Display");

_infoPanelBackColor = Param(nameof(InfoPanelBackColor), Color.DarkGray)
.SetDisplay("Panel Background", "Background color of the info panel.", "Display");

_useSupportPanel = Param(nameof(UseSupportPanel), true)
.SetDisplay("Show Support", "Show the optional support message panel.", "Support");

_supportPanelText = Param(nameof(SupportPanelText), "Need trading support? Contact us!")
.SetDisplay("Support Text", "Text displayed on the support panel.", "Support");

_supportPanelFontColor = Param(nameof(SupportPanelFontColor), Color.Red)
.SetDisplay("Support Font Color", "Font color of the support panel.", "Support");

_supportPanelFontSize = Param(nameof(SupportPanelFontSize), 10)
.SetDisplay("Support Font Size", "Font size of the support panel.", "Support");

_supportPanelFontName = Param(nameof(SupportPanelFontName), "Arial")
.SetDisplay("Support Font", "Font family of the support panel.", "Support");

_supportPanelXDistance = Param(nameof(SupportPanelXDistance), 10)
.SetDisplay("Support X", "Horizontal offset of the support panel.", "Support");

_supportPanelYDistance = Param(nameof(SupportPanelYDistance), 320)
.SetDisplay("Support Y", "Vertical offset of the support panel.", "Support");

_supportPanelXSize = Param(nameof(SupportPanelXSize), 250)
.SetDisplay("Support Width", "Width of the support panel.", "Support");

_supportPanelYSize = Param(nameof(SupportPanelYSize), 30)
.SetDisplay("Support Height", "Height of the support panel.", "Support");
}

/// <summary>
/// Gets or sets the risk percentage per trade.
/// </summary>
public decimal RiskPercent
{
get => _riskPercent.Value;
set => _riskPercent.Value = value;
}

/// <summary>
/// Gets or sets the notional entry price used for calculations.
/// </summary>
public decimal EntryPrice
{
get => _entryPrice.Value;
set => _entryPrice.Value = value;
}

/// <summary>
/// Gets or sets the stop loss distance in percent.
/// </summary>
public decimal StopLossPercent
{
get => _stopLossPercent.Value;
set => _stopLossPercent.Value = value;
}

/// <summary>
/// Gets or sets the take profit distance in percent.
/// </summary>
public decimal TakeProfitPercent
{
get => _takeProfitPercent.Value;
set => _takeProfitPercent.Value = value;
}

/// <summary>
/// Gets or sets the daily loss threshold in percent.
/// </summary>
public decimal MaxDailyRiskPercent
{
get => _maxDailyRiskPercent.Value;
set => _maxDailyRiskPercent.Value = value;
}

/// <summary>
/// Gets or sets how often the snapshot is produced.
/// </summary>
public int UpdateIntervalSeconds
{
get => _updateIntervalSeconds.Value;
set => _updateIntervalSeconds.Value = value;
}

/// <summary>
/// Gets or sets the horizontal offset of the info panel.
/// </summary>
public int InfoPanelXDistance
{
get => _infoPanelXDistance.Value;
set => _infoPanelXDistance.Value = value;
}

/// <summary>
/// Gets or sets the vertical offset of the info panel.
/// </summary>
public int InfoPanelYDistance
{
get => _infoPanelYDistance.Value;
set => _infoPanelYDistance.Value = value;
}

/// <summary>
/// Gets or sets the width of the info panel.
/// </summary>
public int InfoPanelWidth
{
get => _infoPanelWidth.Value;
set => _infoPanelWidth.Value = value;
}

/// <summary>
/// Gets or sets the height of the info panel.
/// </summary>
public int InfoPanelHeight
{
get => _infoPanelHeight.Value;
set => _infoPanelHeight.Value = value;
}

/// <summary>
/// Gets or sets the font size of the info panel.
/// </summary>
public int InfoPanelFontSize
{
get => _infoPanelFontSize.Value;
set => _infoPanelFontSize.Value = value;
}

/// <summary>
/// Gets or sets the font name of the info panel.
/// </summary>
public string InfoPanelFontName
{
get => _infoPanelFontName.Value;
set => _infoPanelFontName.Value = value;
}

/// <summary>
/// Gets or sets the font color of the info panel.
/// </summary>
public Color InfoPanelFontColor
{
get => _infoPanelFontColor.Value;
set => _infoPanelFontColor.Value = value;
}

/// <summary>
/// Gets or sets the background color of the info panel.
/// </summary>
public Color InfoPanelBackColor
{
get => _infoPanelBackColor.Value;
set => _infoPanelBackColor.Value = value;
}

/// <summary>
/// Gets or sets whether the support panel is visible.
/// </summary>
public bool UseSupportPanel
{
get => _useSupportPanel.Value;
set => _useSupportPanel.Value = value;
}

/// <summary>
/// Gets or sets the text displayed in the support panel.
/// </summary>
public string SupportPanelText
{
get => _supportPanelText.Value;
set => _supportPanelText.Value = value;
}

/// <summary>
/// Gets or sets the font color of the support panel.
/// </summary>
public Color SupportPanelFontColor
{
get => _supportPanelFontColor.Value;
set => _supportPanelFontColor.Value = value;
}

/// <summary>
/// Gets or sets the font size of the support panel.
/// </summary>
public int SupportPanelFontSize
{
get => _supportPanelFontSize.Value;
set => _supportPanelFontSize.Value = value;
}

/// <summary>
/// Gets or sets the font family of the support panel.
/// </summary>
public string SupportPanelFontName
{
get => _supportPanelFontName.Value;
set => _supportPanelFontName.Value = value;
}

/// <summary>
/// Gets or sets the horizontal offset of the support panel.
/// </summary>
public int SupportPanelXDistance
{
get => _supportPanelXDistance.Value;
set => _supportPanelXDistance.Value = value;
}

/// <summary>
/// Gets or sets the vertical offset of the support panel.
/// </summary>
public int SupportPanelYDistance
{
get => _supportPanelYDistance.Value;
set => _supportPanelYDistance.Value = value;
}

/// <summary>
/// Gets or sets the width of the support panel.
/// </summary>
public int SupportPanelXSize
{
get => _supportPanelXSize.Value;
set => _supportPanelXSize.Value = value;
}

/// <summary>
/// Gets or sets the height of the support panel.
/// </summary>
public int SupportPanelYSize
{
get => _supportPanelYSize.Value;
set => _supportPanelYSize.Value = value;
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

if (Portfolio == null)
throw new InvalidOperationException("Portfolio must be assigned before running the risk manager.");

if (UpdateIntervalSeconds <= 0)
throw new InvalidOperationException("Update interval must be a positive number of seconds.");

_dailyBaseDate = time.Date;
_dailyPnLBase = PnL;

var interval = TimeSpan.FromSeconds(UpdateIntervalSeconds);
_timer = new Timer(_ => ProcessSnapshot(), null, TimeSpan.Zero, interval);
}

/// <inheritdoc />
protected override void OnStopped()
{
_timer?.Dispose();
_timer = null;

base.OnStopped();
}

/// <inheritdoc />
protected override void OnReseted()
{
_timer?.Dispose();
_timer = null;

_dailyBaseDate = default;
_dailyPnLBase = 0m;

base.OnReseted();
}

/// <inheritdoc />
protected override void OnPnLChanged(decimal diff)
{
base.OnPnLChanged(diff);

UpdateDailyBase(CurrentTime);
}

private void ProcessSnapshot()
{
if (Interlocked.Exchange(ref _isProcessing, 1) == 1)
return;

try
{
var portfolio = Portfolio;
if (portfolio == null)
return;

var security = Security;
var entryPrice = EntryPrice;
var computedStop = entryPrice - (entryPrice * StopLossPercent / 100m);
var computedTake = entryPrice + (entryPrice * TakeProfitPercent / 100m);
var priceStep = security?.PriceStep ?? 0m;
if (priceStep <= 0m)
priceStep = 1m;

var stepPrice = security?.StepPrice ?? 0m;
if (stepPrice <= 0m)
stepPrice = priceStep;

var riskAmount = CalculateRiskAmount();
var recommendedVolume = CalculateRecommendedVolume(entryPrice, computedStop, riskAmount, priceStep, stepPrice);

var tickDistance = priceStep > 0m ? Math.Abs(entryPrice - computedStop) / priceStep : 0m;
var rewardDistance = priceStep > 0m ? Math.Abs(computedTake - entryPrice) / priceStep : 0m;
var rewardRisk = tickDistance > 0m ? rewardDistance / tickDistance : (decimal?)null;

UpdateDailyBase(CurrentTime);
var dailyProfit = PnL - _dailyPnLBase;
var equity = portfolio.CurrentValue ?? portfolio.BeginValue ?? 0m;
var balance = portfolio.BeginValue ?? 0m;
var unrealized = portfolio.CurrentValue.HasValue && portfolio.BeginValue.HasValue
? portfolio.CurrentValue.Value - portfolio.BeginValue.Value
: PnL;
var dailyRiskLimit = equity > 0m ? equity * MaxDailyRiskPercent / 100m : 0m;

var builder = new StringBuilder();
builder.AppendLine($"Risk Manager for {security?.Id ?? "N/A"}");
builder.AppendLine("--------------------------------------------");
builder.AppendLine($"Account: {portfolio?.Name ?? portfolio?.ToString() ?? "N/A"}");
builder.AppendLine($"Balance: {balance:0.##}");
builder.AppendLine($"Equity: {equity:0.##}");
builder.AppendLine($"Unrealized PnL: {unrealized:0.##}");
builder.AppendLine($"Realized PnL: {PnL:0.##}");
builder.AppendLine($"Server Time: {CurrentTime:yyyy-MM-dd HH:mm:ss}");
builder.AppendLine();
builder.AppendLine($"Risk/Trade: {RiskPercent:0.##}%");
builder.AppendLine($"Entry Price: {entryPrice:0.#####}");
builder.AppendLine($"Stop Loss: {computedStop:0.#####} ({StopLossPercent:0.##}%)");
builder.AppendLine($"Take Profit: {computedTake:0.#####} ({TakeProfitPercent:0.##}%)");
builder.AppendLine($"Distance (ticks): {tickDistance:0.##}");
builder.AppendLine($"Risk Amount: {riskAmount:0.##}");
builder.AppendLine($"Recommended Volume: {recommendedVolume:0.####}");

if (rewardRisk.HasValue)
builder.AppendLine($"Reward:Risk Ratio: {rewardRisk.Value:0.##}");

builder.AppendLine();
builder.AppendLine($"Daily P/L: {dailyProfit:0.##}");
builder.AppendLine($"Daily Risk Limit: {dailyRiskLimit:0.##}");

if (dailyRiskLimit > 0m && dailyProfit < -dailyRiskLimit)
builder.AppendLine("*** DAILY RISK LIMIT EXCEEDED! Trading suspended. ***");

builder.AppendLine();
builder.AppendLine($"Info Panel Placement: X={InfoPanelXDistance}, Y={InfoPanelYDistance}, Size={InfoPanelWidth}x{InfoPanelHeight}, Font={InfoPanelFontName} {InfoPanelFontSize}, Colors={InfoPanelFontColor.Name}/{InfoPanelBackColor.Name}");

if (UseSupportPanel)
{
builder.AppendLine($"Support Panel: '{SupportPanelText}' at X={SupportPanelXDistance}, Y={SupportPanelYDistance}, Size={SupportPanelXSize}x{SupportPanelYSize}, Font={SupportPanelFontName} {SupportPanelFontSize}, Color={SupportPanelFontColor.Name}");
builder.AppendLine("Support Link: https://partner.bybit.com/b/forexmt5");
}

LogInfo(builder.ToString().TrimEnd());
}
catch (Exception error)
{
LogError($"Failed to process risk snapshot: {error.Message}");
}
finally
{
Interlocked.Exchange(ref _isProcessing, 0);
}
}

private decimal CalculateRiskAmount()
{
var portfolioValue = Portfolio?.CurrentValue ?? Portfolio?.BeginValue ?? 0m;
return portfolioValue > 0m ? portfolioValue * RiskPercent / 100m : 0m;
}

private decimal CalculateRecommendedVolume(decimal entryPrice, decimal stopPrice, decimal riskAmount, decimal priceStep, decimal stepPrice)
{
if (riskAmount <= 0m || entryPrice <= 0m || stopPrice <= 0m || priceStep <= 0m || stepPrice <= 0m)
return 0m;

var stopDistance = Math.Abs(entryPrice - stopPrice);
if (stopDistance <= 0m)
return 0m;

var stepsCount = stopDistance / priceStep;
if (stepsCount <= 0m)
return 0m;

var riskPerVolume = stepsCount * stepPrice;
if (riskPerVolume <= 0m)
return 0m;

var rawVolume = riskAmount / riskPerVolume;
return NormalizeVolume(rawVolume);
}

private decimal NormalizeVolume(decimal volume)
{
if (volume <= 0m)
return 0m;

var step = Security?.VolumeStep;
if (step.HasValue && step.Value > 0m)
volume = Math.Ceiling(volume / step.Value) * step.Value;

return volume;
}

private void UpdateDailyBase(DateTimeOffset time)
{
if (time == default)
return;

if (time.Date == _dailyBaseDate)
return;

_dailyBaseDate = time.Date;
_dailyPnLBase = PnL;
}
}
