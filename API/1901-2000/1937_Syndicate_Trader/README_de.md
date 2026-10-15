# Syndicate-Trader-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie ist eine StockSharp-Übersetzung des originalen MetaTrader-Skripts **Syndicate_Trader_v_1_04.mq4** aus dem Ordner `MQL/12351`.

Sie handelt auf der Grundlage einer Kreuzung zwischen schnellen und langsamen exponentiellen gleitenden Durchschnitten mit einer Volumspitzen-Bestätigung. Optionale Sitzungsfilter beschränken den Handel auf bestimmte Stunden. Einfache Take-Profit- und Stop-Loss-Niveaus steuern das Risiko.

## Details

- **Einstiegskriterien**:
  - **Long**: Schnelle EMA kreuzt über langsame EMA und das Volumen überschreitet den gleitenden Volumsdurchschnitt multipliziert mit einem konfigurierbaren Faktor.
  - **Short**: Schnelle EMA kreuzt unter langsame EMA mit derselben Volumsbestätigung.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**:
  - Entgegengesetzte Kreuzung.
  - Stop-Loss oder Take-Profit erreicht.
  - Außerhalb des erlaubten Sitzungsfensters.
- **Stops**: Fester Stop-Loss und Take-Profit in Preispunkten.
- **Filter**:
  - Volumsspitzen-Filter.
  - Optionaler Sitzungszeit-Filter.

## Parameter

| Name | Beschreibung |
|------|--------------|
| `FastEmaLength` | Periode der schnellen EMA. |
| `SlowEmaLength` | Periode der langsamen EMA. |
| `VolumeMaLength` | Periode für die Volumsmittelung. |
| `VolumeMultiplier` | Multiplikator, der auf das durchschnittliche Volumen angewendet wird, um eine Spitze zu definieren. |
| `TakeProfitPoints` | Take-Profit in Preispunkten. |
| `StopLossPoints` | Stop-Loss in Preispunkten. |
| `UseSessionFilter` | Sitzungsfilter aktivieren oder deaktivieren. |
| `SessionStartHour/SessionStartMinute` | Startzeit der Handelssitzung. |
| `SessionEndHour/SessionEndMinute` | Endzeit der Handelssitzung. |
| `CandleType` | Kerzentyp und Zeitrahmen. |
