# Estratégia do Trail SL Manager
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Resumo

Trail SL Manager é uma estratégia utilitária que reproduz o comportamento do especialista MetaTrader `trailSL` original.
Não abre negociações por conta própria. Em vez disso, supervisiona as posições existentes e ajusta dinamicamente os seus níveis de parada de proteção.
A lógica reflete o script de origem: primeiro, o stop é pressionado para atingir o ponto de equilíbrio quando o preço avança em um valor configurável e, em seguida, um algoritmo de rastreamento incremental continua bloqueando os lucros à medida que a tendência continua.

## Como funciona

1. Assina o fluxo de velas configurado para monitorar barras finalizadas.
2. Rastreia o preço médio de entrada e a direção da posição atual.
3. Quando o preço se move a favor da negociação em `BreakEvenTriggerPoints`, o stop é empurrado para o preço de entrada mais um deslocamento opcional.
4. Após a ativação do ponto de equilíbrio, ou imediatamente se permitido, a estratégia aumenta o stop em `TrailOffsetPoints` a cada `TrailStepPoints` até que o preço reverta e feche a posição no mercado.

As regras finais são calculadas com a mesma aritmética baseada em pontos da versão MetaTrader, portanto, o comportamento permanece familiar para os traders que migram para StockSharp.

## Parâmetros

| Nome | Descrição | Padrão |
|------|-------------|---------|
| `EnableBreakEven` | Permite mover o stop para atingir o ponto de equilíbrio quando a negociação se tornar lucrativa. | `true` |
| `BreakEvenTriggerPoints` | Distância de lucro em pontos necessários para ativar o movimento de equilíbrio. | `20` |
| `BreakEvenOffsetPoints` | Pontos adicionais adicionados ao preço de entrada quando o ponto de equilíbrio é executado. | `10` |
| `EnableTrailing` | Alterna a lógica do trailing stop. | `true` |
| `TrailAfterBreakEven` | Se `true`, o rastreamento começa somente após o ajuste do ponto de equilíbrio. | `true` |
| `TrailStartPoints` | Lucro mínimo em pontos antes do trailing é permitido. | `40` |
| `TrailStepPoints` | Etapa de lucro entre recálculos finais. | `10` |
| `TrailOffsetPoints` | Pontos adicionados à parada em cada etapa final. | `10` |
| `InitialStopPoints` | Distância da parada de proteção inicial quando surge uma nova posição. | `200` |
| `CandleType` | Assinatura de vela usada para monitorar alterações de preços. | `1 Minute` |

## Uso

1. Anexe a estratégia a um ambiente onde as entradas sejam geradas por outra estratégia ou manualmente.
2. Configure os limites baseados em pontos para corresponder à volatilidade do símbolo e aos requisitos do corretor.
3. Inicie a estratégia para que ela possa monitorar as velas finalizadas e ajustar os stops automaticamente.
4. Monitore os desenhos do gráfico para ver como os níveis de stop evoluem a cada passo final.

> **Nota:** A estratégia fecha posições com ordens de mercado quando o trailing stop simulado é violado. Adicione proteção específica para exchanges (como ordens stop reais), se exigido pelo seu fluxo de trabalho.
