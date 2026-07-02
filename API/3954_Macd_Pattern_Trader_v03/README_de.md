# Macd Pattern Trader v03 (StockSharp-Port)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Macd Pattern Trader v03 ist eine hochrangige StockSharp-Strategie, die aus dem MetaTrader 4-Expertenberater *MacdPatternTraderv03* konvertiert wurde. Der ursprüngliche Roboter durchsucht die Hauptlinie MACD nach einer Umkehrformation mit drei Spitzen und wendet Regeln für teilweise Gewinnmitnahmen an, die auf gleitenden Durchschnitten basieren. Dieser C#-Port behält die Musterlogik bei, während er StockSharp-Abonnements, Indikatoren und Bestellhilfen verwendet.

Die Strategie ist für Trend-Erschöpfungs-Setups bei liquiden FX-Paaren konzipiert, kann aber auf jedes Instrument angewendet werden, das eine glatte MACD-Kurve aufweist. Der Standardzeitrahmen beträgt 30-Minuten-Kerzen, passend zum ursprünglichen Berater, und die Standardhandelsgröße ist ein Kontrakt (oder Lotäquivalent in StockSharp-Begriffen).

## Indikatoren und Datenfluss
* **MACD (Schneller EMA 5, Langsamer EMA 13, Signal 1)** – Hauptindikator zur Erkennung der Triple-Top/Triple-Bottom-Struktur. Die Signalleitung wird nicht verwendet; Die Strategie basiert nur auf der Hauptzeile MACD.
* **EMA(7) und EMA(21)** – kurze und mittlere Durchschnittswerte, die während der Positionsverwaltung verwendet werden.
* **SMA(98) und EMA(365)** – langsame Filter, die den Skalierungsauslöser bilden.

Die Implementierung abonniert den konfigurierten Kerzentyp und bindet die Indikatoren über `Bind` / `BindEx`. Es werden nur fertige Kerzen verarbeitet, um zu vermeiden, dass auf unvollständigen Daten reagiert wird.

## Einreisebestimmungen
### Kurze Einrichtung
1. Aktivieren Sie das Setup, wenn die Hauptlinie MACD über den **Oberen Aktivierungspegel** (Standard 0,0030) steigt.
2. Registrieren Sie den ersten Peak, sobald MACD ein lokales Maximum über dem vorherigen und dem vorherigen Wert ausgibt und dann unter den **Oberen Schwellenwert** (Standard 0,0045) fällt.
3. Registrieren Sie den zweiten Peak, wenn MACD über den Schwellenwert zurückkehrt, ein höheres lokales Maximum erreicht und wieder unter den Schwellenwert fällt.
4. Bestätigen Sie das Muster, wenn ein dritter Rollover auftritt, wobei MACD drei aufeinanderfolgende Balken lang unter dem Schwellenwert bleibt und das letzte lokale Maximum niedriger ist als das vorherige.
5. Wenn keine Long-Position vorhanden ist, glätten Sie die verbleibende Long-Position und eröffnen Sie eine Short-Position mit dem konfigurierten Volumen.

### Lange Einrichtung
1. Aktivieren Sie das Setup, wenn die MACD-Hauptlinie unter den **Lower Activation**-Level (Standard −0,0030) fällt.
2. Registrieren Sie den ersten Tiefpunkt, sobald MACD ein lokales Minimum unter den beiden vorherigen Werten ausgibt und dann über den **Unteren Schwellenwert** (Standard −0,0045) steigt.
3. Registrieren Sie den zweiten Tiefpunkt, wenn MACD wieder unter den Schwellenwert fällt, ein unteres Minimum erreicht und wieder über den Schwellenwert steigt.
4. Bestätigen Sie das bullische Muster, wenn ein dritter Aufschwung beobachtet wird, wobei MACD drei Kerzen lang über dem Schwellenwert bleibt und der letzte Tiefpunkt höher als der vorherige ist.
5. Reduzieren Sie das verbleibende Short-Engagement und kaufen Sie das konfigurierte Volumen.

Die Logik spiegelt die verschachtelten Flags `stops`, `stops1` und `aop_ok*` in der ursprünglichen MQ4-Datei wider, einschließlich Zurücksetzungen, wenn MACD über das Aktivierungsband hinausgeht.

