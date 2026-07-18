# Estratégia legada de captura de tendências
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

**Trend Capture Legacy Strategy** transporta o MetaTrader especialista `TrendCapture.mq4` para o StockSharp API de alto nível. A versão C# mantém o conjunto de regras original com base na direção Parabolic SAR, um filtro baixo ADX e gerenciamento simples de equilíbrio financeiro.

## Ideias centrais
- Processe velas concluídas do período de tempo selecionado e alimente-as com Parabolic SAR (`0.02/0.2`) e Índice Direcional Médio (`14`).
- Entre apenas quando ADX estiver abaixo de `AdxThreshold`, sinalizando um mercado calmo onde as reversões de SAR são mais confiáveis.
- Lembre-se da direção e do resultado da última negociação fechada: repita o mesmo lado após um vencedor, vire para o lado oposto após um perdedor.
- Aplique níveis de stop-loss e take-profit de distância fixa (configurados em faixas de preço) e mova o stop para o ponto de equilíbrio quando a negociação ganhar `BreakEvenGuard` pontos.
- Dimensionar o volume do pedido a partir do valor do portfólio disponível e `MaximumRisk`; retorne à estratégia `Volume` quando as informações do portfólio não estiverem disponíveis.

## Parâmetros
| Nome | Padrão | Descrição |
| --- | --- | --- |
| `SarStep` | 0,02 | Etapa inicial de aceleração Parabolic SAR. |
| `SarMax` | 0,2 | Aceleração máxima Parabolic SAR. |
| `AdxPeriod` | 14 | ADX período médio. |
| `AdxThreshold` | 20 | Valor máximo de ADX que ainda permite uma nova entrada. |
| `TakeProfitPoints` | 180 | Distância de lucro em faixas de preço. |
| `StopLossPoints` | 50 | Distância de stop-loss em faixas de preço. |
| `BreakEvenGuard` | 5 | Buffer de lucro (em pontos) necessário antes de mover o stop para entrada. |
| `MaximumRisk` | 0,03 | Fração da margem livre utilizada para dimensionamento da posição. |
| `CandleType` | Velas de 1 hora | Prazo para cálculos de indicadores e sinais de negociação. |

## Gerenciamento de pedidos
- As entradas longas exigem o preço de fechamento acima de SAR com baixo ADX; as posições vendidas exigem o preço de fechamento abaixo de SAR com o mesmo filtro ADX.
- Os níveis de stop-loss e take-profit são recalculados em cada entrada e avaliados em cada vela concluída.
- A ativação do ponto de equilíbrio simplesmente muda o stop para o preço de entrada. Se nenhum stop-loss estiver configurado (distância zero ou negativa), a guarda é ignorada.

## Indicadores
- `ParabolicSar` para polarização direcional.
- `AverageDirectionalIndex` para o filtro de força (apenas a linha principal ADX é usada).

## Notas
- A estratégia usa `BindEx` para evitar acesso direto ao buffer, seguindo as diretrizes do projeto.
- O cálculo do volume baseado em portfólio respeita as restrições do conselho (`LotStep`, `MinVolume`, `MaxVolume`).
- O histórico de negociação necessário para polarização de direção é coletado por meio de `OnNewMyTrade`, portanto, os preenchimentos parciais permanecem suportados.
