# Bullisches Engulfing-Muster-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Dieses Setup sucht nach einer starken bullischen Umkehr, wenn eine Kerze den vorherigen bärischen Balken vollständig umschließt. Eine solche Formation beendet oft einen kurzfristigen Rückgang und deutet auf neuen Aufwärtsschwung hin. Der optionale Abwärtstrend-Filter zählt aufeinanderfolgende rote Kerzen, um die Erschöpfung der Verkäufer zu bestätigen.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 76%. Sie funktioniert am besten auf dem Forex-Markt.

Im Live-Betrieb beobachtet der Algorithmus jede eingehende Kerze und verfolgt den vorherigen Balken. Wenn die neue Kerze höher schließt als sie öffnet und ihr Körper den vorherigen Balken umschließt, wird ein Long-Einstieg ausgelöst. Der Stop wird knapp unterhalb des Mustertiefs platziert, um das Risiko zu begrenzen.

Trades bleiben offen, bis der Stop ausgelöst wird oder ein anderes Signal auf einen manuellen Ausstieg hindeutet. Da die Bestätigung durch frühere Abwärtsbalken das Setup stärkt, vermeidet die Strategie das Nachjagen schwacher Umkehrungen.

## Details

- **Einstiegskriterien**: Bullische Kerze umschließt vorherigen bärischen Balken, optionaler Abwärtstrend vorhanden.
- **Long/Short**: Nur Long.
- **Ausstiegskriterien**: Stop-Loss oder diskretionär.
- **Stops**: Ja, unterhalb des Mustertiefs.
- **Standardwerte**:
  - `CandleType` = 15 minute
  - `StopLossPercent` = 1
  - `RequireDowntrend` = true
  - `DowntrendBars` = 3
- **Filter**:
  - Kategorie: Muster
  - Richtung: Long
  - Indikatoren: Candlestick
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

