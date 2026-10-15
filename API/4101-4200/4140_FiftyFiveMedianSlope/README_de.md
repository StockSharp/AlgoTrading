# FiftyFiveMedianSlopeStrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Herkunft
- Konvertiert vom MetaTrader 4 Expert Advisor **55_MA_med_FIN.mq4**.
- Konzentriert sich auf die Steigung eines gleitenden 55-Perioden-Durchschnitts, der anhand der mittleren Kerzenpreise berechnet wird.

## Handelslogik
- Abonniert die konfigurierte Kerzenserie (Standard: 1-stündiger Zeitrahmen) und verarbeitet nur abgeschlossene Kerzen.
- Berechnet einen gleitenden Durchschnitt des Medianpreises (\((Hoch + Tief) / 2\)) unter Verwendung der ausgewählten Methode (SMA, EMA, SMMA oder LWMA).
- Speichert die neuesten gleitenden Durchschnittswerte in einem Ringpuffer, um den Wert vor einem Balken mit dem Wert vor `MaShift` Balken zu vergleichen.
- Wenn der Wert vor einem Balken größer ist als der Wert vor `MaShift` Balken, gilt folgende Strategie:
  - Schließt zuerst jede kurze Belichtung.
  - Eröffnet eine Long-Position, wenn das Limit von `MaxOrders` nicht erreicht wurde.
- Wenn der Wert vor einem Balken kleiner ist als der Wert vor `MaShift` Balken, spiegelt dies das Verhalten bei Short-Positionen wider.
- Die Signale werden über interne Flags abgewechselt, sodass die Strategie auf einen entgegengesetzten Übergang wartet, bevor sie in die gleiche Richtung wieder eintritt.
- Der Handel ist nur zulässig, solange die Kerzenöffnungsstunde `StartHour < hour < EndHour` erfüllt. Die Grenzen gelten ausschließlich für die Übereinstimmung mit der ursprünglichen MQL-Implementierung.

## Positionsgrößenbestimmung und Risikomanagement
- `FixedVolume` definiert die Losgröße pro Market-Order. Wenn der Wert auf Null gesetzt ist, wechselt die Strategie zur risikobasierten Dimensionierung unter Verwendung von `RiskPercentage` und dem aktuellen Wert des Portfolios.
- `MaxOrders` begrenzt, wie oft das Basisvolumen in die gleiche Richtung gestapelt werden kann. Bei einem Wert von Null wird die Obergrenze entfernt.
- Optional `StopLossPoints` und `TakeProfitPoints` stellen die MT4-Stop-Loss- und Take-Profit-Distanzen über `StartProtection` unter Verwendung von Preisschritten wieder her.

## Parameter
- `FixedVolume` – primäre Losgröße. Auf Null setzen, um die prozentuale Größenanpassung zu aktivieren.
- `RiskPercentage` – Bruchteil des zugeteilten Portfolios, wenn `FixedVolume` gleich Null ist.
- `TakeProfitPoints` / `StopLossPoints` – Schutzabstände ausgedrückt in Preisschritten.
- `MaPeriod` – Länge des mittleren gleitenden Durchschnitts (Standard 55).
- `MaShift` – Anzahl der Balken zwischen den aktuellen und historischen Snapshots des gleitenden Durchschnitts (Standard 13).
- `MaMethod` – Berechnungstyp des gleitenden Durchschnitts (einfach, exponentiell, geglättet, linear gewichtet).
- `StartHour` / `EndHour` – exklusives Handelsfenster in der Plattformzeit (0–23 Stunden).
- `MaxOrders` – maximale gleichzeitige Einträge pro Richtung.
- `CandleType` – Zeitrahmen, der für die Signalkerzen verwendet wird.

## Nutzungshinweise
- Stellen Sie sicher, dass das abonnierte Instrument einen Wert ungleich Null `PriceStep` und Volumenmetadaten bereitstellt, sodass die Volumenausrichtung den Austauschanforderungen entspricht.
- Bei der risikobasierten Größenbestimmung werden der aktuelle Wert des Portfolios und der letzte Schlusskurs verwendet. Wenn eines davon nicht verfügbar ist, fällt die Strategie auf das Volumen Null zurück (kein Handel).
- Die Strategie bricht das entgegengesetzte Engagement ab, bevor eine neue Position eröffnet wird, und emuliert so das ursprüngliche MT4-Verhalten beim Schließen gegensätzlicher Orders.
