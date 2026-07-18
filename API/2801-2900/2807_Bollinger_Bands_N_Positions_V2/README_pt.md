# Estratégia Bollinger Bands N Posições v2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia replica o expert advisor "Bollinger Bands N positions v2" de Vladimir Karputov. Opera em candles completados e procura rompimentos de preço relativos ao envelope das Bollinger Bands. O port para StockSharp mantém o comportamento de piramidação original, controles de risco e lógica de trailing enquanto adapta o gerenciamento de ordens ao modelo de compensação da plataforma.

## Lógica de trading
- Um indicador de Bollinger Bands (período e desvio configuráveis) é calculado na série de candles selecionada.
- Quando o fechamento do candle termina acima da banda superior, a estratégia fecha qualquer exposição vendida ativa e abre uma posição comprada adicional (até o número máximo configurado de entradas empilhadas).
- Quando o fechamento do candle termina abaixo da banda inferior, a estratégia fecha qualquer exposição comprada ativa e abre uma posição vendida adicional (também limitada pelo parâmetro de entradas máximas).
- O tamanho da posição é aumentado em incrementos fixos (o parâmetro **Volume**) ao piramidizar na mesma direção.
- O preço de entrada médio da posição empilhada é rastreado para gerenciar os níveis de stop loss, take profit e trailing stop de forma consistente.

## Gestão de risco
- As distâncias de stop loss e take profit são inseridas em pips. Elas são convertidas em offsets de preço absolutos multiplicando pelo passo de preço do instrumento. Instrumentos cotados com 3 ou 5 casas decimais multiplicam automaticamente o passo por 10 para emular o ajuste de tamanho de pip do MetaTrader.
- O offset do trailing stop e o passo do trailing também são configurados em pips. O mecanismo de trailing atualiza o preço do stop apenas depois que a operação se move `TrailingStop + TrailingStep` pips a partir da entrada média atual. Cada atualização desloca o stop pelo offset do trailing enquanto respeita o buffer de passo extra para evitar modificações excessivas.
- As ordens de saída protetoras são simuladas dentro da estratégia: sempre que um candle terminado cruzar o nível de stop ou alvo, a posição inteira é fechada usando ordens de mercado.

## Parâmetros
| Parâmetro | Descrição |
|-----------|-----------|
| **Bollinger Period** | Período de retrocesso para a média móvel das Bollinger Bands. |
| **Bollinger Deviation** | Multiplicador de desvio padrão para o envelope das Bollinger Bands. |
| **Max Positions** | Número máximo de entradas empilhadas permitidas por direção. |
| **Volume** | Volume de ordem para cada entrada individual. |
| **Stop Loss (pips)** | Distância de stop loss em pips (0 desabilita o stop). |
| **Take Profit (pips)** | Distância de take profit em pips (0 desabilita o alvo). |
| **Trailing Stop (pips)** | Distância do trailing stop em pips (0 desabilita o trailing). |
| **Trailing Step (pips)** | Lucro adicional em pips necessário antes de mover o trailing stop novamente. Deve ser positivo quando o trailing estiver habilitado. |
| **Candle Type** | Série de candles processada pela estratégia. |

## Notas de implementação
- A estratégia usa subscrições de candles de alto nível com vinculação de indicadores, seguindo as diretrizes do StockSharp.
- Apenas candles terminados são processados para espelhar a lógica original de "nova barra" do MetaTrader.
- Como o StockSharp opera no modo de compensação, a conversão fecha a exposição oposta antes de abrir uma nova camada de pirâmide na outra direção.
- O passo do trailing deve permanecer maior que zero sempre que o trailing stop estiver ativo, correspondendo à verificação de segurança do expert advisor original.
- A implementação em Python não está incluída nesta versão.
