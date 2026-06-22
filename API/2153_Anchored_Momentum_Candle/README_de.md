# Anchored Momentum-Kerzen-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie konvertiert den MQL5-Experten "AnchoredMomentumCandle" in ein StockSharp-C#-Beispiel. Sie berechnet den verankerten Momentum für die Eröffnungs- und Schlusskurse von Kerzen mithilfe von exponentiellen und einfachen gleitenden Durchschnitten. Der Indikator zeichnet eine synthetische Kerze, deren Farbe die Momentum-Richtung widerspiegelt.

Ein Wechsel zu einer **blauen** Kerze öffnet eine Long-Position und schließt jede Short-Position. Ein Wechsel zu einer **rosa** Kerze öffnet eine Short-Position und schließt jede Long-Position.

## Parameter
- **Momentum Period** – Länge der einfachen gleitenden Durchschnitte.
- **Smooth Period** – Länge der exponentiellen gleitenden Durchschnitte.
- **Candle Type** – Zeitrahmen der für Berechnungen verwendeten Kerzen.

Die Strategie abonniert die angegebenen Kerzen, berechnet den Indikator und gibt bei Farbwechseln Market-Orders aus.
