# Hans Indicator Wolkensystem Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie portiert den MQL5 Expert Advisor `Exp_Hans_Indicator_Cloud_System` auf die StockSharp High-Level-API. Sie reproduziert die
Hans-Indikator-"Wolken"-Bereiche, die jeden Handelstag in zwei Referenzsessions unterteilen, und handelt, wenn der Indikator einen
Ausbruch über oder unter diese dynamischen Bereiche meldet. Die Implementierung verwendet eine konfigurierbare Kerzenserie (Standard: M30), verarbeitet
nur abgeschlossene Kerzen und spiegelt die verzögerte Ausführungslogik des Originalskripts wider, indem sie auf der nächsten Kerze nach einer Farbänderung agiert.

## Hans-Indikator-Nachbildung
Der ursprüngliche Indikator verschiebt alle Zeitstempel von der Broker-Zeitzone (`LocalTimeZone`) in eine Zielzeitzone (`DestinationTimeZone`).
Der StockSharp-Port wendet denselben Offset an, bevor er jeden Tag in zwei Sessions aufteilt:

1. **Session 1 (04:00–08:00 Zielzeit)** – die Strategie zeichnet das höchste Hoch und das niedrigste Tief aller Kerzen auf, die in
   dieses Fenster fallen. Sobald das Fenster endet, gilt die Zone als vollständig.
2. **Session 2 (08:00–12:00 Zielzeit)** – der Prozess wiederholt sich für das zweite Fenster. Wenn diese Session endet, ersetzen ihre Hoch/Tief-Werte
   die erste Zone für den Rest des Tages.

Ein konfigurierbarer Buffer (`PipsForEntry`) in Preisschritten wird oberhalb des Hochs und unterhalb des Tiefs der aktiven Zone hinzugefügt. Die
Indikator-Farbenmap wird wie folgt reproduziert:

- `0` – Schlusskurs liegt über der oberen Zone und der Kerzenkörper ist bullisch.
- `1` – Schlusskurs liegt über der oberen Zone und der Kerzenkörper ist bärisch.
- `3` – Schlusskurs liegt unter der unteren Zone und der Kerzenkörper ist bullisch.
- `4` – Schlusskurs liegt unter der unteren Zone und der Kerzenkörper ist bärisch.
- `2` – kein Ausbruch (neutraler Zustand).

Diese Werte werden gespeichert, um die `CopyBuffer`-Nachschläge des MQL5-Experten zu emulieren.

## Handelslogik
- Die Strategie hält eine rollende Geschichte von Farbcodes und schaut `SignalBar` Balken (Standard 1) plus einen extra Balken zurück, passend zum
  `CopyBuffer(..., SignalBar, 2, ...)`-Aufruf aus der Quelle.
- **Long öffnen**: der ältere Balken (`SignalBar + 1`) meldet Farbe `0` oder `1` und der neuere Balken (`SignalBar`) ist nicht mit
  `0`/`1` gefärbt. Jedes bestehende Short-Exposure wird geschlossen, bevor ein neues Long von `TradeVolume` Einheiten eröffnet wird.
- **Short öffnen**: der ältere Balken meldet Farbe `3` oder `4` und der neuere Balken ist nicht mit `3`/`4` gefärbt. Jedes bestehende Long-Exposure
  wird zuerst geflacht und dann ein neues Short eröffnet.
- **Long schließen**: wann immer der ältere Balken mit `3` oder `4` gefärbt ist und Long-Exits aktiviert sind.
- **Short schließen**: wann immer der ältere Balken mit `0` oder `1` gefärbt ist und Short-Exits aktiviert sind.

Exits werden genau wie die Hilfsfunktionen in `TradeAlgorithms.mqh` vor Einstiegen verarbeitet, um sicherzustellen, dass entgegengesetzte
Positionen vor dem Ausgeben neuer Orders geschlossen werden.

## Parameter
- **Kerzentyp** (`CandleType`): Zeitrahmen der verarbeiteten Kerzen.
- **Signalbalken** (`SignalBar`): wie viele abgeschlossene Kerzen zurück auf eine Farbänderung inspiziert werden soll.
- **Lokale Zeitzone** (`LocalTimeZone`): Broker/Server-Zeitzone in Stunden.
- **Zielzeitzone** (`DestinationTimeZone`): Zielzeitzone, die die Session-Fenster definiert.
- **Ausbruchs-Buffer** (`PipsForEntry`): Anzahl von Preisschritten, die über/unter dem erkannten Session-Bereich hinzugefügt werden.
- **Long-Einstiege/Ausstiege aktivieren** (`BuyPosOpen`, `BuyPosClose`): Schalter zur Verwaltung von Long-Positionen.
- **Short-Einstiege/Ausstiege aktivieren** (`SellPosOpen`, `SellPosClose`): Schalter zur Verwaltung von Short-Positionen.
- **Handelsvolumen** (`TradeVolume`): Ordergröße für jede neue Position; wird beim Start auch mit `Strategy.Volume` synchronisiert.

## Hinweise
- Die Python-Übersetzung wird auf Anfrage bewusst weggelassen.
- Die Geldmanagement-Helfer aus `TradeAlgorithms.mqh` (Margin-Modi, dynamische Positionsgrößen, Stop-Loss/Take-Profit-Platzierung)
  werden auf ein festes Handelsvolumen und explizite Exit-Regeln vereinfacht.
- Wenn das Wertpapier `PriceStep` nicht offenlegt, wird der Ausbruchs-Buffer als absolute Preiseinheiten interpretiert, was die beste
  verfügbare Annäherung ohne Tick-Größen-Information darstellt.
