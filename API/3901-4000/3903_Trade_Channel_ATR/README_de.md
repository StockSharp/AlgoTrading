# Handelskanal ATR-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Trade-Channel-Strategie repliziert den ursprünglichen MetaTrader-Expertenberater, der Preiskanäle mit ATR-basierten Stopps handelte. Es wartet darauf, dass die Kanalgrenzen unverändert bleiben und dass die letzte Kerze diese Niveaus berührt oder ablehnt. Wenn das Setup angezeigt wird, eröffnet die Strategie eine Position in der entgegengesetzten Richtung der Berührung und wendet einen adaptiven Trailing Stop an, der in Punkten gemessen wird.

Der Ansatz zielt darauf ab, die Mean-Reversion um einen stabilen Preiskanal herum auszunutzen. Es filtert Signale, sodass der Kanal flach sein muss (keine neuen Hochs oder Tiefs), bevor er eintritt. Schutzstopps werden jenseits des Kanals mithilfe der Average True Range platziert, und ein optionaler Trailing Stop sichert Gewinne, sobald sich die Bewegung entwickelt.

## Einzelheiten

- **Eintrittskriterien**:
  - Short: Das Kanalhoch entspricht dem vorherigen Kanalhoch und die letzte Kerze durchbricht entweder dieses Hoch oder schließt zwischen dem Hoch und dem Pivot `(high + low + close) / 3`.
  - Long: Das Tief des Kanals entspricht dem Tief des vorherigen Kanals und die letzte Kerze durchbricht entweder dieses Tief oder schließt zwischen dem Tief und dem Pivot.
- **Long/Short**: Beide Richtungen, aber jeweils nur eine Position.
- **Ausstiegskriterien**:
  - Long: Der Preis berührt das Kanalhoch, während das Hoch unverändert blieb.
  - Kurz: Der Preis berührt das Kanaltief, während das Tief unverändert blieb.
  - Der optionale Trailing Stop verschärft sich hinter dem Markt, sobald der Gewinn `TrailingDistance` Punkte übersteigt.
- **Stops**: Anfänglicher Stop-Loss bei `channel boundary ± ATR`. Trailing Stop ersetzt ihn, wenn er aktiviert wird.
- **Standardwerte**:
  - `Volume` = 0,1 m
  - `ChannelPeriod` = 20
  - `AtrPeriod` = 4
  - `TrailingDistance` = 30
  - `CandleType` = 30-Minuten-Kerzen
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Beide
  - Indikatoren: Höchster, niedrigster, durchschnittlicher wahrer Bereich
  - Stopps: ATR Stopp, Trailing
  - Komplexität: Mittelschwer
  - Zeitrahmen: Intraday (30 Minuten)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikostufe: Mittel

## Notizen

- `Volume` steuert die Bestellgröße; Es kann jeweils nur eine Position existieren.
- `TrailingDistance` wird in Punkten (Preisschritten) angegeben. Auf Null setzen, um den Trailing Stop zu deaktivieren.
- Die Strategie erfordert historische Kerzen, um die Höchst-/Tiefst- und ATR-Indikatoren vor dem Handel aufzuwärmen.
- Stop-Orders werden automatisch gelöscht, wenn die Position geschlossen oder die Strategie zurückgesetzt wird.
