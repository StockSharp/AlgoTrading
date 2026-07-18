# Bollinger Band Two MA ZigZag Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Hybrides Trendfolge-System, das Bollinger Band-Umkehrungen, zwei gleitende Durchschnitte höherer Zeitrahmen und Swing-Punkte eines ZigZag-Detektors kombiniert. Bei jedem Signal werden zwei Positionen eröffnet: eine mit einem berechneten Take-Profit-Ziel und eine zweite "Läufer"-Position, die auf Trailing- und Break-even-Logik basiert.

## Details

- **Einstiegskriterien**:
  - **Long**: Die vorherige Kerze schloss oberhalb des vorherigen unteren Bollinger-Bandes, nachdem sie zwei Kerzen zuvor darunter schloss, der aktuelle Schlusskurs liegt ebenfalls über diesem unteren Band, und der Preis liegt oberhalb beider gleitender Durchschnitte höherer Zeitrahmen.
  - **Short**: Die vorherige Kerze schloss unterhalb des vorherigen oberen Bollinger-Bandes, nachdem sie zwei Kerzen zuvor darüber schloss, der aktuelle Schlusskurs liegt ebenfalls unter diesem oberen Band, und der Preis liegt unterhalb beider gleitender Durchschnitte höherer Zeitrahmen.
- **Positionsmanagement**:
  - Pro Signal werden zwei Positionen mit `First Volume` (mit Take-Profit) und `Second Volume` (Läufer) eröffnet.
  - Stops sind am jüngsten ZigZag-Swing-Extrem minus/plus `Pivot Offset (pts)` verankert.
  - Der Break-even-Schutz verschiebt den Stop auf den Einstieg plus einem Offset, sobald der unrealisierte Gewinn `Break-even Threshold (pts)` + `Break-even Offset (pts)` überschreitet.
  - Der Trailing Stop bewegt sich, wenn der Preis um `Trailing Step (pts)` über den bestehenden Stop hinaus vorrückt, und hält dabei einen Abstand von `Trailing Stop (pts)`.
- **Take Profit**:
  - Der Take-Profit der ersten Position wird als Prozentsatz (`Take Profit %`) des Abstands zwischen Einstieg und Stop berechnet.
  - Die Läufer-Position hat kein festes Ziel und wird über Stop, Trailing oder entgegengesetzte Signale beendet.
- **Zusätzliche Logik**:
  - Entgegengesetzte Signale schließen sofort alle offenen Positionen in der anderen Richtung, bevor neue Trades eingegangen werden.
  - Die Signalverarbeitung verwendet geschlossene Kerzen; Teildaten werden ignoriert.
- **Standardwerte**:
  - `First Volume` = 0.1
  - `Second Volume` = 0.1
  - `Take Profit %` = 50
  - `Pivot Offset (pts)` = 10
  - `Use Break-even Move` = true
  - `Break-even Offset (pts)` = 80
  - `Break-even Threshold (pts)` = 10
  - `Trailing Stop (pts)` = 80
  - `Trailing Step (pts)` = 120
  - `Bollinger Period` = 20
  - `Bollinger Width` = 2
  - `Base Candle` = 1-Stunden-Kerzen
  - `MA1 Candle` = Tageskerzen
  - `MA2 Candle` = 4-Stunden-Kerzen
  - `MA1 Period` = 20
  - `MA2 Period` = 20
  - `ZigZag Depth` = 12
  - `ZigZag Deviation (pts)` = 5
  - `ZigZag Backstep` = 3
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: Bollinger Bands, Moving Averages, ZigZag
  - Stops: Ja (Swing-Stop, Break-even, Trailing)
  - Komplexität: Fortgeschritten
  - Zeitrahmen: Multi-Zeitrahmen (1h Basis, Daily + 4h Filter)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

## Hinweise

- Die Strategie benötigt Kerzen-Abonnements auf drei verschiedenen Zeitrahmen, um die Filter auszuwerten und Ausstiege zu verwalten.
- Die Swing-Erkennung approximiert die MetaTrader ZigZag-Logik, indem Mindesttiefe, Abweichung und Backstep-Regeln vor der Aktualisierung der Pivot-Niveaus durchgesetzt werden.
- Die Volumina können unabhängig voneinander angepasst werden, um das Verhältnis des Take-Profit-Beins zum Läufer-Bein zu optimieren.
