# XROC2 VG Zeitfilter-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie repliziert den MetaTrader Expert Advisor **Exp_XROC2_VG_Tm** unter Verwendung der StockSharp High-Level-API. Sie erstellt zwei geglättete Rate-of-Change-Kurven (ROC) und eröffnet konträre Trades, wenn die schnellere Kurve die langsamere kreuzt. Ein Handelsfilter und optionale Schutzziele reproduzieren die ursprünglichen Money-Management-Einstellungen.

## Handelslogik

- Zwei ROC-Werte werden aus dem Schlusskurs mit unabhängigen Lookback-Werten berechnet.
- Jeder ROC-Stream wird mit einer konfigurierbaren Moving-Average-Methode geglättet.
- Signale werden auf einem verschobenen Barindex ausgewertet, was das ursprüngliche `SignalBar`-Verhalten nachahmt.
- Wenn die schnelle Linie auf dem vorherigen Bar über der langsamen lag, aber auf dem Signalbar darunter fällt, schließt die Strategie jede Short-Position und kann eine Long-Position eröffnen.
- Wenn die schnelle Linie auf dem vorherigen Bar unter der langsamen lag, aber auf dem Signalbar darüber steigt, schließt die Strategie jede Long-Position und kann eine Short-Position eröffnen.
- Ein optionales Handelsfenster kann alle Positionen außerhalb der erlaubten Sitzung schließen, bevor neue Trades platziert werden.

Die Orderseite wechselt erst, nachdem die vorherige Position vollständig geschlossen ist, was die MetaTrader-Trade-Algorithmen imitiert.

## Indikatoren

- **Schneller ROC** – Momentum, Prozent oder Verhältnis der Preisänderung über `RocPeriod1` Bars, geglättet mit `SmoothMethod1` und Länge `SmoothLength1`.
- **Langsamer ROC** – Gleiche Berechnung über `RocPeriod2` Bars, geglättet mit `SmoothMethod2` und Länge `SmoothLength2`.
- Unterstützte Glättungsmethoden: Einfache, Exponentielle, Geglättete (RMA) und Gewichtete gleitende Durchschnitte. Die originalen JJMA/VIDYA/AMA-Optionen werden durch exponentielle Glättung approximiert.

## Risikomanagement

- `StopLoss` und `TakeProfit` geben optionale Exits mit fester Distanz in absoluten Preiseinheiten an. Wenn einer der Schwellenwerte erreicht wird, wird die Position sofort geschlossen.
- `OrderVolume` definiert die Größe aller neuen Positionen.
- Indikatorbasierte Exits können Positionen auch dann schließen, wenn die Schutzziele deaktiviert sind.

## Sitzungsfilter

- `UseTimeFilter` schaltet das Tageszeit-Fenster ein/aus.
- `StartTime` / `EndTime` geben die Sitzungsgrenzen an. Wenn das Intervall um Mitternacht herum liegt, wird das Fenster als zwei Segmente behandelt, genau wie in der MQL-Version.
- Wenn eine Position noch offen ist, wenn das Fenster schließt, wird sie zum Marktpreis liquidiert, bevor die Strategie neue Einstiege auswertet.

## Parameter

| Parameter | Beschreibung |
| --- | --- |
| `CandleType` | Kerzendatentyp für Berechnungen (Standard: 4-Stunden-Kerzen). |
| `RocPeriod1`, `RocPeriod2` | Lookback-Längen für den schnellen und langsamen ROC-Stream. |
| `SmoothLength1`, `SmoothLength2` | Glättungslängen für jeden Stream. |
| `SmoothMethod1`, `SmoothMethod2` | Moving-Average-Typen, die auf die ROC-Outputs angewendet werden. |
| `RocType` | ROC-Berechnungsformel: Momentum, prozentuale Änderung oder Verhältnis. |
| `SignalShift` | Anzahl der abgeschlossenen Bars zurück, die zum Lesen der Signalwerte verwendet werden. |
| `AllowBuyOpen`, `AllowSellOpen` | Long-/Short-Positionen öffnen aktivieren oder deaktivieren. |
| `AllowBuyClose`, `AllowSellClose` | Indikatorbasierte Exits für Long-/Short-Positionen aktivieren oder deaktivieren. |
| `UseTimeFilter` | Aktiviert das Handelssitzungsfenster. |
| `StartTime`, `EndTime` | Sitzungsstart- und -endzeiten. |
| `OrderVolume` | Volumen für jeden neuen Trade. |
| `StopLoss`, `TakeProfit` | Optionale absolute Abstände für Schutz-Exits. |

## Implementierungshinweise

- Die Strategie hält kurze Historien von Preisen und geglätteten Werten, anstatt Indikator-Buffer zu verwenden, was den ursprünglichen `SignalBar`-Offset reproduziert, ohne sich auf `GetValue` zu verlassen.
- JJMA, VIDYA und AMA-Glättung aus dem MQL-Indikator werden auf exponentielle Glättung gemappt, um im Standard-Indikatorset von StockSharp zu bleiben.
- Alle Kommentare im Code sind auf Englisch und der Namespace folgt den Repository-Richtlinien.
