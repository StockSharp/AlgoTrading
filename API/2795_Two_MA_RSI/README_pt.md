# Estratégia Two MA RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia Two MA RSI é uma conversão do assessor especialista original do MetaTrader "2MA_RSI". Usa um cruzamento de média móvel exponencial (EMA) rápida e lenta confirmado por um filtro de Índice de Força Relativa (RSI). As ordens são dimensionadas com um bloco de gestão de dinheiro estilo martingala que aumenta o volume da próxima ordem após uma perda. A versão StockSharp funciona completamente em velas terminadas e reproduz o comportamento original de take-profit e stop-loss em pontos de preço.

## Dados e indicadores
- A estratégia assina uma única série de velas definida por `CandleType` (velas de 5 minutos por padrão).
- Três indicadores são calculados em cada barra completa:
  - EMA de `FastLength` (aplicada ao fechamento da vela).
  - EMA de `SlowLength`.
  - RSI com comprimento `RsiLength`.
- Os valores históricos dos indicadores são armazenados internamente para detectar cruzamentos de EMA sem extrair dados dos buffers de indicadores.

## Lógica de entrada
1. A vela anterior deve estar terminada para evitar a reavaliação intrabarra.
2. Nenhuma posição ativa é permitida (`Position == 0`).
3. **Entrada comprada:**
   - A EMA rápida cruza acima da EMA lenta (a EMA rápida na barra atual é maior que a EMA lenta, enquanto a barra anterior tinha EMA rápida < EMA lenta).
   - O valor do RSI está abaixo de `RsiOversold`, confirmando um mercado sobrevendido.
4. **Entrada vendida:**
   - A EMA rápida cruza abaixo da EMA lenta com a condição análoga (EMA rápida agora abaixo da EMA lenta, anteriormente acima).
   - O RSI está acima de `RsiOverbought`, sinalizando um mercado sobrecomprado.
5. Quando todas as condições são satisfeitas, a estratégia envia uma ordem a mercado dimensionada de acordo com o módulo de martingala.

## Lógica de saída
- Um stop-loss de proteção e um take-profit são calculados imediatamente após cada entrada. As distâncias são definidas em "pontos" e convertidas através do `PriceStep` do instrumento:
  - **Comprado:**
    - Stop loss = `preço de entrada - StopLossPoints * PriceStep`.
    - Take profit = `preço de entrada + TakeProfitPoints * PriceStep`.
  - **Vendido:**
    - Stop loss = `preço de entrada + StopLossPoints * PriceStep`.
    - Take profit = `preço de entrada - TakeProfitPoints * PriceStep`.
- Apenas esses níveis de proteção fecham uma operação. A estratégia aguarda a próxima vela para confirmar se a mínima/máxima tocou o alvo ou o stop e envia uma ordem `ClosePosition()` a mercado de acordo.
- A prioridade de saída corresponde ao comportamento conservador do robô original: um stop loss é avaliado antes de um take profit se ambos os níveis caírem dentro do mesmo intervalo de vela.

## Dimensionamento de posição e martingala
1. O volume base é calculado em cada entrada como `floor(balance / BalanceDivider) * VolumeStep`. O valor sempre fica em ou acima de um passo de volume e usa `CurrentValue` do portfólio (recorrendo a `BeginValue` quando necessário).
2. Após cada saída perdedora, o estágio de martingala aumenta em um até `MaxDoublings`. O próximo volume de ordem é multiplicado por `2^stage`.
3. Qualquer operação vencedora ou atingir o número máximo de duplicações redefine o estágio para zero, retornando ao volume base.
4. Se `MaxDoublings` for zero ou negativo, o tamanho nunca aumenta e iguala o volume base.

## Comportamento adicional
- A estratégia mantém o registro dos valores anteriores de EMA internamente e não solicita valores históricos de indicadores.
- As ordens são executadas apenas quando a estratégia está online, os indicadores estão formados e a negociação é permitida.
- A saída do gráfico desenha velas de preço, operações próprias e os três indicadores para análise visual.

## Parâmetros
| Parâmetro | Descrição | Padrão |
|-----------|-------------|---------|
| `FastLength` | Comprimento da EMA rápida. | 5 |
| `SlowLength` | Comprimento da EMA lenta. | 20 |
| `RsiLength` | Número de barras usadas no cálculo do RSI. | 14 |
| `RsiOverbought` | Nível RSI que bloqueia novos comprados e permite vendidos. | 70 |
| `RsiOversold` | Nível RSI que permite comprados. | 30 |
| `StopLossPoints` | Distância do stop-loss expressa em passos de preço. | 500 |
| `TakeProfitPoints` | Distância do take-profit em passos de preço. | 1500 |
| `BalanceDivider` | Divide o valor do portfólio para obter o tamanho base da ordem. | 1000 |
| `MaxDoublings` | Número máximo de duplicações de martingala após perdas consecutivas. | 1 |
| `CandleType` | Série de velas utilizada pela estratégia. | Período de 5 minutos |

## Notas de uso
- Fornecer um portfólio e instrumento com metadados válidos de `PriceStep` e `VolumeStep` para que a gestão de risco baseada em pontos e o dimensionamento de posição permaneçam consistentes.
- Como ordens a mercado são usadas para saídas, derrapagem e spreads ainda são possíveis em comparação com as ordens limite da versão MetaTrader, mas a lógica de avaliação de stop/take é preservada.
- A estratégia não cria uma versão Python; apenas a implementação C# é fornecida conforme solicitado.
