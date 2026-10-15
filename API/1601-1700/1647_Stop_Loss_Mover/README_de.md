# Stop-Loss-Verschieber-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Hilfsstrategie überwacht eine offene Position und verschiebt den Stop-Loss auf den Einstiegspreis, wenn der Markt ein vordefiniertes Niveau erreicht. Sie abonniert Kerzendaten und prüft jede abgeschlossene Kerze. Bei Long-Positionen wird, sobald das Kerzenhoch den konfigurierten `MoveSlPrice` übersteigt, eine Stop-Order zum Einstiegspreis platziert. Bei Short-Positionen wird der Stop verschoben, wenn das Kerzentief unter das Niveau fällt.

Die Strategie generiert keine neuen Handelssignale. Zu Demonstrationszwecken öffnet sie beim Start eine einzelne Long-Position und schützt diese dann, indem der Stop auf Break-even verschoben wird, sobald die Bedingungen erfüllt sind. So können Trader Gewinne sichern und den Trade gleichzeitig weiterlaufen lassen.

## Details

- **Einstiegskriterien**: Beim Start wird eine Long-Position eröffnet. Es werden keine zusätzlichen Signale verwendet.
- **Long/Short**: Unterstützt beides, aber das Beispiel öffnet eine Long-Position.
- **Ausstiegskriterien**: Position wird beendet, wenn die Stop-Order zum Einstiegspreis ausgelöst wird.
- **Stops**: Stop-Loss wird auf den Einstiegspreis verschoben, wenn `MoveSlPrice` erreicht wird.
- **Standardwerte**:
  - `MoveSlPrice` = 0 (muss vor dem Start angepasst werden).
  - `CandleType` = 1-Minuten-Zeitrahmen.
- **Filter**:
  - Kategorie: Risikomanagement
  - Richtung: Beide
  - Indikatoren: Keine
  - Stops: Ja
  - Komplexität: Einfach
  - Zeitrahmen: Kurzfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Niedrig
