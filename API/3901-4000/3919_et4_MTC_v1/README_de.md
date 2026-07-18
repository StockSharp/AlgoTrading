# Et4 MTC v1-Strategie (StockSharp Conversion)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
- **Herkunft**: MetaTrader 4 Fachberater `et4_MTC_v1.mq4` aus der GlobeInvestFund-Sammlung.
- **Zweck**: Bereitstellung einer StockSharp-nativen Vorlage, die die Money-Management-Helfer und Timing-Sicherheitsmaßnahmen des ursprünglichen Beraters widerspiegelt und gleichzeitig die Handelseinstiegs-/-ausstiegslogik für die weitere Entwicklung offen lässt.
- **Handelsstil**: Skelettstrategie – standardmäßig werden keine automatischen Einträge generiert. Die Klasse konzentriert sich auf die Durchsetzung von Zeitbeschränkungen und die Replikation der Parameterschnittstelle des MQL4-Skripts, damit sie als Grundlage für benutzerdefinierte Regeln dienen kann.

## Kernfunktionen
1. **Parameterparität**
   - Macht die Eigenschaften `TakeProfit`, `StopLoss`, `Slippage`, `Lots` und `EnableLogging` verfügbar, die den externen Variablen des Experten eins zu eins zugeordnet sind.
   - Fügt `TradeCooldown` hinzu, um die hartcodierte 30-Sekunden-Verzögerung zwischen Vorgängen im Quellcode zu beschreiben.
   - Veröffentlicht den Diagrammdatenkontext über `CandleType`, um das Verhalten des „aktuellen Zeitrahmens“ von MetaTrader-Diagrammen zu emulieren.
2. **Balance-basierte Positionsgrößenbestimmung**
   - Unterstützt negative Lot-Eingaben (ursprüngliche Skript-Standardeinstellung), um das Bestellvolumen aus dem Kontostand abzuleiten: `floor((balance / 1000 * |Lots|) / 10) / 10`, mit einem Minimum von 0,1 Lot.
3. **Durchsetzung der Handelsabklingzeit**
   - Blockiert alle weiteren Handelsversuche, bis `TradeCooldown` nach der letzten Auftragsaktivität (Registrierung, Änderung, Stornierung oder ausgeführter Handel) verstrichen ist. Dies spiegelt den `CurTime() - LastTradeTime < 30`-Wächter in `start()` wider.
4. **Erkennung neuer Kerzen**
   - Behält die `CheckLevels`-Semantik bei, indem `IsNewCandle` durch einen Zeitvergleich zwischen aufeinanderfolgenden fertigen Kerzen markiert wird. Während das Flag intern ist, können die Hooks in `OpenPosition`, `ManagePosition` und `ClosePosition` es verwenden, wenn benutzerdefinierte Logik hinzugefügt wird.
5. **Hochrangige StockSharp API-Nutzung**
   - Verwendet `SubscribeCandles().Bind(...)` für die Datenbereitstellung.
   - Wendet `StartProtection()` einmal beim Start an und befolgt dabei die Best Practices des Frameworks.
   - Weist nicht explizit benutzerdefinierte Sammlungen zu und fordert den Indikatorverlauf nicht explizit an, was den projektweiten Richtlinien entspricht.

## Parameterreferenz
| Eigentum | Standard | Optimierbar | Beschreibung |
| --- | --- | --- | --- |
| `TakeProfit` | 150 | ✔️ | Zielentfernung in Punkten (Platzhalter für benutzerdefinierte Ausgangsregeln). |
| `Lots` | -10 | ✔️ | Feste Lose bei ≥ 0; Balance-proportionale Größe, wenn negativ. |
| `StopLoss` | 50 | ✔️ | Stoppdistanz in Punkten, bereit für Erweiterungslogik. |
| `Slippage` | 3 | ✖️ | Ausführungstoleranz in Punkten; aus Kompatibilitätsgründen aufbewahrt. |
| `EnableLogging` | `false` | ✖️ | Druckt Informationsmeldungen, wenn die Abklingzeit den Handel blockiert. |
| `TradeCooldown` | 30 Sekunden | ✖️ | Minimale Verzögerung zwischen aufeinanderfolgenden Trades. |
| `CandleType` | 1-Minuten-Zeitrahmenkerzen | ✖️ | Marktdatenabonnement, das für das Candle-Timing verwendet wird. |

