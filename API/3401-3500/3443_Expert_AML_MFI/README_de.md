# Experten-AML-MFI-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **Expert AML MFI Strategy** repliziert den MetaTrader 5 Expert Advisor „Expert_AML_MFI“ unter Verwendung des StockSharp High-Level API. Es konzentriert sich auf das Kerzenmuster *Meeting Lines* und validiert jedes Signal mit dem Oszillator **Money Flow Index (MFI)**. Die Strategie verwaltet automatisch die erforderlichen Kerzenstatistiken, identifiziert bullische oder bärische Umkehrungen und verwaltet offene Positionen, wenn der MFI überverkaufte oder überkaufte Schwellenwerte überschreitet.

## Handelslogik
1. **Kerzenvorbereitung** – die Strategie abonniert den ausgewählten Zeitrahmen (standardmäßig H1) und behält die letzten beiden abgeschlossenen Kerzen zusammen mit dem gleitenden Durchschnitt der Kerzenkörper bei. Die durchschnittliche Körpergröße wird durch Anwendung eines `SimpleMovingAverage` auf die absolute Kerzenkörpergröße berechnet, was die MT5-Implementierung widerspiegelt.
2. **Mustererkennung** – zwei spezialisierte Helfer erkennen *Bullish Meeting Lines* und *Bearish Meeting Lines*:
   - Bullisches Setup: eine lange bärische Kerze, gefolgt von einer langen zinsbullischen Kerze, die in der Nähe des vorherigen Schlusskurses schließt (innerhalb von 10 % des durchschnittlichen Körpers).
   - Bärisches Setup: eine lange bullische Kerze, gefolgt von einer langen bärischen Kerze mit ähnlichen Schlusskursen.
3. **MFI-Bestätigung** – der vorherige MFI-Wert muss für Long-Trades unter dem bullischen Einstiegsniveau (Standard 40) oder für Short-Trades über dem bärischen Einstiegsniveau (Standard 60) liegen.
4. **Positionsmanagement** – die letzten beiden MFI-Messwerte werden verfolgt, um Überschreitungen der überverkauften (30) und überkauften (70) Ebenen zu erkennen:
   - Ein Kreuz über einem der beiden Niveaus führt zum Ausstieg aus Short-Positionen.
   - Ein Cross unter dem überverkauften Niveau oder über dem überkauften Niveau führt zum Ausstieg aus Long-Positionen.
5. **Auftragsausführung** – wenn ein gültiges Muster und eine MFI-Bestätigung vorliegen, schließt die Strategie alle gegenteiligen Engagements und eröffnet eine neue Position zum Markt mit dem konfigurierten Basisvolumen.

## Parameter
| Name | Beschreibung | Standard |
| --- | --- | --- |
| `CandleType` | Für das Kerzenabonnement verwendeter Zeitrahmen. | Zeitrahmen 1 Stunde |
| `MfiPeriod` | Anzahl der Balken für den MFI-Oszillator. | 12 |
| `BodyAveragePeriod` | Fensterlänge für die Berechnung der durchschnittlichen Körpergröße. | 4 |
| `BullishEntryLevel` | Maximal zulässiger MFI-Wert für bullische Einstiege. | 40 |
| `BearishEntryLevel` | Erforderlicher Mindest-MFI-Wert für rückläufige Einstiege. | 60 |
| `OversoldLevel` | Überverkaufter Wert, der für Ausstiegssignale verwendet wird. | 30 |
| `OverboughtLevel` | Überkaufter Wert, der für Ausstiegssignale verwendet wird. | 70 |
| `TradeVolume` | Auf neue Geschäfte angewendetes Basisauftragsvolumen. | 1 |

Dank der `StrategyParam`-Wrapper können alle Parameter direkt im StockSharp-Designer optimiert werden.

## Indikatoren und Visuals
- **Geldflussindex** – zur Bestätigung an das Kerzenabonnement gebunden und auf dem Chart angezeigt, wenn ein Chartbereich verfügbar ist.
- **Einfacher gleitender Durchschnitt der Kerzenkörper** – nur für den internen Gebrauch, Reproduktion der MT5-Durchschnittskörperberechnung.

## Notizen
- Die Strategie ruft `StartProtection()` einmal auf, um integrierte Positionsschutzfunktionen zu aktivieren.
- Handelsbefehle verwenden die Helfer `BuyMarket` und `SellMarket`, um die aktuelle Position zu reduzieren, bevor eine neue eröffnet wird, was dem Verhalten des Expertenberaters MetaTrader entspricht.
- Gemäß den Projektanforderungen wird kein Python-Port bereitgestellt.
