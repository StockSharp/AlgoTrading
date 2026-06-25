# Strategie für durchschnittliche Kerzenveränderung
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie konvertiert vom MetaTrader-Experten `Exp_AverageChangeCandle`. Sie recreiert die ursprüngliche Logik in StockSharp, indem sie Kerzen-Ratios relativ zu einem dynamischen Basis-gleitenden Durchschnitt glättet und auf bullische/bärische Farbübergänge reagiert.

## Kernidee

1. Einen Basis-gleitenden Durchschnitt (`MaMethod1`, `Length1`) über den ausgewählten angewandten Preis berechnen.
2. Den aktuellen Kerzen-Eröffnungs- und Schlusskurs als Ratios zur Basis ausdrücken und sie zur Potenz `Power` erheben.
3. Die transformierten Eröffnungs- und Schlusswerte mit einem sekundären gleitenden Durchschnitt (`MaMethod2`, `Length2`) glätten.
4. Die Kerzenfarbe klassifizieren: bullisch wenn geglätteter Schluss &gt; geglättete Eröffnung, bärisch wenn geglätteter Schluss &lt; geglättete Eröffnung.
5. Handelssignale generieren, wenn sich die Farbe nach der konfigurierten `SignalBar`-Verzögerung ändert.

Nur abgeschlossene Kerzen werden verarbeitet. Die Strategie öffnet Marktpositionen in Richtung der neuen Farbe und schließt optional die entgegengesetzte Seite.

## Parameter

| Parameter | Standard | Beschreibung |
|-----------|---------|-------------|
| `OrderVolume` | `1` | Volumen beim Öffnen einer neuen Position. |
| `MaMethod1` | `Lwma` | Glättung des Basis-Ratios (Teilmenge von SMA/EMA/SMMA/LWMA/JJMA/AMA). Nicht unterstützte Typen fallen auf EMA zurück. |
| `Length1` | `12` | Zeitraum des Basis-gleitenden Durchschnitts. |
| `Phase1` | `15` | Jurik-Phasenparameter für die Basis (aus Kompatibilitätsgründen beibehalten). |
| `PriceSource` | `Median` | Angewandter Preis vor der Basis-Berechnung. |
| `MaMethod2` | `Jjma` | Glättung der transformierten Ratios. |
| `Length2` | `5` | Zeitraum des Signal-gleitenden Durchschnitts. |
| `Phase2` | `100` | Jurik-Phasenparameter für die Signalglättung. |
| `Power` | `5` | Exponent beim Erheben der Eröffnungs-/Schlusskurs-Ratios. |
| `SignalBar` | `1` | Anzahl der geschlossenen Balken vor einer Reaktion auf eine Farbänderung. |
| `BuyOpenEnabled` | `true` | Long-Positionen öffnen erlauben. |
| `SellOpenEnabled` | `true` | Short-Positionen öffnen erlauben. |
| `BuyCloseEnabled` | `true` | Longs schließen wenn ein bärisches Signal erscheint. |
| `SellCloseEnabled` | `true` | Shorts schließen wenn ein bullisches Signal erscheint. |
| `StopLossPoints` | `0` | Absoluter Stop-Loss-Abstand. `0` deaktiviert den Stop. |
| `TakeProfitPoints` | `0` | Absoluter Take-Profit-Abstand. `0` deaktiviert das Ziel. |
| `CandleType` | `H4`-Zeitrahmen | Kerzenserie, die von der Strategie verarbeitet wird. |

## Handelsregeln

- **Bullischer Übergang** (`color` ändert sich auf 2): aktive Shorts schließen (wenn erlaubt) und eine Long-Position öffnen wenn `Position <= 0` und `BuyOpenEnabled` wahr ist.
- **Bärischer Übergang** (`color` ändert sich auf 0): aktive Longs schließen (wenn erlaubt) und eine Short-Position öffnen wenn `Position >= 0` und `SellOpenEnabled` wahr ist.
- Farbe 1 (neutral) löst keine Trades aus.
- Signale werden anhand der Balken ausgewertet, die `SignalBar` Schritte hinter der zuletzt abgeschlossenen Kerze liegen, um das ursprüngliche MetaTrader-Timing nachzuahmen.

## Risikomanagement

`StopLossPoints` und `TakeProfitPoints` konfigurieren `StartProtection` mit absoluten Abständen. Wenn einer der Werte null ist, wird der jeweilige Schutz deaktiviert.

## Hinweise

- Nur die in StockSharp verfügbaren Glättungsmethoden werden direkt implementiert. JurX, ParMA, T3 und VIDYA aus dem Originalcode werden als funktionaler Fallback zu EMA zugeordnet.
- Phasenparameter werden aus Kompatibilitätsgründen beibehalten, betreffen aber nur Jurik/Kaufman-basierte Durchschnitte.
- Die Strategie verwendet Marktaufträge, genau wie der ursprüngliche Expertenberater. Die Slippage-Verwaltung der MQL-Version wird nicht reproduziert, da StockSharp die Ausführung über Konnektoren handhabt.
