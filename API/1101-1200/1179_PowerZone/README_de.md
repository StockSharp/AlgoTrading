# PowerZone Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie identifiziert "PowerZone"-Orderblöcke, die durch eine bearishe Kerze gefolgt von aufeinanderfolgenden bullischen Kerzen entstehen (und umgekehrt). Ein Ausbruch über die Bull-Zone löst einen Long-Trade aus, während ein Einbruch unter die Bear-Zone eine Short-Position eröffnet. Ziele und Stops basieren auf dem Bereich der Zone.

## Details

- **Einstiegskriterien**:
  - **Long**: Bearishe Kerze vor `Periods+1` Bars gefolgt von `Periods` bullischen Kerzen und Preis bricht über das Zonenhoch.
  - **Short**: Bullische Kerze vor `Periods+1` Bars gefolgt von `Periods` bearishen Kerzen und Preis bricht unter das Zonentief.
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**: Take-Profit und Stop-Loss als Vielfache des Zonenbereichs.
- **Indikatoren**: Keine.
- **Standardwerte**:
  - `Periods` = 5
  - `Threshold` = 0
  - `UseWicks` = false
  - `Take Profit Factor` = 1.5
  - `Stop Loss Factor` = 1
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Beide
  - Indikatoren: Keine
  - Stops: Ja
  - Komplexität: Moderat
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Moderat

## Berechnung und Ausführung

C# und Python verwenden abgeschlossene Kerzen. Auf Entscheidungsbar `t` ist der Anker `t - Periods - 1`; die folgenden `Periods` Bars bis `t - 1` müssen alle strikt über ihrer Eröffnung schließen für eine Bull-Zone, oder darunter für eine Bear-Zone. Der Anker hat die strikt entgegengesetzte Farbe; Dojis zählen nicht. Die aktuelle Entscheidungsbar bestätigt die Zone nicht. Vor Abschluss der Bestätigung ist die Zone unbekannt.

Erforderlich ist `100 * abs(Close[t - 1] - Anker-Close) / abs(Anker-Close) >= Threshold`; Anker-Close null ist ungültig. Die Zone besteht aus dem Ankerkörper `[min(Open, Close), max(Open, Close)]` oder bei `UseWicks=true` aus `[Low, High]`. Bestätigungsextreme vergrößern sie nicht; die Breite muss positiv sein. Letzte Bull- und Bear-Zone bleiben getrennt bis zur Ersetzung bestehen, mit höchstens einem akzeptierten Einstieg je Zone.

Long verlangt `aktueller Close > Bull-Zonenhoch`, Short `Close < Bear-Zonentief`. Gleichheit hält die Position. Eine neue Zone kann auf ihrer ersten Entscheidungsbar einsteigen, auch wenn der Preis bereits außerhalb liegt. Aus einer Nullposition `Volume` eröffnen oder mit `Volume + abs(Position)` vollständig umkehren; keine gleichgerichteten Zusätze. Sind beide Richtungen aus null gültig, hat Long Vorrang. Ausstehende Orders sperren weitere Orders. Keine EMA, Wartefrist oder anderen Indikatoren.

Beim akzeptierten Einstieg werden `Zonenbreite * StopLossFactor` und `Zonenbreite * TakeProfitFactor` eingefroren. Basis ist die tatsächliche Ausführung, bei gleichgerichteten Teilerhöhungen volumengewichtet; Reduzierungen behalten sie, Umkehrungen setzen sie zurück. Spätere Zonen bewegen die Abstände nicht. Lokaler Schutz prüft ausführbaren Long-Bid, Short-Ask und abgeschlossene Kerzenschlusskurse und schließt die gesamte Restposition per Marktorder, vor neuen Einstiegen auf derselben Bar. Hoch-/Tiefberührungen sind keine angenommenen Intrabar-Ausführungen; Kurslücken können den Ausführungspreis ändern.

Gemeinsame Standardwerte: API-Parameter `TakeProfitFactor=1.5`, `StopLossFactor=1`, fünfminütiger `CandleType`. Veröffentlichter Parametername ist `UseWicks`, C#-Eigenschaft `IsUseWicks`. `Periods` und Faktoren positiv; `Threshold` ist ein nichtnegativer Prozentsatz. Diese Definitionen präzisieren vorher nicht festgelegte Schwellen-, Grenz-, Verbrauchs- und Ausführungsbasisregeln.
