# Strategie zum Ausbruch der vorherigen Kerze
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie repliziert den klassischen MetaTrader-Expert-Advisor "BreakOut" von Soubra2003. Sie überwacht das Hoch und Tief
der zuletzt abgeschlossenen Kerze und reagiert, wenn der aktuelle Schlusskurs diese Referenzniveaus durchbricht. Der Ansatz
ist vollständig symmetrisch: Long-Positionen werden bei bullischen Ausbrüchen eröffnet, und Short-Positionen werden bei
bearischen Einbrüchen eröffnet. Optionale Stop-Loss- und Take-Profit-Puffer in Preiseinheiten ermöglichen es dem Benutzer,
das Risiko zu begrenzen oder Gewinne zu sichern.

## Überblick

- Abonniert eine einzelne Kerzenserie (standardmäßig 1-Stunden-Zeitrahmen).
- Speichert Hoch und Tief der vorherigen Kerze als Ausbruchsauslöser.
- Handelt nur bei Kerzenschluss, um die ursprüngliche tick-basierte Logik ohne Intrabar-Daten zu spiegeln.
- Unterstützt sowohl Long- als auch Short-Trades und bleibt immer flach, wenn keine Ausbruchsbedingung aktiv ist.

## Handelsregeln

1. **Ausbruchseinstieg / Umkehr**
   - Wenn der Schlusskurs der aktuellen abgeschlossenen Kerze strikt über dem Hoch der vorherigen Kerze liegt:
     - Jede offene Short-Position wird zu Marktpreisen geschlossen.
     - Unmittelbar danach wird eine neue Long-Position eröffnet (die Umkehr erfolgt innerhalb desselben Kerzenverarbeitungsschritts).
   - Wenn der Schlusskurs strikt unter dem Tief der vorherigen Kerze liegt:
     - Jede offene Long-Position wird zu Marktpreisen geschlossen.
     - Anschließend wird eine neue Short-Position eröffnet.
2. **Schützende Ausstiege (optional)**
   - Wenn ein Stop-Loss-Offset konfiguriert ist (> 0), verlässt die Strategie ein Long, wenn der Schlusskurs `offset` Einheiten
     unter den Einstiegspreis fällt, oder verlässt ein Short, wenn der Schlusskurs `offset` Einheiten über den Einstiegspreis steigt.
   - Wenn ein Take-Profit-Offset konfiguriert ist (> 0), verlässt die Strategie ein Long, wenn der Schlusskurs `offset` Einheiten
     über den Einstiegspreis steigt, oder verlässt ein Short, wenn der Schlusskurs `offset` Einheiten unter den Einstiegspreis fällt.
3. **Zustandsreset**
   - Nach jeder verarbeiteten Kerze werden das zuletzt gesehene Hoch und Tief zu den neuen Ausbruchs-Referenzniveaus.

## Parameter

- **Candle Type** – Datentyp für das Abonnement (standardmäßig stündlicher Zeitrahmen). Stellen Sie dies auf die Balkengröße ein,
  die dem in MetaTrader für den ursprünglichen Expert verwendeten Chart entspricht.
- **Stop Loss** – Abstand in absoluten Preiseinheiten zwischen dem Einstiegspreis und dem Schutz-Stop. Bei `0` belassen, um
  die Stop-Loss-Behandlung zu deaktivieren.
- **Take Profit** – Abstand in absoluten Preiseinheiten zwischen dem Einstiegspreis und dem Gewinnziel. Bei `0` belassen, um
  die Take-Profit-Behandlung zu deaktivieren.

## Hinweise

- Die Stop-Loss- und Take-Profit-Berechnungen werden auf Kerzenschlusspreisen durchgeführt. Die ursprüngliche MQL4-Version hängte
  statische SL/TP-Niveaus an die Orders; in StockSharp werden die Ausstiege simuliert, indem Marktorders gesendet werden, sobald
  die Schwellenwerte erreicht sind.
- Verwenden Sie instrumentenspezifische Preiserhöhungen beim Konfigurieren der Offsets. Wenn das Instrument beispielsweise mit
  einer Tick-Größe von 0.01 handelt und Sie einen 20-Tick-Stop möchten, setzen Sie den Stop-Loss-Parameter auf `0.20`.
- Da die Logik immer auf die unmittelbar vorangehende Kerze verweist, funktioniert die Strategie am besten bei Trendingstrumenten
  oder während hochvolatiler Sitzungen, bei denen Ausbrüche bedeutungsvoll sind.

## Herkunft

- **Quelle**: `MQL/17306/BreakOut.mq4` (BreakOut-Expert-Advisor von Soubra2003)
- **Autor**: https://www.mql5.com/en/users/soubra2003
