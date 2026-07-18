# Estratégia de fuga de lucro
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia replica o especialista em "take-profit" MetaTrader procurando quatro velas consecutivas com máximas e aberturas estritamente monótonas. Quando a vela atual termina com máximos ascendentes e abre, o algoritmo trata a sequência como um impulso de alta e envia uma compra de mercado. Uma condição espelhada com máximas e aberturas em queda produz uma venda no mercado. Os pedidos são gerenciados com uma meta de lucro no nível da conta, um trailing stop que pode fechar parcialmente a exposição e um stop loss fixo opcional definido em etapas de preço.

A configuração padrão é negociada em velas de um minuto. A estratégia pode ser ajustada para diferentes instrumentos ajustando o tipo de vela, índices de mudança que controlam quais velas são comparadas, distância final, distância de stop-loss, meta de lucro e modo de dimensionamento de posição. Ele suporta um tamanho de lote fixo ou um volume dinâmico calculado a partir do patrimônio do portfólio e da porcentagem de risco definida pelo usuário. Quando o trailing stop avança, o algoritmo pode opcionalmente fechar metade da posição restante para garantir lucros enquanto mantém um runner ativo.

Atingir a meta de lucro configurada medida no patrimônio do portfólio liquida imediatamente a posição atual e cancela quaisquer ordens de trabalho. Isso reflete o especialista MQL original que fechou todas as negociações quando o patrimônio da conta excedeu o saldo mais o ganho desejado. A ramificação de gerenciamento de riscos valida a porcentagem de risco configurada e garante que o volume solicitado respeite a etapa do volume de segurança.

## Detalhes

- **Lógica de entrada**:
  - **Longo**: as quatro velas monitoradas mostram máximas estritamente crescentes e aberturas estritamente crescentes.
  - **Curto**: as quatro velas monitoradas mostram máximas estritamente decrescentes e aberturas estritamente decrescentes.
- **Gerenciamento de posição**:
  - Stop-loss opcional colocado ao preço de entrada menos/mais o número configurado de etapas de preço.
  - O trailing stop segue o preço de fechamento quando ele se move mais do que a distância final da entrada.
  - A saída parcial (50% do volume restante) é executada toda vez que o trailing stop se move, sujeito à etapa de volume do título e ao lote mínimo negociável.
- **Account Target**: encerra todas as exposições e cancela pedidos ativos quando `portfolio equity ≥ initial equity + ProfitTarget`.
- **Gerenciamento de Riscos**:
  - O modo de lote fixo usa o parâmetro `Lots` configurado (ou `Volume` da base da estratégia, se especificado).
  - O modo percentual de risco dimensiona o pedido como `equity * RiskPercent / max(stopDistance, price)` e normaliza o resultado pela etapa de volume.
- **Parâmetros padrão**:
  - `Shift1` = 0, `Shift2` = 1, `Shift3` = 2, `Shift4` = 3.
  - `TrailingStopPoints` = 1, `StopLossPoints` = 0, `ProfitTarget` = 1 (unidades monetárias da conta).
  - `Lots` = 1, `RiskPercent` = 1, `MaxOrders` = 1.
  - `CandleType` = período de 1 minuto.
- **Melhores Mercados**: tendências de futuros, principais FX e pares de criptomoedas líquidas onde o impulso de curto prazo persiste em várias velas.
- **Pontos fortes**: detecção rápida de impulso, meta de patrimônio configurável, expansão parcial e controles de risco simples.
- **Pontos fracos**: sensível a faixas ruidosas, depende dos tamanhos corretos dos passos e assume o modo de compensação (posição agregada única).

## Parâmetros

| Nome | Descrição |
| --- | --- |
| `Shift1` – `Shift4` | Índices das velas comparados para a sequência de breakout. |
| `TrailingStopPoints` | Distância final em etapas de preço. |
| `StopLossPoints` | Distância de parada inicial em etapas de preço; zero desativa o stop loss. |
| `ProfitTarget` | Meta de lucro aplicada ao patrimônio do portfólio antes de fechar todas as negociações. |
| `Lots` | Volume de negociação fixo quando o gerenciamento de risco está desativado. |
| `RiskManagement` | Permite o dimensionamento baseado em risco usando `RiskPercent`. |
| `RiskPercent` | Percentagem de capital da carteira arriscada em cada negociação quando a gestão de risco está ativa. |
| `PartialClose` | Se habilitado, fecha metade da posição sempre que o trailing stop se move. |
| `MaxOrders` | Número máximo de unidades base permitidas simultaneamente (limite de posição líquida). |
| `CandleType` | Período de tempo usado para geração de sinal. |

## Dicas de uso

1. Alinhe os parâmetros `Shift` com a volatilidade do instrumento. Mudanças maiores analisam sequências de momentum mais longas.
2. Defina `TrailingStopPoints` em relação à etapa de preço do título; valores muito pequenos podem gerar saídas parciais rápidas.
3. Use o dimensionamento percentual de risco com um `StopLossPoints` explícito para que o tamanho da posição reflita o risco monetário real por negociação.
4. Monitore a curva de patrimônio: uma vez atingida a meta global, a estratégia para de negociar até ser reiniciada, imitando o EA original.
