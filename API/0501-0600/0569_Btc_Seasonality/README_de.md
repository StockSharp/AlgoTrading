# BTC-Saisonalitäts-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie eröffnet eine Position auf Basis vordefinierter Wochentag- und Stundenregeln in der Eastern Standard Time (EST). Der Benutzer wählt den Einstiegstag und die Einstiegsstunde, den Ausstiegstag und die Ausstiegsstunde sowie ob Long oder Short gehandelt werden soll. Die Position wird zum angegebenen Einstiegszeitpunkt eröffnet und zum angegebenen Ausstiegszeitpunkt geschlossen.

## Details

- **Einstiegskriterien**:
  - Der aktuelle EST-Tag entspricht `EntryDay` und die aktuelle Stunde entspricht `EntryHour`.
- **Long/Short**: Konfigurierbar.
- **Ausstiegskriterien**:
  - Der aktuelle EST-Tag entspricht `ExitDay` und die aktuelle Stunde entspricht `ExitHour`.
- **Stops**: Keine.
- **Standardwerte**:
  - `EntryDay` = Saturday
  - `ExitDay` = Monday
  - `EntryHour` = 10
  - `ExitHour` = 10
  - `IsLong` = true
- **Filter**:
  - Kategorie: Saisonalität
  - Richtung: Konfigurierbar
  - Indikatoren: Keine
  - Stops: Nein
  - Komplexität: Niedrig
  - Zeitrahmen: Beliebig
  - Saisonalität: Ja
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Niedrig
