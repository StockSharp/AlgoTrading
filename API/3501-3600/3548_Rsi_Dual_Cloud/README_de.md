# RSI Dual-Cloud-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **RSI Dual-Cloud-Strategie** ist eine StockSharp-Portierung des MetaTrader-Expertenberaters „RSI Dual Cloud EA“.
Es handelt mit einer konfigurierbaren Kerzenserie und analysiert zwei Berechnungen des Relative Strength Index (RSI) – und zwar schnell
und eine langsame Linie. Signale werden generiert, wenn der schnelle RSI einen definierten überverkauften/überkauften Wert erreicht, darin bleibt oder ihn verlässt
Zone oder wenn die schnelle Linie die langsame Linie kreuzt. Die Strategie kann optional ihre Signale invertieren und kann eingeschränkt werden
auf Nur-Lang- oder Nur-Kurz-Betrieb.

Die Strategie funktioniert nur mit Marktaufträgen. Bei Empfang eines neuen Signals wird die bestehende Position umgekehrt
Die Richtung wird geschlossen, bevor eine neue Position eröffnet wird. Die Positionsgröße wird über einen einzelnen Volumenparameter gesteuert.

## Signallogik
1. **Eingangssignal** – wird ausgelöst, wenn der schnelle RSI in die Zone eindringt:
   - Lang: vorheriges RSI über der unteren Ebene und aktuelles RSI darunter.
   - Kurz: vorheriges RSI unterhalb der oberen Ebene und aktuelles RSI darüber.
2. **Signal sein** – wird ausgelöst, solange das schnelle RSI innerhalb der Zone bleibt:
   - Lang: schnell RSI unter dem unteren Niveau.
   - Kurz: schnell RSI über dem oberen Niveau.
3. **Abfahrsignal** – wird ausgelöst, wenn der schnelle RSI die Zone verlässt:
   - Lang: vorheriges RSI unterhalb der unteren Ebene und aktuelles RSI darüber.
   - Kurz: vorheriges RSI über der oberen Ebene und aktuelles RSI darunter.
4. **Kreuzungssignal** – nutzt das Dual-Cloud-Verhalten:
   - Lang: schnelles RSI, das über das langsame RSI kreuzt.
   - Kurz: Schnelles RSI, das unter dem langsamen RSI kreuzt.

Jede Kombination der vier Bedingungen kann aktiviert werden. Damit Einträge erfolgen, muss mindestens eine Bedingung aktiv sein.
Wenn die Option **Reverse** aktiviert ist, werden Long- und Short-Signale vertauscht.

## Parameter
| Name | Beschreibung |
| --- | --- |
| **Kerzentyp** | Die für die Berechnungen verwendete Kerzenreihe (Standard: 1 Stunde). |
| **Schnell RSI / Langsam RSI** | Zeiträume für die schnellen und langsamen RSI-Berechnungen. |
| **Obere Ebene / Untere Ebene** | RSI Schwellenwerte für die überkauften und überverkauften Zonen. |
| **Bestellvolumen** | Volumen für Marktaufträge. |
| **Eingang / Sein / Verlassen / Überqueren nutzen** | Schaltet für jede Signalfamilie um. |
| **Geschlossene Kerzen** | Wenn aktiviert, werden Signale nur bei fertigen Kerzen ausgewertet. |
| **Umgekehrt** | Tauscht Long- und Short-Signale aus. |
| **Handelsmodus** | Beschränkt den Handel auf Long-, Short- oder beide Richtungen. |

## Nutzungshinweise
- Die Strategie abonniert eine einzelne Kerzenserie und führt zwei RSI-Indikatoren aus, die durch das übergeordnete API gebunden sind.
- Es werden nur Marktaufträge verwendet; Jedes offene Exposure in die entgegengesetzte Richtung wird geschlossen, bevor ein neuer Trade platziert wird.
- Die Standardkonfiguration entspricht dem ursprünglichen Expert Advisor (schnell RSI 5, langsam RSI 15, Stufen 25/75).
- Kombinieren Sie die Signalschalter, um die Anzeigekombinationen der MetaTrader-Version zu reproduzieren.
