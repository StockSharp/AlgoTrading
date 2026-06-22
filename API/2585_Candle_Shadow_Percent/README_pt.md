# Estratégia de Percentual de Sombra de Vela
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia de Percentual de Sombra de Vela** é uma portagem direta do consultor especialista do MetaTrader *Candle shadow percent*. Ela procura velas onde a sombra superior ou inferior atinge uma porcentagem configurável do corpo da vela. Quando uma sombra superior longa aparece, a estratégia abre uma posição vendida; quando uma sombra inferior profunda aparece, abre uma posição comprada. A direção da operação está alinhada com o algoritmo original e mantém o fluxo de trabalho de gestão de risco intacto.

## Notas de conversão
* O especialista original dependia de um indicador personalizado. Na versão do StockSharp, as proporções de sombra e corpo são calculadas diretamente a partir de velas terminadas, portanto não há dependências de indicadores externos.
* Os valores de pip são derivados de `Security.PriceStep`. Ajuste `StopLossPips`, `TakeProfitPips` e `MinBodyPips` para corresponder ao tamanho do tick do instrumento.
* O dimensionamento de posição baseado em risco replica a lógica `CMoneyFixedMargin` do MetaTrader ao arriscar uma porcentagem do valor atual do portfólio contra a distância de stop-loss configurada.

## Qualificação de vela
Uma vela é considerada para trading quando:
1. Seu tamanho de corpo absoluto é pelo menos `MinBodyPips * Security.PriceStep`.
2. A sombra correspondente é positiva.
3. O ratio sombra-corpo satisfaz a lógica de limiar selecionada:
   * **Sombra superior** (configuração de venda): `(High − max(Open, Close)) / Body * 100` é maior ou igual a `TopShadowPercent` quando `TopShadowIsMinimum = true`, caso contrário deve ser menor ou igual a esse valor.
   * **Sombra inferior** (configuração de compra): `(min(Open, Close) − Low) / Body * 100` é maior ou igual a `LowerShadowPercent` quando `LowerShadowIsMinimum = true`, caso contrário deve ser menor ou igual a esse valor.
4. Quando ambas as sombras satisfazem seus limiares na mesma vela, a estratégia mantém apenas o lado com o maior ratio de sombra para evitar sinais duplos.

## Regras de entrada
* **Entrada vendida** – acionada por um sinal de sombra superior válido enquanto a estratégia está plana ou comprada. A estratégia reverte a exposição comprada existente se necessário e estabelece as ordens de proteção imediatamente.
* **Entrada comprada** – acionada por um sinal de sombra inferior válido enquanto a estratégia está plana ou vendida. A exposição vendida existente é fechada automaticamente antes de estabelecer a nova posição comprada.

## Regras de saída
* **Stop-loss** – colocado a `StopLossPips * Security.PriceStep` do preço de entrada. Posições compradas usam `entrada − distânciaStop`; posições vendidas usam `entrada + distânciaStop`.
* **Take-profit** – alvo opcional localizado a `TakeProfitPips * Security.PriceStep` da entrada. Quando `TakeProfitPips = 0`, o alvo está desabilitado e as posições dependem exclusivamente do stop-loss ou sinal oposto para sair.
* A estratégia monitora velas concluídas. Se um intervalo de vela tocar o stop ou o alvo, a posição é fechada no próximo ciclo de processamento.

## Dimensionamento de posição
* O risco por operação é calculado como `Portfolio.CurrentValue * (RiskPercent / 100)`. Se o valor do portfólio não estiver disponível, a estratégia recorre ao volume de estratégia configurado.
* A quantidade é igual ao valor de risco dividido pela distância do stop-loss. Ao reverter, o algoritmo adiciona o tamanho absoluto da exposição atual para garantir uma reversão completa, correspondendo ao comportamento do especialista original do MetaTrader.

## Parâmetros
| Parâmetro | Descrição |
|-----------|-----------|
| `CandleType` | Período ou tipo de dados usado para subscrições de velas. |
| `StopLossPips` | Distância do stop-loss expressa em pips/ticks relativa ao instrumento. Deve ser maior que zero. |
| `TakeProfitPips` | Distância do take-profit em pips/ticks. Usar zero para desabilitar o alvo. |
| `RiskPercent` | Porcentagem do valor do portfólio arriscada por operação. |
| `MinBodyPips` | Tamanho mínimo do corpo da vela (em pips/ticks) necessário antes de avaliar os ratios de sombra. |
| `EnableTopShadow` | Habilita sinais vendidos baseados no comprimento da sombra superior. |
| `TopShadowPercent` | Porcentagem limiar para o ratio sombra superior-corpo. |
| `TopShadowIsMinimum` | Quando true, o ratio deve ser maior ou igual ao limiar; quando false, deve ser menor ou igual a ele. |
| `EnableLowerShadow` | Habilita sinais comprados baseados no comprimento da sombra inferior. |
| `LowerShadowPercent` | Porcentagem limiar para o ratio sombra inferior-corpo. |
| `LowerShadowIsMinimum` | Controla se o limiar de sombra inferior é tratado como condição mínima ou máxima. |

## Dicas de uso
* Comece com um período similar ao EA original (p. ex., velas de 5 minutos) e ajuste as distâncias em pips para o seu instrumento.
* Aumente `MinBodyPips` se o ruído produz muitos sinais; diminua para capturar reversões menores.
* Combine a estratégia com filtros adicionais (como indicadores de tendência) estendendo a classe—vinculações para indicadores adicionais podem ser adicionadas dentro de `OnStarted`.
* Sempre valide a interpretação do tamanho do tick em um portfólio demo antes de implantar em produção.
