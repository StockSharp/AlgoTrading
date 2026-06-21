# Perceptron AC Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie implementiert ein einfaches Perzeptron auf Basis des Accelerator Oscillator (AC).
Der AC-Wert der aktuellen Kerze und von drei vergangenen Offsets wird mit einstellbaren Gewichten multipliziert.
Die Summe dieser Produkte bildet den Perzeptron-Ausgang, der die Handelsrichtung bestimmt.

## Funktionsweise

1. Den Accelerator Oscillator (AC) aus der Differenz zwischen dem Awesome Oscillator und dessen 5-Perioden-SMA berechnen.
2. Die letzten 22 AC-Werte speichern, um auf Offsets von 0, 7, 14 und 21 Balken zuzugreifen.
3. Den Perzeptron-Ausgang berechnen:
   `P = (X1-100)*AC[0] + (X2-100)*AC[7] + (X3-100)*AC[14] + (X4-100)*AC[21]`.
4. Wenn `P > 0` eine Long-Position öffnen oder halten; wenn `P < 0` eine Short-Position öffnen oder halten.
5. Wenn eine Position mindestens `StopLoss` Punkte über dem anfänglichen Stop-Niveau gewinnt:
   - Wenn das Perzeptron die Richtung ändert, die Position umkehren.
   - Andernfalls den Stop auf den neuen Preis minus/plus `StopLoss` nachziehen.

## Parameter

- **X1** – Gewicht für den aktuellen AC-Wert (Standard 288).
- **X2** – Gewicht für AC vor 7 Balken (Standard 216).
- **X3** – Gewicht für AC vor 14 Balken (Standard 144).
- **X4** – Gewicht für AC vor 21 Balken (Standard 72).
- **Stop Loss** – Trailing- und Umkehrschwelle in Preiseinheiten (Standard 300).
- **Volume** – Ordervolumen (Standard 1).
- **Candle Type** – Kerzenserie, die abonniert werden soll (Standard 5 Minuten).

## Handelsregeln

- Long einsteigen, wenn `P > 0` und keine Position offen ist.
- Short einsteigen, wenn `P < 0` und keine Position offen ist.
- Bei offenen Positionen den Stop-Loss verschieben, nachdem sich der Kurs um `Stop Loss * 2` in Gewinnrichtung bewegt hat.
- Die Position umkehren, wenn der Perzeptron-Ausgang zu diesem Zeitpunkt das Vorzeichen ändert.

## Originalversion

Konvertiert aus dem MQL4-Skript `auto_m5.mq4` in `MQL/11102`.
