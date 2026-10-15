# Estratégia PivotEMA3RLHv4
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

PivotEMA3RLHv4 é uma estratégia de acompanhamento de tendências que combina o nível de pivô diário com filtros de impulso de curto prazo. Ele rastreia uma média móvel exponencial de 3 períodos (EMA) calculada nos preços de abertura das velas e a compara com o mesmo EMA calculado nos preços de fechamento. A configuração é validada com velas Heiken Ashi para confirmar a direção e com múltiplas medições de Average True Range (ATR) para garantir que a volatilidade esteja se expandindo. A estratégia negocia um único instrumento no período intradiário selecionado e sempre espera que a vela atual termine antes de tomar uma decisão.

## Lógica de negociação

1. **Filtro de pivô** – O EMA(3) anterior do preço de abertura deve estar abaixo (para posições compradas) ou acima (para posições vendidas) do nível do pivô diário, enquanto o EMA(3) atual do preço de abertura precisa cruzar para o lado oposto do pivô.
2. **Confirmação de Heiken Ashi** – A vela Heiken Ashi atual deve ser de alta (fechamento acima da abertura) para posições compradas ou de baixa (fechamento abaixo da abertura) para vendas.
3. **Verificação de Momentum** – O EMA(3) baseado nos preços de fechamento deve liderar o EMA nas aberturas na direção comercial.
4. **Expansão da volatilidade** – Pelo menos um dos valores ATR(4), ATR(8), ATR(12) ou ATR(24) deve aumentar em comparação com a vela anterior, e o True Range (ATR com comprimento 1) deve aumentar nesta barra ou ter aumentado na barra anterior.
5. **Gerenciamento de posição** – Apenas uma posição está ativa por vez. Paradas e metas de proteção são simuladas internamente e executadas por meio de ordens de mercado quando atingidas.

Os sinais de saída refletem as regras de entrada: quando aparecem as condições opostas, a estratégia fecha a negociação atual. Além disso, os mecanismos opcionais de stop-loss, take-profit e trailing stop podem fechar uma negociação mais cedo.

## Parâmetros

| Parâmetro | Descrição |
| --- | --- |
| `CandleType` | Prazo de trabalho para as velas estratégicas. |
| `StopLossPips` | Distância de parada inicial em pips a partir do preço de entrada. Defina como zero para desativar. |
| `TakeProfitPips` | Distância alvo de lucro em pips. Defina como zero para desativar. |
| `UseTrailingStop` | Ativa ou desativa o gerenciamento de trailing stop. |
| `TrailingStopType` | Modo de trilha: 1 mantém uma distância fixa, 2 é ativado após o preço se mover em `TrailingStopPips`, 3 usa a escada de vários estágios descrita abaixo. |
| `TrailingStopPips` | Distância (em pips) usada pelo tipo 2 à direita. |
| `FirstMovePips` / `FirstStopLossPips` | Distância de disparo e deslocamento de parada resultante para o primeiro estágio do tipo de arrasto 3. |
| `SecondMovePips` / `SecondStopLossPips` | Distância de disparo e deslocamento de parada resultante para o segundo estágio do tipo de arrasto 3. |
| `ThirdMovePips` / `TrailingStop3Pips` | Distância de disparo e distância de fuga dinâmica para o estágio final do tipo de fuga 3. |

## Modos de parada final

- **Tipo 1** – Reposiciona o stop para que ele nunca fique mais atrasado em relação ao preço do que a distância inicial do stop.
- **Tipo 2** – Espera que o preço se mova em `TrailingStopPips` antes de bloquear lucros com a mesma distância.
- **Tipo 3** – Usa até três limites: os dois primeiros movem o stop para deslocamentos predefinidos, enquanto o terceiro se transforma em um trailing stop regular.

## Notas

- A estratégia assina velas diárias para calcular o nível de pivô da máxima, mínima e fechamento do dia anterior.
- Os indicadores são atualizados dentro do manipulador de velas usando apenas barras acabadas, o que mantém a lógica compatível com ambientes online e de backtesting.
- A versão original do MetaTrader dependia de paradas do lado do corretor; esta porta os simula e sai com ordens de mercado quando necessário.
