# Strategiediagramm einer EMA-Abweichungsleiter
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Das Diagramm verbindet das EMA(30)/StandardDeviation(14)-Rückkehrsignal aus C# mit der Grid-Idee des README. 1,5σ aktiviert ein marktfähiges nahes Limit und eine ausstehende 2,5σ-Stufe; ausgestiegen wird an der Quellgrenze EMA ± 0,5σ.

![schema](schema.svg)

## Strategieübersicht

- Fertige Fünfminuten-Replaykerzen aktualisieren EMA, Abweichung und einen nach beiden Indikatoren synchronisierten Schluss.
- Unter EMA − 1,5σ werden bei Position <= 0 Käufe auf −1,5σ und −2,5σ gesetzt; oben gilt dies spiegelbildlich bei Position >= 0.
- Die nahe Stufe wird gewöhnlich sofort ausgeführt, die ferne wartet auf eine stärkere Bewegung.
- Long schließt bei Close > EMA + 0,5σ, Short bei Close < EMA − 0,5σ, genau wie im Code.
- Jeder Ausstieg storniert beide fernen Orders; ein Gegensignal entfernt die alte ferne Stufe der vorherigen Seite.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Beim Unterschreiten von EMA − 1,5σ mit Position <= 0 werden Kauf-Limits über je eine Einheit bei −1,5σ und −2,5σ registriert.
- **Short-Einstieg**: Beim Überschreiten von EMA + 1,5σ mit Position >= 0 werden Verkauf-Limits über je eine Einheit bei +1,5σ und +2,5σ registriert.
- **Ausstieg**: Close > EMA + 0,5σ schließt Long, Close < EMA − 0,5σ schließt Short. ClosePosition berechnet das gesamte aktuelle Volumen automatisch.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| Candle Time Frame | 00:05:00 | Fertiges Intervall; fünf Minuten passen den C#-Standard vier Stunden an. |
| EMA Length | 30 | Zentrale EMA-Periode und echter C#-Parameter. |
| Standard Deviation Length | 14 | Breitenperiode; 14 ist C#-Literal. |
| Near Entry Deviation | 1.5σ | Quell-Einstiegsmultiplikator 1,5. |
| Far Grid Deviation | 2.5σ | Zweite Pending-Stufe aus dem README. |
| Mean-Reversion Exit Deviation | 0.5σ | Quell-Rückkehrgrenze 0,5. |
| Volume per Rung | 1 | Eigenes Volumen jeder Stufe. |

## Diagrammdetails

- Nur EmaLength und CandleType sind StrategyParam in C#. Periode 14 sowie 1,5/0,5 sind Literale, die hier nur zum Lernen freigegeben werden.
- C# nutzt vier Stunden; fünf Minuten sind eine Replay-Anpassung für genügend Ereignisse im Monat.
- Der ausführbare Code besitzt trotz Three Level Grid nur die 1,5σ-Schwelle. 2,5σ stammt ausdrücklich aus dem README.
- Die Quelle kehrt mit zwei sofortigen Market-Orders um. Hier kann das nahe Limit erst glätten und das ferne später die Umkehr vollenden.
- Crossing erzeugt eine Leiter je Ausflug; Stornierung verhindert spätere unverwaltete Positionen durch Restorders.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
