# Estratégia ProfitLossTrailStrategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

ProfitLossTrailStrategy é um auxiliar de gestão de risco convertido do expert advisor do MetaTrader **ProfitLossTrailEA v2.30**. A estratégia não gera entradas por conta própria. Em vez disso, supervisiona a posição atualmente aberta no ativo configurado e aplica automaticamente saídas de proteção:

- níveis iniciais de stop-loss e take-profit;
- gestão de trailing stop com distância de ativação opcional e controle de passo trailing;
- proteção break-even com gatilho de lucro e offset configuráveis;
- capacidade de remover níveis de proteção existentes quando o trader deseja gerenciá-los manualmente.

O comportamento corresponde de perto ao modo de gestão de "cesta" do EA original: todas as ordens da mesma direção são tratadas como uma única posição e os níveis de proteção são recalculados sempre que a exposição muda.

## Referência de parâmetros

| Parâmetro | Descrição |
|-----------|-----------|
| **Manage As Basket** | Quando habilitado (padrão), cada fill na mesma direção recalcula o preço médio de entrada e atualiza níveis de stop-loss/take-profit. Desabilite o flag para manter os níveis iniciais após o primeiro fill. |
| **Enable Take Profit** | Liga ou desliga o tratamento automático de take-profit. |
| **Take Profit (pips)** | Distância em pips entre o preço de entrada e o alvo de take-profit. |
| **Enable Stop Loss** | Liga ou desliga o tratamento automático de stop-loss. |
| **Stop Loss (pips)** | Distância em pips entre o preço de entrada e o stop de proteção inicial. |
| **Enable Trailing Stop** | Ativa a gestão dinâmica do stop quando a posição está em lucro. |
| **Trailing Activation (pips)** | Lucro mínimo em pips necessário antes que o trailing stop possa mover. Use `0` para ativar imediatamente. |
| **Trailing Stop (pips)** | Distância trailing base expressa em pips. |
| **Trailing Step (pips)** | Lucro adicional que deve ser obtido antes de apertar ainda mais o trailing stop. |
| **Enable Break-Even** | Habilita a rotina break-even que move o stop para lucro após uma distância de gatilho. |
| **Break-Even Trigger (pips)** | Distância de lucro que ativa o movimento break-even. |
| **Break-Even Offset (pips)** | Offset extra adicionado acima (comprado) ou abaixo (vendido) do preço de entrada quando break-even ativa. |
| **Remove Take Profit** | Quando definido como `true`, qualquer valor atual de take-profit é limpo e nenhuma saída por take-profit é emitida. |
| **Remove Stop Loss** | Quando definido como `true`, qualquer valor atual de stop-loss é limpo e nenhuma saída por stop-loss ou trailing é emitida. |
| **Candle Type** | Série de candles usada para monitorar a ação do preço. Verificações de trailing, break-even e saída são avaliadas em candles concluídos. |

## Notas de uso

1. Anexe a estratégia a um ativo e garanta que ordens sejam colocadas externamente ou por outra estratégia. ProfitLossTrailStrategy foca puramente em gerenciar a exposição aberta.
2. Configure os parâmetros baseados em pips para corresponder à precificação do instrumento. O tamanho de pip é derivado automaticamente de `Security.PriceStep`.
3. Quando break-even e trailing stop estão habilitados, o ajuste de break-even acontece primeiro. Passos trailing subsequentes só apertam o stop se o novo nível melhorar o preço de proteção atual em pelo menos a distância de passo trailing especificada.
4. Definir **Remove Stop Loss** desabilita stop-loss, trailing e lógica break-even simultaneamente, espelhando o comportamento do EA original.
5. A estratégia usa ordens a mercado (`BuyMarket`/`SellMarket`) para fechar posições quando níveis de proteção são atingidos.

## Notas de conversão

- Os modos "Order_By_Order" e "Same_Type_As_One" do MetaTrader são representados pelo flag **Manage As Basket**. A gestão de níveis de stop por ticket não é suportada no StockSharp, então o modo cesta é aplicado por padrão.
- Filtros de magic number e comentário do EA original não são necessários; a estratégia atua apenas no `Strategy.Security` configurado.
- Desenho de tela, alertas sonoros e atualizações de UI baseadas em timer foram omitidos porque o StockSharp já expõe diagnósticos por logs e bindings de gráfico.
