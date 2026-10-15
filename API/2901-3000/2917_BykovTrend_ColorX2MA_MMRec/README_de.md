# BykovTrend + ColorX2MA MMRec Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese StockSharp-Strategie reproduziert den MQL5-Experten `Exp_BykovTrend_ColorX2MA_MMRec`. Sie kombiniert zwei unabhängige Module:
BykovTrend, das Kerzen mit einem Williams %R-Filter einfärbt, und ColorX2MA, das die Steigung eines doppelt geglätteten gleitenden
Durchschnitts prüft. Einträge werden ausgegeben, wenn das ausgewählte Modul eine neue Farb-/Steigungsänderung erkennt, und das
Geldmanagement wird auf die Verwendung des Strategie-Volumens vereinfacht. Optionale prozentuale Stop-Loss- und Take-Profit-Werte können
über den eingebauten StockSharp-Schutzblock aktiviert werden.

## Strategielogik

### BykovTrend-Modul
- Verwendet einen Williams %R (`BykovTrendWprLength`), der auf `BykovTrendCandleType` (Standard 2-Stunden-Kerzen) berechnet wird.
- `BykovTrendRisk` steuert die bullischen/bearischen Schwellenwerte (`33 - Risk` und `-Risk`).
- Die Indikatorfarbe wird an Balken `BykovTrendSignalBar` (Versatz vom zuletzt geschlossenen Balken) ausgewertet.
- Eine bullische Farbe (< 2) schließt Shorts, wenn `AllowBykovTrendCloseSell` aktiviert ist, und kann Longs öffnen, wenn
  `EnableBykovTrendBuy` true ist und die vorherige Farbe nicht bullisch war.
- Eine bearische Farbe (> 2) schließt Longs, wenn `AllowBykovTrendCloseBuy` aktiviert ist, und kann Shorts öffnen, wenn
  `EnableBykovTrendSell` true ist und die vorherige Farbe nicht bearisch war.

### ColorX2MA-Modul
- Zwei Glättungsstufen (`ColorX2MaMethod1`, `ColorX2MaLength1` und `ColorX2MaMethod2`, `ColorX2MaLength2`) werden auf
  den durch `ColorX2MaPriceType` definierten Preis mit Kerzen von `ColorX2MaCandleType` angewendet.
- Die Ausgabe der zweiten Stufe wird mit dem Vorwert verglichen, um Steigungszustände zu generieren: steigend (1), fallend (2) oder flach (0).
- Der Steigungszustand wird an Balken `ColorX2MaSignalBar` (Versatz vom letzten geschlossenen Balken) ausgewertet.
- Eine steigende Neigung schließt Shorts (`AllowColorX2MaCloseSell`) und kann Longs öffnen (`EnableColorX2MaBuy`), wenn die vorherige Neigung
  noch nicht stieg.
- Eine fallende Neigung schließt Longs (`AllowColorX2MaCloseBuy`) und kann Shorts öffnen (`EnableColorX2MaSell`), wenn die vorherige Neigung
  noch nicht fiel.

### Trade-Management
- Schließsignale werden vor Öffnungen ausgeführt, um die Orderreihenfolge des ursprünglichen Experten zu emulieren.
- Orders verwenden `Strategy.Volume` als Positionsgröße; der komplexe Geldmanagement-Recomputer aus der MQL-Version wird nicht repliziert.
- `StopLossPercent` und `TakeProfitPercent` aktivieren `StartProtection` mit prozentbasierten Ausstiegen, wenn sie größer als null sind.

## Details

- **Long/Short**: Beide Richtungen unterstützt.
- **Einstiegskriterien**:
  - BykovTrend bullischer Farbübergang.
  - ColorX2MA steigender Neigungsübergang.
- **Ausstiegskriterien**:
  - Entgegengesetzte Farbe/Neigung je nach aktivierten Modulen.
  - Optionaler prozentualer Stop-Loss/Take-Profit.
- **Filter**: Keine über die Indikatorlogik hinaus.
- **Positionsgröße**: Fest über `Strategy.Volume`.

## Parameter

