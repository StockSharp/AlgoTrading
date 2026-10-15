# ProffessorV3-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht

Diese Strategie ist eine vollständige Konvertierung des MetaTrader-Experten *ProffessorV3* in die
StockSharp High-Level-API. Sie behält das ursprüngliche Konzept der Kombination von ADX-Regimefilterung
mit einem Raster aus Schutz- und Durchschnittsorders bei.

- **Indikator**: 14-Perioden Average Directional Index (ADX) mit +DI/-DI-Werten.
- **Modi**: flaches Regime (ADX unter Schwellenwert) und Trendregime (ADX über Schwellenwert).
- **Orders**: eröffnet eine Marktposition und umgibt den Kurs mit ausstehenden Orders
  zum Hedgen, Pyramidisieren oder Mean-Reverting.
- **Ausstieg**: schließt alle Positionen und ausstehende Orders, wenn das konfigurierte Gewinn-
  oder Verlustniveau erreicht wird.
- **Zeitplan**: handelt nur innerhalb des ausgewählten Stundenbereichs.

## Handelslogik

### Regimeerkennung
1. Den konfigurierten Kerzentyp abonnieren und ADX-Werte berechnen.
2. Das ADX-Signal um die konfigurierte Anzahl geschlossener Kerzen (`BarOffset`) verzögern,
   um die ursprüngliche Verwendung von `CopyBuffer(handle, shift)` zu replizieren.
3. Wenn keine Position offen ist, die letzten verzögerten ADX-Werte auswerten:
   - *Flat bullisch*: `ADX < AdxFlatLevel` und `+DI > -DI`.
   - *Flat bärisch*: `ADX < AdxFlatLevel` und `+DI < -DI`.
   - *Trend bullisch*: `ADX ≥ AdxFlatLevel` und `+DI > -DI`.
   - *Trend bärisch*: `ADX ≥ AdxFlatLevel` und `+DI < -DI`.

### Order-Platzierung
Für jeden Modus eröffnet die Strategie eine Marktposition mit dem Basisvolumen und
platziert dann ein symmetrisches Raster um den aktuellen Kurs. Rasterabstände werden
in „Punkten" ausgedrückt, genau wie im MQL-Code, und automatisch durch den
Instrument-Preisschritt skaliert.

- **Flat bullisch**: Long-Markteinstieg, schützender Sell-Stop unter dem Bid, Buy-Limits
  unterhalb des Ask und Sell-Limits über dem Bid, um Schwingungen zu erfassen.
- **Flat bärisch**: Short-Markteinstieg, schützender Buy-Stop über dem Ask, Buy-Limits
  bei Rücksetzern und Sell-Limits höher, um Shorts neu zu laden.
- **Trend bullisch**: Long-Markteinstieg, Sell-Stops für Hedging und Buy-Stops
  für Ausbruch-Pyramidisierung.
- **Trend bärisch**: Short-Markteinstieg, Sell-Stops, um dem Trend zu folgen, und
  Buy-Stops, um Umkehrungen zu begrenzen.

Der Rasterabstand wird mit derselben Formel wie das Original berechnet: Jedes Level
addiert `GridStep + GridDeltaIncrement * level / 2`. Das Volumen für jede ausstehende Order
wird mit `LotMultiplier` und `LotAddition` angepasst, dann auf den Exchange-Volumenschritt
und -Grenzen normiert.

### Exit-Management
- Der unrealisierte Gewinn wird aus dem Strategiepositionen-Durchschnittspreis und dem
  letzten Kerzenschlusskurs berechnet.
- Wenn der Gewinn `ProfitTarget` übersteigt oder unter `LossLimit` fällt (wenn
  letzteres ungleich null ist), schließt die Strategie die Nettoposition und storniert alle
  ausstehenden Orders.
- Der Handel wird außerhalb des Intervalls `[StartHour, EndHour)` übersprungen, was dem
  ursprünglichen `Time()`-Helfer entspricht.

## Implementierungshinweise

- Bid/Ask-Preise für ausstehende Orders werden aus dem letzten Kerzenschlusskurs plus/minus
  der halben Preisschrittgröße angenähert. Dies spiegelt die tick-basierte Logik in einem
  kerzengetriebenen Umfeld wider.
- Punktwerte werden durch den Symbol-Preisschritt skaliert und für drei- und fünfstellige
  Kurse angepasst, genau wie die MQL-Variable `m_adjusted_point`.
- Volumen- und Preisnormalisierung respektiert den Symbol-Schritt sowie Mindest- und
  Maximalbeschränkungen vor dem Senden von Orders.
- Die Strategie verarbeitet nur abgeschlossene Kerzen, um verfrühte Signale zu vermeiden.

## Parameter

| Parameter | Beschreibung |
|-----------|--------------|
| `Volume` | Basisvolumen der Marktorder. |
| `LotMultiplier` | Multiplikator, der auf das Volume jeder ausstehenden Order angewendet wird. |
| `LotAddition` | Zusätzliches Volumen, das nach dem Multiplikator zu ausstehenden Orders hinzugefügt wird. |
| `MaxLevels` | Maximale Anzahl von Rasterebenen pro Seite. |
| `GridDeltaIncrement` | Inkrement, das dem Rasterabstand hinzugefügt wird, wenn Ebenen tiefer werden (Punkte). |
| `GridInitialOffset` | Abstand zur ersten Schutz-Order (Punkte). |
| `GridStep` | Basisabstand zwischen aufeinanderfolgenden Ebenen (Punkte). |
| `ProfitTarget` | Unrealisierter Gewinnstand, der das Schließen aller Positionen auslöst. |
| `LossLimit` | Unrealisierter Verluststand, der das Schließen aller Positionen auslöst (0 deaktiviert). |
| `AdxFlatLevel` | ADX-Schwellenwert, der das flache vom Trendregime trennt. |
| `BarOffset` | Anzahl geschlossener Kerzen zur Verzögerung der ADX-Werte. |
| `StartHour` | Stunde, zu der das Handelsfenster öffnet (UTC). |
| `EndHour` | Stunde, zu der das Handelsfenster schließt (UTC). |
| `CandleType` | Kerzenserie, die für Berechnungen verwendet wird. |
