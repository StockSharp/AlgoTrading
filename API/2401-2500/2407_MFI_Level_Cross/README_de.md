# MFI-Niveau-Kreuzungs-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie verwendet den Money Flow Index (MFI) Oszillator, um überkaufte und überverkaufte Bedingungen zu identifizieren. Wenn der MFI vordefinierte Schwellenniveaus kreuzt, eröffnet oder kehrt die Strategie Positionen um. Je nach ausgewähltem Trend-Modus kann sie in Richtung der Kreuzung oder in der entgegengesetzten Richtung operieren.

Die Standardkonfiguration überwacht Vier-Stunden-Kerzen und wertet den 14-Perioden-MFI aus. Die Strategie eröffnet eine Long-Position, wenn der MFI unter den unteren Schwellenwert fällt, und eine Short-Position, wenn er über den oberen Schwellenwert steigt. Im "Against"-Modus wird die Einstiegslogik umgekehrt, um gegen die Indikatorrichtung zu handeln.

Das Risikomanagement erfolgt über integrierte Stop-Loss- und Take-Profit-Parameter, die als Prozentsatz des Einstiegspreises ausgedrückt werden.

## Details

- **Einstiegskriterien**:
  - **Trend Mode: Direct**:
    - **Long**: Vorheriger MFI > Low-Niveau und aktueller MFI ≤ Low-Niveau.
    - **Short**: Vorheriger MFI < High-Niveau und aktueller MFI ≥ High-Niveau.
  - **Trend Mode: Against**:
    - **Long**: Vorheriger MFI < High-Niveau und aktueller MFI ≥ High-Niveau.
    - **Short**: Vorheriger MFI > Low-Niveau und aktueller MFI ≤ Low-Niveau.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Position wird umgekehrt, wenn das entgegengesetzte Signal erscheint, oder vom Schutzmodul geschlossen.
- **Stops**: Stop-Loss und Take-Profit in Prozent vom Einstiegspreis.
- **Standardwerte**:
  - `Candle Type` = 4-Stunden-Kerzen.
  - `MFI Period` = 14.
  - `Low Level` = 40.
  - `High Level` = 60.
  - `Stop Loss %` = 1.
  - `Take Profit %` = 2.
- **Filter**:
  - Kategorie: Oszillator
  - Richtung: Konfigurierbar
  - Indikatoren: Money Flow Index
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

## Hinweise

Diese Implementierung basiert auf der High-Level-API von StockSharp. Sie abonniert Kerzendaten, bindet den MFI-Indikator direkt und führt Marktorders aus, wenn Kreuzungsbedingungen erfüllt sind. Der Positionsschutz wird einmal beim Start initialisiert, um das Risiko automatisch zu verwalten.
