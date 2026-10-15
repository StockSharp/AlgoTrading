# Estratégia Bollinger Bands N Posições
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

Esta estratégia é um port para StockSharp do expert advisor MetaTrader **Bollinger Bands N positions**. Ela monitora os preços de fechamento em relação a um envelope de Bandas de Bollinger e entra em uma posição sempre que o mercado finaliza uma barra fora do canal. O gerenciamento de posição replica o expert original impondo um limite na exposição total, colocando offsets fixos de stop-loss e take-profit, e ativando um trailing stop assim que a operação está suficientemente no lucro.

## Lógica de negociação

1. Assinar o tipo de candle configurado e calcular Bandas de Bollinger com o período e a largura selecionados.
2. Em cada candle finalizado, a estratégia primeiro verifica se uma posição existente deve ser fechada:
   - Posições compradas saem quando o preço atinge o stop-loss fixo, o take-profit fixo, ou quando o nível de trailing stop é violado.
   - Posições vendidas aplicam a lógica simétrica.
3. Se o trading for permitido e nenhuma saída tiver ocorrido na barra atual, os sinais de entrada são avaliados:
   - Quando o preço de fechamento estiver acima da banda superior, a estratégia achata qualquer exposição vendida e, se estiver dentro do limite de posição, abre uma nova posição comprada com o volume solicitado.
   - Quando o preço de fechamento estiver abaixo da banda inferior, achata qualquer exposição comprada e abre uma posição vendida da mesma forma.
4. Os trailing stops se movem em incrementos definidos pelo parâmetro de passo do trailing assim que a operação está à frente pela distância do trailing mais o passo. O nível do trailing fica atrás do preço pela distância do trailing e só avança quando o lucro aumenta pelo menos um passo do trailing.

## Gestão de posição

- **Max Positions** define a exposição líquida máxima medida como `MaxPositions × Volume`. Como o StockSharp opera em modo de netting, a estratégia pode manter apenas uma posição líquida por vez. O parâmetro atua como um limite de segurança que impede a estratégia de reentrar quando a posição absoluta atual já atinge o limite configurado.
- As distâncias de stop-loss e take-profit são especificadas em pips. A estratégia as converte em preços usando o `PriceStep` do ativo. Se o instrumento usa preços de pip fracionário, pode ser necessário ajustar os valores de acordo.
- Os trailing stops exigem que tanto a distância quanto o passo sejam positivos. Quando a distância do trailing stop é definida como zero, o módulo de trailing é desabilitado.

## Parâmetros

| Parâmetro | Descrição | Padrão |
|-----------|-----------|--------|
| `Volume` | Tamanho da ordem em lotes usado para cada entrada. | `0.1` |
| `MaxPositions` | Limite de posição líquida expresso em múltiplos de `Volume`. | `9` |
| `BollingerPeriod` | Período de retrospectiva para a média móvel de Bollinger. | `20` |
| `BollingerWidth` | Multiplicador de desvio padrão para as Bandas de Bollinger. | `2` |
| `StopLossPips` | Distância stop-loss em pips. | `50` |
| `TakeProfitPips` | Distância take-profit em pips. | `50` |
| `TrailingStopPips` | Distância do trailing stop em pips. Defina como `0` para desabilitar. | `5` |
| `TrailingStepPips` | Incremento mínimo de lucro necessário antes que o trailing stop avance. | `5` |
| `CandleType` | Período ou tipo de candle personalizado usado para construir as Bandas de Bollinger. | `Período de 1 minuto` |

## Diferenças do expert MQL5

- O expert original opera no modo de hedging do MetaTrader e pode manter posições compradas e vendidas simultâneas. As estratégias do StockSharp são de netting, portanto este port achata a exposição contrária antes de entrar em uma nova operação. O parâmetro `MaxPositions` limita portanto o tamanho absoluto da posição líquida em vez do número de tickets independentes.
- Os stops de ordens são simulados dentro da estratégia em vez de serem enviados como ordens stop adjuntas. Isso corresponde à lógica de trailing da implementação MQL, mas significa que as saídas ocorrem no próximo candle finalizado.
- A configuração do trailing é validada na inicialização. Habilitar um trailing stop com um passo de trailing zero lança uma exceção para imitar a verificação de inicialização original.

## Notas de uso

1. Configure `Volume`, `MaxPositions` e os parâmetros de risco para corresponder ao tamanho do contrato do instrumento e ao valor do tick.
2. Certifique-se de que o ativo expõe um `PriceStep` válido. Se o passo for zero ou ausente, a estratégia usa `1` como fallback, que pode não se encaixar em todos os mercados.
3. Inicie a estratégia somente após o período de aquecimento do indicador (período de Bollinger) ter sido concluído para evitar agir sobre dados incompletos.
4. Monitore os registros em busca de erros de validação do passo de trailing ao personalizar as configurações de risco.
