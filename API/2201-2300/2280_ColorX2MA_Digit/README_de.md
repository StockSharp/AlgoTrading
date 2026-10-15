# ColorX2MA Digit-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie ist ein Port des MQL5-Experten **Exp_ColorX2MA_Digit**.
Der ursprüngliche Algorithmus färbt einen doppelt geglätteten gleitenden Durchschnitt je nach seiner Neigung in verschiedenen Farben ein und verwendet diese Farben zur Erzeugung von Handelssignalen.
In dieser C#-Version wird das Verhalten durch zwei einfache gleitende Durchschnitte angenähert und auf deren Kreuzungen gehandelt.

## Handelslogik

- Ein **schneller** gleitender Durchschnitt glättet die Preisreihe.
- Ein **langsamer** gleitender Durchschnitt glättet das Ergebnis des schnellen.
- Wenn der schnelle Durchschnitt den langsamen von unten nach oben kreuzt, eröffnet die Strategie eine Long-Position und schließt eine vorhandene Short-Position.
- Wenn der schnelle Durchschnitt den langsamen von oben nach unten kreuzt, eröffnet die Strategie eine Short-Position und schließt eine vorhandene Long-Position.
- Signale werden nur nach dem Schließen der Kerze verarbeitet.

## Parameter

- `FastLength` – Länge der ersten Glättung (Standard 12).
- `SlowLength` – Länge der zweiten Glättung (Standard 5).
- `CandleType` – Zeitrahmen der für Berechnungen verwendeten Kerzen.

Die Strategie verwendet ausschließlich die High-Level-API: `SubscribeCandles` mit `Bind` zur Versorgung der Indikatoren und `BuyMarket`/`SellMarket` zur Positionsverwaltung. Kommentare im Code sind auf Englisch für einfachere Wartung.
