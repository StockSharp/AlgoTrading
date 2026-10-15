# Live Alligator-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie handelt Trendumkehrungen mit einer dynamischen Alligator-Konfiguration und mehreren EMA-Filtern.
Sie eröffnet eine neue Position, wenn die Alligator-Linien die Richtung wechseln und fünf EMAs die Bewegung bestätigen.
Ein optionaler Handelszeiten-Filter begrenzt Einstiege auf eine gewählte Sitzung.
Die offene Position wird geschlossen, wenn der Preis einen gleitenden Trailing-SMMA kreuzt.

- **Einstiegskriterien**
  - Alligator Lips über Jaws mit Teeth unter Jaws und vorheriger Balken Lips unter Jaws -> Long nach einem Bärtrend öffnen.
  - Alligator Lips unter Jaws mit Teeth über Jaws und vorheriger Balken Lips über Jaws -> Short nach einem Bullentrend öffnen.
  - Fünf EMAs auf Schluss-, gewichteten, typischen, medianen und Eröffnungspreisen müssen streng in Trendrichtung geordnet sein.
- **Ausstiegskriterien**
  - Preis kreuzt den Trailing SMMA basierend auf `TrailPeriod`.
  - Optionaler Stop-Loss bei Trade-Eröffnung angewendet.
- **Verwendete Indikatoren**
  - Geglättete Gleitende Durchschnitte für Alligator-Linien und Trailing Stop.
  - Exponentielle Gleitende Durchschnitte auf verschiedenen Preistypen.

Parameter ermöglichen die Konfiguration der Alligator-Basisperiode, EMA-Bestätigungsperiode, Trailing-Periode, Stop-Loss und Handelszeiten-Fenster.
