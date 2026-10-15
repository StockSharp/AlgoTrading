# XDPO-Histogramm-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht

Die XDPO-Histogramm-Strategie adaptiert den ursprünglichen MQL5-Experten *Exp_XDPO_Histogram*. Sie erstellt einen doppelt geglätteten detrendierten Preisoszillator (XDPO) aus Schlusskursen. Der Oszillator wird ermittelt, indem ein gleitender Durchschnitt vom Preis subtrahiert und diese Differenz mit einem zweiten gleitenden Durchschnitt geglättet wird. Die Histogrammdynamik liefert Signale für das Öffnen und Schließen von Positionen.

## Handelslogik

- Wenn der Oszillator nach oben dreht, werden alle Short-Positionen geschlossen. Wenn der aktuelle Oszillatorwert den vorherigen übersteigt, wird eine neue Long-Position eröffnet.
- Wenn der Oszillator nach unten dreht, werden alle Long-Positionen geschlossen. Wenn der aktuelle Oszillatorwert unter dem vorherigen liegt, wird eine neue Short-Position eröffnet.
- Berechnungen werden nur auf abgeschlossenen Kerzen durchgeführt.

## Parameter

- `FirstMaLength` – Länge des ersten gleitenden Durchschnitts, der auf den Preis angewendet wird.
- `SecondMaLength` – Länge des gleitenden Durchschnitts, der auf die Differenz zwischen Preis und erstem MA angewendet wird.
- `CandleType` – Kerzentyp, der für alle Berechnungen verwendet wird.

## Hinweise

- Gleitende Durchschnitte werden mit `SimpleMovingAverage`-Indikatoren implementiert.
- Die Strategie verwendet Marktorders (`BuyMarket` und `SellMarket`) und schließt entgegengesetzte Positionen, bevor neue eröffnet werden.
