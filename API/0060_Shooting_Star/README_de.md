# Shooting-Star-Muster (Shooting Star Pattern)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Shooting-Star-Kerze erscheint oft nach einem Anstieg und warnt vor einer Umkehr. Diese Strategie sucht nach einem langen oberen Docht im Verhältnis zum Kerzenkörper und kaum einem unteren Docht.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 67 %. Die Strategie eignet sich am besten für den Aktienmarkt.

Wenn eine Bestätigung erforderlich ist, muss die nächste Kerze tiefer schließen, bevor eine Short-Position eingegangen wird. Andernfalls kann der Trade sofort eingegangen werden. Stops werden oberhalb des Musthochs platziert.

## Details

- **Einstiegskriterien**: Shooting Star erkannt und Bestätigung, falls aktiviert.
- **Long/Short**: Nur Short.
- **Ausstiegskriterien**: Stop-Loss oder diskretionärer Ausstieg.
- **Stops**: Ja.
- **Standardwerte**:
  - `ShadowToBodyRatio` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(15)
  - `StopLossPercent` = 1.0m
  - `ConfirmationRequired` = true
- **Filter**:
  - Kategorie: Muster
  - Richtung: Nur Short
  - Indikatoren: Candlestick
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
