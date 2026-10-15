# Tester v0.14 Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Beispielstrategie ist ein vereinfachter Port des MQL4-Skripts "Tester v0.14", das ursprünglich für EURUSD auf dem H4-Zeitrahmen entwickelt wurde.

## Logik

- Berechnet einen einfachen gleitenden Durchschnitt mit 14 Perioden und MACD.
- Generiert ein Kaufsignal, wenn der Schlusskurs über dem SMA liegt und MACD positiv ist.
- Generiert ein Verkaufssignal, wenn der Schlusskurs unter dem SMA liegt und MACD negativ ist.
- Nach dem Öffnen einer Order wird die Position nach einer konfigurierbaren Anzahl von Bars geschlossen.

Dieser Port verwendet die High-Level-StockSharp-API und basiert auf `SubscribeCandles` und `Bind` zum Empfangen von Indikatorwerten.

## Parameter

- **MinSignSum** – Mindestzahl der erforderlichen Signale, um eine Position zu eröffnen.
- **Risk** – Prozentsatz des Kontosaldos für das Money Management.
- **TakeProfit / StopLoss** – feste Levels in Punkten.
- **BarsNumber** – Anzahl der Bars, für die eine Position offen bleibt.
- **CandleType** – verwendete Kerzenserie (Standard: 4H).

## Hinweise

Die originale MQL-Datei enthielt Hunderte von Regelkombinationen. Dieses C#-Beispiel veranschaulicht die Struktur anhand eines reduzierten Regelsatzes zur besseren Übersichtlichkeit.
