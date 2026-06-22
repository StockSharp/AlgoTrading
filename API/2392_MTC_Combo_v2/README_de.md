# MTC Combo v2-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Konvertiert aus dem MetaTrader-Skript "MTC Combo v2 (barabashkakvn's edition)".

## Logik
- Verwendet die Steigung eines gleitenden Durchschnitts zur Bestimmung des grundlegenden Trends.
- Optionaler Perzeptron-Filter berechnet die gewichtete Summe der letzten Eröffnungspreisunterschiede über konfigurierbare Verzögerungen.
- Der Parameter `Pass` wählt aus, welche Perzeptron-Zweige verwendet werden:
  - 4: erfordert perceptron3 > 0 und perceptron2 > 0 für Long; perceptron3 <= 0 und perceptron1 < 0 für Short.
  - 3: verwendet perceptron2 > 0 für Long.
  - 2: verwendet perceptron1 < 0 für Short.
  - andere Werte: handelt nur auf Basis der MA-Steigung.

Stop-Loss- und Take-Profit-Level werden aus den Parametern `Sl*` und `Tp*` entnommen.

## Parameter
- `MaPeriod` – Länge des gleitenden Durchschnitts.
- `P2`, `P3`, `P4` – Verzögerungen für Perzeptronen.
- `Pass` – Entscheidungsmodus.
- `Sl1`/`Tp1`, `Sl2`/`Tp2`, `Sl3`/`Tp3` – Stop und Ziel für jeden Zweig.
- `CandleType` – zu verarbeitende Kerzenserie.

## Hinweise
Die Strategie hält jeweils eine einzelne Position und schließt sie, wenn Stop Loss oder Take Profit erreicht wird.

## Haftungsausschluss
Nur für Bildungszwecke. Keine Anlageberatung.
