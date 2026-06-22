# Millenium Code Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die **Millenium Code** Strategie ist ein positionelles System, das höchstens einen Trade pro Tag eröffnet. Die Richtung wird durch einen gleitenden Durchschnitt-Crossover bestimmt, der durch jüngste Hochs und Tiefs gefiltert wird. Trades werden zu einer benutzerdefiniert Zeit platziert und durch Zeit, Stop-Loss, Take-Profit oder maximale Dauer geschlossen.

## Handelslogik

1. Zur angegebenen Öffnungszeit prüft die Strategie, ob für den aktuellen Wochentag der Handel erlaubt ist.
2. Schnelle und langsame einfache gleitende Durchschnitte werden verglichen. Wenn der schnelle MA den langsamen MA nach oben kreuzt und der Preis den Ausbruch bestätigt, wird eine Long-Position eröffnet. Die entgegengesetzten Bedingungen eröffnen eine Short-Position.
3. Pro Tag ist nur ein Trade erlaubt. Folgesignale werden bis zum nächsten Handelstag ignoriert.
4. Positionen werden geschlossen, wenn:
   - Stop-Loss- oder Take-Profit-Niveau erreicht wird.
   - Die konfigurierte Schließzeit eintritt.
   - Die maximale Trade-Dauer überschritten wird.

## Parameter

- **Candle Type** – Zeitrahmen der Eingangskerzen.
- **Fast MA** – Periode des schnellen gleitenden Durchschnitts.
- **Slow MA** – Periode des langsamen gleitenden Durchschnitts.
- **HighLow Bars** – Anzahl der Kerzen zur Suche nach jüngsten Hochs und Tiefs.
- **Reverse** – Kauf-/Verkaufssignale umkehren.
- **Stop Loss** – Abstand zum Stop-Loss in Preisschritten.
- **Take Profit** – Abstand zum Take-Profit in Preisschritten.
- **Open Hour/Minute** – Zeit für den Start der Eingangssuche (-1 deaktiviert).
- **Close Hour/Minute** – Zeit zum Schließen von Positionen (-1 deaktiviert).
- **Duration** – maximale Trade-Lebensdauer in Stunden (0 deaktiviert).
- **Sunday ... Friday** – Handel für jeden Wochentag aktivieren.

## Hinweise

Diese Strategie verwendet ausschließlich High-Level-API-Funktionen und vermeidet den direkten Zugriff auf den Indikatorverlauf. Sie ist als Lehrbeispiel gedacht und keine Anlageberatung.
