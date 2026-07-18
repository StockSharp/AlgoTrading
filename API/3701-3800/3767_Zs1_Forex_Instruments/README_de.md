# Zs1-Strategie für Forex-Instrumente
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie reproduziert die abgesicherte Grid-Logik des MetaTrader-Experten **Zs1_www_forex-instruments_info**. Der Algorithmus eröffnet ein gleichzeitiges Kauf-/Verkaufspaar, überwacht, wie weit sich der Preis vom Startpunkt entfernt, und reagiert auf fünf diskrete Handelszonen. Der verbleibende Teil der Absicherung wird mit Martingal-Multiplikatoren gemittelt, während der Korb durch einen aktienbasierten Ausstieg geschützt ist.

## Kernverhalten

- Eröffnen Sie eine anfängliche Marktabsicherung (ein Kauf und ein Verkauf) mit dem konfigurierten Basisvolumen.
- Sobald eine Seite profitabel wird, schließen Sie sie und behalten Sie die Verliererseite als Ankerorder bei.
- Verfolgen Sie Preisverschiebungen mithilfe des Parameters `Orders Space (pips)`. Wenn eine neue Zone erreicht wird, führen Sie dieselbe Verzweigungslogik wie beim ursprünglichen Experten aus:
  - Zone −2: Korb bei Gewinn schließen, sonst Durchschnitt gegen die Bewegung.
  - Zone −1: Fügen Sie eine Position gegenüber dem ursprünglichen Anker hinzu.
  - Zone 0: Fügen Sie eine Position in Richtung des Ankers hinzu.
  - Zone +1: Schließen Sie den Korb bei Gewinn, andernfalls öffnen Sie die gegenüberliegende Seite.
- Wenn drei oder mehr Geschäfte aktiv sind, beenden Sie den Handel sofort, wenn der variable Gewinn nicht negativ ist.
- Nachdem alle Positionen geschlossen sind, startet der Zyklus automatisch neu.

## Parameter

| Name | Beschreibung |
| --- | --- |
| `Orders Space (pips)` | Abstand in Pips zwischen benachbarten Rasterebenen. |
| `Zone Offset (pips)` | Zusätzlicher Puffer, der durchbrochen werden muss, bevor eine neue Zone bestätigt wird. |
| `Initial Volume` | Basisvolumen, das für die Eröffnungshedge und für die Martingalskalierung verwendet wird. |

## Notizen

- Die Martingal-Multiplikatoren folgen der ursprünglichen Tunnelsequenz (1, 3, 6, 12, ...).
- Bei der Volumenvalidierung werden die Mindest-, Höchst- und Schrittbeschränkungen des Wertpapiers berücksichtigt, bevor eine Bestellung gesendet wird.
- Alle Entscheidungen basieren auf den besten Gebots-/Briefaktualisierungen aus Level1-Daten und entsprechen der tickbasierten Logik der MQL-Version.
