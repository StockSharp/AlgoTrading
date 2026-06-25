# Multi-Stochastic-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Die Multi-Stochastic-Strategie ist eine StockSharp High-Level-Implementierung des MetaTrader 5 Expert Advisors "Multi Stochastic (barabashkakvn's edition)". Sie überwacht bis zu vier Währungspaare gleichzeitig und verlässt sich auf synchronisierte Signale aus Stochastic Oscillator-Lesungen (5, 3, 3). Die Strategie öffnet eine einzelne Marktposition pro Symbol, wenn ein überverkaufter oder überkaufter Crossover auftritt, und schließt Trades über feste Pip-basierte Stop-Loss- und Take-Profit-Ziele.

## Handelslogik
- Jedes konfigurierte Symbol erhält seinen eigenen Stochastic Oscillator (Länge 5, %K-Glättung 3, %D-Glättung 3).
- Ein Long-Signal wird erzeugt, wenn das aktuelle %K unter dem OversoldLevel (Standard 20) liegt, der vorherige Bar %K unter %D hatte, und der aktuelle Bar mit %K über %D schließt.
- Ein Short-Signal wird erzeugt, wenn das aktuelle %K über dem OverboughtLevel (Standard 80) liegt, der vorherige Bar %K über %D hatte, und der aktuelle Bar mit %K unter %D schließt.
- Nur eine offene Position pro Instrument ist erlaubt. Zusätzliche Signale werden ignoriert, bis die bestehende Position geschlossen ist.

## Risikomanagement
- Stop-Loss- und Take-Profit-Werte werden in Pips ausgedrückt. Die Strategie konvertiert Pips automatisch in absolute Preisabstände, indem sie mit dem Instrument-Preisschritt multipliziert und für 3- oder 5-stellige Forex-Kursangaben anpasst (Pip = Schritt × 10 für diese Instrumente).
- Long-Positionen schließen, wenn das Kerzentief das Stop-Loss-Level berührt oder das Kerzenhoch das Take-Profit-Level erreicht.
- Short-Positionen schließen, wenn das Kerzenhoch das Stop-Loss-Level berührt oder das Kerzentief das Take-Profit-Level erreicht.

## Parameter
- `CandleType` – Zeitrahmen für alle abonnierten Kerzen (Standard: 1 Stunde).
- `StochasticLength` – Basislänge des Stochastic Oscillators (Standard: 5).
- `StochasticKPeriod` – Glättungsperiode für %K (Standard: 3).
- `StochasticDPeriod` – Glättungsperiode für %D (Standard: 3).
- `OversoldLevel` – Schwellenwert zur Erkennung überverkaufter Bedingungen (Standard: 20).
- `OverboughtLevel` – Schwellenwert zur Erkennung überkaufter Bedingungen (Standard: 80).
- `StopLossPips` – Abstand zum Schutz-Stop in Pips (Standard: 50).
- `TakeProfitPips` – Abstand zum Gewinnziel in Pips (Standard: 10).
- `UseSymbol1` … `UseSymbol4` – Aktiviert den Handel für den jeweiligen Symbol-Slot (Standard: true).
- `Symbol1` … `Symbol4` – Von jedem Slot gehandelte Wertpapiere. Symbol 1 fällt auf das Haupt-Strategie-Wertpapier zurück, wenn nicht angegeben.

## Implementierungshinweise
- Jedes Symbol-Abonnement ist unabhängig. Jedes verwendet `SubscribeCandles` mit `BindEx`, um `StochasticOscillatorValue`-Updates zusammen mit Kerzendaten zu empfangen.
- Vorherige %K- und %D-Werte werden pro Symbol zwischengespeichert, um die MT5-Crossover-Erkennungslogik zu emulieren.
- Risikoparameter werden für jeden Einstieg neu berechnet, und Stop-/Take-Levels werden nach dem Schließen einer Position oder wenn keine Position existiert zurückgesetzt.
- Orders werden mit `BuyMarket`/`SellMarket` unter Verwendung der gemeinsamen `Volume`-Eigenschaft gesendet, entsprechend der Ein-Positions-Einschränkung des ursprünglichen Experten.

## Unterschiede zur MT5-Version
- Die StockSharp-Version nutzt High-Level-Abonnements anstelle von manuellen Rate-Aktualisierungsaufrufen.
- Die Pip-Größenerkennung basiert auf `Security.PriceStep` und `Security.Decimals`. Wenn Metadaten nicht verfügbar sind, bleiben Stops und Ziele deaktiviert, um falsche Risikoberechnungen zu verhindern.
- Protokollierungs- und Chart-Zeichnungs-Hooks sind für die Erweiterung bereit, aber nicht für das Kernverhalten erforderlich.

## Verwendungstipps
1. Weise den Symbol-Slots die gewünschten Wertpapiere zu und passe den Kerzen-Zeitrahmen an deinen Handelshorizont an.
2. Stelle sicher, dass Stop-Loss- und Take-Profit-Abstände mit der Instrument-Tick-Größe kompatibel sind, um sofortige Schließungen zu vermeiden.
3. Deaktiviere ungenutzte Symbol-Slots, um den Ressourcenverbrauch zu reduzieren, wenn weniger Instrumente überwacht werden.
