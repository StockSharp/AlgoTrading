# Estratégia básica de modelo de MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A **Estratégia básica de modelo de MA** é uma versão StockSharp fiel do MetaTrader 4 consultor especialista da entrada do repositório `MQL/27964`. O robô original negociou um único símbolo em uma média móvel de período de tempo superior e abriu uma posição sempre que a vela anterior cruzou a média. Esta versão C# mantém a estrutura minimalista enquanto expõe cada controle como um parâmetro para que o comportamento possa ser ajustado ou otimizado diretamente dentro de StockSharp.

O modelo espera por uma vela totalmente concluída e compara seus preços de abertura e fechamento com uma média móvel alterada. Se a barra abrir acima da média e fechar abaixo dela, a estratégia abre uma posição curta. Quando acontece o inverso, abre-se uma negociação longa. O sistema permite apenas uma posição de mercado por vez, refletindo a verificação MQL "nenhum ticket ativo". As distâncias protetoras de stop-loss e take-profit são definidas em pips. No início, a estratégia converte essas distâncias de pip em compensações de preços absolutos usando a etapa do instrumento e a precisão decimal, replicando a lógica de conversão ponto a pip que dependia dos dígitos de cotação em MetaTrader.

## Lógica de negociação

- **Fonte de dados**: uma única série de velas determinada pelo parâmetro `CandleType` (padrão H4).
- **Indicador**: média móvel configurável (`SMA`, `EMA`, `SMMA` ou `LWMA`). O parâmetro `MovingAverageShift` desloca o indicador para frente exatamente como a função MetaTrader `iMA`.
- **Regras de entrada**:
  - Longo: a vela anterior abriu abaixo e fechou acima da média móvel deslocada enquanto nenhuma posição está aberta.
  - Curto: a vela anterior abriu acima e fechou abaixo da média móvel deslocada enquanto nenhuma posição está aberta.
- **Regras de saída**: tratadas automaticamente pelo módulo StockSharp `StartProtection` usando distâncias de take-profit e stop-loss baseadas em pip. Quando ambos os alvos são zero, a estratégia ainda ativa o serviço de proteção, de modo que saídas manuais ou de rastreamento permanecem possíveis.
- **Filtro de posição**: a estratégia ignora novos sinais enquanto uma posição está ativa, mantendo o comportamento idêntico à rotina `PosSelect()` original.

## Parâmetros

| Parâmetro | Descrição | Padrão |
|-----------|-------------|---------|
| `CandleType` | Agregação de velas usada para sinais. | H4 (velas de 4 horas) |
| `MovingAveragePeriod` | Duração do período da média móvel. | 49 |
| `MovingAverageShift` | Deslocamento para frente aplicado ao buffer de média móvel. | 0 |
| `MovingAverageMethod` | Modo de cálculo de média móvel (`Simple`, `Exponential`, `Smoothed`, `LinearWeighted`). | `Simple` |
| `TakeProfitPips` | Distância de lucro em pips convertida em compensações de preço absoluto em tempo de execução. | 38,5 |
| `StopLossPips` | Distância de stop-loss em pips convertida em compensações de preço absoluto em tempo de execução. | 48,5 |

### Tratamento de riscos

O subsistema de proteção recebe as distâncias absolutas calculadas e as atribui a cada ordem de mercado. Como o tamanho do pip é derivado do passo do símbolo e da precisão decimal (aspas de 5 e 3 dígitos multiplicam o passo por dez), os níveis de stop respeitam o espaçamento mínimo imposto pelos corretores na versão MetaTrader.

### Notas de conversão

- A colocação de ordem em duas etapas no estilo ECN original é simplificada para StockSharp ordens de mercado com proteção automática, que já trata da anexação de SL/TP após a execução.
- As rotinas `CheckVolumeValue` e `CheckMoneyForTrade` são omitidas. O dimensionamento da posição deve ser configurado através das configurações de risco padrão StockSharp.
- As instruções de registro são substituídas por ganchos de desenho de gráfico para que a média móvel e as negociações executadas possam ser visualizadas diretamente na área do gráfico de estratégia.

Essa conversão mantém o modelo de decisão idêntico ao adotar APIs idiomáticas de alto nível StockSharp (`SubscribeCandles`, `Bind` e `StartProtection`). Use-o como uma estrutura leve para construir sistemas de média móvel mais avançados.
