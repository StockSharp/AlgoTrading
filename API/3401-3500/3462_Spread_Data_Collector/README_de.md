# Spread-Data-Collector-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **Spread Data Collector-Strategie** ist ein StockSharp-Port des MetaTrader 5-Dienstprogramms „Spread Data Collector“ (MQL-Eintrag 33314). Der ursprüngliche Fachberater erteilt keine Aufträge; Stattdessen hört es den Geld-/Briefstrom ab und zählt, wie viele Ticks in vordefinierte Spread-Bereiche fallen. Immer wenn das Handelsjahr wechselt oder der Experte stoppt, druckt er eine statistische Zusammenfassung. Diese C#-Version reproduziert das gleiche Verhalten mithilfe des übergeordneten `SubscribeLevel1()` API und stellt die Bereichsschwellenwerte als konfigurierbare Parameter bereit.

## Betriebsdetails
- Die Strategie abonniert Level1-Aktualisierungen (Bid/Ask) des Haupt-`Security`, wenn sie startet.
- Immer wenn sowohl Geld- als auch Briefkurse verfügbar sind, berechnet die Strategie den Spread und wandelt ihn in Preiseinheiten um, indem sie die konfigurierten Punktlimits mit `Security.PriceStep` multipliziert.
- Es werden sechs Zähler verwaltet:
  1. Verbreitung strikt unterhalb des ersten Schwellenwerts.
  2. Verteilung zwischen dem ersten und zweiten Schwellenwert.
  3. Verteilen Sie sich zwischen dem zweiten und dritten Schwellenwert.
  4. Verteilen Sie sich zwischen dem dritten und vierten Schwellenwert.
  5. Verteilen Sie sich zwischen dem vierten und fünften Schwellenwert.
  6. Spread über dem fünften Schwellenwert.
- Jahresübergänge werden anhand des Austauschzeitstempels (`Level1ChangeMessage.ServerTime`) erkannt. Wenn das Jahr wechselt, druckt die Strategie die Zusammenfassung des abgeschlossenen Jahres und setzt die Zähler zurück.
- Wenn die Strategie stoppt, druckt sie vor dem Herunterfahren die Statistiken des aktuellen Jahres aus.

Der Port behält den reinen Protokollierungscharakter des MQL-Dienstprogramms bei und ermöglicht es Händlern, das Verhalten der Spreads in verschiedenen Zeiträumen zu analysieren, ohne Aufträge zu senden oder Positionen zu manipulieren.

## Parameter
Alle Eingaben werden in **Punkten** ausgedrückt (MetaTrader-Terminologie). Der tatsächliche Preisabstand wird als `points × Security.PriceStep` berechnet.

| Parameter | Standard | Beschreibung |
|-----------|---------|-------------|
| `FirstBucketPoints` | 10 | Obergrenze des ersten Spread-Buckets. Spreads, die strikt unterhalb dieser Grenze liegen, werden zur ersten Kategorie gezählt. |
| `SecondBucketPoints` | 20 | Obergrenze des zweiten Spread-Buckets. Hier werden Spreads in `[FirstBucketPoints, SecondBucketPoints)` gezählt. |
| `ThirdBucketPoints` | 30 | Obergrenze des dritten Spread-Buckets. Spreads in `[SecondBucketPoints, ThirdBucketPoints)` erhöhen diesen Zähler. |
| `FourthBucketPoints` | 40 | Obergrenze des vierten Spread-Buckets. Spreads in `[ThirdBucketPoints, FourthBucketPoints)` werden hier aufgezeichnet. |
| `FifthBucketPoints` | 50 | Obergrenze des fünften Spread-Buckets. Spreads in `[FourthBucketPoints, FifthBucketPoints)` erhöhen diesen Zähler. |

Alle Schwellenwerte müssen streng ansteigend sein. Der Versuch, die Strategie mit ungültigen oder nicht positiven `Security.PriceStep`-Werten zu starten, führt zu einer Laufzeitausnahme, die den Benutzer vor inkonsistenten Statistiken schützt.

## Protokolle und Ausgaben
Die Statistiken werden über `AddInfoLog` im folgenden Format gedruckt:

„
Jahr=2024 Spread<=10pts=15342 Spread_10_20pts=2841 Spread_20_30pts=912 ... Spread>50pts=37
„

Diese Ausgabe spiegelt die `Print`-Aussagen des MetaTrader-Experten wider und erleichtert so den Vergleich beider Umgebungen. Verwenden Sie den Log-Viewer StockSharp oder leiten Sie Protokolle zur weiteren Analyse in eine Datei um.

## Checkliste für die Nutzung
1. Weisen Sie das Zielinstrument `Strategy.Security` zu und stellen Sie sicher, dass sein `PriceStep` mit der Punktgröße MetaTrader übereinstimmt (bei den meisten Forex-Symbolen entspricht dies 0,0001).
2. Passen Sie die Schaufelschwellen an, wenn Sie unterschiedliche Streubereiche benötigen. Halten Sie die Werte streng aufsteigend.
3. Starten Sie die Strategie und lassen Sie sie laufen. Es werden keine Bestellungen versendet.
4. Überprüfen Sie die jährlichen Protokolle, um das Ausbreitungsverhalten über Sitzungen hinweg zu verstehen.

Die Strategie ist bewusst leichtgewichtig und kann sicher neben Live-Handelssystemen ausgeführt werden. Es hilft Desks dabei, historische Spread-Verteilungen zu erstellen, Liquiditätsannahmen zu validieren und die Brokerbedingungen über lange Zeiträume zu überwachen.
