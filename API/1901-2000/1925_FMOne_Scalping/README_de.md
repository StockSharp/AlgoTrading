# FmOne-Scalping-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Die FmOne-Scalping-Strategie ist eine vereinfachte Umsetzung des FMOneEA MetaTrader-4-Expertenberaters. Die Strategie kombiniert einen schnellen und einen langsamen exponentiellen gleitenden Durchschnitt mit dem MACD-Indikator, um kurzfristigen Momentum auf beliebigen Zeitrahmen zu erfassen.

## Funktionsweise
1. Der schnelle und der langsame EMA definieren die aktuelle Trendrichtung.
2. Das MACD-Histogramm bestätigt den Momentum in Trendrichtung.
3. Eine Kauforder wird eröffnet, wenn der schnelle EMA über dem langsamen EMA liegt und das MACD-Histogramm positiv ist.
4. Eine Verkaufsorder wird eröffnet, wenn der schnelle EMA unter dem langsamen EMA liegt und das MACD-Histogramm negativ ist.
5. Jede Position wird mit konfigurierbaren Stop-Loss- und Take-Profit-Niveaus abgesichert. Trailing Stop kann aktiviert werden, um profitable Bewegungen zu verfolgen.

## Parameter
- **FastMaPeriod** – Länge des schnellen EMA.
- **SlowMaPeriod** – Länge des langsamen EMA.
- **MacdSignalPeriod** – Signallinienperiode für den MACD-Indikator.
- **StopLossPercent** – Stop-Loss-Größe in Prozent des Einstiegspreises.
- **TakeProfitPercent** – Take-Profit-Größe in Prozent des Einstiegspreises.
- **EnableTrailingStop** – Aktiviert die Trailing-Stop-Verwaltung.
- **CandleType** – Zeitrahmen für eingehende Kerzen.

## Hinweise
Dieser Port fokussiert sich auf die Kernlogik des ursprünglichen EA. Erweiterte Funktionen wie Rückzahlungszyklen und Break-Even-Automatisierung aus der MQL-Version werden bewusst weggelassen, um das Beispiel lesbar zu halten.
