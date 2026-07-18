# X2MA Digit DM 361-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie kombiniert zwei gleitende Durchschnitte mit dem Average Directional Index (ADX).
Eine Long-Position wird eröffnet, wenn der schnelle gleitende Durchschnitt über dem langsamen liegt und der positive Richtungsindex (+DI) größer als der negative Richtungsindex (-DI) ist.
Eine Short-Position wird eröffnet, wenn der schnelle gleitende Durchschnitt unter dem langsamen liegt und -DI größer als +DI ist.

Die Strategie verwendet prozentbasierte Stop-Loss- und Take-Profit-Absicherungen. Kerzen für Berechnungen werden aus dem angegebenen Zeitrahmen entnommen.

## Parameter
- **Fast MA Length** – Länge des schnellen gleitenden Durchschnitts.
- **Slow MA Length** – Länge des langsamen gleitenden Durchschnitts.
- **ADX Length** – Periode für die Berechnung des Average Directional Index.
- **Stop Loss %** – Stop-Loss-Größe in Prozent des Einstiegspreises.
- **Take Profit %** – Take-Profit-Größe in Prozent des Einstiegspreises.
- **Candle Type** – Kerzen-Zeitrahmen für die Verarbeitung.
