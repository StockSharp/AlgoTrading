# KPrmSt-Kreuzungs-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Die KPrmSt Cross-Strategie ist eine Portierung des MetaTrader 5-Experten `exp_kprmst.mq5`. Sie verwendet einen Stochastic-ähnlichen Oszillator namens KPrmSt, um Umkehrungen zu erfassen, wenn die Hauptlinie des Oszillators die Signallinie kreuzt.

Die Strategie abonniert Kerzen eines konfigurierbaren Zeitrahmens und berechnet den `Stochastic`-Indikator (als KPrmSt-Annäherung). Wenn die %K-Linie die %D-Linie von oben kreuzt, wird eine Long-Position eröffnet; wenn %K die %D-Linie von unten kreuzt, wird eine Short-Position eröffnet. Bestehende Positionen werden entsprechend umgekehrt.

## Parameter
- `Candle Type` – Zeitrahmen der Kerzen für die Berechnungen.
- `K Period` – Anzahl der Balken für die Berechnung der Hauptlinie.
- `D Period` – Periode für die Glättung der Signallinie.
- `Slowing` – zusätzliche Glättung von %K.
- `Stop Loss` – Schutzloss in Preiseinheiten. Auf 0 setzen zum Deaktivieren.
- `Take Profit` – Zielgewinn in Preiseinheiten. Auf 0 setzen zum Deaktivieren.

## Handelslogik
1. Die Strategie berücksichtigt nur abgeschlossene Kerzen.
2. Die Stochastic-Oszillatorwerte werden gespeichert, um Kreuzungen zu erkennen.
3. Wenn %K nach einem Aufenthalt darüber unter %D fällt, wird eine Long-Position eröffnet oder die Short-Position geschlossen.
4. Wenn %K nach einem Aufenthalt darunter über %D steigt, wird eine Short-Position eröffnet oder die Long-Position geschlossen.
5. Optionale Stop-Loss- und Take-Profit-Niveaus schließen die Position bei Erreichen.

## Hinweise
- Der KPrmSt-Indikator aus dem Original-Experten wird durch StockSharps `Stochastic`-Indikator angenähert.
- Money-Management-Optionen aus dem Originalskript sind nicht implementiert.
- Die Strategie benötigt einen Marktdaten-Feed und Orderrouting, das von StockSharp unterstützt wird.
