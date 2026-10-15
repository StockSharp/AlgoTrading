# Estratégia do Método de Túnel EMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia do Método de Túnel EMA** replica o consultor especialista original do MetaTrader "Método de Túnel" no StockSharp API de alto nível. Ele opera com velas horárias e compara três médias móveis exponenciais (EMAs) baseadas nos preços de fechamento:

- **EMA rápida (12 períodos)** captura mudanças imediatas de impulso.
- **Médio EMA (144 períodos)** reflete o centro do "túnel" usado para validar sinais curtos.
- **EMA lenta (169 períodos)** fornece o filtro direcional de longo prazo para negociações longas.

A estratégia mantém posições mutuamente exclusivas (longas, curtas ou planas) e gerencia dinamicamente o risco por meio de controles explícitos de stop-loss, take-profit e trailing-stop.

## Lógica de Sinais
### Entradas longas
1. Aguarde a conclusão de uma vela (sem decisões intrabarras).
2. Detecte um cruzamento de alta onde o EMA rápida (12) se move de baixo para cima do EMA lenta (169).
3. Confirme se nenhuma posição está aberta no momento e envie uma ordem de compra a mercado para o volume configurado.

### Entradas curtas
1. Espere por uma vela completa.
2. Detecte um cruzamento de baixa onde o EMA rápida (12) se move de cima para baixo do meio EMA (144).
3. Confirme que nenhuma posição está aberta no momento e envie uma ordem de venda a mercado.

### Gerenciamento de posição
- **Stop-Loss**: Fecha a negociação quando o preço se move contra a posição em `StopLossPoints` (convertido em preço absoluto usando a etapa de preço do título).
- **Take-Profit**: bloqueia os ganhos quando o preço avança `TakeProfitPoints` em relação ao preço de entrada.
- **Trailing Stop**: Depois que a negociação acumula pelo menos `TrailingTriggerPoints` de lucro, a estratégia segue o preço usando `TrailingStopPoints`. Para negociações longas, segue a máxima mais alta desde a entrada; para negociações curtas, segue a mínima mais baixa desde a entrada. Uma reversão para o nível final fecha a posição.
- **Redefinição de estado**: Após cada saída (manual ou protetora), o estado final interno é redefinido para evitar interferência nas negociações subsequentes.

## Parâmetros padrão
| Parâmetro | Padrão | Descrição |
|-----------|---------|-------------|
| `CandleType` | `TimeSpan.FromHours(1).TimeFrame()` | Velas horárias usadas para cálculos de EMA. |
| `FastLength` | 12 | Duração do EMA rápido que reage à ação recente do preço. |
| `MediumLength` | 144 | Comprimento do centro do túnel EMA para validação curta. |
| `SlowLength` | 169 | Comprimento do limite do túnel EMA para validação longa. |
| `StopLossPoints` | 25 | Distância de parada protetora nos pontos do instrumento. |
| `TakeProfitPoints` | 230 | Distância alvo de lucro em pontos de instrumento. |
| `TrailingStopPoints` | 35 | Distância mantida pelo trailing stop quando ativo. |
| `TrailingTriggerPoints` | 20 | Limite de lucro necessário antes do início do rastreamento. |

## Filtros e características
- **Categoria**: Crossover que segue tendências.
- **Instrumentos**: Funciona em qualquer instrumento que forneça velas horárias e uma etapa de preço confiável.
- **Direção**: Negociações longas e curtas, nunca mantendo posições simultâneas.
- **Período**: velas de 1 hora por padrão (configuráveis por meio de `CandleType`).
- **Controles de risco**: Hard stop-loss, take-profit e trailing stop implementados dentro da lógica da estratégia.
- **Requisitos de dados**: Depende exclusivamente dos preços de fechamento das velas; não são necessários indicadores adicionais ou profundidade de mercado.

## Notas
- Todos os valores dos indicadores são provenientes de implementações EMA do StockSharp para garantir consistência com diretrizes API de alto nível.
- A estratégia ignora velas inacabadas para evitar sinais de contagem dupla ou agir com base em dados parciais.
- Os ajustes de trailing stop respeitam o `PriceStep` do título via `ShrinkPrice`, mantendo os níveis de saída alinhados com incrementos de tick válidos.
- Os parâmetros padrão refletem as configurações originais do MQL, mas podem ser otimizados por meio das ferramentas de otimização de parâmetros do StockSharp.
