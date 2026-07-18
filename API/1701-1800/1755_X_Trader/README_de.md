# X Trader-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie implementiert ein konträres System aus gleitenden Durchschnitts-Kreuzungen, das ursprünglich in MQL als **X trader** geschrieben wurde.
Es verwendet zwei einfache gleitende Durchschnitte und eröffnet Positionen entgegen der Kreuzungsrichtung. Das Risiko wird
mithilfe fester Take-Profit- und Stop-Loss-Werte in absoluten Punkten über `StartProtection` verwaltet.

## Funktionsweise

1. Kerzdaten des angegebenen Zeitrahmens abonnieren.
2. Zwei gleitende Durchschnitte mit konfigurierbaren Perioden berechnen.
3. Die letzten zwei Werte jedes Durchschnitts verfolgen, um eine Kreuzung zu erkennen.
4. Wenn der schnelle Durchschnitt den langsamen von unten kreuzt und zwei Balken lang darüber bleibt, während er vor zwei Balken darunter war,
   wird eine **Short**-Position eröffnet.
5. Wenn der schnelle Durchschnitt den langsamen von oben kreuzt und zwei Balken lang darunter bleibt, während er vor zwei Balken darüber war,
   wird eine **Long**-Position eröffnet.
6. Es kann jeweils nur eine Position offen sein. Der Schutz schließt Trades automatisch, wenn sich der Preis um den
   konfigurierten Take-Profit- oder Stop-Loss-Betrag bewegt.

## Parameter

- `CandleType` – zu verwendende Kerzenserie.
- `Ma1Period` – Periode des ersten gleitenden Durchschnitts.
- `Ma2Period` – Periode des zweiten gleitenden Durchschnitts.
- `TakeProfitPoints` – Gewinnziel in Preispunkten.
- `StopLossPoints` – Verlustlimit in Preispunkten.

## Indikator

- `SimpleMovingAverage` – zweimal mit unterschiedlichen Perioden verwendet.

## Risikomanagement

`StartProtection` wird in `OnStarted` aktiviert und wendet die Take-Profit- und Stop-Loss-Werte auf alle Positionen an.
