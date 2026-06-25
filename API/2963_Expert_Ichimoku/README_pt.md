# Estratégia Expert Ichimoku
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A estratégia Expert Ichimoku replica a lógica do assessor especializado original MQL5 "Expert Ichimoku" usando a API de alto nível do StockSharp. O sistema é um modelo de seguimento de tendência direcional que combina múltiplos componentes do indicador Ichimoku Kinko Hyo com filtros de ação de preço e um módulo opcional de dimensionamento de posição estilo martingale.

A estratégia avalia sinais em velas completadas de um período configurável. As operações compradas e vendidas são mutuamente exclusivas — a estratégia mantém uma única posição líquida e muda de direção apenas após fechar a exposição existente. Todos os valores do indicador são calculados na série de velas subscrita; nenhum dado externo é necessário.

## Lógica principal

### Configuração do indicador

* **Tenkan-sen (Linha de conversão):** Média móvel rápida usada para detecção de cruzamentos.
* **Kijun-sen (Linha base):** Média móvel lenta que forma o parceiro do cruzamento.
* **Senkou Span A / Senkou Span B:** Limites da nuvem avaliados na barra anterior para confirmar a estrutura de mercado altista ou baixista.
* **Chikou Span (Linha defasada):** Confirmação de momentum via condições de rompimento de preço defasado.

Os comprimentos do indicador são configuráveis pelo usuário e correspondem aos padrões do especialista MQL5 (9 / 26 / 52).

### Regras de entrada

Uma posição comprada é aberta quando todas as seguintes condições são satisfeitas:

1. **Gatilho de momentum:** Seja que
   * Tenkan-sen cruzou acima de Kijun-sen na barra fechada mais recente (Tenkan<sub>t-1</sub> ≤ Kijun<sub>t-1</sub> e Tenkan<sub>t</sub> > Kijun<sub>t</sub>), ou
   * O Chikou Span rompeu acima do preço histórico (Chikou<sub>t-1</sub> ≤ Close<sub>t-11</sub> e Chikou<sub>t</sub> > Close<sub>t-10</sub>),
2. **Filtro de nuvem:** O fechamento atual está acima de ambos os spans de Senkou da barra anterior (preço completamente acima da nuvem),
3. **Filtro de ação de preço:** A vela anterior fechou altista (Close<sub>t-1</sub> > Open<sub>t-1</sub>),
4. **Filtro de posição:** Nenhuma exposição comprada está atualmente ativa. Se existe uma posição vendida, ela é fechada primeiro; a nova comprada é enviada somente após o vendido ser achatado.

Uma posição vendida é aberta sob condições simétricas:

1. **Gatilho de momentum:** Seja que
   * Tenkan-sen cruzou abaixo de Kijun-sen (Tenkan<sub>t-1</sub> ≥ Kijun<sub>t-1</sub> e Tenkan<sub>t</sub> < Kijun<sub>t</sub>), ou
   * O Chikou Span rompeu abaixo do preço histórico (Chikou<sub>t-1</sub> ≥ Open<sub>t-11</sub> e Chikou<sub>t</sub> < Open<sub>t-10</sub>),
2. **Filtro de nuvem:** O fechamento atual está abaixo de ambos os spans de Senkou da barra anterior,
3. **Filtro de ação de preço:** A vela anterior fechou baixista (Close<sub>t-1</sub> < Open<sub>t-1</sub>),
4. **Filtro de posição:** A exposição comprada existente é fechada antes de abrir o vendido.

### Dimensionamento de posição e opção de martingale

* O tamanho base da ordem é igual à propriedade `Volume` da estratégia.
* Quando **Use Martingale** está habilitado, o próximo tamanho de entrada dobra se a operação completada anterior fechou com prejuízo. Operações lucrativas ou no ponto de equilíbrio redefinem o multiplicador.
* O tamanho de ordem resultante é limitado por `Volume × Max Position Multiplier`, espelhando a proteção de número máximo de posições no EA original.

### Gestão de risco

* **Stop-Loss / Take-Profit estático:** Deslocamentos de preço absolutos opcionais são aplicados a cada nova posição. Se o preço de fechamento atinge o stop ou o alvo, a posição é fechada a mercado.
* **Stop de rastreamento:** Quando tanto `Trailing Stop Offset` quanto `Trailing Step` são positivos, o nível de stop é apertado apenas após o preço avançar além de `offset + step` da entrada, emulando a lógica de rastreamento incremental da versão MQL5.
* A estratégia opera uma posição líquida. Ao sair (via stop, alvo, rastreamento ou reversão), o PnL realizado é avaliado para atualizar o sinalizador de martingale para o próximo sinal.

## Parâmetros

| Nome | Descrição | Padrão |
| --- | --- | --- |
| Tenkan Period | Comprimento da linha Tenkan-sen. | 9 |
| Kijun Period | Comprimento da linha Kijun-sen. | 26 |
| Senkou Span B Period | Comprimento da linha Senkou Span B. | 52 |
| Stop Loss Offset | Distância absoluta entre o preço de entrada e o stop de proteção. Defina como 0 para desabilitar. | 0 |
| Take Profit Offset | Distância absoluta entre o preço de entrada e o alvo de lucro. Defina como 0 para desabilitar. | 0 |
| Trailing Stop Offset | Distância base de rastreamento aplicada após ativação. | 0 |
| Trailing Step | Movimento adicional necessário antes de apertar o stop de rastreamento. | 0 |
| Max Position Multiplier | Limite superior para o tamanho efetivo da ordem (relativo a `Volume`). | 5 |
| Use Martingale | Se deve dobrar o próximo tamanho de operação após uma operação perdedora. | true |
| Candle Type | Série de velas usada para cálculos. | Período de 1 hora |

## Notas práticas

* A estratégia requer pelo menos 12 velas completadas antes que todas as condições possam ser avaliadas (as comparações de Chikou referenciam preços até 11 barras atrás).
* Como as estratégias StockSharp operam em posições líquidas, o parâmetro `Max Position Multiplier` limita o tamanho máximo do contrato em vez de gerenciar múltiplos tickets independentes. Isso mantém o comportamento alinhado com o limite de exposição da implementação MQL5.
* A lógica de stop de rastreamento espelha o EA: o stop é movido apenas quando o preço avançou por `Trailing Stop Offset + Trailing Step`. Definir qualquer parâmetro como zero desabilita ajustes de rastreamento.
* As instruções de registro relatam cada entrada e saída, facilitando a auditoria dos pontos de decisão ao reproduzir dados de mercado.

## Fluxo de trabalho de uso

1. Configure o tipo de vela e o instrumento desejados em um `StrategyContainer` ou modelo de designer.
2. Defina o `Volume` base e ajuste os parâmetros de risco de acordo com a volatilidade do símbolo (por exemplo, converta distâncias baseadas em pips do EA original em unidades de preço para o mercado selecionado).
3. Inicie a estratégia. Uma vez que o indicador tenha histórico suficiente, ele avaliará cruzamentos e confirmações da linha defasada em cada barra completada, gerenciando automaticamente saídas e dimensionamento de martingale.
