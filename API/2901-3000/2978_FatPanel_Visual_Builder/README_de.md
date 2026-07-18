# FatPanel Visueller Builder Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die **FatPanel Visueller Builder Strategie** ist eine StockSharp-Übersetzung des Legacy FAT Panel Expert Advisors aus MetaTrader. Die ursprüngliche MQL-Implementierung bot eine reichhaltige Drag-and-Drop-Leinwand, auf der Benutzer Indikator-, Logik-, Zustands- und Auftragsblöcke verknüpfen konnten. Dieser C#-Port behält die modulare Philosophie bei, drückt aber jede Blockverbindung durch ein einzelnes JSON-Dokument aus, das die Strategie beim Start liest.

## Wie die Konvertierung funktioniert

* Das MQL-Panel erstellte Buttons, Tabs und einen timer-basierten Dispatcher. Diese UI-Anliegen werden vollständig entfernt. Stattdessen analysiert die Strategie den `Configuration`-Parameter (eine JSON-Zeichenfolge) und instanziiert die entsprechenden Signal- und Logikblöcke intern.
* Blöcke werden auf jeder abgeschlossenen Kerze des konfigurierten `CandleType` ausgewertet. Indikatorblöcke verwenden StockSharp-Indikatoren (`SMA`, `EMA`, `SMMA`, `WMA`) und verlassen sich nie auf manuelle Puffer.
* Die ursprünglichen Auftragsblöcke erlaubten Symbol-Auswahl, Stop-Loss und Take-Profit in "Punkten". In StockSharp wird die Standardsicherheit aus `Strategy.Security` übernommen; Stop-Loss und Take-Profit werden durch die Strategieparameter `StopLossPoints` und `TakeProfitPoints` wiedereingeführt und mit `Security.PriceStep` in absolute Preisabstände umgerechnet.
* Zeit- und Wochentag-Zustandsfilter spiegeln die MQL-Logik wider. Das Bid-Preis-Signal abonniert Level1-Daten nur, wenn mindestens eine Regel dies anfordert, und repliziert damit das On-Demand-Aktualisierungsverhalten des Panel-Dispatchers.

## Parameter

| Parameter | Beschreibung |
| --- | --- |
| `CandleType` | Datentyp und Zeitrahmen, der jedes Signal speist. |
| `Configuration` | JSON-Dokument, das Regeln, Bedingungen und Aktionen beschreibt. Der Standardwert reproduziert die EMA/SMA-Kreuzungsstrategie aus dem Panel. |
| `Volume` | Standard-Ordergröße, die von Aktionen verwendet wird, es sei denn, eine Regel überschreibt sie. |
| `StopLossPoints` | Abstand in Preisschritten für den integrierten Risikoausschutz. Auf `0` setzen, um den Stop-Loss zu deaktivieren. |
| `TakeProfitPoints` | Abstand in Preisschritten für den integrierten Take-Profit. Auf `0` setzen zum Deaktivieren. |

`StopLossPoints` und `TakeProfitPoints` werden nur aktiviert, wenn ein positiver Wert angegeben wird **und** die Sicherheit einen gültigen `PriceStep` exponiert.

## Konfigurationsstruktur

Das JSON-Schema ist so gestaltet, dass es der FAT Panel-Blocksprache nahe bleibt:

```json
{
  "rules": [
    {
      "name": "Regelname (optional)",
      "all": [ /* Bedingungen, die alle wahr sein müssen */ ],
      "any": [ /* optionale Bedingungen, mindestens eine muss wahr sein */ ],
      "none": [ /* optionale Bedingungen, die alle falsch sein müssen */ ],
      "action": { "type": "Buy" | "SellShort" | "Close", "volume": 1.0 }
    }
  ]
}
```

Jedes Bedingungselement hat ein `type`-Feld mit einem der folgenden Werte:

| Typ | JSON-Felder | Zweck |
| --- | --- | --- |
| `comparison` | `operator`, `left`, `right`, `threshold` | Verbindet zwei Signalblöcke durch logische Operatoren (`Greater`, `Less`, `Equal`, `CrossAbove`, `CrossBelow`). Schwellenwerte werden als absolute Preisdifferenzen interpretiert. Kreuzungsoperatoren feuern, wenn die vorherige Kerze auf der entgegengesetzten Seite war und die aktuelle Differenz den Schwellenwert übersteigt. |
| `position` | `required` | Spiegelt die FAT-Panel-Zustandsblöcke wider (`Any`, `FlatOnly`, `FlatOrShort`, `FlatOrLong`, `LongOnly`, `ShortOnly`). |
| `time` | `start`, `end` | Intraday-Sitzungsfilter im `HH:mm`-Format. Start > Ende behält das Übernacht-Verhalten des MQL-Panels. |
| `dayOfWeek` | `days` | Liste der Tagesnamen. Wenn weggelassen, ist die Bedingung standardmäßig Montag–Freitag, was den Panel-Standardwerten entspricht. |

