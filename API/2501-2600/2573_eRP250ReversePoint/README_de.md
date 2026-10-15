# eRP250ReversePoint-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie ist eine StockSharp-Portierung des MetaTrader 5-Expertenberaters `e_RP_250`. Das ursprüngliche System handelt Umkehrungen, die von einem benutzerdefinierten *rPoint*-Indikator erkannt werden. Da dieser Indikator in StockSharp nicht verfügbar ist, recreiert die Konvertierung dasselbe Verhalten mit rollierenden Höchst- und Tiefstkurs-Trackern. Wenn ein neues Swing-Hoch oder Swing-Tief erscheint, kehrt die Strategie die Position um und fügt dieselbe Stop-Loss-, Take-Profit- und optionale Trailing-Logik wie die MQL-Version hinzu.

Der ursprüngliche Quellcode hat keine verifizierten Performance-Ergebnisse veröffentlicht, daher sollten Sie vor dem Einsatz der Strategie im Produktionsbetrieb Ihre eigene Bewertung durchführen.

## Handelslogik

- Abonnierung von Kerzen, die durch den Parameter `CandleType` definiert werden (standardmäßig 5-Minuten-Kerzen).
- Verfolgung des höchsten Hochs und niedrigsten Tiefs über die letzten `ReversePoint` Bars (standardmäßig 250).
- Wenn die aktuelle Kerze ein neues höchstes Hoch setzt, Long-Position schließen und Short-Position eröffnen.
- Wenn die aktuelle Kerze ein neues niedrigstes Tief setzt, Short-Position schließen und Long-Position eröffnen.
- Schützende Stop-Loss- und Take-Profit-Level werden in Preispunkten ausgedrückt und durch `StartProtection` reproduziert.
- Optionale Trailing-Stops sichern Gewinne, sobald der Preis die konfigurierte Anzahl an Punkten bewegt.

Zu jedem Zeitpunkt ist nur eine Position aktiv. Die Strategie blockiert auch doppelte Orders während derselben Kerze, indem sie sich die letzte Ausführungszeit merkt und damit die `TimeN`-Schutzmaßnahme aus dem MQL-Skript repliziert.

## Parameter

| Parameter | Beschreibung |
|-----------|--------------|
| `TakeProfitPoints` | Abstand in Preispunkten für die Take-Profit-Order (Standard **15**). Auf null setzen, um die automatische Gewinnmitnahme zu deaktivieren. |
| `StopLossPoints` | Abstand in Preispunkten für die Stop-Loss-Order (Standard **999**). Auf null setzen, um ohne festen Stop zu handeln. |
| `TrailingStopPoints` | Optionaler Trailing-Stop-Abstand in Preispunkten (Standard **0** deaktiviert die Trailing-Logik). |
| `ReversePoint` | Anzahl der Kerzen zur Erkennung von Umkehrpunkten. Größere Werte reagieren langsamer, filtern aber Rauschen heraus. |
| `CandleType` | Zu analysierende Kerzen-Aggregation. Standard ist ein 5-Minuten-Zeitrahmen, kann aber auf jeden `DataType` geändert werden. |

## Positionsverwaltung

- `StartProtection` wendet dieselben Stop-Loss- und Take-Profit-Abstände wie der MT5-Experte an.
- Der Trailing-Stop verfolgt den günstigsten Preis nach dem Einstieg und schließt die Position, wenn der Preis um den konfigurierten Betrag zurückkehrt.
- Umkehrsignale von der entgegengesetzten Seite schließen sofort die aktuelle Position, bevor eine neue eröffnet wird.

## Verwendungshinweise

- Stellen Sie sicher, dass die Datenquelle den ausgewählten Kerzentyp unterstützt, andernfalls werden keine Signale generiert.
- Die Strategie ist auf Dezimalpreise angewiesen. Überprüfen Sie, dass die `PriceStep`-Eigenschaft des Wertpapiers den Punktwert korrekt widerspiegelt.
- Testen Sie verschiedene `ReversePoint`-Werte, um die Ausbruchsempfindlichkeit an die Volatilität des gehandelten Instruments anzupassen.
