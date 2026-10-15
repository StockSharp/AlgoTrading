# Divergenz-Händler-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie vergleicht zwei einfache gleitende Durchschnitte (SMA) und handelt auf Basis der Divergenz zwischen ihnen.

Sie verwendet die Differenz zwischen dem schnellen und langsamen SMA der vorherigen Kerze als Divergenzmaß. Ist diese Divergenz positiv und innerhalb eines bestimmten Bereichs, eröffnet die Strategie eine Long-Position. Ist die Divergenz negativ und innerhalb des gespiegelten Bereichs, wird eine Short-Position eröffnet. Das Risiko wird durch optionale Stop-Loss- und Take-Profit-Niveaus gesteuert.

## Details

- **Einstiegskriterien**:
  - **Long**: Vorheriger schneller SMA - vorheriger langsamer SMA >= `DvBuySell` und <= `DvStayOut`.
  - **Short**: Vorheriger schneller SMA - vorheriger langsamer SMA <= `-DvBuySell` und >= `-DvStayOut`.
- **Ausstiegskriterien**: Positionen werden über Stop-Loss oder Take-Profit geschlossen, wenn konfiguriert.
- **Stops**: Unterstützt über `StartProtection` mit absoluten Preisabständen.
- **Standardwerte**:
  - `FastPeriod` = 7
  - `SlowPeriod` = 88
  - `DvBuySell` = 0.0011
  - `DvStayOut` = 0.0079
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: SMA
  - Stops: Optional
  - Komplexität: Grundlegend
  - Zeitrahmen: Kurzfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Ja
  - Risikolevel: Mittel
