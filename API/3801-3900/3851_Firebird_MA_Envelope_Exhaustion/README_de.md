# Strategie zur Erschöpfung des Firebird MA-Umschlags
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie repliziert den Umschlagumkehrexperten von Firebird v0.60. Es misst einen einfachen gleitenden Durchschnitt und verrechnet ihn um einen Prozentsatz, um obere und untere Hüllkurven zu bilden. Wenn der Preis das obere Band durchbricht, verkauft die Strategie, und wenn das untere Band durchbricht, kauft sie. Zusätzliche Positionen werden nur dann gemittelt, wenn sich der Preis um mindestens einen konfigurierbaren Pip-Schritt über den vorherigen Eintrag hinaus bewegt. Der gesamte Stop-Loss wird auf alle Einträge aufgeteilt, wodurch verhindert wird, dass außer Kontrolle geratene Trends wiederholt in die gleiche Richtung eintreten.

## Einzelheiten

- **Eintrittskriterien**:
  - Berechnen Sie einen SMA entweder für Kerzeneröffnungen oder den Hoch-/Tief-Mittelpunkt.
  - Oberer Umschlag = SMA × (1 + Prozent/100); unterer Umschlag = SMA × (1 − Prozent/100).
  - Steigen Sie Short ein, wenn der Schlusskurs über dem oberen Band liegt (es sei denn, ein aktueller Stopp hat Shorts gesperrt), steigen Sie Long ein, wenn der Schlusskurs unterhalb des unteren Bandes liegt (es sei denn, Long-Positionen sind gesperrt).
  - Average-in-Trades sind zulässig, sobald sich der Preis um `PipStep` Pips (optional skaliert nach Stärke) über die letzte Füllung hinaus bewegt.
- **Lang/Kurz**: Lang und kurz.
- **Ausstiegskriterien**:
  - Geteilter Take-Profit zum durchschnittlichen Einstiegspreis ± `TakeProfit` Pips.
  - Gemeinsamer Stop-Loss zum durchschnittlichen Einstiegspreis ∓ `StopLoss / position count` Pips.
  - Sperrflag verhindert Wiedereinfahrt in die gleiche Richtung, bis nach einem Stopp ein Gegensignal ausgelöst wird.
- **Stops**: Ja, aggregierter Stop-Loss und Take-Profit.
- **Standardwerte**:
  - `MaLength` = 10
  - `Percent` = 0,3
  - `TradeOnFriday` = wahr
  - `UseHighLow` = false (Öffnet verwenden)
  - `PipStep` = 30
  - `IncreasementPower` = 0
  - `TakeProfit` = 30
  - `StopLoss` = 200
  - `TradeVolume` = 1
- **Filter**:
  - Kategorie: Mean-Reversion
  - Richtung: Beide
  - Indikatoren: SMA Umschläge
  - Stoppt: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Beliebig
  - Saisonalität: Optionaler Freitagsfilter
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikostufe: Hoch aufgrund der Mittelwertbildung
