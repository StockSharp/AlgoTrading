# Bollinger Ausbruch-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Bollinger Ausbruch versucht, Bewegungen zu erfassen, die über die Bollinger Bands
hinausgehen und weiter in dieselbe Richtung laufen. Wenn der Preis über das obere
Band oder unter das untere Band schließt, tritt die Strategie in Richtung des
Ausbruchs ein, sofern optionale Bestätigungen den Trade unterstützen.

RSI-, Aroon- und Moving-Average-Filter können aktiviert werden, um Momentum und
Trend zu validieren. Ein optionaler Stop-Loss hilft, das Risiko zu kontrollieren.
Positionen werden geschlossen, wenn der Preis das gegenüberliegende Band erreicht
oder der Stop ausgelöst wird.

Dieser Ansatz bevorzugt Märkte, die zu starken Trends neigen, bei denen Band-Ausbrüche
zu einer Fortsetzung statt zu Mean Reversion führen.

## Details
- **Daten**: Kurskerzen.
- **Einstiegskriterien**:
  - **Long**: Schlusskurs über oberem Band und alle aktivierten Filter bestätigen.
  - **Short**: Schlusskurs unter unterem Band und alle aktivierten Filter bestätigen.
- **Ausstiegskriterien**: Berührung des gegenüberliegenden Bandes oder Stop-Loss wenn `UseSL`.
- **Stops**: Optionaler Stop-Loss (`UseSL`).
- **Standardwerte**:
  - `UseRSI` = True
  - `UseAroon` = False
  - `UseMA` = True
  - `UseSL` = True
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Long/Short
  - Indikatoren: Bollinger Bands, RSI, Aroon, Moving Average
  - Komplexität: Moderat
  - Risikolevel: Hoch
