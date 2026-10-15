# Kanal-Scalper-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Ein ATR-basiertes Kanalausbruch-Scalping-System. Für jede Kerze wird der Mittelpunkt als Durchschnitt von Hoch und Tief berechnet. Die obere und untere Bande werden durch Addition und Subtraktion des Average True Range multipliziert mit einem Faktor aufgebaut. Wenn der Schlusskurs die vorherige obere Bande durchbricht, wird eine Long-Position eröffnet. Ein Durchbruch unter die untere Bande löst eine Short-Position aus. Die Banden folgen der Handelsrichtung und dienen als dynamische Stops; ein Kreuzen der gegenüberliegenden Bande kehrt die Position um.

## Details

- **Einstiegskriterien**:
  - **Kauf**: Der Schlusskurs kreuzt über die vorherige obere Bande.
  - **Verkauf**: Der Schlusskurs kreuzt unter die vorherige untere Bande.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**:
  - Umkehrsignal, wenn der Preis die gegenüberliegende Bande kreuzt.
- **Stops**: Trailing-Kanalbänder fungieren als Stops.
- **Filter**: Keine.

## Parameter

- **ATR Period** – Anzahl der Balken für die ATR-Berechnung.
- **ATR Multiplier** – Faktor, der auf den ATR für den Bandenabstand angewendet wird.
- **Candle Type** – Zeitrahmen der Eingabekerzen.
