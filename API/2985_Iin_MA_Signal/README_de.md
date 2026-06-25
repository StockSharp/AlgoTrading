# Iin MA Signal-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die Strategie reproduziert das Verhalten des klassischen MQL5-Expert-Advisors **Iin MA Signal**. Sie beobachtet eine Kreuzung zwischen einem schnellen und einem langsamen gleitenden Durchschnitt und reagiert auf der Bar, die durch den `SignalBar`-Parameter definiert wird, genau wie die ursprüngliche Vorlage, die die Indikatorpuffer abfragte. Bullische Kreuze öffnen Long-Positionen und schließen optional bestehende Shorts, während bärische Kreuze Shorts öffnen und optional Longs schließen. Stops und Ziele können automatisch über den StockSharp-Positionsschutz angehängt werden.

## Handelslogik
1. Abonnieren einer einzelnen Kerzenserie, die durch `CandleType` angegeben wird (Standard: 1-Stunden-Kerzen).
2. Erstellen von zwei gleitenden Durchschnitten mit den Typen und Längen, die durch `FastMaType`/`FastPeriod` und `SlowMaType`/`SlowPeriod` definiert werden. SMA, EMA, SMMA (RMA) und LWMA werden unterstützt, um die im MQL-Quellcode verfügbaren Kombinationen abzudecken.
3. Speichern eines rollenden Fensters von gleitenden Durchschnittswerten, damit die Kreuzung am durch `SignalBar` angegebenen Kerzenindex ausgewertet werden kann. Dies ahmt die `CopyBuffer`-Anfragen des ursprünglichen Expert-Advisors nach.
4. Erkennen einer bullischen Kreuzung, wenn die schnelle MA auf der vorherigen Bar des Fensters unter der langsamen MA lag und auf der Signalbar darüber ansteigt, während der vorherige Trend nicht bereits bullisch war. Eine bärische Kreuzung wird symmetrisch erkannt.
5. Aktualisieren des internen Trendflags nach jeder bestätigten Kreuzung, um doppelte Einstiege zu vermeiden und die `trend`-Schutzvariable des MQL-Indikators zu replizieren.
6. Wenn Trading erlaubt ist (`IsFormedAndOnlineAndAllowTrading()` gibt true zurück), die durch die Einstiegs-/Ausstiegsflags definierten Marktorders senden.

## Einstiegsregeln
- **Long-Einstieg**: wird bei einer bullischen Kreuzung ausgelöst, wenn `AllowLongEntries` aktiviert ist und die aktuelle Position flach oder short ist. Jeder offene Short kann zuerst geschlossen werden, wenn `CloseShortOnSignal` wahr ist.
- **Short-Einstieg**: wird bei einer bärischen Kreuzung ausgelöst, wenn `AllowShortEntries` aktiviert ist und die aktuelle Position flach oder long ist. Jeder offene Long kann zuerst geschlossen werden, wenn `CloseLongOnSignal` wahr ist.

## Ausstiegsregeln
- Entgegengesetzte Signale können Positionen gemäß den `CloseLongOnSignal`- und `CloseShortOnSignal`-Schaltern schließen.
- Optionale Schutzausstiegsniveaus verwenden absolute Preisabstände: `StopLossPoints` und `TakeProfitPoints`. Wenn einer der Werte größer als null ist, ruft die Strategie `StartProtection` auf, um den Stop-Loss und/oder Take-Profit mit Marktorders zu aktivieren.

## Parameter
| Parameter | Beschreibung | Standard |
| --- | --- | --- |
| `CandleType` | Datentyp, der die für Berechnungen verwendete Kerzenserie beschreibt. | 1-Stunden-Zeitrahmen |
| `FastPeriod` | Periode des schnellen gleitenden Durchschnitts. | 10 |
| `FastMaType` | Typ des schnellen gleitenden Durchschnitts (`Sma`, `Ema`, `Smma`, `Lwma`). | `Ema` |
| `SlowPeriod` | Periode des langsamen gleitenden Durchschnitts. | 22 |
| `SlowMaType` | Typ des langsamen gleitenden Durchschnitts (`Sma`, `Ema`, `Smma`, `Lwma`). | `Sma` |
| `SignalBar` | Anzahl der abgeschlossenen Bars zurück, die die Kreuzung enthalten müssen (1 reproduziert den MQL-Standard). | 1 |
| `AllowLongEntries` | Long-Einstiege aktivieren oder deaktivieren. | `true` |
| `AllowShortEntries` | Short-Einstiege aktivieren oder deaktivieren. | `true` |
| `CloseLongOnSignal` | Long-Positionen schließen, wenn ein bärisches Signal erscheint. | `true` |
| `CloseShortOnSignal` | Short-Positionen schließen, wenn ein bullisches Signal erscheint. | `true` |
| `StopLossPoints` | Absoluter Stop-Loss-Abstand in Preiseinheiten (0 deaktiviert). | 1000 |
| `TakeProfitPoints` | Absoluter Take-Profit-Abstand in Preiseinheiten (0 deaktiviert). | 2000 |

## Implementierungshinweise
- High-Level StockSharp-APIs werden durchgehend verwendet: `SubscribeCandles` fordert Marktdaten an und `Bind` streamt die MA-Werte direkt in die Strategie ohne manuelle Historienverwaltung.
- Die Moving-Average-Factory (`CreateMa`) ordnet die Enum-Werte StockSharp-Indikatoren zu und vermeidet benutzerdefinierte Berechnungen.
- Ein kompakter In-Memory-Puffer hält nur `SignalBar + 2` Samples, was ausreicht, um die Kreuzung auf der angeforderten Bar und der vorherigen auszuwerten.
- Schutzorders sind optional und werden nur initialisiert, wenn Abstände ungleich null konfiguriert sind, und replizieren das optionale MM-Modul der MQL-Version.
- Alle Kommentare im Code sind gemäß Repository-Regeln auf Englisch geschrieben.

## Verwendung
1. Die Lösung bauen (`dotnet build AlgoTrading.sln`), um die neue Strategie zu kompilieren.
2. `IinMaSignalStrategy` in Ihrer Anwendung instanziieren, die gewünschten Parameter konfigurieren und vor dem Start einen Connector/Wertpapier/Portfolio zuweisen.
3. Optional die Strategie an ein Chart anhängen, um die schnellen und langsamen gleitenden Durchschnitte zusammen mit ausgeführten Trades zu visualisieren.
4. Die MA-Perioden, die Signalbar und die Risikoeinstellungen optimieren, um die Vorlage an verschiedene Märkte anzupassen.

## Unterschiede zum ursprünglichen MQL-Expert-Advisor
- Die StockSharp-Version verwendet High-Level-Abonnements und Indikator-Binding anstelle manueller Buffer-Abfragen.
- Money-Management-Helfer aus `TradeAlgorithms.mqh` werden durch `StartProtection` ersetzt, das gleichwertige Stop- und Zielautomatisierung bietet.
- Das Positionsmanagement ist standardmäßig flach: Die Strategie vermeidet Hedging, indem keine neue Position geöffnet wird, während die Gegenseite noch aktiv ist, es sei denn, das Schließ-Flag ist deaktiviert.
- Chart-Rendering nutzt StockSharp-Hilfsmethoden und versucht nicht, die ursprünglichen Pfeil-Buffer zu replizieren.
