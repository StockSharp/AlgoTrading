# TCPivot Stop-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie handelt Ausbrüche durch die tägliche Pivot-Linie. Sie berechnet klassische Floor-Trader-Pivot-Level aus dem Hoch, Tief und Schlusskurs des Vortages. Eine Long-Position wird eröffnet, wenn der Schlusskurs den Pivot von unten nach oben kreuzt. Eine Short-Position wird eröffnet, wenn der Schlusskurs den Pivot von oben nach unten kreuzt.

Nach dem Einstieg verwendet das System eines der Support- oder Resistance-Level sowohl als Kursziel als auch als Stop-Loss. Das Level wird über den Parameter **Target Level** ausgewählt:

- **1** – verwendet `Support1`/`Resistance1`.
- **2** – verwendet `Support2`/`Resistance2`.
- **3** – verwendet `Support3`/`Resistance3`.

Wenn **Intraday Only** aktiviert ist, werden alle offenen Positionen um 23:00 Plattformzeit geschlossen.

## Details

- **Einstiegskriterien**
  - **Long**: vorheriger Schlusskurs ≤ Pivot und aktueller Schlusskurs > Pivot.
  - **Short**: vorheriger Schlusskurs ≥ Pivot und aktueller Schlusskurs < Pivot.
- **Ausstiegskriterien**
  - **Long**: Schluss ≥ ausgewähltes Resistance-Level oder Schluss ≤ ausgewähltes Support-Level.
  - **Short**: Schluss ≤ ausgewähltes Support-Level oder Schluss ≥ ausgewähltes Resistance-Level.
  - Wenn *Intraday Only* aktiv ist, werden alle Positionen um 23:00 geschlossen.
- **Indikatoren**: nur klassische Pivot-Berechnung.
- **Zeitrahmen**: konfigurierbar; Standard 5-Minuten-Kerzen.
- **Stops**: Stop-Loss und Take-Profit aus dem gewählten Pivot-Level.
