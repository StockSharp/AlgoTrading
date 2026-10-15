# Roman Inversão de Direção
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia recria o consultor especialista MQL original publicado como `roman.mq5`. Sempre mantém uma posição aberta e alterna a direção da operação apenas após a operação anterior ser fechada. Enquanto a posição continua lucrativa, repete a mesma direção; após um stop-loss, a estratégia muda para o lado oposto. A versão StockSharp trabalha com dados de nível 1 e usa as melhores cotações de oferta/demanda para emular as saídas baseadas em pips do MetaTrader.

## Lógica da estratégia
1. **Direção inicial** – na inicialização, o parâmetro `StartWithBuy` define se a primeira ordem é uma compra ou uma venda. A decisão é armazenada em `_nextTradeBuy` para que persista entre negócios.
2. **Entrada no mercado** – quando a estratégia está plana e não há ordens pendentes, ela submete uma ordem de mercado na direção predefinida. Para ordens de compra, o melhor ask atual é armazenado como preço de entrada de referência, e para ordens de venda, o melhor bid atual é usado. Isso reflete a implementação do MetaTrader onde compras são executadas no ask e vendas no bid.
3. **Monitoramento da posição aberta** – após a ordem ser preenchida, a estratégia ouve as atualizações de nível 1. Cada atualização fornece o último bid/ask para que o algoritmo possa calcular o lucro não realizado expresso em passos de preço (pips). O `PriceStep` do instrumento é usado como denominador, com um fallback de `1` se o passo for desconhecido.
4. **Regra de take-profit** – quando o ganho não realizado atinge ou excede `TakeProfitSteps`, a posição é fechada com `ClosePosition()`. O flag `_nextTradeBuy` mantém o mesmo valor para que a próxima ordem siga a direção que acabou de ter sucesso.
5. **Regra de stop-loss** – quando a perda não realizada atinge ou excede `StopLossSteps`, a posição é fechada e `_nextTradeBuy` é alternado. A seguinte operação entra na direção oposta, correspondendo ao comportamento do EA original onde o booleano `bs` muda em uma perda.
6. **Throttling de ordens** – `_orderPending` impede que o algoritmo submeta múltiplas ordens enquanto uma solicitação anterior ainda está sendo processada. O flag é reiniciado em `OnPositionChanged` após a atualização do tamanho da posição.

Esta sequência simples mantém a estratégia investida em todos os momentos e alterna a direção apenas após uma operação perdedora. Como resultado, o sistema se assemelha a um interruptor de seguidor de tendência: após um stop-loss, assume que a tendência mudou e segue o novo lado.

## Parâmetros
- `OrderVolume` *(decimal, padrão = 0.1)* – quantidade enviada com cada ordem de mercado. Definir para o tamanho de contrato necessário para negociação ao vivo ou simulações.
- `TakeProfitSteps` *(int, padrão = 46)* – número positivo de passos de preço necessários para acionar o take-profit. Passos correspondem a `Security.PriceStep`, portanto em um símbolo com tamanho de tick de 0.01 o padrão equivale a 0.46 unidades de preço.
- `StopLossSteps` *(int, padrão = 31)* – máximo movimento adverso de preço (em passos) antes do fechamento da posição e inversão da direção.
- `StartWithBuy` *(bool, padrão = true)* – determina se a primeira operação é comprada (`true`) ou vendida (`false`). Operações subsequentes dependem dos resultados de posições anteriores.

Cada parâmetro é exposto através de `StrategyParam<T>`, suporta otimização (exceto o interruptor booleano), e é visível na UI graças aos metadados `SetDisplay`.

## Detalhes de dados e execução
- Inscreve-se em `SubscribeLevel1()` para receber as melhores cotações de oferta/demanda. Não são necessários dados de velas ou indicadores.
- Usa `BuyMarket`/`SellMarket` para entradas e `ClosePosition()` para saídas, garantindo que a lógica permaneça próxima à versão MQL que dependia de ordens de mercado imediatas.
- Armazena localmente o último bid/ask conhecido para imitar o cálculo de ganho baseado em `_Point` do MetaTrader.

## Gestão de risco
- O take-profit e stop-loss fixos em passos de preço garantem que cada operação tenha níveis de saída predefinidos.
- A inversão de direção após uma perda pode levar a alternância rápida em mercados choppy, portanto o tamanho da posição (`OrderVolume`) deve ser calibrado de acordo com a tolerância ao risco da conta.
- Como a estratégia quase sempre mantém uma posição, é sensível a gaps noturnos e saltos repentinos de cotação; considerar proteções externas se isso for uma preocupação.

## Valores padrão
- `OrderVolume` = 0.1
- `TakeProfitSteps` = 46
- `StopLossSteps` = 31
- `StartWithBuy` = true

## Filtros
- **Categoria**: Seguidor de tendência / interruptor de direção
- **Direção**: Ambos (comprado e vendido)
- **Indicadores**: Nenhum
- **Stops**: Sim (take-profit e stop-loss de passo fixo)
- **Complexidade**: Básico
- **Período**: Tick / cotações Level1
- **Sazonalidade**: Não
- **Redes neurais**: Não
- **Divergência**: Não
- **Nível de risco**: Alto (sempre no mercado)

## Notas
- O EA original armazenava a próxima direção em um booleano chamado `bs`. O port para StockSharp mantém a mesma ideia através de `_nextTradeBuy` enquanto adiciona throttling de ordens para evitar submissões duplicadas.
- A granularidade do passo de preço importa: se seu instrumento usa pips fracionários, ajuste os padrões para que os alvos de ganho/perda reflitam os valores monetários desejados.
