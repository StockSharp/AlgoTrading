# N Candles Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die N Candles Strategie repliziert den MQL-Expertenberater, der einen Trade eingeht, wenn eine konfigurierbare Anzahl aufeinanderfolgender Kerzen dieselbe Richtung hat. Sobald die letzten `N` abgeschlossenen Kerzen alle bullisch sind, sendet die Strategie eine Kauf-Marktorder. Wenn alle bärisch sind, sendet sie eine Verkauf-Marktorder. Es ist keine Ausstiegslogik enthalten; die Position muss extern oder durch zusätzliche Strategien verwaltet werden.

## Überblick

- **Marktregime**: Funktioniert am besten in Märkten mit kurzen Momentum-Ausbrüchen.
- **Instrumente**: Jedes Instrument, das Endloshandel unterstützt (FX, Futures, Krypto).
- **Zeitrahmen**: Konfigurierbar; Standard sind 1-Stunden-Kerzen.
- **Ordertypen**: Marktorders ohne Schutz-Stops oder Ziele.

## Funktionsweise

1. Bei jeder abgeschlossenen Kerze bewertet die Strategie die letzten `N` Kerzen.
2. Wenn jede Kerze in diesem Fenster bullisch ist, gibt sie eine Kauf-Marktorder mit dem konfigurierten Volumen aus.
3. Wenn jede Kerze bärisch ist, gibt sie eine Verkauf-Marktorder aus.
4. Doji-Kerzen (gleiche Eröffnung und Schluss) setzen den Zähler zurück und unterdrücken den Handel, bis eine neue Serie beginnt.
5. Die Strategie verwaltet offene Positionen nicht; wiederholte Signale fügen der bestehenden Richtung auf Netting-Konten hinzu.

## Parameter

- **Consecutive Candles**: Anzahl identischer Kerzen, die vor der Orderplatzierung erforderlich sind.
- **Volume**: Marktordergröße, die bei jedem Signal gesendet wird.
- **Candle Type**: Für die Serienerkennung verwendete Kerzenserie (Zeitrahmen oder benutzerdefinierter Kerzentyp).

## Verwendungshinweise

- Da der Strategie Stops oder Ausstiege fehlen, kombinieren Sie sie mit manueller Verwaltung, Schutzstrategien oder Portfolio-Risikokontrollen.
- In hochvolatilen Märkten erwägen Sie, die Kerzenanzahl oder den Zeitrahmen zu reduzieren, um schnellere Serien zu erfassen.
- Übermäßige aufeinanderfolgende Serien können große Positionen ansammeln; überwachen Sie Leverage und Kontolimits.
