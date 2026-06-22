# WPR-Verlangsamungs-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die WPR-Verlangsamungs-Strategie nutzt den Williams %R Oszillator, um Umkehrungen zu erkennen, wenn das Momentum in der Nähe extremer Niveaus ins Stocken gerät. Eine Verlangsamung tritt auf, wenn der aktuelle Williams %R-Wert vom vorherigen Wert um weniger als einen Punkt abweicht. Wenn eine solche Verlangsamung über dem oberen Schwellenwert auftritt, schließt die Strategie Short-Positionen und eröffnet optional eine Long-Position. Eine Verlangsamung unter dem unteren Schwellenwert schließt Long-Positionen und eröffnet optional eine Short-Position.

## Ein- und Ausstiegsregeln
- **Long-Einstieg**: Williams %R liegt über `LevelMax` und die Verlangsamungsbedingung ist erfüllt. Short-Positionen können geschlossen werden, wenn erlaubt.
- **Short-Einstieg**: Williams %R liegt unter `LevelMin` und die Verlangsamungsbedingung ist erfüllt. Long-Positionen können geschlossen werden, wenn erlaubt.
- **Long-Ausstieg**: Ausgelöst durch ein Short-Einstiegssignal, wenn `BuyPosClose` aktiviert ist.
- **Short-Ausstieg**: Ausgelöst durch ein Long-Einstiegssignal, wenn `SellPosClose` aktiviert ist.

## Parameter
- `WprPeriod` – Periode zur Berechnung von Williams %R.
- `LevelMax` – oberes Signalniveau (Standard -20) markiert die überkaufte Zone.
- `LevelMin` – unteres Signalniveau (Standard -80) markiert die überverkaufte Zone.
- `SeekSlowdown` – aktiviert die Verlangsamungserkennung zwischen aufeinanderfolgenden Williams %R-Werten.
- `BuyPosOpen` – Öffnen von Long-Positionen erlauben.
- `SellPosOpen` – Öffnen von Short-Positionen erlauben.
- `BuyPosClose` – Schließen von Long-Positionen bei Verkaufssignalen erlauben.
- `SellPosClose` – Schließen von Short-Positionen bei Kaufsignalen erlauben.
- `CandleType` – Kerzentyp für Indikatorberechnungen (Standard 6-Stunden-Kerzen).

## Hinweise
Die Strategie konzentriert sich ausschließlich auf die Williams %R Verlangsamungslogik des originalen MQL5 Experten. Benachrichtigungen, Geldverwaltung und andere Hilfsfunktionen werden der Übersichtlichkeit halber weggelassen. Stop-Loss und Take-Profit-Funktionalität kann bei Bedarf manuell hinzugefügt werden.
