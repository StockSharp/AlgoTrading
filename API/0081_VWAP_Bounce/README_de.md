# VWAP Bounce-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Der volumengewichtete Durchschnittspreis (VWAP) ist ein beliebter Intraday-Referenzwert. Wenn der Kurs erheblich vom VWAP abweicht und dann eine Kerze zurück in Richtung VWAP druckt, folgt oft eine kurze Umkehrbewegung. Diese Strategie handelt diese Abpraller.

Tests zeigen eine durchschnittliche Jahresrendite von etwa 130%. Die Strategie eignet sich am besten für den Aktienmarkt.

Für jeden Balken wird der aktuelle VWAP berechnet. Wenn eine bullische Kerze unterhalb des VWAP schließt, geht das System Long; wenn eine bearische Kerze oberhalb des VWAP schließt, geht es Short. Ein fester Stop-Loss-Prozentsatz steuert das Risiko, und Positionen werden in der Regel nur gehalten, bis ein entgegengesetztes Signal entsteht oder der Stop erreicht wird.

Da die Methode gegen Intraday-Extreme handelt, funktioniert sie am besten in Range-gebundenen Märkten statt in starken Trends.

## Details

- **Einstiegskriterien**: Schlusskurs unterhalb VWAP mit bullischer Kerze oder oberhalb VWAP mit bearischer Kerze.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: Entgegengesetztes Signal oder Stop-Loss.
- **Stops**: Ja, prozentbasiert.
- **Standardwerte**:
  - `CandleType` = 5 minute
  - `StopLoss` = 2%
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Beide
  - Indikatoren: VWAP
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

