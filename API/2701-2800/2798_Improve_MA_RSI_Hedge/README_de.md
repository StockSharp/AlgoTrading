# Improve MA & RSI Hedge Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie portiert den ursprünglichen MetaTrader-Expert "Improve" unter Verwendung der High-Level-API nach StockSharp. Sie handelt gleichzeitig zwei Instrumente: das für die Strategie ausgewählte Hauptsymbol und ein Hedge-Symbol. Die Handelsrichtung wird durch die Beziehung zwischen zwei geglätteten gleitenden Durchschnitten am Hauptinstrument und dem Relative Strength Index (RSI) definiert. Das Hedge-Bein spiegelt die Richtung des Hauptbeins wider und schafft eine gepaarte Exposition, die darauf abzielt, von synchronisierten Momentum-Bewegungen zu profitieren und gleichzeitig das Einzelinstrument-Risiko zu begrenzen.

## Strategielogik

- Zwei Geglättete Gleitende Durchschnitte (SMMA) auf dem primären Symbol mit konfigurierbaren schnellen und langsamen Perioden berechnen.
- RSI auf denselben Kerzen berechnen und überverkaufte/überkaufte Schwellenwerte überwachen.
- **Long** auf beiden Instrumenten eingehen, wenn die langsame SMMA über der schnellen SMMA liegt und RSI beim oder unter dem Überverkauft-Schwellenwert ist.
- **Short** auf beiden Instrumenten eingehen, wenn die langsame SMMA unter der schnellen SMMA liegt und RSI beim oder über dem Überkauft-Schwellenwert ist.
- Positionen bleiben offen, bis der kombinierte offene Gewinn beider Beine das konfigurierte Geldziel übersteigt, woraufhin die Strategie beide Seiten liquidiert.

Der Algorithmus verfolgt die aktuellsten Schlusskurse jedes Instruments. Der kombinierte Gewinn wird aus der Differenz zwischen dem aktuellen Schluss und dem gespeicherten Einstiegspreis jedes Beins geschätzt. Da kein Stop-Loss angewendet wird, können Positionen für längere Zeiträume offen bleiben, wenn der Preis das Gewinnziel nicht erreicht.

## Parameter

| Parameter | Beschreibung |
| --- | --- |
| **Volume** | Ordermenge für beide Instrumente, das primäre und das Hedge-Instrument. |
| **Profit Target** | Gemeinsames Geldziel für beide Beine; wenn erreicht, schließt die Strategie jede offene Position. |
| **Hedge Security** | Sekundäres Instrument, das zusammen mit dem primären Wertpapier gehandelt wird. |
| **Fast MA** | Periode des schnellen Geglätteten Gleitenden Durchschnitts (Standard 8). |
| **Slow MA** | Periode des langsamen Geglätteten Gleitenden Durchschnitts (Standard 21). Muss größer als die schnelle MA-Periode sein. |
| **RSI Period** | Für die RSI-Berechnung verwendete Länge (Standard 21). |
| **Oversold** | RSI-Niveau, das Long-Einstiege zusammen mit der MA-Bedingung auslöst (Standard 30). |
| **Overbought** | RSI-Niveau, das Short-Einstiege zusammen mit der MA-Bedingung auslöst (Standard 70). |
| **Candle Type** | Zeitrahmen für Berechnungen; standardmäßig 1-Stunden-Kerzen, kann aber angepasst werden. |

## Indikatoren

- **Geglätteter Gleitender Durchschnitt (SMMA)** – wird zweimal verwendet, um die schnellen und langsamen Trendkomponenten zu definieren.
- **Relative Strength Index (RSI)** – bestimmt Überverkauft-/Überkauft-Bedingungen zur Bestätigung.

## Einstiegs- und Ausstiegsregeln

1. **Long-Einstieg**
   - Langsame SMMA &gt; Schnelle SMMA am primären Symbol.
   - RSI ≤ Überverkauft.
   - Beide Beine werden mit Marktorders in dieselbe Richtung eröffnet (Kauf/Kauf).
2. **Short-Einstieg**
   - Langsame SMMA &lt; Schnelle SMMA am primären Symbol.
   - RSI ≥ Überkauft.
   - Beide Beine werden mit Marktorders in dieselbe Richtung eröffnet (Verkauf/Verkauf).
3. **Ausstieg**
   - Wenn `(primärer Gewinn + Hedge-Gewinn) ≥ Profit Target`, schließt die Strategie beide Positionen mit Marktorders.
   - Keine zusätzliche Stop-Loss- oder Trailing-Logik wird angewendet; Risikomanagement sollte extern hinzugefügt werden, wenn erforderlich.

## Verwendungshinweise

- Sicherstellen, dass sowohl das primäre Wertpapier als auch das Hedge-Wertpapier vor dem Starten der Strategie zugewiesen sind; andernfalls wird eine Ausnahme ausgelöst.
- Die kombinierte Gewinnschätzung basiert auf Kerzenschlusskursen. Slippage und Ausführungsunterschiede zwischen den beiden Beinen können den tatsächlich realisierten Gewinn beeinflussen.
- Da die Strategie beide Beine gleichzeitig öffnet, eignet sie sich für korrelierte Instrumente (zum Beispiel Währungspaare oder verwandte Futures), bei denen ein gleichzeitiges Bewegen erwartet wird.
- Erwägen Sie, Portfolio-Level-Risikokontrollen hinzuzufügen, wenn Sie live handeln, da der ursprüngliche Algorithmus nur das virtuelle Gewinnziel für Ausstiege verwendet.
