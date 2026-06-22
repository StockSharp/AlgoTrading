# Estratégia NUp1Down
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia NUp1Down** é uma conversão direta do especialista do MetaTrader 5 "N bars up, then one bar down" (arquivo `NUp1Down.mq5`). Ela verifica velas concluídas entregues pelo StockSharp e entra em uma operação curta quando uma vela de baixa aparece após uma sequência configurável de velas de alta que continuam fazendo fechamentos mais altos. A estratégia é projetada para traders discricionários que querem automatizar um padrão clássico de reversão de swing dentro do StockSharp Designer, Shell ou Runner.

## Lógica de trading
1. Trabalhar apenas com velas terminadas fornecidas pelo parâmetro `CandleType`.
2. Manter as últimas `BarsCount + 1` velas na memória. A vela mais nova deve fechar abaixo de sua abertura (vela de configuração baixista).
3. As `BarsCount` velas anteriores devem fechar acima de suas aberturas. Cada uma dessas velas de alta (exceto a mais antiga) também deve fechar acima do fechamento da vela que veio logo antes, impondo um movimento "escada" para cima.
4. Quando o padrão é validado e não há posição curta ativa, a estratégia envia uma ordem de venda a mercado.
5. O dimensionamento da posição usa o parâmetro `RiskPercent`. O algoritmo estima quantos contratos podem ser abertos para que o capital em risco (distância ao stop-loss convertida em valor monetário) não exceda a porcentagem escolhida do portfólio. A propriedade base `Volume` continua sendo o tamanho mínimo de lote e o modelo de risco só pode aumentar o tamanho da operação.

## Gestão de posição
- Ao entrar, um stop-loss protetor e um nível de take-profit são calculados a partir do preço de entrada. Ambas as distâncias são expressas em pips e traduzidas em preços usando o `PriceStep` do instrumento. Para símbolos com três ou cinco dígitos decimais, o tamanho do pip é ajustado automaticamente para corresponder à definição de pip do MetaTrader.
- Um stop trailing é recalculado em cada vela terminada. A distância de trailing é igual a `TrailingStopPips` e o stop é deslocado apenas se o preço se moveu pelo menos `TrailingStepPips` a favor da operação. A lógica de trailing emula o especialista original: para operações curtas segue o preço de oferta para baixo, enquanto operações longas não são produzidas por esta estratégia.
- As condições de saída são avaliadas antes de buscar novas entradas em cada vela. A estratégia fecha a posição quando o stop-loss ou o take-profit é atingido, ou quando a lógica de trailing aperta o stop acima do preço de oferta atual.

## Parâmetros
| Nome | Descrição |
| ---- | --------- |
| `BarsCount` | Número de velas de alta necessárias antes da vela de configuração baixista (padrão: 3). |
| `TakeProfitPips` | Distância de take-profit em pips aplicada ao preço de entrada (padrão: 50). |
| `StopLossPips` | Distância de stop-loss em pips aplicada ao preço de entrada (padrão: 50). |
| `TrailingStopPips` | Distância entre o preço de mercado e o stop trailing (padrão: 10). |
| `TrailingStepPips` | Movimento favorável mínimo antes de o stop trailing avançar (padrão: 5). |
| `RiskPercent` | Porcentagem do capital do portfólio a ser arriscada em cada operação (padrão: 5). |
| `CandleType` | Tipo de dados de velas / período usado para detecção do padrão (padrão: 1 hora). |

## Notas de uso
- Configure a propriedade `Volume` para o tamanho mínimo de ordem permitido pelo seu broker. O dimensionamento baseado em risco pode aumentar o tamanho da operação mas nunca o reduz abaixo de `Volume`.
- A estratégia mantém apenas uma posição curta agregada a qualquer momento. Se existir uma posição comprada, ela será fechada antes de abrir a vendida.
- O algoritmo trabalha com dados de velas. Os hits de stop-loss ou take-profit intrabar são detectados usando a máxima/mínima da vela, portanto o tempo de execução real pode diferir da execução em nível de tick.
- Nenhuma versão Python é fornecida nesta versão. Apenas a implementação C# dentro de `API/2574/CS/NUp1DownStrategy.cs` está disponível.
