# Color Fisher M11-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Color Fisher M11 ist eine Trendfolge-Strategie, die den Exp_ColorFisher_m11-Expertenberater aus MetaTrader 5 nachbildet. Sie verwendet eine benutzerdefinierte Fisher-Transform-Variante, die Kerzen mit fünf Farbzuständen einfärbt, um extreme bullische und bärische Momentum-Phasen hervorzuheben. Signale werden um eine konfigurierbare Anzahl geschlossener Kerzen verzögert, um das Trading auf unvollständigen Daten zu vermeiden, während optionale Schalter das unabhängige Deaktivieren von Einstiegen oder Ausstiegen für jede Seite ermöglichen.

## Indikatorlogik
Die Strategie berechnet den Color-Fisher-Indikator in Echtzeit:

- Bestimmt das höchste Hoch und das tiefste Tief über das **Range Periods**-Fenster.
- Normalisiert den Mittelpreis der aktuellen Kerze innerhalb dieser Spanne und wendet **Price Smoothing** (EMA-Stil) zur Stabilisierung der Schwankungen an.
- Wendet den Fisher Transform mit einem zusätzlichen **Index Smoothing**-Faktor an, um den endgültigen Oszillatorwert zu erzeugen.
- Klassifiziert den Oszillator in fünf diskrete Farbbänder mithilfe der Schwellenwerte **High Level** und **Low Level**:
  - `0` – starker bullischer Impuls oberhalb des hohen Niveaus.
  - `1` – moderates bullisches Momentum zwischen null und dem hohen Niveau.
  - `2` – neutrale Zone um null.
  - `3` – moderates bärisches Momentum zwischen null und dem niedrigen Niveau.
  - `4` – starker bärischer Impuls unterhalb des niedrigen Niveaus.

Das Signal wird `Signal Bar` Kerzen zurück ausgewertet, um das Verhalten des Original-Expertenberaters nachzuahmen. Der vorherige Farbzustand wird ebenfalls verfolgt, um neue Übergänge in die extremen Bänder zu erkennen.

## Handelsregeln
- **Long-Einstieg** – erlaubt wenn `Enable Buy Entry` wahr ist, die verzögerte Farbe gleich `0` (stark bullisch) ist und die vorherige Farbe sich von `0` unterscheidet. Jedes Short-Exposure wird umgekehrt und die Position wird long.
- **Short-Einstieg** – erlaubt wenn `Enable Sell Entry` wahr ist, die verzögerte Farbe gleich `4` (stark bärisch) ist und die vorherige Farbe sich von `4` unterscheidet. Jedes Long-Exposure wird umgekehrt und die Position wird short.
- **Long-Ausstieg** – ausgelöst wenn `Enable Buy Exit` wahr ist und die verzögerte Farbe auf `3` oder `4` wechselt, was bärische Kontrolle signalisiert.
- **Short-Ausstieg** – ausgelöst wenn `Enable Sell Exit` wahr ist und die verzögerte Farbe auf `0` oder `1` wechselt, was bullische Kontrolle signalisiert.

Um mehrere Orders pro Signal zu verhindern, merkt sich die Strategie die nächste Balken-Schließzeit für jede Richtung und lehnt neue Einstiege ab, bis die nächste Kerze abgeschlossen ist.

## Risikomanagement
`Stop Loss (pts)` und `Take Profit (pts)` konvertieren die ursprünglichen Pip-Abstände in absolute Preisschritte unter Verwendung des Instrumenten-Preisschritts. Wenn ein positiver Abstand angegeben wird, werden Schutzorders über `StartProtection` aktiviert. Setzen Sie einen Wert auf null, um diesen Schutzmechanismus zu deaktivieren.

## Parameter
- **Range Periods** – Lookback-Länge für die Hoch-/Tief-Spanne des Fisher Transforms (Standard 10).
- **Price Smoothing** – Glättungsfaktor vor der Transformation, 0…0.99 (Standard 0.3).
- **Index Smoothing** – Glättungsfaktor nach der Transformation, 0…0.99 (Standard 0.3).
- **High Level / Low Level** – Schwellenwerte, die bullische und bärische Extreme definieren (Standard +1.01 und –1.01).
- **Signal Bar** – Anzahl der geschlossenen Kerzen zur Verzögerung der Signalauswertung (Standard 1).
- **Enable Buy Entry / Enable Sell Entry** – Schalter zum Öffnen neuer Long- oder Short-Trades.
- **Enable Buy Exit / Enable Sell Exit** – Schalter für indikatorgesteuerte Ausstiege.
- **Stop Loss (pts) / Take Profit (pts)** – Schutzabstände in Preisschritten ausgedrückt.
- **Candle Type** – Zeitrahmen für das Kerzen-Abonnement; Standard: 4-Stunden-Kerzen.

## Hinweise
- Die Strategie verwendet High-Level-StockSharp-Bindings (`SubscribeCandles().BindEx`) und speichert keine historischen Sammlungen über das minimale Farbhistorie hinaus, das für das verzögerte Signal benötigt wird.
- In dieser Version ist kein Python-Port enthalten, gemäß der Anforderungsspezifikation.
- Fügen Sie die Strategie einem Diagrammbereich hinzu, um sowohl den Preis als auch den berechneten Color-Fisher-Oszillator zu visualisieren.
