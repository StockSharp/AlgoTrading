# JMA Kerzensignal-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie verwendet zwei Jurik Moving Averages (JMA), die auf den Eröffnungs- und Schlusskursen jeder Kerze berechnet werden. Ein bullisches Signal entsteht, wenn der JMA des Eröffnungspreises den JMA des Schlusskurses von oben nach unten kreuzt, was einen Long-Einstieg auslöst. Ein bärisches Signal entsteht, wenn der JMA des Eröffnungspreises den JMA des Schlusskurses von unten nach oben kreuzt, was einen Short-Einstieg auslöst.

Der Standardzeitrahmen sind Vier-Stunden-Kerzen mit einer JMA-Periode von sieben. Stop-Loss- und Take-Profit-Niveaus sind in Punkten definiert und werden durch das integrierte Risikomanagement angewendet. Die Strategie reagiert nur auf abgeschlossene Kerzen und hält höchstens eine offene Position.

## Parameter
- **JMA Length** – Periode für beide JMAs.
- **Candle Type** – Zeitrahmen der verarbeiteten Kerzen.
- **Take Profit** – Gewinnziel in Punkten.
- **Stop Loss** – maximaler Verlust in Punkten.
