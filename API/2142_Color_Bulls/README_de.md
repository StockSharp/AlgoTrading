# Color Bulls-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Diese Strategie ist ein Port des MetaTrader-Experten `Exp_ColorBulls`. Sie basiert auf dem Color-Bulls-Indikator, der die Differenz zwischen dem Höchstkurs der Kerze und einem gleitenden Durchschnitt berechnet. Der resultierende Wert wird durch einen weiteren gleitenden Durchschnitt geglättet und als Histogramm mit verschiedenen Farben für steigende und fallende Werte dargestellt.

Die Strategie reagiert auf Farbänderungen dieses Histogramms:

- Wenn der Indikator von steigend (grün) zu fallend (magenta) wechselt, wird eine Long-Position eröffnet.
- Wenn der Indikator von fallend zu steigend wechselt, wird eine Short-Position eröffnet.
- Entgegengesetzte Positionen werden vor dem Eingehen neuer Positionen automatisch geschlossen.

Es werden nur abgeschlossene Kerzen verarbeitet und für Ein- und Ausstiege werden Marktorders verwendet.

## Parameter

- **Fast MA Length** – Periode des auf Höchstkurse angewendeten gleitenden Durchschnitts.
- **Smooth Length** – Periode des zur Glättung des Bulls-Wertes verwendeten gleitenden Durchschnitts.
- **Candle Type** – Zeitrahmen der für Berechnungen verwendeten Kerzen.

## Hinweise

Dieses Beispiel demonstriert die Integration eines benutzerdefinierten Indikators mit der High-Level-API von StockSharp. Stop-Loss- und Take-Profit-Management ist nicht enthalten.
