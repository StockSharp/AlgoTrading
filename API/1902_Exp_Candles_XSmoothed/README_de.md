# Exp Candles XSmoothed-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie überwacht Kerzenhochs und -tiefs, die durch einen gewichteten gleitenden Durchschnitt (WMA) geglättet werden. Wenn der Schlusskurs über das geglättete Hoch plus einen konfigurierbaren Puffer steigt, wird eine Long-Position eröffnet und eine bestehende Short-Position geschlossen. Umgekehrt öffnet ein Schluss unter dem geglätteten Tief minus dem Puffer eine Short-Position und schließt eine bestehende Long-Position.

## Parameter
- **MA Length** – Anzahl der Perioden für die gewichteten gleitenden Durchschnitte, die auf Hochs und Tiefs angewendet werden.
- **Level** – Ausbruchspuffer in Punkten, der zum geglätteten Hoch addiert und vom geglätteten Tief subtrahiert wird.
- **Candle Type** – Zeitrahmen der Kerzen, die für die Analyse verwendet werden.
- **Buy Open / Sell Open** – Berechtigungen zum Öffnen von Long- oder Short-Positionen.
- **Buy Close / Sell Close** – Berechtigungen zum Schließen bestehender Positionen, wenn ein entgegengesetzter Ausbruch auftritt.

Die Strategie zeichnet geglättete Hoch- und Tieflinien im Diagramm zur visuellen Bestätigung und verwendet den eingebauten Positionsschutz, sobald sie gestartet wurde.
