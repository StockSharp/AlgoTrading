# Demo GPT - Intraday-Scalping-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Geht long nach zwei aufeinanderfolgenden grünen Kerzen, wenn die zweite Kerze über dem Schlusskurs der ersten eröffnet und das Volumen den 20-Bar-Durchschnitt überschreitet. Schließt die Position bei zwei aufeinanderfolgenden roten Kerzen, bei denen die zweite unter dem Schlusskurs der ersten eröffnet und dieselbe Volumenbedingung erfüllt ist. Berechnet EMA(20), VWAP und Camarilla-Levels zur Referenz.

### Tatsächliche Regeln und Standardwerte

Standard: `EmaLength=20`, `VolumeLookback=20`, `CandleType=1 Minute`. Nach **20 vorangegangenen abgeschlossenen Kerzen** gilt `volumeHigh = aktuelles TotalVolume > Durchschnitt der letzten 20 Volumenwerte`, die aktuelle Kerze gehört nicht zu ihrem eigenen Mittelwert. Aus Flat **nur Long kaufen**, wenn beide Kerzen grün sind (`Close > Open`), die aktuelle **`Open > vorherige Close`** aufweist und das Volumen bestätigt. Den **gesamten tatsächlichen Long-Rest** schließen, wenn beide rot sind (`Close < Open`), `aktuelle Open < vorherige Close` und hohes Volumen vorliegt. Ohne strikte Eröffnungslücke oder Volumenbestätigung **kein Signal**, niemals Short. Referenzwerte ohne Signalwirkung: EMA20 über Schlusskurse, täglich zurückgesetzter VWAP `sum(typischer Preis × Volumen)/sum(Volumen)`; voriger Camarilla `H3/L3 = voriger Schluss ± 1.1 × (voriges Hoch − Tief) / 4`. Kein zusätzlicher Stop-Loss.

