# Einfache MACD EA-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die Simple MACD EA-Strategie ist eine direkte Portierung des klassischen MetaTrader Expert Advisors „Simple MACD EA“. Der Ansatz verwendet zwei exponentielle gleitende Durchschnitte (EMAs), um das MACD-Histogramm zu emulieren und den vorherrschenden Trend bei Ein-Minuten-Kerzen zu bestimmen. Long-Positionen werden eröffnet, wenn der schnelle EMA (Periode 100) den langsamen EMA (benutzerdefiniertes MACD-Niveau) überschreitet. Short-Positionen werden eröffnet, wenn der schnelle EMA unter den langsamen EMA fällt. Es wird immer nur eine Position beibehalten.

## Handelsmanagementlogik
- **Trenderkennung:** Die Differenz zwischen dem 100-Perioden-EMA und dem konfigurierbaren MACD EMA definiert die aktuelle Trendrichtung (`+1`, `0`, `-1`). Eine Umkehr von negativ zu positiv eröffnet eine Long-Position. Eine Umkehr von positiv zu negativ eröffnet eine Short-Position.
- **Momentum-Bestätigung:** Die Strategie verfolgt den Unterschied zwischen dem MACD EMA und einem etwas langsameren EMA (`MACD level + 1`). Wenn die Differenz gegenüber dem aktuellen Handel kleiner wird, nachdem sich der Preis um mindestens fünf Punkte im Gewinn bewegt hat, wird die Position vorzeitig geschlossen.
- **Zeitbasierter Schutz:** Nachdem ein Trade für eine benutzerdefinierte Anzahl von Bewertungszyklen offen bleibt, aktiviert das System einen Soft-Stop, der die Toleranz für ungünstige Preisbewegungen im Verhältnis zum Einstiegspreis verringert.
- **Trailing-Exit:** Sobald der Trade in die Gewinnzone geht und für genügend Zyklen aktiv bleibt, wird ein interner Trailing-Stop aktiviert. Das Stop-Level folgt dem Preis um die konfigurierte Anzahl von Punkten und kann eine begrenzte Anzahl von Malen aktualisiert werden. Bei Erreichen des Limits wird die Position geschlossen.
- **Ausstieg bei Trendumkehr:** Wenn das Trendsignal in die entgegengesetzte Richtung wechselt, während der Preis bereits fünf Punkte im Gewinn liegt, wird die Position sofort geschlossen.

## Parameter
- **Kerzentyp** – Zeitrahmen, der für die EMA-Berechnungen verwendet wird (Standard: 1-Minuten-Kerzen).
- **Volumen** – Bestellvolumen für Neuzugänge.
- **MACD Level** – EMA Länge, die die langsame MACD-Komponente definiert. Ein sekundäres EMA mit der Länge `MACD Level + 1` wird automatisch abgeleitet.
- **Trailing Stop** – Entfernung in Punkten für den Trailing Exit. Zum Deaktivieren auf Null setzen.
- **Trailing-Updates** – Maximale Anzahl von Trailing-Stop-Anpassungen pro Trade.
- **Wartezyklen** – Anzahl der Kerzenauswertungen, die gewartet werden müssen, bevor der adaptive Softstopp aktiv wird.

## Zusätzliche Hinweise
- Die Strategie flacht immer die aktuelle Position ab, bevor die Richtung umgekehrt wird.
- Preisschrittinformationen des ausgewählten Wertpapiers werden verwendet, um punktbasierte Entfernungen in tatsächliche Preise umzuwandeln.
- Die Implementierung basiert auf dem High-Level-Kerzenabonnement API von StockSharp und stellt keine benutzerdefinierten Indikatorpuffer in die Warteschlange.
