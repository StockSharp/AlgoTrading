# マルチカレンシー・トレーディングパネル戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、オリジナルのMQL「Multicurrency trading panel」エキスパートアドバイザーの動作をエミュレートします。3つの通貨ペア（EURUSD、USDJPY、GBPUSD）を監視し、最新のローソク足を7つの単純な指標（始値、高値、安値、(高値+安値)/2、終値、(高値+安値+終値)/3、(高値+安値+終値+終値)/4）を使って前のローソク足と比較します。
各比較でBUYまたはSELLスコアが増加します。自動取引が有効な場合、BUYスコアがSELLスコアを上回るか、またはその逆の場合に、ペアでポジションを開設または反転させます。

## パラメーター
- **EURUSD** – 最初の証券。
- **USDJPY** – 2番目の証券。
- **GBPUSD** – 3番目の証券。
- **Candle Type** – ローソク足の時間軸。
- **Auto Trade** – 自動注文発注の切り替え。

この戦略は簡略化されたデモであり、実際の取引を目的としていません。
