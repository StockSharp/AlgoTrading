# Estratégia de mudança Krohabor Up3x1
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia **up3x1 Krohabor D** é uma conversão do MetaTrader 4 consultor especialista `up3x1_Krohabor_D.mq4`. Ele mantém a ideia original de alinhar três médias móveis simples deslocadas (SMA) para detectar rompimentos de continuação de tendência no período de tempo ativo. A implementação C# usa o StockSharp API de alto nível com assinaturas de velas e ligações de indicadores, enquanto adapta o gerenciamento de risco e posição ao ambiente .NET.

## Lógica de negociação
- Três SMAs são calculados sobre os preços de fechamento do instrumento:
  - Rápido SMA (padrão 24 barras)
  - Médio SMA (padrão 60 barras)
  - Lento SMA (padrão 120 barras)
- Cada média móvel é deslocada para frente por um número configurável de velas concluídas (padrão 6). A estratégia compara o valor atual deslocado e o valor da vela anterior para cada média.
- **Requisitos de entrada longa**:
  - Os valores lentos atuais e anteriores de SMA permanecem abaixo dos valores atuais e anteriores de rápido/médio SMA, indicando separação de alta.
  - O meio SMA está caindo em relação ao rápido SMA (meio anterior acima do rápido anterior, meio atual abaixo do rápido atual).
- **Entrada curta** reflete a lógica longa com todas as comparações invertidas.
- Apenas uma posição pode ser aberta por vez. Quando nenhuma posição está ativa a estratégia aguarda um novo sinal de entrada; caso contrário, ele gerencia saídas.

## Regras de saída e proteção
- As ordens de proteção iniciais são simuladas monitorando os máximos e mínimos das velas:
  - A distância de stop-loss é expressa em etapas de preço (padrão 110 pontos) e aplicada assim que uma posição é aberta.
  - A distância de lucro usa a mesma representação (padrão 5 pontos).
- Um trailing stop (padrão 10 pontos) é ativado quando o lucro não realizado excede o limite configurado. O stop segue o mercado a favor da posição aberta, sem nunca recuar.
- As saídas de reversão da média móvel fecham a negociação quando o SMA rápido volta pelas médias média e lenta, imitando a lógica de fechamento do EA original.
- A redução dinâmica do volume após perdas consecutivas replica o comportamento do script MT4: o tamanho da negociação diminui proporcionalmente ao número de negociações perdidas, respeitando um limite mínimo de volume.

## Parâmetros
| Nome | Descrição |
|------|-------------|
| `FastPeriod` | Período do jejum SMA. |
| `MediumPeriod` | Período do meio SMA. |
| `SlowPeriod` | Período da lentidão SMA. |
| `MaShift` | Número de velas concluídas usadas para deslocar todas as médias móveis para frente. |
| `Volume` | Volume base de pedidos para novas entradas. |
| `MinVolume` | Volume mínimo permitido após ajustes baseados em perdas. |
| `LossReductionFactor` | Divisor aplicado ao diminuir o volume após negociações consecutivas com perdas. |
| `StopLossPoints` | Distância de stop-loss medida em etapas de preço. |
| `TakeProfitPoints` | Distância de lucro medida em etapas de preço. |
| `TrailingPoints` | Distância do trailing-stop e limite de ativação em etapas de preço. |
| `CandleType` | Tipo de dados de vela (período de tempo) usado para análise. |

## Notas
- A estratégia usa `SubscribeCandles` junto com `Bind` para transmitir os resultados dos indicadores, evitando a recuperação manual do valor do indicador.
- O comportamento de stop-loss, take-profit e trailing é implementado dentro do ciclo estratégico para permanecer independente do corretor. Em ambientes de negociação ao vivo, você pode substituir esses blocos por ordens de proteção reais, se necessário.
- Todos os comentários no código-fonte são escritos em inglês para cumprir as diretrizes do projeto.
- Nenhum teste automatizado é fornecido; use backtesting dentro de StockSharp para validar conjuntos de parâmetros para seus instrumentos.