## Ausführungsablauf
1. **Startup**
   - Berechnet den anfänglichen `Volume` mithilfe des ausgleichsbewussten Größenhilfsgeräts.
   - Abonniert den konfigurierten Kerzenstrom und startet Schutzmechanismen.
2. **Bei Kerzenschluss**
   - Bestätigt, dass die Kerze fertig ist, bevor fortgefahren wird (entspricht dem Schließen von `Time[0]` in MT4).
   - Aktualisiert den New-Candle-Tracker (`_isNewCandle`).
   - Überprüft `IsFormedAndOnlineAndAllowTrading()`, um den Engine-Status zu berücksichtigen.
   - Bricht ab, wenn die Handelsabklingzeit aktiv ist, und protokolliert die nächste verfügbare Zeit, wenn sie aktiviert ist.
   - Führt Platzhalter-Hooks (`OpenPosition`, `ManagePosition`, `ClosePosition` aus und kehrt früh zurück, wenn ein Schritt eine Aktion ausführt.
3. **Rückrufe bestellen und handeln**
   - `OnOrderRegistered`, `OnOrderChanged`, `OnOrderCanceled` und `OnNewMyTrade` aktualisieren `_lastTradeTime` und stellen sicher, dass jede Art von Vorgang die Abklingzeit zurücksetzt, genau wie es die Wrapper-Funktionen (`MOrderSend`, `MOrderModify` usw.) im Originalcode getan haben.

## Erweitern der Vorlage
- Implementieren Sie die Eingabelogik in `OpenPosition` (geben Sie `true` zurück, nachdem Sie Befehle gesendet haben, um die weitere Verarbeitung derselben Kerze zu stoppen).
- Fügen Sie das Stop-Management-Verhalten in `ManagePosition` ein, indem Sie die beibehaltenen Parameter verwenden.
- Füllen Sie `ClosePosition` mit Exit-Regeln. Die Methode gibt derzeit `false` zurück, um dem ruhenden Verhalten des Quellskripts zu entsprechen.
- Verwenden Sie `_isNewCandle`, wenn die Regeln einmal pro Balken ausgelöst werden müssen.

## Portierungshinweise
- Der MQL4-Experte wurde ohne Handelsregeln versendet; Es waren nur Infrastrukturroutinen vorhanden. Folglich priorisiert die StockSharp-Konvertierung die Parität unterstützender Funktionen, anstatt spekulative Indikatoren hinzuzufügen.
- Alle Kommentare sind in englischer Sprache verfasst und entsprechen den Repository-Standards.
- Tabulatoren werden zum Einrücken verwendet, um den in `AGENTS.md` definierten Stilrichtlinien zu entsprechen.
- Gemäß der Konvertierungsanforderung wird absichtlich auf die Python-Übersetzung verzichtet.

## Nutzungsschritte
1. Referenzieren Sie `Et4MtcV1Strategy` in einem StockSharp-Projekt und weisen Sie vor dem Start ein `Security` und ein `Portfolio` zu.
2. Passen Sie `Lots` oder andere Parameter über die bereitgestellten Eigenschaften oder UI-Bindungen an.
3. Überschreiben Sie die Platzhaltermethoden oder erben Sie von der Klasse, um konkrete Handelslogik einzufügen.
4. Führen Sie die Strategie aus. Der Abklingschutz stellt sicher, dass innerhalb des angegebenen Intervalls keine aufeinanderfolgenden Vorgänge stattfinden.

## Testen
- Dieser Vorlage liegen keine automatisierten Tests bei, da in der Upstream-Quelle auch ausführbare Regeln fehlten. Manuelle Strategieerweiterungen sollten relevante Tests einführen, wenn konkretes Handelsverhalten implementiert wird.
