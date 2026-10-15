# Tipu MACD EA Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die Strategie ist ein High-Level-StockSharp-Port des **Tipu MACD EA** von MQL4. Es handelt ein einzelnes Symbol mithilfe von MACD-basierten Signalen und spiegelt die ursprünglichen Funktionen des Expert Advisors wider:

* Optionaler Handelsstundenfilter mit zwei konfigurierbaren Zeitfenstern.
* MACD-Nulllinien- und Signalleitungs-Crossover-Einträge mit einstellbaren EMA-Längen und -Verschiebung.
* Automatisches Positionsmanagement einschließlich Take-Profit, Stop-Loss, Trailing Stop und Breakeven.
* Volumenbegrenzung, die die Einstellung „Maximale Anzahl“ aus dem Quellcode emuliert.

Alle Operationen verwenden Marktaufträge. Schutzniveaus werden intern verfolgt und Aufträge werden geschlossen, sobald eine Kerze das Stop-Loss- oder Take-Profit-Niveau durchbricht.

## Handelslogik
1. Abonnieren Sie den konfigurierten Kerzentyp und berechnen Sie einen `MovingAverageConvergenceDivergenceSignal`-Indikator (MACD-Linie + Signallinie).
2. Bewerten Sie MACD-Werte anhand der ausgewählten Verschiebung (`MacdShift` 0 = aktuelle Kerze, 1 = vorherige Kerze) und erstellen Sie Crossover-Signale:
   * **Nulllinienübergang** (optional) – kaufen, wenn MACD über Null kreuzt, verkaufen, wenn es darunter kreuzt.
   * **Signallinienkreuzung** (optional) – kaufen, wenn MACD die Signallinie überschreitet, verkaufen, wenn sie darunter liegt.
3. Bevor Sie eine Position eröffnen, stellen Sie sicher, dass die aktuelle Stunde zu mindestens einem der beiden Zeitfenster gehört, wenn der Filter aktiviert ist.
4. Wenn ein langes Signal erscheint:
   * Wenn die Absicherung deaktiviert ist und ein Short offen ist, schließen Sie ihn optional (`CloseOnReverseSignal`) oder überspringen Sie den neuen Trade.
   * Geben Sie eine Kauf-Market-Order für den kleineren Betrag von `TradeVolume` und dem verbleibenden Volumen auf, bis `MaxPositionVolume` erreicht ist.
   * Aktualisieren Sie den Long-Entry-Snapshot und berechnen Sie schützende Stopp-/Take-Level, falls aktiviert.
5. Wenn ein Short-Signal erscheint, befolgen Sie die symmetrische Logik für Verkaufsaufträge.
6. Während eine Position aktiv ist:
   * Überwachen Sie Stopps und Ziele für jede fertige Kerze und schließen Sie den Handel, wenn eines der beiden Niveaus durchbrochen wird.
   * Wenn das Trailing aktiviert ist und der Preis um `TrailingPips + TrailingCushionPips` steigt, verschieben Sie den Stopp, um einen Abstand von `TrailingPips` zum Preis beizubehalten.
   * Wenn das Breakeven-Modul aktiv ist und der Gewinn `RiskFreePips` übersteigt, verschieben Sie den Stop auf den Einstiegspreis.

## Parameter
| Name | Beschreibung |
| --- | --- |
| `CandleType` | Kerzenreihe, die für MACD-Berechnungen verwendet wird. |
| `TradeVolume` | Volumen jedes Markteintritts (Lots). |
| `MaxPositionVolume` | Maximal zulässige kumulative lange oder kurze Exposition. |
| `UseTimeFilter` | Aktiviert den Dual-Window-Handelsstundenfilter. |
| `Zone1StartHour`, `Zone1EndHour` | Start-/Endzeiten für das erste Handelsfenster (einschließlich Börsenzeit). |
| `Zone2StartHour`, `Zone2EndHour` | Start-/Endzeiten für das zweite Handelsfenster. |
| `FastPeriod`, `SlowPeriod`, `SignalPeriod` | MACD schnelle EMA, langsame EMA und Signallängen von SMA. |
| `MacdShift` | 0 = den aktuellen Balken auswerten, 1 = den vorherigen Balken auswerten (entsprechend dem MQL `iShift`). |
| `UseZeroCross` | Ermöglicht MACD Nulllinien-Kreuzeingaben. |
| `UseSignalCross` | Ermöglicht MACD vs. Signalleitungskreuzeinträge. |
| `AllowHedging` | Ermöglicht den Aufbau sowohl langer als auch kurzer Belichtungen, ohne zuerst die gegenüberliegende Seite zu schließen. |
| `CloseOnReverseSignal` | Schließt die Gegenposition, wenn ein neues Signal erscheint (wird verwendet, wenn die Absicherung deaktiviert ist). |
| `UseTakeProfit`, `TakeProfitPips` | Aktiviert und konfiguriert die Take-Profit-Distanz (Pips). |
| `UseStopLoss`, `StopLossPips` | Aktiviert und konfiguriert den Stop-Loss-Abstand (Pips). |
| `UseTrailingStop`, `TrailingPips`, `TrailingCushionPips` | Ermöglicht die Nachlaufverwaltung, legt den Nachlaufabstand und das Polster (Pips) fest. |
| `UseRiskFree`, `RiskFreePips` | Verschiebt den Stop auf die Gewinnschwelle, sobald der Gewinn die angegebenen Pips überschreitet. |

## Nutzungshinweise
* Konfigurieren Sie den Kerzentyp so, dass er dem in MetaTrader verwendeten Zeitrahmen entspricht (standardmäßige 15-Minuten-Balken).
* Die Pip-Größe wird von `Security.PriceStep` abgeleitet. Fehlen dem Instrument diese Metadaten, wird der Standardwert 0,0001 verwendet.
* Die Strategie geht von der sofortigen Ausführung von Marktaufträgen aus. Stellen Sie beim Betrieb unter Spannung ggf. eine ordnungsgemäße Handhabung des Schlupfs sicher.
* Wenn sowohl Nulllinien- als auch Signallinieneinträge deaktiviert sind, bleibt die Strategie im Leerlauf.
