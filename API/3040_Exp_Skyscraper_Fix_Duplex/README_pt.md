# Estratégia Exp Skyscraper Fix Duplex
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Exp Skyscraper Fix Duplex é um port do expert advisor MQL5 *Exp_Skyscraper_Fix_Duplex*. A estratégia executa o canal Skyscraper Fix nos lados comprado e vendido de forma independente, permitindo que cada lado use seu próprio período, janela ATR e sensibilidade. As operações compradas e vendidas podem portanto reagir a diferentes regimes de mercado enquanto compartilham a mesma lógica de execução dentro do StockSharp.

## Lógica do indicador
O indicador personalizado **Skyscraper Fix** reproduz o script original:

- Um ATR com um período interno fixo de 15 é calculado para cada candle finalizado.
- Os valores mais altos e mais baixos de ATR na janela `Length` configurável determinam o passo de preço adaptativo.
- Dependendo do `Mode` selecionado, o High/Low da barra ou o preço de fechamento é usado para projetar níveis de canal superior e inferior a o dobro da distância do passo.
- O breakout mais recente acima do nível superior ou abaixo do nível inferior inverte a tendência interna e fixa o nível de trailing para que nunca se mova contra o viés atual.
- O cruzamento da linha de trailing oposta produz disparadores discretos de compra ou venda (espelhando os buffers de setas do indicador em MQL).

O indicador expõe o nível superior de trailing, o nível inferior de trailing, disparadores de entrada e uma linha média que pode ser plotada se desejado.

## Regras de negociação
As operações compradas e vendidas são avaliadas separadamente para cada candle finalizado da assinatura respectiva:

- **Entrada comprada** – acionada quando o indicador comprado reporta um novo nível de compra. Qualquer exposição vendida existente é coberta primeiro, então uma nova ordem comprada de mercado é enviada com o volume configurado.
- **Saída comprada** – acionada quando o indicador comprado reporta a linha de trailing oposta. Qualquer posição comprada existente é fechada com uma venda de mercado.
- **Entrada vendida** – acionada quando o indicador vendido reporta um novo nível de venda. A exposição comprada existente é fechada primeiro, então uma nova ordem vendida de mercado é enviada.
- **Saída vendida** – acionada quando o indicador vendido reporta a linha de trailing oposta. Qualquer posição vendida ativa é coberta com uma compra de mercado.

Os sinais podem ser atrasados com os parâmetros `SignalBar` para que a estratégia aja sobre o candle fechado mais recentemente (`0`) ou sobre candles mais atrás na história (`1` imita a configuração MQL padrão).

## Parâmetros
- `TradeVolume` – tamanho da ordem para entradas de mercado.
- `EnableLongEntries` / `EnableLongExits` – interruptores para o trading do lado comprado.
- `LongCandleType` – série de candles usada para o indicador comprado.
- `LongLength`, `LongKv`, `LongPercentage`, `LongMode`, `LongSignalBar` – configurações de Skyscraper Fix para o lado comprado.
- `EnableShortEntries` / `EnableShortExits` – interruptores para o trading do lado vendido.
- `ShortCandleType` – série de candles usada para o indicador vendido.
- `ShortLength`, `ShortKv`, `ShortPercentage`, `ShortMode`, `ShortSignalBar` – configurações de Skyscraper Fix para o lado vendido.

## Notas de uso
- A estratégia define a propriedade global `Volume` a partir de `TradeVolume`, de modo que as chamadas padrão `BuyMarket()` e `SellMarket()` usam esse tamanho automaticamente.
- Ambas as instâncias do indicador leem o `PriceStep` do instrumento. Se for zero, o indicador aguarda silenciosamente até que um passo de preço válido esteja disponível.
- `StartProtection()` é invocado no início para que as proteções no nível da plataforma estejam ativas antes que a primeira ordem seja enviada.
- Não há uma implementação Python separada; o diretório `PY` é omitido intencionalmente conforme solicitado.
