# Exp Trading-Kanal-Index-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie ist ein StockSharp-Port des MQL5-Expert-Advisors `Exp_Trading_Channel_Index`. Sie folgt dem Trading Channel Index (TCI)-Oszillator, einem volatilitätsbereinigten Momentum-Indikator, der jede Bar entsprechend ihrer Position relativ zu zwei Kanalniveaus einfärbt. Die Strategie reagiert, wenn sich die Farbe einer historischen Bar ändert, und ahmt das Verhalten des ursprünglichen Expert-Advisors nach.

Die Implementierung abonniert eine konfigurierbare Kerzenserie (Standard: H4) und verarbeitet nur abgeschlossene Kerzen. Alle Handelsentscheidungen werden beim Open der nächsten Bar nach einem Farbwechsel getroffen, genau wie im Quellskript.

## Trading Channel Index-Indikator
Der TCI wird in drei Stufen berechnet:

1. **Primäre Glättung** der gewählten Preisquelle über einen konfigurierbaren gleitenden Durchschnitt (SMA, EMA, SMMA, WMA oder Jurik). Dies erzeugt den Basiswert `XMA`.
2. **Volatilitätsschätzung** durch Glättung der absoluten Abweichung zwischen Preis und Basislinie.
3. **Normalisierung** der Abweichung durch den konfigurierten Koeffizienten und eine zweite Glättungsstufe. Der resultierende Wert wird mit den Schwellenwerten `HighLevel` und `LowLevel` verglichen, um einen von fünf Farbcodes zuzuweisen:
   - `0` (Limette) – Wert liegt über `HighLevel`.
   - `1` (Blaugrün) – Wert ist positiv, aber unter `HighLevel`.
   - `2` (Grau) – Wert ist nahe null.
   - `3` (Orange) – Wert ist negativ, aber über `LowLevel`.
   - `4` (Gold) – Wert liegt unter `LowLevel`.

Die StockSharp-Version verwendet native Indikatorklassen für die gleitenden Durchschnitte. Jurik MA berücksichtigt die `Phase`-Eingabe, während andere Methoden sie ignorieren, was dem ursprünglichen Verhalten entspricht, bei dem der Phasenparameter nur für JJMA relevant ist.

## Einstiegs- und Ausstiegskriterien
Der Algorithmus untersucht die durch `SignalBar` angegebene Bar (Standard 1, d. h. die letzte geschlossene Kerze) und die Bar davor:

- **Long öffnen**: Vor zwei Bars (`SignalBar + 1`) hatte Farbe `0` (extremes Positiv) und die letzte Bar (`SignalBar`) hat eine andere Farbe. Eine Short-Position wird zuerst geschlossen, falls vorhanden, dann wird ein neues Long von `TradeVolume` Lots eröffnet.
- **Short öffnen**: Vor zwei Bars hatte Farbe `4` (extremes Negativ) und die letzte Bar hat eine andere Farbe. Eine Long-Position wird zuerst geschlossen, falls vorhanden, dann wird ein neues Short eröffnet.
- **Long schließen**: Immer wenn die ältere Bar (vor zwei Bars) mit Farbe `4` gefärbt ist und bearishe Erschöpfung signalisiert.
- **Short schließen**: Immer wenn die ältere Bar mit Farbe `0` gefärbt ist und bullishe Erschöpfung signalisiert.

Die Logik reproduziert das flagbasierte Management von `TradeAlgorithms.mqh`: Ausstiege werden vor Einstiegen bewertet, und entgegengesetzte Trades werden vor dem Eröffnen einer neuen Position glattgestellt.

## Risikomanagement
Optionale Schutzorders werden in Preisschritteinheiten implementiert:

- `StopLossPoints` definiert den Abstand zwischen dem Einstiegspreis und dem Stop-Loss-Niveau. Der Stop wird unter Long-Einstiegen und über Short-Einstiegen platziert.
- `TakeProfitPoints` definiert den Gewinnzielabstand mit demselben schrittbasierten Maß.

Stops werden bei jeder abgeschlossenen Kerze geprüft. Wenn sowohl Stop als auch Ziel auf derselben Bar ausgelöst würden, schließt die erste zutreffende Bedingung die Position.

## Parameter
- **Trade Volume** (`TradeVolume`): Ordermenge für jede neue Position.
- **Stop Loss (pts)** (`StopLossPoints`): Stop-Loss-Abstand in Preisschritten.
- **Take Profit (pts)** (`TakeProfitPoints`): Take-Profit-Abstand in Preisschritten.
- **Enable Long Entries/Exits** (`BuyPositionOpen`, `BuyPositionClose`): Schalter für Long-Signale.
- **Enable Short Entries/Exits** (`SellPositionOpen`, `SellPositionClose`): Schalter für Short-Signale.
- **Signal Bar** (`SignalBar`): Wie viele Bars zurück für den Farbwechsel ausgewertet werden sollen.
- **High Level / Low Level** (`HighLevel`, `LowLevel`): Schwellenwerte für die Farbzuweisung.
- **Primary / Secondary Method** (`Method1`, `Method2`): Typen gleitender Durchschnitte für beide Glättungsstufen.
- **Length #1 / Length #2** (`Length1`, `Length2`): Von den gleitenden Durchschnitten verwendete Perioden.
- **Phase #1 / Phase #2** (`Phase1`, `Phase2`): Jurik-Phaseneinstellungen (von anderen Methoden ignoriert).
- **Coefficient** (`Coefficient`): Auf die Abweichung angewendeter Normalisierungsfaktor.
- **Applied Price** (`AppliedPrice`): Preisquelle (close, open, high, low, median, typical, weighted, simple, quarter, trend-follow, trend-follow average, Demark).
- **Candle Type** (`CandleType`): Zeitrahmen für Indikatorberechnungen.

## Hinweise
- Der Python-Port wird wie gewünscht bewusst weggelassen.
- Die StockSharp-Version hält die Tab-basierte Einrückungsrichtlinie ein und fügt englische Kommentare im gesamten Code hinzu.
- Der Indikator zeichnet keine Farbhistogramme; jedoch sind sowohl der numerische Wert als auch der Farbindex über die benutzerdefinierte `TradingChannelIndexValue`-Klasse für weitere Visualisierungen verfügbar.
