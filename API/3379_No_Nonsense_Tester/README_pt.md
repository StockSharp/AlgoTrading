# Estratégia de teste sem sentido
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **No Nonsense Tester Strategy** é uma versão StockSharp do consultor especialista MQL4 "NoNonsenseTester". A implementação se concentra no fluxo de trabalho principal do NNFX que valida uma linha de base de tendência, aguarda dois indicadores de confirmação, verifica a volatilidade usando ATR e supervisiona negociações com lógica de saída estrita. A estratégia é projetada para experimentação multiparâmetro e, portanto, expõe todos os limites importantes por meio de objetos `StrategyParam` para que possam ser otimizados dentro de StockSharp.

## Lógica de negociação
1. **Filtro de linha de base** – um EMA com comprimento configurável define a direção da tendência primária. As entradas só são consideradas quando o preço fecha na linha de base.
2. **Confirmação #1** – um RSI deve estar no lado de alta (acima do limite) ou de baixa (abaixo do limite complementar) para confirmar a quebra da linha de base.
3. **Confirmação #2** – um CCI deve concordar com a tendência e exceder a magnitude absoluta configurada para bloquear sinais fracos.
4. **Filtro de volatilidade** – ATR deve ser maior que o valor `AtrMinimum`, garantindo que as negociações sejam realizadas apenas quando o mercado mostrar faixa suficiente.
5. **Entrada** – quando o cruzamento da linha de base, as duas confirmações e o filtro de volatilidade estão alinhados, a estratégia abre uma posição na direção do movimento. O tamanho da posição pode opcionalmente ser dimensionado com ATR por meio do parâmetro `AtrEntryMultiplier`.
6. **Stop e meta** – imediatamente após a entrada, a estratégia calcula os níveis de stop loss e takeprofit baseados em ATR. O rastreamento opcional ATR continua atualizando o stop de proteção enquanto a negociação se move a favor.
7. **Sobreposição de saída** – um RSI adicional com período mais curto supervisiona as negociações abertas. Se cruzar abaixo da banda inferior para posições compradas ou acima da banda superior para posições vendidas, a posição será fechada mesmo que o preço não tenha atingido os níveis de proteção.

## Parâmetros
| Parâmetro | Descrição |
|-----------|-------------|
| `BaselineLength` | Período da linha de base EMA. |
| `ConfirmationRsiLength` | Comprimento do indicador de confirmação RSI. |
| `ConfirmationRsiThreshold` | Nível RSI separando confirmações de alta e baixa. |
| `ConfirmationCciLength` | Comprimento do indicador de confirmação CCI. |
| `ConfirmationCciThreshold` | Magnitude absoluta mínima CCI para aceitar um sinal. |
| `AtrPeriod` | período ATR de retrospectiva. |
| `AtrEntryMultiplier` | Multiplicador ATR opcional que dimensiona o volume negociado. |
| `AtrTakeProfitMultiplier` | Multiplicador de ATR para o nível de lucro. |
| `AtrStopLossMultiplier` | Multiplicador ATR para o nível de stop loss. |
| `AtrTrailingMultiplier` | Multiplicador ATR usado para rastreamento dinâmico. Defina como `0` para desativar. |
| `AtrMinimum` | Valor mínimo de ATR exigido antes de abrir negociações. |
| `ExitRsiLength` | Comprimento da sobreposição de saída RSI. |
| `ExitRsiUpperLevel` | Nível RSI que força saídas curtas. |
| `ExitRsiLowerLevel` | Nível RSI que força saídas longas. |
| `CandleType` | Tipo de vela (período de tempo) usado para cálculos. |

## Objetos de gráfico
A estratégia desenha automaticamente:
- Velas de origem.
- EMA linha de base.
- Marcadores de negociações executadas.

## Notas de otimização
Cada `StrategyParam` usado na lógica expõe intervalos de otimização que refletem a flexibilidade do testador original. Use ferramentas de otimização StockSharp para varrer comprimentos de linha de base, limites de confirmação e configurações de risco para reproduzir os testes de grade de parâmetros fornecidos pela versão MQL.

## Dicas de uso
- Combine a estratégia com predefinições de indicadores NNFX ajustando os limites para corresponder às suas ferramentas personalizadas.
- Fique de olho no filtro ATR; um `AtrMinimum` diferente de zero impede negociações durante sessões de baixa volatilidade.
- Ao testar as negociações de continuação, defina `AtrTrailingMultiplier` maior que zero para permitir que as posições lucrativas respirem enquanto bloqueiam os ganhos.
