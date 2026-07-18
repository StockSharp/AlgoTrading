# Mean-Reversion-Strategie mit inkrementellem Einstieg
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie geht Trades ein, wenn der Kurs um einen definierten Prozentsatz von einem einfachen gleitenden Durchschnitt abweicht. Zusätzliche Aufträge werden schrittweise platziert, wenn der Kurs sich weiter vom Durchschnitt entfernt.

Positionen werden geschlossen, sobald der Kurs zum gleitenden Durchschnitt zurückkehrt.

## Details

- **Einstiegskriterien:**
  - **Long:** `Low < SMA` und prozentuale Differenz zwischen `Low` und `SMA` ≥ `Initial Percent`.
  - **Short:** `High > SMA` und prozentuale Differenz zwischen `High` und `SMA` ≥ `Initial Percent`.
- **Inkrementelle Einstiege:** Neue Aufträge werden alle `Percent Step` weiter vom vorherigen Einstieg hinzugefügt.
- **Ausstiegskriterien:**
  - **Long:** `Close ≥ SMA`.
  - **Short:** `Close ≤ SMA`.
- **Indikatoren:** SMA.
- **Standardwerte:**
  - `MA Length` = 30.
  - `Initial Percent` = 5.
  - `Percent Step` = 1.
