# Exp Skyscraper Fix ColorAML-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie bildet den MetaTrader 5-Expertenberater **Exp_Skyscraper_Fix_ColorAML** im StockSharp-Framework nach. Sie
kombiniert zwei unabhängige Signalgeneratoren:

1. **Skyscraper Fix** – ein ATR-basierter Kanal, der bullische oder bärische Regime je nach Richtung der adaptiven Bänder
   einfärbt.
2. **ColorAML** – ein adaptiver Marktpegel-Oszillator, der lokale Fraktal-Ranges vergleicht, um Expansions- oder
   Kontraktionsphasen zu erkennen.

Die ursprüngliche MQL-Implementierung verwaltete zwei separate Magic Numbers und konnte gleichzeitig gehedgte Positionen halten.
StockSharp-Strategien operieren auf einer Nettoposition, daher gleichen sich widersprüchliche Signale gegenseitig aus und die
letzte Eröffnung definiert das Exposure. Das README hebt diese Unterschiede hervor, damit Nutzer ihre Erwartungen beim
Backtesting oder beim Handel mit der konvertierten Variante anpassen können.

## Parameter
### Skyscraper Fix-Modul
- **SkyscraperCandleType** – Zeitrahmen für den Aufbau des Skyscraper Fix-Indikators. Standard: `4h`-Kerzen.
- **SkyscraperEnableLongEntry / SkyscraperEnableShortEntry** – erlauben dem Modul, Long- oder Short-Positionen zu eröffnen.
- **SkyscraperEnableLongExit / SkyscraperEnableShortExit** – erlauben dem Modul, offene Trades in der entsprechenden Richtung
  zu schließen.
- **SkyscraperLength** – Anzahl der ATR-Samples zur Bestimmung der Treppenstufengröße. Standard: `10` Bars.
- **SkyscraperMultiplier** – Koeffizient, der auf den ATR-basierten Schritt angewendet wird. Standard: `0.9`.
- **SkyscraperPercentage** – optionaler prozentualer Versatz der Mittellinie (0 deaktiviert die Verschiebung).
- **SkyscraperMode** – wählt zwischen High/Low- oder Close-basiertem Kanalaufbau.
- **SkyscraperSignalBar** – Anzahl der abgeschlossenen Kerzen, die beim Lesen des Farb-Puffers zurückgeblickt werden. Werte
  müssen mindestens `1` betragen.
- **SkyscraperVolume** – Marktordervolumen bei jedem Einstieg.
- **SkyscraperStopLoss / SkyscraperTakeProfit** – Schutzabstände ausgedrückt in Preisschritten.

### ColorAML-Modul
- **ColorAmlCandleType** – Zeitrahmen für den ColorAML-Oszillator. Standard: `4h`-Kerzen.
- **ColorAmlEnableLongEntry / ColorAmlEnableShortEntry** – aktivieren neue Long- oder Short-Einstiege.
- **ColorAmlEnableLongExit / ColorAmlEnableShortExit** – aktivieren Schließorders für die jeweilige Richtung.
- **ColorAmlFractal** – Länge der Fraktal-Range für den Aufbau der adaptiven Level. Standard: `6` Bars.
- **ColorAmlLag** – Lag-Parameter zur Steuerung der exponentiellen Glättung. Standard: `7`.
- **ColorAmlSignalBar** – Anzahl der abgeschlossenen Kerzen im Farb-Puffer zu untersuchen.
- **ColorAmlVolume** – Ordervolumen für ColorAML-gesteuerte Einstiege.
- **ColorAmlStopLoss / ColorAmlTakeProfit** – Schutzabstände in Preisschritten.

## Handelslogik
Die Strategie abonniert die angeforderten Kerzenserien für jedes Modul und wertet nur abgeschlossene Kerzen aus. Beide
Indikatoren sind in C# implementiert und folgen den mathematischen Definitionen des ursprünglichen MQL-Codes:

- **Skyscraper Fix** berechnet einen SuperTrend-ähnlichen Kanal. Wenn der Farb-Puffer auf **teal (0)** wechselt, schließt das
  Modul jedes Short-Exposure (falls erlaubt) und bereitet, wenn die vorherige Farbe anders war, einen Long-Einstieg vor. Wenn
  der Puffer auf **firebrick (1)** wechselt, werden Longs geschlossen und ein Short-Einstieg geplant.
- **ColorAML** vergleicht Fraktal-Ranges, um eine adaptive Pegellinie aufzubauen. Farbe `2` signalisiert bullische Expansion,
  schließt Shorts und öffnet optional Longs. Farbe `0` signalisiert bärische Kontraktion, schließt Longs und öffnet optional
  Shorts. Neutrales `1` behält die aktuelle Position bei.

Jeder Einstieg verwendet Marktorders mit dem Volumen `KonfiguriertemVolumen + |aktuelle Position|`. Dadurch wird sichergestellt,
dass eine Umkehrorder gleichzeitig das entgegengesetzte Exposure schließt und die neue Position aufbaut, wenn Hedging nicht
verfügbar ist.

## Risikomanagement
`StartProtection()` wird beim Start aktiviert. Immer wenn ein Modul eine neue Position eröffnet, speichert die Strategie den
Einstiegspreis und berechnet Stop-Loss- und Take-Profit-Level anhand der modulspezifischen Einstellungen. Nachfolgende Kerzen
lösen Ausstiege aus, wenn ihr Hoch oder Tief die konfigurierten Schwellen durchbricht. Das Setzen der Abstände auf null
deaktiviert die Schutzlogik.

## Implementierungshinweise
- Die Berechnungen von Skyscraper Fix und ColorAML wurden direkt portiert und laufen auf internen Kerzenpuffern. Es müssen keine
  externen Indikatoren manuell zur Strategie hinzugefügt werden.
- StockSharp verwaltet eine einzige Nettoposition pro Strategie. Infolgedessen werden gleichzeitige Long- und Short-Trades des
  ursprünglichen EA saldiert. Nutzer, die auf Hedging angewiesen waren, sollten diesen Unterschied beachten.
- Es werden nur abgeschlossene Kerzen verarbeitet. `SignalBar` muss mindestens `1` betragen; eine Intrabar-Auswertung (Tick für
  Tick) wird nicht reproduziert.
- Stops werden durch Überwachung der Kerzenextrema durchgesetzt, nicht durch serverseitige Orders, was dem Verhalten des
  konvertierten Frameworks entspricht.

## Verwendung
1. Binden Sie die Strategie an das gewünschte Wertpapier und Portfolio.
2. Konfigurieren Sie die Parameter für beide Module und stimmen Sie die Kerzentypen mit den verfügbaren Daten ab.
3. Starten Sie die Strategie. Sie abonniert automatisch die notwendigen Kerzen, berechnet die Indikatorfarben und platziert
   Marktorders entsprechend den Modulsignalen.
4. Beobachten Sie das Log oder die Charts, um Regimewechsel, manuelle Risikomanagement-Ereignisse und ausgeführte Trades zu
   verfolgen.
