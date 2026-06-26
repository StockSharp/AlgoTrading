# JS MA SAR Trades-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

JS MA SAR Trades konvertiert den MetaTrader 5-Experten „JS MA SAR Trades" in die StockSharp High-Level-API. Die Strategie sucht nach höheren Swing-Tiefs oder niedrigeren Swing-Hochs, die über einen ZigZag-ähnlichen Filter erkannt werden, bestätigt den Schwung mit zwei gleitenden Durchschnitten und steigt dann in Richtung eines Parabolic-SAR-Ausbruchs ein. Positionen werden durch klassische Stops, optionale Trailing-Stops und einen expliziten Handelssitzungsfilter geschützt.

## Logik-Übersicht

1. **Swing-Struktur** – Highest/Lowest-Indikatoren mit der konfigurierten Tiefe nähern den originalen ZigZag an. Die beiden aktuellsten Swing-Tiefs und -Hochs werden verfolgt. Ein Long-Setup erfordert, dass das letzte Tief höher als das vorherige ist (aufsteigende Struktur), während ein Short-Setup erfordert, dass das letzte Hoch niedriger als das vorherige ist (absteigende Struktur). Ein Abweichungsfilter (in Pips) und ein minimaler Backstep (Balken zwischen Pivots) verhindern, dass Rauschpivots akzeptiert werden.
2. **Gleitender Durchschnitt zur Bestätigung** – Beide gleitenden Durchschnitte verwenden denselben Glättungstyp und angewendeten Preis wie die MT5-Version, einschließlich optionaler positiver Verschiebungen (Balken nach rechts). Ein Long-Signal erfordert, dass der verschobene schnelle MA über dem verschobenen langsamen MA bleibt; ein Short-Signal erfordert die entgegengesetzte Beziehung.
3. **Parabolic-SAR-Auslöser** – Sobald die Swing- und MA-Bedingungen erfüllt sind, wird der Trade nur ausgeführt, wenn die Kerze jenseits des Parabolic-SAR-Niveaus schließt: Schlusskurs über SAR für Longs und darunter für Shorts. SAR-Wechsel auf die andere Seite schließen alle bestehenden Positionen, auch außerhalb des Einstiegsfensters.
4. **Risikomanagement** – Stop-Loss- und Take-Profit-Niveaus werden in Pips berechnet (umgerechnet über den Instrument-Preisschritt). Der optionale Trailing-Stop ahmt die MT5-Logik nach: Der Stop wird erst verschoben, nachdem sich der Preis um den konfigurierten Trailing-Stop plus Trailing-Step-Abstand vom Einstiegspreis bewegt hat.
5. **Sitzungsfilter** – Wenn aktiviert, sind Orders nur zwischen den angegebenen Start- und Endstunden (inklusiv) erlaubt. Schützende Ausstiege (Stop/Take/Trailing und SAR-Umkehr) werden weiterhin bei jeder abgeschlossenen Kerze ausgewertet.

## Einstiegs- und Ausstiegsregeln

- **Long-Einstieg**: höheres Swing-Tief, Parabolic SAR unterhalb des Schlusskurses, schneller MA (mit Verschiebung) oberhalb des langsamen MA und Schlusskurs innerhalb des Handelsfensters. Die Strategie kauft `OrderVolume + |Position|`, um Shorts zu schließen und die Long-Position zu eröffnen.
- **Short-Einstieg**: niedrigeres Swing-Hoch, Parabolic SAR über dem Schlusskurs, schneller MA (mit Verschiebung) unterhalb des langsamen MA und Zeitfilter erfüllt.
- **Long-Ausstieg**:
  - Schlusskurs kreuzt unterhalb des Parabolic SAR;
  - Stop-Loss-, Trailing-Stop- oder Take-Profit-Niveau wird erreicht.
- **Short-Ausstieg**:
  - Schlusskurs kreuzt oberhalb des Parabolic SAR;
  - Stop-Loss-, Trailing-Stop- oder Take-Profit-Niveau wird erreicht.

## Parameter

| Parameter | Standard | Beschreibung |
|-----------|----------|--------------|
| `OrderVolume` | `1` | Basis-Ordergröße für neue Einstiege; die Strategie addiert die absolute aktuelle Position, um sofort umzukehren. |
| `StopLossPips` | `50` | Abstand in Pips zwischen Einstiegspreis und Stop-Loss. Auf `0` setzen, um zu deaktivieren. |
| `TakeProfitPips` | `50` | Abstand in Pips zwischen Einstiegspreis und Take-Profit. Auf `0` setzen, um zu deaktivieren. |
| `TrailingStopPips` | `5` | Trailing-Stop-Abstand in Pips. Arbeitet zusammen mit `TrailingStepPips`. |
| `TrailingStepPips` | `5` | Zusätzliche Distanz, die der Preis (in Pips) zurücklegen muss, bevor der Trailing-Stop enger gezogen wird. Muss positiv sein, wenn Trailing aktiviert ist. |
| `UseTimeFilter` | `true` | Start/Ende-Stundenfilter für neue Einstiege aktivieren. |
| `StartHour` | `19` | Beginn des Handelsfensters (inklusiv, Börsenzeit). |
| `EndHour` | `22` | Ende des Handelsfensters (inklusiv). |
| `FastMaPeriod` | `55` | Periode des schnellen gleitenden Durchschnitts. |
| `FastMaShift` | `3` | Vorwärtsverschiebung (in Balken) für die schnellen MA-Werte. |
| `SlowMaPeriod` | `120` | Periode des langsamen gleitenden Durchschnitts. |
| `SlowMaShift` | `0` | Vorwärtsverschiebung (in Balken) für den langsamen MA. |
| `MaType` | `Smoothed` | Glättungsmethode des gleitenden Durchschnitts (Simple, Exponential, Smoothed, Weighted). |
| `AppliedPrice` | `Median` | Preisquelle für beide gleitenden Durchschnitte (Close, Open, High, Low, Median, Typical, Weighted). |
| `SarStep` | `0.02` | Anfangsbeschleunigungsfaktor des Parabolic SAR. |
| `SarMax` | `0.2` | Maximaler Beschleunigungsfaktor des Parabolic SAR. |
| `ZigZagDepth` | `12` | Rückblickfenster (Balken) zur Swing-Erkennung. |
| `ZigZagDeviation` | `5` | Mindest-Swing-Größe in Pips, um einen neuen Pivot zu akzeptieren. |
| `ZigZagBackstep` | `3` | Mindestanzahl Balken zwischen aufeinanderfolgenden Pivots desselben Typs. |
| `CandleType` | `H1` | Handelszeitrahmen für das Kerzen-Abonnement. |

## Hinweise

- Die Strategie hält die Schutzlogik auch außerhalb des Einstiegsfensters aktiv und stellt so sicher, dass Stops und SAR-Wechsel eingehalten werden.
- Der Trailing-Stop reproduziert die MT5-Implementierung: Sobald der Preis sich um `TrailingStop + TrailingStep` vorbewegt hat, wird der Stop auf `Close - TrailingStop` für Longs gesetzt (gespiegelt für Shorts).
- Gleitende Durchschnitte werden auf dem ausgewählten angewendeten Preis bewertet; die Verschiebung emuliert den MT5-Indikator-Offset.
- Stellen Sie sicher, dass das Instrument einen gültigen `PriceStep` hat, andernfalls werden pip-basierte Abstände übersprungen.
