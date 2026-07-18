# Fibo Candles Trend-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie verwendet die benutzerdefinierte **Fibo Candles**-Technik zur Bestimmung der Trendrichtung.
Der Indikator färbt jede Kerze in einer von zwei Farben basierend auf einem Fibonacci-Verhältnisvergleich
zwischen dem aktuellen Schlusskurs und dem jüngsten Hoch/Tief-Bereich. Ein Farbwechsel signalisiert eine mögliche
Umkehr. Wenn die Farbe bullisch wird, schließt die Strategie jede Short-Position und eröffnet eine Long-Position.
Wenn die Farbe bärisch wird, schließt sie jede Long-Position und eröffnet eine Short-Position.

Die Methode passt sich durch einen Lookback-Zeitraum und ein wählbares Fibonacci-Niveau an die Marktvolatilität an.
Ein Stop-Loss und Take-Profit in absoluten Punkten schützen jeden Trade.

## Details

- **Einstiegskriterien**:
  - **Long**: Die aktuelle Kerzenfarbe wechselt von bärisch zu bullisch.
  - **Short**: Die aktuelle Kerzenfarbe wechselt von bullisch zu bärisch.
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**:
  - Bestehende Positionen werden geschlossen, wenn die entgegengesetzte Farbe erscheint.
- **Stops**: Fester Stop-Loss und Take-Profit in Punkten über `StartProtection`.
- **Standardwerte**:
  - `Period` = 10 (Kerzen zur Messung des Hoch/Tief-Bereichs).
  - `Fibo Level` = 0.236 (Verhältnis für die Trendentscheidung).
  - `Stop Loss` = 1000 Punkte.
  - `Take Profit` = 2000 Punkte.
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: Highest, Lowest
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Standardmäßig stündlich
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Moderat