Signale (`left` / `right`) werden definiert als:

```json
{ "type": "MovingAverage", "period": 20, "method": "Exponential", "price": "Close" }
{ "type": "Bid" }
{ "type": "Constant", "level": 1.2345 }
```

* `MovingAverage` unterstützt `Simple`, `Exponential`, `Smoothed` und `LinearWeighted` Methoden mit jeder der OHLC-Preisquellen. Der Indikator teilt den Kerzenstrom der Strategie, genau wie das Panel chart-ausgewählte Zeitrahmen verwendete.
* `Bid` verwendet den letzten besten Geldkurs aus Level1-Aktualisierungen (fällt auf den Kerzen-Schlusskurs zurück, bis ein Kurs eintrifft).
* `Constant` reproduziert den HLINE-Block und liefert ein statisches Niveau.

Regelaktionen replizieren die Auftragsblöcke:

* `Buy` – öffnet oder kehrt zu einer Long-Position um, wenn die aktuelle Position flach oder short ist.
* `SellShort` – öffnet oder kehrt zu einer Short-Position um, wenn die Position flach oder long ist.
* `Close` – verlässt jede offene Position mit `ClosePosition()`.

Ein aktionsspezifisches `volume` kann den Standard-`Volume`-Parameter überschreiben.

## Ausführungsablauf

1. Wenn die Strategie startet, analysiert sie das Konfigurations-JSON. Ungültige Dokumente stoppen die Strategie und emittieren ein Fehlerlog.
2. Indikatoren werden instanziiert und gecacht, damit mehrere Regeln dieselben Signaldefinitionen ohne doppelte Berechnungen wiederverwenden können.
3. Für jede abgeschlossene Kerze aktualisiert die Strategie Signalwerte und wertet dann jede Regel der Reihe nach aus. `all`-Bedingungen müssen alle passen, `any` muss mindestens einmal passen (falls angegeben), und `none` muss vollständig scheitern.
4. Wenn eine Aktion ausgelöst wird, protokolliert die Strategie den Regelnamen und führt die angeforderte Marktorder aus.
5. Optionale Stop-Loss- und Take-Profit-Schutzmaßnahmen werden einmal während `OnStarted` mit den angegebenen Punktabständen aktiviert.

## Einschränkungen und Hinweise

* Nur die primäre `Strategy.Security` wird unterstützt. Cross-Symbol-Routing aus dem ursprünglichen Panel würde mehrere Strategieinstanzen erfordern.
* Der MQL-Dispatcher erlaubte tiefes Verschachteln von Logikblöcken (z.B. AND innerhalb von OR). Die JSON-Struktur bietet ähnliche Kontrolle durch die `all`/`any`/`none`-Arrays, aber extrem komplexe Graphen müssen möglicherweise noch manuell angepasst werden.
* Der `Cross`-Operator verwendet nur die letzte Kerze. Der MQL-Block exponierte einen Lookback-Puffer und Delta in "Punkten"; passen Sie das `threshold`-Feld an, um die erforderliche Empfindlichkeit zu emulieren.
* UI-Funktionen wie Drag-Positionen, Dialogfenster und Toolbar-Icons haben kein direktes Äquivalent in StockSharp und werden bewusst weggelassen.

## Beispielkonfiguration

Die in der Strategie eingebettete Standardkonfiguration wird unten zur Bequemlichkeit reproduziert:

```json
{
  "rules": [
    {
      "name": "EMA crosses above SMA",
      "all": [
        {
          "type": "comparison",
          "operator": "CrossAbove",
          "left": { "type": "MovingAverage", "period": 20, "method": "Exponential", "price": "Close" },
          "right": { "type": "MovingAverage", "period": 50, "method": "Simple", "price": "Close" }
        },
        { "type": "dayOfWeek", "days": ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday"] },
        { "type": "time", "start": "09:00", "end": "17:00" },
        { "type": "position", "required": "FlatOrShort" }
      ],
      "action": { "type": "Buy" }
    },
    {
      "name": "EMA crosses below SMA",
      "all": [
        {
          "type": "comparison",
          "operator": "CrossBelow",
          "left": { "type": "MovingAverage", "period": 20, "method": "Exponential", "price": "Close" },
          "right": { "type": "MovingAverage", "period": 50, "method": "Simple", "price": "Close" }
        },
        { "type": "position", "required": "LongOnly" }
      ],
      "action": { "type": "Close" }
    }
  ]
}
```

Dieses Beispiel spiegelt die Aktien-Panel-Vorlage wider: eine Long-Position bei einem bullischen EMA 20/50 SMA-Kreuz während der regulären Sitzung öffnen und die Position beim inversen Kreuz schließen.
