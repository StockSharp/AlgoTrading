# EMA-Kreuzungssignal-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie handelt auf die Kreuzung zweier exponentieller gleitender Durchschnitte (EMA). Ein schnellerer EMA und ein langsamerer EMA werden aus der gewählten Kerzenserie berechnet. Wenn der schnelle EMA den langsamen EMA von unten nach oben kreuzt, kann die Strategie eine bestehende Short-Position schließen und optional eine Long-Position eröffnen. Wenn der schnelle EMA den langsamen EMA von oben nach unten kreuzt, kann sie eine Long-Position schließen und optional eine Short-Position eröffnen.

Zum Risikomanagement erlaubt die Strategie das Setzen von Take-Profit- und Stop-Loss-Orders nach dem Eröffnen einer neuen Position. Beide Abstände werden in Ticks angegeben. Diese Schutzorders werden bei jedem neuen Einstieg storniert und neu erstellt.

Die Strategie bietet separate Schalter zum Aktivieren oder Deaktivieren von Long- und Short-Einstiegen sowie zum unabhängigen Schließen von Long- und Short-Positionen beim entgegengesetzten Signal. Alle Berechnungen verwenden nur abgeschlossene Kerzen.

## Parameter
- **Fast Period** – Länge des schnellen EMA.
- **Slow Period** – Länge des langsamen EMA.
- **Candle Type** – Zeitrahmen der für Berechnungen verwendeten Kerzen.
- **Allow Buy Open** – Long öffnen, wenn der schnelle EMA den langsamen EMA von unten nach oben kreuzt.
- **Allow Sell Open** – Short öffnen, wenn der schnelle EMA den langsamen EMA von oben nach unten kreuzt.
- **Allow Buy Close** – Long schließen, wenn der schnelle EMA den langsamen EMA von oben nach unten kreuzt.
- **Allow Sell Close** – Short schließen, wenn der schnelle EMA den langsamen EMA von unten nach oben kreuzt.
- **Take Profit Ticks** – Take-Profit-Abstand in Ticks vom Einstiegspreis.
- **Stop Loss Ticks** – Stop-Loss-Abstand in Ticks vom Einstiegspreis.
