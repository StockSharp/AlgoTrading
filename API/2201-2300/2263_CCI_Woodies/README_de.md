# CCI Woodies-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie handelt auf Basis des Kreuzungsverfahrens zweier Commodity Channel Index (CCI)-Linien, abgeleitet aus der Woodies-CCI-Methode. Ein schneller CCI und ein langsamer CCI werden auf dem angegebenen Zeitrahmen berechnet. Wenn die schnelle Linie unter die langsame Linie kreuzt, wird eine Long-Position eröffnet und eine bestehende Short-Position geschlossen. Wenn die schnelle Linie über die langsame Linie kreuzt, wird eine Short-Position eröffnet und eine bestehende Long-Position geschlossen.

## Parameter
- **FastPeriod** – Länge des schnellen CCI-Indikators.
- **SlowPeriod** – Länge des langsamen CCI-Indikators.
- **CandleType** – Zeitrahmen der für Berechnungen verwendeten Kerzen.
- **InvertSignals** – wenn aktiviert, werden Kauf- und Verkaufsregeln vertauscht.
- **TakeProfitPoints** – Gewinnziel in Preispunkten.
- **StopLossPoints** – Verlustlimit in Preispunkten.

## Hinweise
Die Strategie verwendet die High-Level-API von StockSharp. Indikatoren werden über `Bind` gebunden, und die Risikosteuerung wird mit `StartProtection` unter Verwendung von Stop-Loss- und Take-Profit-Niveaus gehandhabt.
