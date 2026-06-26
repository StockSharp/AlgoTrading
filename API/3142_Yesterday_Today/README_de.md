# Yesterday Today-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Yesterday Today-Strategie reproduziert den klassischen MetaTrader-Ausbruch, bei dem der heutige Preis mit dem gestrigen Hoch und Tief verglichen wird. Die Strategie verfolgt die letzte abgeschlossene Tageskerze und beobachtet dann Intraday-Kerzen, um schnell zu reagieren, wenn der Preis die gestrige Range verlässt. Vor einer Umkehr schließt sie immer die entgegengesetzte Exposition, was einen sauberen Ein-Positions-Workflow ermöglicht.

## Überblick

- Verfolgt die vorherige Tages-Range und wartet auf den Schluss einer Intraday-Kerze, um diese zu durchbrechen.
- Öffnet Long-Positionen, wenn der Schluss das gestrige Hoch überschreitet; öffnet Short-Positionen, wenn der Schluss unter das gestrige Tief fällt.
- Wendet feste Stop-Loss- und Take-Profit-Abstände an, die in Pips ausgedrückt werden. Die Pip-Größe passt sich an 3- oder 5-stellige Forex-Kurse an, genau wie in der originalen MQL-Implementierung.
- Risikoniveaus werden bei jeder abgeschlossenen Intraday-Kerze anhand ihrer Hochs/Tiefs bewertet, um Stop-Loss- oder Take-Profit-Treffer zu erkennen.
- Verwendet das integrierte Schutz-Framework zum Schutz vor unerwarteten Margin-Problemen.

## Workflow

1. Tageskerzen abonnieren und Hoch/Tief der letzten abgeschlossenen Sitzung speichern.
2. Intraday-Kerzen abonnieren (standardmäßig 15-Minuten-Kerzen) für die Signalbewertung.
3. Bei jeder abgeschlossenen Intraday-Kerze:
   - Sofort aussteigen, wenn die Kerze den aktiven Stop-Loss oder Take-Profit verletzt.
   - Long eingehen, wenn der Schluss über dem gestrigen Hoch liegt und keine Long-Position offen ist.
   - Short eingehen, wenn der Schluss unter dem gestrigen Tief liegt und keine Short-Position offen ist.
   - Jede entgegengesetzte Position wird zuerst durch Erhöhung des Marktorder-Volumens geschlossen.
4. Wann immer eine neue Tageskerze abgeschlossen wird, die gespeicherte Range für den nächsten Handelstag aktualisieren.

## Parameter

- `TradeVolume` — Lotgröße für neue Positionen. Bei der Umkehr fügt die Strategie automatisch die entgegengesetzte Exposition hinzu, um zunächst zu glätten.
- `StopLossPips` — Abstand vom Einstiegspreis zum schützenden Stop, ausgedrückt in Pips. Ein Wert von `0` deaktiviert den Stop.
- `TakeProfitPips` — Abstand vom Einstiegspreis zum Gewinnziel, ausgedrückt in Pips. Ein Wert von `0` deaktiviert das Ziel.
- `SignalCandleType` — Intraday-Kerzentyp für die Ausbruchserkennung (Standard: 15-Minuten-Kerzen).

## Details

- **Einstiegskriterien**: Intraday-Kerze schließt über dem gestrigen Hoch (Long) oder unter dem gestrigen Tief (Short).
- **Long/Short**: Beide Richtungen unterstützt.
- **Ausstiegskriterien**: Stop-Loss- oder Take-Profit-Niveaus, die von Intraday-Kerzenextremen berührt werden.
- **Stops**: Ja, feste Pip-Abstände.
- **Standardwerte**:
  - `TradeVolume` = 1
  - `StopLossPips` = 50
  - `TakeProfitPips` = 50
  - `SignalCandleType` = `TimeSpan.FromMinutes(15).TimeFrame()`
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Beide
  - Indikatoren: Price Action
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday-Einträge mit Tageskontext
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

## Hinweise

- Die Strategie ist für ein einzelnes Instrument ausgelegt. Konfigurieren Sie `Security` und `Portfolio` vor dem Start.
- Die Pip-Größe wird aus `Security.PriceStep` berechnet und für Forex-Symbole mit 3 oder 5 Dezimalstellen automatisch skaliert, entsprechend der originalen EA-Logik.
- Der Schutz wird in `OnStarted` aktiviert, sodass globale Kontoschutzmaßnahmen aktiv bleiben, wenn die Strategie handelt.
