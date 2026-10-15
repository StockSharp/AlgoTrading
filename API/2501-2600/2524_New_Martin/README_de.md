# New Martin Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die New Martin Strategie repliziert den originalen MetaTrader "New Martin"-Expertenberater durch Ausführung einer symmetrischen Martingale-Absicherung auf beiden Seiten des Marktes. Die Strategie hält jederzeit eine anfängliche Long- und Short-Position offen und gleicht die Absicherung aus, wenn die schnellen und langsamen geglätteten gleitenden Durchschnitte (SMMA) sich kreuzen. Wenn eine Seite der Absicherung verliert, multipliziert der Algorithmus die Exposition auf dieser Seite und realisiert gleichzeitig Gewinne auf dem profitabelsten Bein. Take-Profit-Exits recyceln die Absicherung, indem sie die fehlende Seite wiedereröffnen und optional sowohl die besten als auch die schlechtesten Performer löschen, um das Grid kompakt zu halten.

Die Implementierung richtet sich an StockSharp's High-Level-API und erwartet ein Portfolio mit Hedging-Unterstützung, damit Long- und Short-Beine koexistieren können. Orders werden der Einfachheit halber als Marktorders gesendet, was die ursprüngliche MQL-Logik widerspiegelt, bei der Füllungen als sofortig angenommen werden.

## Indikatoren und Signale
- **Schneller SMMA (Standardlänge 5):** verfolgt die kurzfristige Preisrichtung.
- **Langsamer SMMA (Standardlänge 20):** repräsentiert den dominanten Trend.
- **Kreuzungserkennung:** ein Kreuzungspunkt der vorherigen zwei abgeschlossenen Bars löst die Martingale-Ergänzung auf dem schlechtesten Bein aus. Das Signal wird auf einmal pro Kerze gedrosselt, indem die Kerzen-Öffnungszeit des letzten Kreuzungspunkts gespeichert wird.

## Positionsmanagement
- **Anfängliche Absicherung:** sobald Indikatoren gebildet sind, eröffnet die Strategie eine Long- und eine Short-Position mit dem konfigurierten anfänglichen Volumen. Beide Trades verwenden einen symmetrischen Take-Profit-Abstand in Pips.
- **Take-Profit-Recycling:** wenn der Preis das Take-Profit-Niveau eines Beins berührt, schließt die Strategie diese Position, erfasst das Ereignis und schließt optional sowohl die profitabelsten als auch die verlierendsten verbleibenden Positionen, um Gewinne und Verluste paarweise zu realisieren. Fehlende Seiten werden sofort mit dem Basisvolumen wiedereröffnet, damit die Absicherung ausgewogen bleibt.
- **Martingale-Mittelung:** bei jeder SMMA-Kreuzung identifiziert der Algorithmus die Position mit dem niedrigsten nicht realisierten Gewinn. Er erhöht die Exposition auf dieser Seite durch Multiplikation des Trade-Volumens mit dem Martingale-Multiplikator (Standard 1.6) nach Anpassung an den Wertpapier-Volumenschritt. Die profitabelste offene Position wird direkt nach dem Mittelungs-Trade geschlossen, um gesperrten Gewinn freizusetzen.

## Risikomanagement
- **Kapital-Drawdown-Schutz:** das höchste beobachtete Portfolio-Kapital wird verfolgt. Wenn der Rückgang von diesem Höhepunkt den konfigurierten Prozentsatz überschreitet, werden alle offenen Positionen liquidiert und die Absicherungsinitialisierung bis zur nächsten Kerze verschoben.
- **Dynamisches Basisvolumen:** wenn das Kapital um mindestens den Martingale-Multiplikator relativ zum zuvor aufgezeichneten Saldo wächst, wird das Basis-Absicherungsvolumen um denselben Multiplikator erhöht (unter Berücksichtigung der Exchange-Volumenlimits). Dies spiegelt das ursprüngliche EA-Verhalten wider, bei dem Gewinne reinvestiert werden, um das Grid zu skalieren.
- **Volumen-Normalisierung:** jedes angeforderte Volumen wird auf den Exchange-Volumenschritt abgerundet und zwischen dem Mindest- und Höchstvolumen des Wertpapiers begrenzt, um Order-Ablehnungen zu vermeiden.

## Parameter
- **Take Profit (pips):** Abstand vom Einstiegspreis zum Take-Profit-Ziel für jedes Bein. Standard 50 Pips.
- **Initial Volume:** Basisvolumen pro Seite der Absicherung. Standard 0.1 Kontrakte.
- **Slow MA / Fast MA:** Längen der langsamen und schnellen SMMA-Indikatoren (Standards 20 und 5). Die langsame Periode muss größer als die schnelle Periode bleiben.
- **Equity DD %:** maximaler erlaubter Drawdown vom Kapitalspitzenwert, bevor alle Positionen geschlossen werden. Standard 12%.
- **Multiplier:** Martingale-Faktor für Averaging-Down und für Skalierung des Basisvolumens nach bedeutendem Kapitalwachstum. Standard 1.6.
- **Candle Type:** Zeitrahmen der für Berechnungen verwendeten Kerzen. Standard 15-Minuten-Kerzen, kann aber geändert werden, um dem Chartzeitrahmen des ursprünglichen EAs zu entsprechen.

## Hinweise
- Die Strategie erfordert Hedging-fähige Konten, da sie Long- und Short-Positionen gleichzeitig offen hält.
- Marktorders werden für Einstiege und Ausstiege verwendet, genau wie der MQL-Experte, der auf sofortige Füllungen angewiesen war. Passen Sie die Order-Logik an, wenn Slippage-Kontrolle benötigt wird.
- Stellen Sie sicher, dass die Wertpapier-Metadaten (Preisschritt, Volumenschritt, Min/Max-Volumen) korrekt konfiguriert sind, damit die Volumen-Normalisierung wie erwartet funktioniert.
