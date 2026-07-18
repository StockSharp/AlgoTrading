# RSI EA Crossover-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die RSI EA-Strategie repliziert den MetaTrader 5 "RSI EA" Expert Advisor. Sie überwacht den Relative Strength Index (RSI) auf der ausgewählten Kerzenserie und reagiert, wenn der Momentum konfigurierbare überverkaufte oder überkaufte Niveaus kreuzt. Die Konvertierung behält die Stop-Loss-, Take-Profit-, Trailing-Stop- und automatischen Geldverwaltungsideen des Originalsystems bei, während sie diese an die StockSharp High-Level-Strategie-API anpasst.

## Strategielogik

### Indikatoren
- **RSI** mit einem konfigurierbaren Zeitraum, angewendet auf den gewählten Kerzentyp.

### Einstiegskriterien
- **Long**: der RSI kreuzt **aufwärts** `RsiBuyLevel` (vorheriger Wert unter dem Schwellenwert, aktueller Wert über dem Schwellenwert) und Long-Handel ist aktiviert.
- **Short**: der RSI kreuzt **abwärts** `RsiSellLevel` (vorheriger Wert über dem Schwellenwert, aktueller Wert unter dem Schwellenwert) und Short-Handel ist aktiviert.

Es wird nur eine Nettoposition gehalten. Wenn die Strategie bereits im Markt ist, werden keine zusätzlichen Absicherungspositionen eröffnet.

### Ausstiegskriterien
- **Signalbasierter Ausstieg**: wenn `CloseBySignal` aktiviert ist, schließt der entgegengesetzte RSI-Crossover sofort die aktive Position.
- **Schützender Stop**: wenn `StopLoss` größer als null ist, überwacht die Strategie die Preisdistanz vom durchschnittlichen Einstandspreis und tritt aus, sobald der Verlust den angegebenen Betrag erreicht.
- **Take-Profit**: wenn `TakeProfit` größer als null ist, wird die Position geschlossen, sobald die Zieldistanz erreicht ist.
- **Trailing Stop**: wenn `TrailingStop` größer als null ist, folgt das Stop-Niveau dem Preis. Bei Long-Positionen wird der Stop auf `Close - TrailingStop` angehoben, sobald der Preis mindestens `TrailingStop` vom aktuellen Stop voranzieht; Shorts verhalten sich symmetrisch.

### Positionsgrößenbestimmung
- Wenn `UseAutoVolume` `true` ist, wird das Volumen aus dem Kontokapital und Risiko berechnet: `Volume = Equity * RiskPercent / (100 * stopDistance)`, wobei `stopDistance` `StopLoss` verwendet, wenn verfügbar, andernfalls `TrailingStop`. Wenn keine Schutzdistanz festgelegt ist, fällt die Strategie auf das manuelle Volumen zurück.
- Wenn `UseAutoVolume` `false` ist, wird der feste `ManualVolume`-Parameter für jede Order verwendet.

## Parameter
- `CandleType`: Kerzenserie, die für die Indikatorberechnung verwendet wird (Standard: 1-Minuten-Zeitrahmen).
- `RsiPeriod`: Anzahl der Bars im RSI-Berechnungsfenster (Standard: 14).
- `RsiBuyLevel`: Überverkaufsgrenze, die Long-Einstiege und Short-Ausstiege auslöst (Standard: 30).
- `RsiSellLevel`: Überkaufsgrenze, die Short-Einstiege und Long-Ausstiege auslöst (Standard: 70).
- `EnableLong`: Long-Trades aktivieren oder deaktivieren (Standard: true).
- `EnableShort`: Short-Trades aktivieren oder deaktivieren (Standard: true).
- `CloseBySignal`: Positionen schließen, wenn der RSI den entgegengesetzten Schwellenwert kreuzt (Standard: true).
- `StopLoss`: Stop-Loss-Distanz in Preiseinheiten (Standard: 0, deaktiviert).
- `TakeProfit`: Take-Profit-Distanz in Preiseinheiten (Standard: 0, deaktiviert).
- `TrailingStop`: Trailing-Stop-Distanz in Preiseinheiten (Standard: 0, deaktiviert).
- `UseAutoVolume`: risikobasierte Positionsgrößenbestimmung einschalten (Standard: true).
- `RiskPercent`: Prozentsatz des Kapitals, der beim aktiven Auto-Sizing riskiert wird (Standard: 10).
- `ManualVolume`: feste Ordergröße, wenn Auto-Sizing deaktiviert ist (Standard: 0.1).

## Implementierungshinweise
- Die StockSharp-Implementierung verwendet den High-Level-Workflow `SubscribeCandles(...).Bind(...)`, der es dem RSI-Indikator ermöglicht, Werte direkt an die Strategie zu liefern, ohne manuelle Pufferverwaltung.
- Die Strategie setzt alle Schutzniveaus zurück, wenn die Position auf null zurückkehrt, um veraltete Stop- oder Take-Profit-Werte zu vermeiden.
- Die Trailing-Logik spiegelt den MQL-Code wider: Der Stop wird nur angepasst, nachdem der Preis mehr als das Doppelte der Trailing-Distanz über das aktuelle Stop-Niveau hinaus reist, was ein vorzeitiges Anziehen verhindert.
- Da StockSharp-Strategien in einer Netting-Umgebung operieren, ist es nicht möglich, gleichzeitig Long- und Short-Positionen wie im ursprünglichen Hedging-EA zu halten. Stattdessen wartet die Strategie darauf, dass die aktuelle Position schließt, bevor sie in die entgegengesetzte Richtung öffnet.
- Automatisches Sizing erfordert, dass entweder `StopLoss` oder `TrailingStop` definiert ist; andernfalls wird das manuelle Volumen verwendet, da die Risikoentfernung unbekannt ist.

## Standardkonfiguration
- Zeitrahmen: 1-Minuten-Kerzen.
- RSI: Periode 14, Niveaus 30/70.
- Geldverwaltung: Auto-Volumen aktiviert, 10% Kapitalrisiko, manuelles Fallback-Volumen 0.1.
- Risikokontrollen: standardmäßig kein Stop-Loss, Take-Profit oder Trailing Stop (müssen für den Live-Handel konfiguriert werden).

## Verwendungstipps
- Stellen Sie `CandleType` entsprechend dem Instrument und dem Zeithorizont ein, den Sie handeln möchten; die Strategie funktioniert in jedem von StockSharp-Kerzen unterstützten Intervall.
- Geben Sie realistische Stop-Loss- oder Trailing-Stop-Distanzen an, bevor Sie Auto-Sizing aktivieren, damit die Risikoberechnung aussagekräftige Werte verwendet.
- Kombinieren Sie die Strategie mit `StartProtection()` (bereits im Code aufgerufen), um dem Framework zu ermöglichen, unerwartete Verbindungsabbrüche oder verwaiste Positionen zu verwalten.
- Überwachen Sie Ausführungen und passen Sie die RSI-Niveaus an, wenn Sie die Strategie auf verschiedenen Märkten anwenden, da optimale Schwellenwerte variieren können.
