# MelBar EuroSwiss-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die MelBar EuroSwiss-Strategie reproduziert die Logik des Expertenberaters „MelBar EuroSwiss M30 500 1,85x 2Y“. Es kombiniert Bollinger Band-Breakout-Einträge mit einem Ausstiegsfilter basierend auf dem Relative Vigor Index (RVI). Die Standardvorlage ist auf das EUR/CHF-Paar im M30-Zeitrahmen abgestimmt, die Parameter können jedoch für andere Symbole optimiert werden.

Zu Beginn jeder fertigen Kerze liest die Strategie die Bollinger-Bänder und die auf Schlusskursen berechneten RVI-Werte. Neue Positionen werden eröffnet, wenn der aktuelle Balken über die Hüllkurve hinaus öffnet, während der vorherige Balken wieder innerhalb des Kanals geöffnet hat. Dieses Verhalten imitiert die Gap-Style-Breakout-Logik des ursprünglichen MQL5-Roboters. Long-Trades nutzen das untere Band als Auslöser, während Short-Trades auf das obere Band reagieren. Bestehende Positionen werden geschlossen, wenn der verzögerte RVI ein absolutes Niveau überschreitet oder unterschreitet, was auf eine Erschöpfung des Momentums in Richtung des Handels hinweist. Optionale Schutzbefehle werden anhand fester Pip-Abstände festgelegt.

Das Standardvolumen beträgt 0,2 Lots, aber der Parameter `TradeVolume` ermöglicht eine feine Steuerung der Positionsgröße. Sowohl Stop-Loss als auch Take-Profit werden in Pips ausgedrückt und über den konfigurierbaren Parameter `PipSize` in Preis-Offsets umgewandelt. Die gleiche Pip-Größe wird wiederverwendet, um das Schutzmodul beim Start zu aktivieren. Alle Berechnungen basieren auf fertigen Kerzen, um eine Voreingenommenheit zu vermeiden.

## Einzelheiten
- **Eintrittskriterien**:
  - **Long**: Aktuelle Kerzenöffnung < vorheriges unteres Bollinger-Band UND vorheriges Kerzenöffnungsband > unteres Band von vor zwei Kerzen.
  - **Short**: Aktuelle Kerzenöffnung > vorheriges oberes Bollinger-Band UND vorherige Kerzenöffnung < oberes Band von vor zwei Kerzen.
- **Ausstiegskriterien**:
  - **Long**: Schließen, wenn der historische RVI-Wert +`RviLevel` überschreitet.
  - **Short**: Schließen, wenn der historische RVI-Wert unter -`RviLevel` fällt.
- **Stops**: Optionale feste Stop-Loss- und Take-Profit-Abstände in Pips.
- **Indikatoren**: Bollinger-Bänder (Zeitraum `BollingerPeriod`, Abweichung `BollingerDeviation`) und Relative Vigor Index (`RviPeriod`).
- **Standardwerte**:
  - `TradeVolume` = 0,2 Lose
  - `BollingerPeriod` = 18
  - `BollingerDeviation` = 2,75
  - `RviPeriod` = 15
  - `RviLevel` = 0,30
  - `StopLossPips` = 13
  - `TakeProfitPips` = 61
  - `PipSize` = 0,0001
  - `CandleType` = TimeSpan.FromMinutes(30)
- **Andere Hinweise**:
  - Kategorie: Ausbruchsumkehr
  - Richtung: Sowohl lang als auch kurz
  - Zeitrahmen: Intraday (standardmäßig M30)
  - Risikostufe: Mittel aufgrund fester Pip-basierter Risikokontrollen
  - Trailing Stop: Standardmäßig nicht aktiviert (kann extern implementiert werden)

Die bereitgestellten Parameter spiegeln die ursprüngliche Konfiguration wider und dienen als solider Ausgangspunkt für Walk-Forward-Tests oder Optimierungsläufe in StockSharp.
