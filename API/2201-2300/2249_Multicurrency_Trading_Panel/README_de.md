# Strategie Multicurrency Trading Panel
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie emuliert das Verhalten des ursprünglichen MQL-Expertenberaters "Multicurrency trading panel". Sie überwacht drei Währungspaare (EURUSD, USDJPY, GBPUSD) und vergleicht die neueste Kerze mit der vorherigen anhand von sieben einfachen Metriken (Eröffnung, Hoch, Tief, (Hoch+Tief)/2, Schluss, (Hoch+Tief+Schluss)/3, (Hoch+Tief+Schluss+Schluss)/4).
Für jeden Vergleich wird ein KAUF- oder VERKAUF-Score erhöht. Wenn der automatische Handel aktiviert ist, eröffnet oder kehrt die Strategie Positionen auf einem Paar um, wenn der KAUF-Score den VERKAUF-Score übersteigt oder umgekehrt.

## Parameter
- **EURUSD** – erstes Wertpapier.
- **USDJPY** – zweites Wertpapier.
- **GBPUSD** – drittes Wertpapier.
- **Candle Type** – Zeitrahmen der Kerzen.
- **Auto Trade** – Umschalter für automatische Ordererteilung.

Die Strategie ist eine vereinfachte Demo und ist nicht für den echten Handel gedacht.