| Parameter | Beschreibung | Standard |
|-----------|--------------|----------|
| `EnableBykovTrendBuy` | BykovTrend erlauben, Long-Trades zu öffnen. | `true` |
| `EnableBykovTrendSell` | BykovTrend erlauben, Short-Trades zu öffnen. | `true` |
| `AllowBykovTrendCloseBuy` | Longs schließen, wenn BykovTrend bearisch wird. | `true` |
| `AllowBykovTrendCloseSell` | Shorts schließen, wenn BykovTrend bullisch wird. | `true` |
| `BykovTrendRisk` | Williams %R-Empfindlichkeit (kleinere Werte reagieren schneller). | `3` |
| `BykovTrendWprLength` | Williams %R-Periode. | `9` |
| `BykovTrendSignalBar` | Balkenindex (Versatz) zur Bewertung der BykovTrend-Farbe. | `1` |
| `BykovTrendCandleType` | Kerzentyp/-zeitrahmen für BykovTrend. | `2h` |
| `EnableColorX2MaBuy` | ColorX2MA erlauben, Long-Trades zu öffnen. | `true` |
| `EnableColorX2MaSell` | ColorX2MA erlauben, Short-Trades zu öffnen. | `true` |
| `AllowColorX2MaCloseBuy` | Longs schließen, wenn die ColorX2MA-Neigung bearisch wird. | `true` |
| `AllowColorX2MaCloseSell` | Shorts schließen, wenn die ColorX2MA-Neigung bullisch wird. | `true` |
| `ColorX2MaMethod1` | Gleitender-Durchschnitt-Typ für Stufe 1. | `Simple` |
| `ColorX2MaLength1` | Periode für Stufe-1-Glättung. | `12` |
| `ColorX2MaPhase1` | Phasen-Platzhalter für Dokumentation (nicht verwendet). | `15` |
| `ColorX2MaMethod2` | Gleitender-Durchschnitt-Typ für Stufe 2. | `Jurik` |
| `ColorX2MaLength2` | Periode für Stufe-2-Glättung. | `5` |
| `ColorX2MaPhase2` | Phasen-Platzhalter für Dokumentation (nicht verwendet). | `15` |
| `ColorX2MaPriceType` | Preisquelle für ColorX2MA-Glättung. | `Close` |
| `ColorX2MaSignalBar` | Balkenindex (Versatz) zur Bewertung des Steigungszustands. | `1` |
| `ColorX2MaCandleType` | Kerzentyp/-zeitrahmen für ColorX2MA. | `2h` |
| `StopLossPercent` | Optionaler Schutz-Stop in Prozent (0 deaktiviert). | `0` |
| `TakeProfitPercent` | Optionaler Schutz-Take-Profit in Prozent (0 deaktiviert). | `0` |

## Hinweise

- `ColorX2MaPhase1` und `ColorX2MaPhase2` werden beibehalten, um die ursprünglichen Eingaben widerzuspiegeln, werden aber nicht genutzt,
  da StockSharp-Implementierungen von gleitenden Durchschnitten keinen Phasenparameter freilegen.
- Es werden nur die in StockSharp verfügbaren Glättungsmethoden bereitgestellt; nicht unterstützte SmoothAlgorithms-Optionen fallen auf
  das nächste Analogon zurück.
- Geldmanagement-Recomputer aus `TradeAlgorithms.mqh` sind nicht portiert; Positionsgrößenbestimmung sollte durch externe Risikokontrollen
  oder benutzerdefinierte Logik in StockSharp gehandhabt werden.

## Verwendung

1. Das gewünschte Instrument zuweisen und `Strategy.Volume` auf die zu handelnde Lotgröße setzen.
2. Kerzentypen für BykovTrend und ColorX2MA konfigurieren, wenn der Standard-2-Stunden-Zeitrahmen nicht angemessen ist.
3. Glättungsmethoden/-längen und Signalbalkenversätze anpassen, um der ursprünglichen Konfiguration oder eigenen Tests zu entsprechen.
4. Optionally den Schutzblock aktivieren, indem `StopLossPercent` und/oder `TakeProfitPercent` größer als null gesetzt wird.
5. Die Strategie starten; sie abonniert die konfigurierten Kerzen-Streams, überwacht beide Module und gibt Marktorders in der
   oben definierten Reihenfolge aus.
