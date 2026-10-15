# Batman ATR Trailing-Stop-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie implementiert einen ATR-basierten Trailing-Stop-Ansatz, inspiriert vom ursprünglichen "Batman" Expert Advisor.
Sie verfolgt dynamische Unterstützungs- und Widerstandsniveaus, die aus dem **Average True Range (ATR)**-Indikator abgeleitet werden, und reagiert, wenn der Preis diese Niveaus kreuzt.

## Logik

1. ATR mit der konfigurierbaren Periode berechnen.
2. Unterstützung und Widerstand bestimmen:
   - `support = price - ATR * factor`
   - `resistance = price + ATR * factor`
3. Die nächstgelegene Unterstützung oder den nächstgelegenen Widerstand abhängig vom aktuellen Trend aufrechterhalten.
4. Wenn der Preis den Widerstand nach oben durchbricht, eine **Long**-Position eröffnen.
5. Wenn der Preis die Unterstützung nach unten durchbricht, eine **Short**-Position eröffnen.

Der Preis kann entweder der Schlusskurs oder der typische Preis `(High + Low + Close) / 3` sein.

## Parameter

| Name | Beschreibung |
|------|--------------|
| `ATR Period` | Periode des ATR-Indikators. |
| `ATR Factor` | Multiplikator, der auf den ATR-Wert angewendet wird, um die Stop-Linien zu erstellen. |
| `Use Typical Price` | Wenn aktiviert, wird `(High + Low + Close)/3` anstelle des Schlusskurses verwendet. |
| `Candle Type` | Kerzentyp für die Berechnungen. |

## Hinweise

- Die Strategie verwendet die High-Level-API mit `SubscribeCandles` und `Bind`.
- `StartProtection()` wird beim Start aufgerufen, um Positionssicherheit zu gewährleisten.
- Der Handel erfolgt nur bei abgeschlossenen Kerzen.