## Handelsmanagement
* **Skalierung** – wenn der nicht realisierte Gewinn (berechnet als `(Close − Entry) * Position`) `ProfitThreshold` (Standardpreiseinheiten 5) übersteigt, wendet die Strategie zwei abgestufte Ausstiege an:
  * Stufe 1 (lang): Der Schlusskurs der vorherigen Kerze muss über EMA(21) bleiben. Die Strategie verkauft ein Drittel der anfänglichen Long-Position. Für Leerverkäufe ist die Voraussetzung der vorherige Schlusskurs unter EMA(21) und ein Drittel des anfänglichen Leerverkaufsvolumens wird zurückgekauft.
  * Stufe 2 (lang): Das vorherige Kerzenhoch muss den Durchschnitt von SMA(98) und EMA(365) durchbrechen. Die Hälfte der ursprünglichen Long-Position ist geschlossen. Shorts spiegeln dies wider, wobei der vorherige Tiefstwert unter den gemittelten Filter fällt.
* **Restposition** – was auch immer übrig bleibt, nachdem die Skalierungssequenz von diesem Port nicht verwaltet wird, passend zur Quelle EA.
* **Risikoaufträge** – die MetaTrader-Version platzierte Stop-Loss- und Take-Profit-Aufträge basierend auf rollierenden Hochs und Tiefs. Da StockSharp Schutzanordnungen anders verwaltet, fügt dieser Port Stopps/Ziele nicht automatisch hinzu. Benutzer können die Strategie bei Bedarf mit `StartProtection()` oder einem externen Risikomodul kombinieren.

## Parameter
| Name | Standard | Beschreibung |
| ---- | ------- | ----------- |
| `Volume` | 1 | Bei jedem Eintrag angegebene Handelsgröße. |
| `CandleType` | 30-minütiger Zeitrahmen | Für Indikatorberechnungen verwendete Kerzenserien. |
| `FastEmaLength` / `SlowEmaLength` | 5 / 13 | MACD schnelle und langsame EMA Zeiträume. |
| `UpperThreshold` / `LowerThreshold` | 0,0045 / −0,0045 | Erschöpfungsband, in dem Musterbestätigungen stattfinden. |
| `UpperActivation` / `LowerActivation` | 0,0030 / −0,0030 | Äußeres Band, das die bärischen/bullischen Setups unterstützt. |
| `EmaOneLength` / `EmaTwoLength` | 7 / 21 | Hilfs-EMAs zur Visualisierung und Skalierungslogik. |
| `SmaLength` | 98 | Langsamer Einsatz von SMA zusammen mit EMA(365) während der Ausgänge der zweiten Stufe. |
| `EmaFourLength` | 365 | Langfristige Verwendung von EMA bei Ausstiegen der zweiten Stufe. |
| `ProfitThreshold` | 5 | Mindestens erforderlicher nicht realisierter PnL (Preis * Volumeneinheiten), der vor der Skalierung erforderlich ist. |

## Praktische Hinweise
* Stellen Sie sicher, dass der Broker-Adapter eine teilweise Positionsreduzierung unterstützt. Das Original EA schloss 1/3 und 1/2 Portionen; Dieser Port repliziert dieselben Brüche mithilfe von Marktaufträgen.
* Da Schutzanordnungen nicht automatisch angehängt werden, sollten Sie erwägen, `StartProtection()` zu aktivieren oder benutzerdefinierte Risikoregeln hinzuzufügen, wenn Sie harte Stopps benötigen.
* Die Gewinnschwelle wird in Rohpreis * Volumeneinheiten ausgedrückt. Passen Sie es entsprechend der Pip-Größe oder dem Tick-Wert des Instruments an, um der Annahme „5 Währungseinheiten“ aus dem ursprünglichen MQ4-Code zu entsprechen.
* Die Strategie erwartet eine reibungslose MACD-Dynamik; Übermäßiges Rauschen oder illiquide Instrumente können die Auslösung der Drei-Peak-Logik verhindern.

## Unterschiede zur MQ4-Version
* Verwendet StockSharp-Indikatorbindungen anstelle wiederholter `iMACD`-Aufrufe.
* Die Berechnung des nicht realisierten Gewinns basiert auf `Position` und `PositionAvgPrice`, was bedeutet, dass die Rundungsregeln des Brokers von den `OrderProfit()` von MetaTrader abweichen können.
* Stop-Loss- und Take-Profit-Orders werden nicht automatisch generiert; Bei Bedarf müssen manuelle Risikotools hinzugefügt werden.
* Der MQ4-Parameter `sum_bars_bup` ist nicht vorhanden, da er in der Originalquelle nicht verwendet wurde.
