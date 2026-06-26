# Estratégia de iCCI iRSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia de iCCI iRSI** é uma conversão direta do consultor especialista do MetaTrader 5 `iCCI iRSI.mq5`. O sistema original combina o Índice de Canal de Commodities (CCI) e o Índice de Força Relativa (RSI) para detectar zonas de exaustão. Quando ambos os osciladores concordam em um estado de sobrevenda ou sobrecompra, o advisor abre uma posição, anexa ordens protetoras e opcionalmente segue o stop conforme o trade entra em lucro. Este port StockSharp espelha esse comportamento com APIs de alto nível, incluindo entradas baseadas em pips, fechamento automático de posições opostas e um modo de sinal reversível.

## Lógica de trading
1. Assinar o tipo de vela configurado e calcular um `CommodityChannelIndex` com período `CciPeriod` e um `RelativeStrengthIndex` com período `RsiPeriod`.
2. Avaliar apenas velas concluídas. O ruído intrabarra é ignorado, assim como a implementação MQL que aguarda uma nova barra.
3. Quando ambos os indicadores caem abaixo de seus respectivos limites inferiores (`CciLowerLevel` e `RsiLowerLevel`), a estratégia abre ou reverte para uma posição comprada. Quando ambos os indicadores sobem acima dos limites superiores (`CciUpperLevel` e `RsiUpperLevel`), uma configuração vendida é ativada. Habilitar `ReverseSignals` troca as direções.
4. Antes de enviar uma nova ordem, a exposição oposta atual é fechada para que a posição líquida sempre corresponda ao sinal ativo.
5. Após a entrada, a estratégia monitora o preço de fechamento das velas seguintes. Níveis de take-profit e stop-loss expressos em pips são convertidos para unidades de preço usando o `PriceStep` do instrumento. Para símbolos forex de 3 ou 5 dígitos, um ajuste adicional de ×10 reproduz a definição de pip do MetaTrader.
6. Se `TrailingStopPips` for positivo, o stop-loss é avançado em direção ao mercado sempre que o preço se mover mais de `TrailingStopPips + TrailingStepPips` na direção favorável. As atualizações respeitam o passo configurado para evitar modificações rápidas do stop.

## Gestão de risco e operações
- **Take-profit / Stop-loss** – distâncias opcionais em pips que se tornam níveis de preço absolutos imediatamente após uma execução. Quando qualquer nível é atingido no fechamento de uma vela, a posição é liquidada a mercado.
- **Trailing stop** – replica a lógica de trailing do EA. Os lucros devem superar a distância de trailing mais o passo de trailing antes de o stop ser ajustado.
- **Volume** – um parâmetro `TradeVolume` fixo substitui o seletor original de lote ou risco (`ENUM_LOT_OR_RISK`). Use otimização para descobrir volumes adequados se variantes de gestão monetária forem necessárias.
- **Higiene de posição** – quando um novo sinal aparece, a estratégia zera qualquer holding oposto antes de abrir o novo trade, assim como o EA realiza `ClosePositions`.

## Parâmetros
- **Candle Type** – série de dados de velas processada pelos indicadores (padrão: velas de 1 hora).
- **CciPeriod** – comprimento de média do CCI (padrão: 14).
- **CciUpperLevel / CciLowerLevel** – limites de sobrecompra e sobrevenda do CCI (padrões: +80 / −80).
- **RsiPeriod** – comprimento de média do RSI (padrão: 42).
- **RsiUpperLevel / RsiLowerLevel** – níveis de disparo do RSI (padrões: 60 / 30).
- **ReverseSignals** – inverte a interpretação dos sinais do oscilador (padrão: `false`).
- **TradeVolume** – tamanho da ordem a mercado. Definir para corresponder à entrada de lote do MT5 (padrão: 0.1).
- **StopLossPips / TakeProfitPips** – distâncias protetoras em pips (padrões: 0 e 140). Definir como zero para desabilitar.
- **TrailingStopPips / TrailingStepPips** – distância do trailing stop e passo mínimo (padrões: 5 / 5). Uma distância de trailing zero desabilita o trailing mesmo que um passo seja fornecido.

## Notas de implementação
- Os indicadores StockSharp (`CommodityChannelIndex`, `RelativeStrengthIndex`) entregam valores decimais prontos para uso através da API `Bind`, portanto não é necessária lógica manual `CopyBuffer`.
- Todo o gerenciamento de operações ocorre em velas concluídas. Isso corresponde ao guarda `PrevBars` do EA e previne múltiplas entradas dentro da mesma barra.
- A conversão de pips respeita cotações de pips fracionários multiplicando o `PriceStep` por 10 para instrumentos com 3 ou 5 casas decimais – um análogo direto da lógica `digits_adjust` do MQL.
- Os alvos protetores são simulados via saídas a mercado porque as estratégias StockSharp operam dentro de um ambiente sandbox onde modificações síncronas de ordens não estão disponíveis.
- Áreas de gráfico adicionais desenham as linhas CCI e RSI para validação visual das zonas de entrada.

## Diferenças em relação ao Expert Advisor original
- O módulo MetaTrader `MoneyFixedMargin` não está portado. O dimensionamento de posição é agora um parâmetro de volume fixo simples.
- Verificações específicas de broker como `FreezeStopsLevels` não estão disponíveis no StockSharp. O trailing stop observa apenas distância de preço e requisitos de passo.
- As strings de logging e alerta foram removidas em favor de saída limpa da estratégia. O sistema de logging do StockSharp pode ser anexado externamente se necessário.
- O gerenciamento de operações funciona nos fechamentos das velas. A versão MT5 podia reagir intrabarra quando o stop ou take-profit é tocado, mas a aproximação ao final da barra mantém a lógica determinística para backtests.

## Dicas de uso
1. Começar com o período padrão de 1 hora para espelhar o template original. Períodos mais curtos podem introduzir mais sinais mas também mais armadilhas.
2. Otimizar `CciUpperLevel`, `CciLowerLevel`, `RsiUpperLevel` e `RsiLowerLevel` juntos – o EA depende da concordância entre ambos os osciladores, então limites equilibrados são essenciais.
3. Ao operar em pares forex, verificar que os metadados do instrumento expõem `PriceStep` e `Decimals` para que as distâncias em pips sejam convertidas corretamente.
4. Desabilitar `ReverseSignals` para comportamento clássico de reversão de tendência. Habilitar para operar rompimentos de zonas de sobrecompra/sobrevenda.
5. Combinar com módulos de risco do StockSharp (stop de patrimônio, proteção de drawdown) se controles em nível de portfólio forem necessários – eles substituem o helper `m_money` do MT5.

Esta documentação fornece todo o contexto necessário para implantar, personalizar e estender a estratégia iCCI iRSI dentro do ambiente StockSharp.
