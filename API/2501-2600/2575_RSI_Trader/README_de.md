# RSI Trader-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Diese Strategie repliziert den MetaTrader-Expertenberater *"RSI trader v0.15"* in der StockSharp High-Level API. Sie richtet die Trendrichtung zwischen Preisaktion und einem geglätteten Relativen Stärke-Index (RSI) aus. Der Handel wird standardmäßig auf einem einzigen Instrument mit Ein-Stunden-Kerzen durchgeführt, aber der Zeitrahmen ist über den Parameter `CandleType` konfigurierbar.

## Handelslogik
1. Berechnung eines Standard-RSI mit einem konfigurierbaren Zeitraum.
2. Glättung des RSI mit zwei einfachen gleitenden Durchschnitten (SMA): ein schneller Signal-Durchschnitt und ein langsamerer Bestätigungs-Durchschnitt.
3. Verfolgung von zwei gleitenden Durchschnitten des Schlusskurses: ein kurzer einfacher gleitender Durchschnitt und ein langer gewichteter gleitender Durchschnitt, um das ursprüngliche MQL SMA/LWMA-Paar anzunähern.
4. Generierung von Trendzuständen bei jeder abgeschlossenen Kerze:
   - **Bullische Ausrichtung**: kurzer Preis-SMA über dem langen **und** schneller RSI-SMA über dem langsamen.
   - **Bärische Ausrichtung**: kurzer Preis-SMA unter dem langen **und** schneller RSI-SMA unter dem langsamen.
   - **Seitwärts / Uneinigkeit**: gleitende Durchschnitte zeigen in entgegengesetzte Richtungen, was keinen klaren Trend signalisiert.
5. Reaktion auf den erkannten Zustand:
   - Eröffnen einer Long-Position, wenn bullische Ausrichtung erscheint und derzeit keine Position offen ist.
   - Eröffnen einer Short-Position, wenn bärische Ausrichtung erscheint und derzeit keine Position offen ist.
   - Sofortiges Schließen jeder offenen Position, wenn der Seitwärtszustand erkannt wird, was den Schutzausstieg der MQL-Version spiegelt.
6. Der optionale Umkehrmodus dreht alle Einstiegsrichtungen um und ermöglicht es dem Benutzer, gegen den Trend der erkannten Signale zu handeln.

Die Strategie respektiert die integrierte Schutzbehandlung von StockSharp und erfordert abgeschlossene Kerzen vor einer Aktion.

## Parameter
| Parameter | Beschreibung | Standard |
|-----------|--------------|----------|
| `RsiPeriod` | Rückblickzeitraum für die RSI-Berechnung. | 14 |
| `ShortRsiMaPeriod` | Länge des schnellen SMA, angewendet auf RSI-Werte. | 9 |
| `LongRsiMaPeriod` | Länge des langsamen SMA, angewendet auf RSI-Werte. | 45 |
| `ShortPriceMaPeriod` | Länge des kurzen SMA, angewendet auf Schlusskurse. | 9 |
| `LongPriceMaPeriod` | Länge des langen gewichteten gleitenden Durchschnitts auf Preise. | 45 |
| `Reverse` | Wenn `true`, werden Kauf- und Verkaufsorders vertauscht (spiegelt den ursprünglichen "Reverse"-Eingang). | `false` |
| `CandleType` | Datentyp für Preiskerzen. Standard ist ein Ein-Stunden-Zeitrahmen. | `1h` |

Alle ganzzahligen Parameter setzen Optimierungsbereiche frei, die die Flexibilität der MetaTrader-Experten-Eingabeeinstellungen widerspiegeln.

## Risikomanagement
- Positionen werden sofort geschlossen, wenn Preis- und RSI-Trends nicht übereinstimmen (Seitwärtszustand), was das sofortige Ausstiegsverhalten des EA reproduziert.
- `StartProtection()` wird beim Start aktiviert, um mit der Schutzinfrastruktur von StockSharp zusammenzuarbeiten.

## Hinweise
- Die Strategie nutzt die Basis-`Volume`-Eigenschaft von `Strategy` zur Definition der Handelsgröße.
- Nur abgeschlossene Kerzen werden verarbeitet; partielle Aktualisierungen werden ignoriert, um vorzeitige Signale zu vermeiden.
- Der gewichtete gleitende Durchschnitt wird verwendet, um dem ursprünglichen langen LWMA auf Preisschlüssen zu entsprechen.
