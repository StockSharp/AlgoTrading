# Fractals & Alligator-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie repliziert den MetaTrader-Experten "Fractals & Alligator", indem sie die Alligator-Ausrichtung von Bill Williams mit Fraktal-Ausbrüchen, einer Momentum-Bestätigungsschicht und Bereichsfiltern kombiniert. Sie verarbeitet abgeschlossene Kerzen auf einem höheren Zeitrahmen, um die ursprüngliche Multi-Timeframe-Logik zu emulieren.

## Details
- **Einstiegskriterien**: Warten, bis sich Alligator-Lippen, -Zähne und -Kiefer in dieselbe Richtung ausweiten, während sich ein frisches Fraktal jenseits des Mundes bildet. Ein Long-Setup erfordert, dass der Schlusskurs das neueste bullische Fraktal über den Zähnen bricht und mindestens eine der letzten drei Momentum-Messwerte den Kaufschwellenwert überschreitet. Shorts spiegeln die Regeln auf der Unterseite wider.
- **Long/Short**: Öffnet sowohl Long- als auch Short-Positionen. Es wird nur eine Nettoposition gehalten; neue Signale kehren die bestehende Exposition um.
- **Ausstiegskriterien**: Positionen werden geschlossen, wenn das entgegengesetzte Fraktal durchbrochen oder die Alligator-Ausrichtung zusammenbricht. Schutzorders übernehmen die verbleibenden Ausstiege.
- **Stops**: Verwendet StockSharp-Schutzorders für Stop-Loss, Take-Profit und einen optionalen Trailing-Stop in Preisschritten, entsprechend der ursprünglichen Geldmanagement-Idee.
- **Standardwerte**: Alligator-Längen 13/8/5 mit Verschiebungen 8/5/3, 14-Perioden-Momentum, 10-Balken-Bereichsrückblick, 20-Schritt-Festbox (wenn ATR-Filter deaktiviert), Take-Profit 50 Schritte, Stop-Loss 20 Schritte, Trailing-Stop 40 Schritte.
- **Filter**: Optionaler ATR-Multiplikator bestätigt, dass der Preis mindestens einen ATR vom jüngsten Bereich entfernt ist; andernfalls wird eine Festbox in Preisschritten verwendet. Momentum-Schwellenwerte (0,3%) unterdrücken energiearme Ausbrüche.
