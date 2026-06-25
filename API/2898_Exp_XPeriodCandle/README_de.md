# Exp XPeriodCandle-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie ist ein StockSharp-Port des MQL5-Expertenberaters `Exp_XPeriodCandle`. Sie rekonstruiert den benutzerdefinierten XPeriodCandle-Indikator mit High-Level-API-Komponenten und verwendet Kerzenfarbübergänge zum Öffnen und Schließen von Positionen.

## Konzept

* Glättung von Eröffnung, Hoch, Tief und Schluss jeder abgeschlossenen Kerze mit einer konfigurierbaren gleitenden Durchschnittsnäherung.
* Verfolgung der resultierenden "Kerzenfarbe" (bullisch, wenn der geglättete Schluss über der geglätteten Eröffnung liegt, sonst bärisch).
* Verwendung der Farbe der letzten zwei abgeschlossenen Kerzen (konfigurierbarer Versatz) zur Erkennung von Umkehrungen und Ausgabe von Handelssignalen.
* Optionales Schließen von Gegenpositionen bei Erscheinen eines neuen Signals und Anwendung von schützenden Stop-Loss/Take-Profit-Niveaus, ausgedrückt in Preispunkten.

## Implementierungsdetails

* Direkt unterstützte Glättungstypen: Simple, Exponential, Smoothed (RMA) und Linear Weighted. Alle anderen Optionen werden mit einem exponentiellen Glätter approximiert, da StockSharp keine direkten Äquivalente zu JJMA/JurX/Parabolic/T3/VIDYA/AMA enthält. Im Code dokumentiert, um das Verhalten transparent zu halten.
* Gleitende Warteschlangen speichern die letzten `Period` geglätteten Hochs und Tiefs, um den Preisbereich konsistent mit dem ursprünglichen Indikator zu halten.
* Die Strategie wartet, bis genug Historie verfügbar ist, bevor `BuyMarket`/`SellMarket` aufgerufen wird, und markiert sich als geformt, um mit StockSharp-Backtesting-Filtern zu arbeiten.
* Optionale Slippage-, Stop-Loss- und Take-Profit-Konvertierungen basieren auf dem Kursschrittpreis des Wertpapiers. Wenn der Schritt unbekannt ist, werden die rohen Punktwerte verwendet.

## Parameter

| Parameter | Beschreibung |
|-----------|-------------|
| `CandleType` | Zeitrahmen der verarbeiteten Kerzen. |
| `Period` | Tiefe des Glättungsfensters (entspricht dem Indikatorperiodenwert). |
| `SmoothingMethods` | Gleitende Durchschnittsnäherung für alle OHLC-Reihen. Nicht unterstützte Methoden fallen auf EMA zurück. |
| `SmoothingLength` | Längenparameter für den Glätter. |
| `SmoothingPhase` | Zusätzliche Phaseneingabe (für Vollständigkeit beibehalten; nur in der ursprünglichen MQL-JJMA-Familie aktiv). |
| `SignalBar` | Welche abgeschlossene Kerze ausgewertet werden soll (1 = vorherige Kerze, repliziert den MQL-Expertenstandard). |
| `EnableLongEntry` / `EnableShortEntry` | Öffnen von Positionen in der entsprechenden Richtung erlauben. |
| `EnableLongExit` / `EnableShortExit` | Vorhandene Positionen schließen, wenn ein entgegengesetztes Signal erkannt wird. |
| `StopLossPoints` / `TakeProfitPoints` | Schützende Ausstiege in Preispunkten. Auf null setzen zum Deaktivieren. |
| `SlippagePoints` | Erlaubter Slippage in Preispunkten für Marktaufträge. |

## Handelsregeln

1. Die letzte abgeschlossene Kerze glätten und ihre Farbe zum rollierenden Verlauf hinzufügen.
2. Wenn `SignalBar`- und ältere Farben vorhanden sind:
   * Wenn die ältere Kerze bullisch war (Farbe < 1) und die neuere Kerze nicht bullisch ist (Farbe > 0), Long-Position eröffnen (wenn erlaubt) und optional Shorts schließen.
   * Wenn die ältere Kerze bärisch war (Farbe > 1) und die neuere Kerze nicht bärisch ist (Farbe < 2), Short-Position eröffnen (wenn erlaubt) und optional Longs schließen.
3. Die Positionsgröße folgt der `Volume`-Einstellung der Strategie; gegenläufiges Engagement wird vor Umkehrung aufgelöst.
4. Das Risikomanagement wird von `StartProtection` mit den angegebenen Punktabständen übernommen.

## Hinweise

* Der ursprüngliche Experte verwendet das proprietäre `SmoothAlgorithms.mqh`. Da StockSharp keine direkten JJMA/JurX/T3-Implementierungen hat, approximiert die C#-Konvertierung diese Modi mit exponentieller Glättung. Dieses Verhalten ist in Code-Kommentaren und der README dokumentiert, damit Optimierer die Parameter bei Bedarf anpassen können.
* Eingaben und Standardwerte spiegeln die MQL-Version wider und ermöglichen ähnliche Optimierungsbereiche.
