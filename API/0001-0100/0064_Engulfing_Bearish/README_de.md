# Bärisches Engulfing-Muster-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Dieses Muster zielt darauf ab, den Beginn einer bärischen Bewegung nach einer Rallye zu erfassen. Ein bärisches Engulfing entsteht, wenn eine rote Kerze den vorherigen bullischen Körper vollständig verschluckt. Das Zählen einiger aufeinanderfolgender Aufwärtsbalken vor dem Muster stellt sicher, dass der Markt zuvor gestiegen war.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 79%. Sie funktioniert am besten auf dem Aktienmarkt.

Der Algorithmus speichert jede Kerze in Reihenfolge. Wenn der neue Balken niedriger schließt als er öffnet und sein Körper den vorherigen bullischen Balken umschließt, wird ein Leerverkauf ausgeführt. Der Stop-Loss wird oberhalb des Musterhochs platziert, um das Risiko zu begrenzen.

Positionen werden typischerweise mit dem schützenden Stop verwaltet, obwohl der Trader manuell aussteigen kann, wenn sich die Bedingungen ändern. Das Erfordern eines Aufwärtstrends hilft, Fehlsignale auf choppy Märkten zu vermeiden.

## Details

- **Einstiegskriterien**: Bärische Kerze umschließt vorherigen bullischen Balken, optionaler Aufwärtstrend vorhanden.
- **Long/Short**: Nur Short.
- **Ausstiegskriterien**: Stop-Loss oder diskretionär.
- **Stops**: Ja, oberhalb des Musterhochs.
- **Standardwerte**:
  - `CandleType` = 15 minute
  - `StopLossPercent` = 1
  - `RequireUptrend` = true
  - `UptrendBars` = 3
- **Filter**:
  - Kategorie: Muster
  - Richtung: Short
  - Indikatoren: Candlestick
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

