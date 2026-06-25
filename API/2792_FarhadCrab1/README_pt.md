# Estratégia FarhadCrab1 (C#)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia FarhadCrab1 é um sistema de seguidor de tendência que entra em operações em retrocessos para uma média móvel exponencial (EMA) e gerencia as saídas usando stops fixos, take-profits, um trailing stop inspirado no Parabolic SAR e um filtro de período de tempo superior. O assessor especialista original do MetaTrader 5 depende de velas horárias para execução enquanto referencia dados diários para decidir quando fechar posições abertas. Este port em C# mantém a mesma lógica central combinando um filtro EMA de período de trabalho com uma regra de saída por cruzamento de EMA diária.

## Conceitos principais
- **Filtro de tendência:** Uma EMA calculada no período de trabalho (padrão: EMA de 15 períodos em velas de 1 hora). Apenas sinais de compra são permitidos quando a mínima da vela anterior permanece acima da EMA, e apenas sinais de venda são permitidos quando a máxima da vela anterior fica abaixo da EMA.
- **Filtro diário:** Uma EMA separada calculada em velas diárias. Quando a EMA diária cruza acima do fechamento diário, todas as posições compradas são fechadas. Quando cruza abaixo, todas as posições vendidas são fechadas. Isso imita a lógica original `ClosePositions` do código MQL5.
- **Controles de risco:** Os níveis fixos de stop-loss e take-profit são derivados de distâncias em pips. Um trailing stop move o stop de proteção quando a posição ganha lucro suficiente, emulando a função de trailing do MT5 que combina as configurações `TrailingStop` e `TrailingStep`.
- **Gerenciamento de posição única:** A estratégia opera com uma única posição líquida. Entrar em uma posição comprada enquanto se mantém uma vendida (ou vice-versa) primeiro fecha a exposição oposta antes de abrir a nova operação.

## Regras de negociação
1. **Detecção de sinal (período de trabalho):**
   - Entrada comprada quando a mínima da vela anterior é maior que o valor da EMA (após aplicar o deslocamento configurado).
   - Entrada vendida quando a máxima da vela anterior é menor que o valor da EMA.
2. **Dimensionamento de posição:** O parâmetro `Volume` define o tamanho base da ordem. Ao reverter de vendido para comprado (ou vice-versa), o motor envia automaticamente a quantidade adicional necessária para virar a posição líquida.
3. **Stop-loss e take-profit:**
   - As distâncias são definidas em pips. O tamanho do pip se adapta automaticamente ao tamanho do tick do instrumento, com símbolos FX de cinco e três dígitos usando um multiplicador de 10x para corresponder ao comportamento do MT5.
   - O stop-loss ou take-profit pode ser desabilitado definindo a respectiva distância em pips como zero.
4. **Trailing stop:**
   - Ativa somente quando `TrailingStopPips` é maior que zero.
   - O stop é movido para `preço_atual - TrailingStopPips` (para comprados) ou `preço_atual + TrailingStopPips` (para vendidos) quando o lucro da posição excede `TrailingStopPips + TrailingStepPips`.
   - O passo adicional de trailing evita modificações frequentes.
5. **Filtro de saída diário:**
   - Usa as últimas duas velas diárias completadas.
   - Posições compradas são fechadas se a EMA diária estava abaixo do fechamento diário dois dias atrás e está acima do fechamento diário no dia mais recente (cruzamento de baixa).
   - Posições vendidas são fechadas se o cruzamento oposto ocorrer.

## Parâmetros
| Nome | Tipo | Padrão | Descrição |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | Período de 1 hora | Período de trabalho usado para a EMA de execução e lógica de entrada. |
| `MaLength` | `int` | 15 | Período da EMA no período de trabalho. |
| `MaShift` | `int` | 0 | Número de velas completadas usadas para deslocar a EMA para trás. |
| `DailyMaLength` | `int` | 15 | Período da EMA diária que fornece o filtro de saída por cruzamento. |
| `StopLossPips` | `decimal` | 50 | Distância do stop-loss em pips. Defina como `0` para desabilitar. |
| `TakeProfitPips` | `decimal` | 50 | Distância do take-profit em pips. Defina como `0` para desabilitar. |
| `TrailingStopPips` | `decimal` | 10 | Distância do trailing stop em pips. Defina como `0` para desabilitar o trailing. |
| `TrailingStepPips` | `decimal` | 5 | Ganho adicional mínimo em pips antes de o trailing stop ser atualizado novamente. |
| `Volume` | `decimal` | 0.1 | Tamanho base da operação em lotes/contratos. |

## Notas e diferenças em relação à versão MQL
- Este port sempre usa médias móveis exponenciais, refletindo o padrão original (`MODE_EMA`). Outros modos de suavização do MT5 não são suportados.
- O assessor especialista do MT5 trabalha com cotações de oferta/demanda em cada tick. Esta tradução opera em velas terminadas, portanto as verificações de stop-loss e take-profit são avaliadas nas máximas/mínimas das velas.
- O indicador Parabolic SAR presente no arquivo original não influenciou as decisões de negociação e, portanto, é omitido da implementação em C#.
- A lógica de trailing ajusta o nível de stop armazenado, mas não envia ordens de stop ao corretor. A saída ocorre quando o intervalo da vela toca o nível de stop ou take-profit calculado.

## Dicas de uso
- Escolher um tipo de vela que corresponda ao horizonte de negociação desejado. As velas de uma hora padrão replicam o comportamento do script fonte.
- Ajustar `MaLength` e `DailyMaLength` juntos para sintonizar a capacidade de resposta entre entradas intradiárias e filtros de tendência de período de tempo superior.
- Para símbolos FX cotados com cinco dígitos (p.ex., EURUSD), as distâncias em pips serão automaticamente escaladas para que 1 pip equivalha a 0.0001.
- Ao executar backtests, garantir que o fluxo de dados diários esteja disponível para que o filtro de saída possa funcionar corretamente.
