# Lego EA-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die **Lego EA-Strategie** ist ein direkter Port des MetaTrader-Expertenberaters "Lego EA". Sie verwendet eine konfigurierbare Kombination technischer Filter—Commodity Channel Index, duale gleitende Durchschnitte, stochastischer Oszillator, Accelerator Oscillator, DeMarker und Awesome Oscillator—zur Validierung von Ein- und Ausstiegen. Jeder Filter kann für Einstiege und Ausstiege unabhängig ein- oder ausgeschaltet werden, was es ermöglicht, das Original-"Lego" Block für Block nachzubauen oder mit eigenen Setups zu experimentieren.

## Parameter
- `Volume` – Basishandelsvolumen, das verwendet wird, wenn der vorherige Trade profitabel war.
- `LotMultiplier` – Multiplikator, der nach einem Verlust-Trade auf das zuletzt ausgeführte Volumen angewendet wird (Martingale-ähnliche Erholung).
- `StopLossPips` – Schutz-Stop in Pips (intern anhand der Tick-Größe des Symbols umgerechnet).
- `TakeProfitPips` – Gewinnziel in Pips.
- `UseCciForEntry` / `UseCciForExit` – CCI-Filter beim Öffnen oder Schließen von Positionen aktivieren.
- `UseMaForEntry` / `UseMaForExit` – Schnelle/langsame gleitende Durchschnitt-Kreuzung für Bestätigungen verwenden.
- `UseStochasticForEntry` / `UseStochasticForExit` – Stochastische %K/%D-Ausrichtung innerhalb konfigurierter Schwellenwerte erfordern.
- `UseAcceleratorForEntry` / `UseAcceleratorForExit` – Accelerator Oscillator-Beschleunigungsmuster erfordern.
- `UseDemarkerForEntry` / `UseDemarkerForExit` – DeMarker-Level-Prüfungen anwenden.
- `UseAwesomeForEntry` / `UseAwesomeForExit` – Awesome Oscillator-Momentum-Bestätigung einbeziehen.
- `CciPeriod` – Periode des Commodity Channel Index.
- `MaFastPeriod` / `MaSlowPeriod` – Lookback-Längen für schnelle und langsame gleitende Durchschnitte.
- `MaShift` – Anzahl abgeschlossener Bars, um gleitende Durchschnittswerte zeitlich zurückzuverschieben, reproduziert den MT5-Horizontalverschiebungsparameter.
- `MaMethod` – Glättungsmethode (einfach, exponentiell, geglättet oder gewichtet).
- `MaPrice` – Kerzenkursquelle für beide gleitende Durchschnitte.
- `StochasticKPeriod`, `StochasticDPeriod`, `StochasticSlow` – Konfiguration des stochastischen Oszillators.
- `StochasticLevelUp` / `StochasticLevelDown` – Überkauft/überverkauft-Schwellenwerte für Signale.
- `DemarkerPeriod`, `DemarkerLevelUp`, `DemarkerLevelDown` – DeMarker-Oszillator-Einstellungen.
- `CandleType` – Zeitrahmen der Kerzenserie, die von allen Indikatoren verwendet wird.

## Handels-Workflow
1. Bei jeder abgeschlossenen Kerze sammelt die Strategie Indikatorwerte aus den ausgewählten Filtern.
2. Jeder Filter berechnet die Kauf-/Verkaufsbereitschaft basierend auf dem vorherigen vollständig geformten Bar (entspricht dem `iGetArray(..., 1)`-Offset des ursprünglichen EA).
3. Ein Long-Einstieg ist nur zulässig, wenn **alle aktivierten Einstiegsfilter** ein bullisches Signal bestätigen. Ebenso erfordert ein Short-Einstieg einstimmige bearische Bestätigung.
4. Wenn das Konto flat ist und ein gültiges Eintrittssignal erscheint, wird eine Marktorder mit entweder dem Basis-`Volume` oder dem letzten Verlust-Trade-Volumen multipliziert mit `LotMultiplier` gesendet.
5. Bei bestehender Position werden die aktivierten Exit-Filter auf dieselbe Weise ausgewertet. Die Position wird nur geschlossen, wenn alle Exit-Filter ein entgegengesetztes Signal bestätigen.
6. Stop-Loss- und Take-Profit-Schutz wird automatisch mit `StartProtection` installiert, wobei Pip-Eingaben in absolute Preisabstände basierend auf der Tick-Größe des Symbols umgerechnet werden.

## Geldmanagement
- Nach einem gewinnbringenden Trade kehrt die nächste Order zum Basis-`Volume` zurück.
- Nach einem Verlust-Trade wird das Volumen mit `LotMultiplier` multipliziert, entsprechend der Lot-Eskalationslogik des ursprünglichen EA.
- Exchange-Volumensgrenzen (Schritt, Min. und Max.) werden vor jeder Order durchgesetzt.

## Hinweise und Unterschiede zur MetaTrader-Version
- Indikatorpreisquellen werden StockSharp-Äquivalenten zugeordnet. CCI verwendet intern den typischen Preis und gleitende Durchschnitte verwenden die ausgewählte `MaPrice`-Quelle.
- Alle Indikatorberechnungen basieren auf vollständig geschlossenen Kerzen. Dies vermeidet teilweise geformte Daten und imitiert die "neue Bar"-Verarbeitung des EA.
- Freeze-Level-Prüfungen und manuelle SL/TP-Preisplatzierung werden von StockSharp's `StartProtection`-Dienst behandelt.
- Partielle Positions-Exits aktualisieren den Verlustverfolgungsstatus nur, wenn die gesamte Position flat ist, entsprechend der `DEAL_ENTRY_OUT`-Logik des EA.

## Verwendungshinweise
- Beginnen Sie mit der Originalkonfiguration (MA-Filter aktiviert, andere Filter deaktiviert), um das Basisverhalten zu reproduzieren, und aktivieren Sie dann zusätzliche Filter, um die Signalqualität zu verbessern.
- Überwachen Sie die Kontoexposition bei hohen `LotMultiplier`-Werten; das Risiko wächst schnell während Verlustserien.
- Kombinieren Sie die Strategie mit dem Backtester, um zu überprüfen, ob Ihre gewählte Filterkombination zu den Instrumenten passt, die Sie handeln möchten.

Diese Strategie hat aktuell keine Python-Version.
