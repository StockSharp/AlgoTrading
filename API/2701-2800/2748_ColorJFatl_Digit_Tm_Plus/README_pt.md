# Estratégia ColorJFatl Digit TM Plus
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A estratégia ColorJFatl Digit TM Plus é um port direto do assessor especialista do MetaTrader 5 *Exp_ColorJFatl_Digit_Tm_Plus*. Ela opera reversões de inclinação de uma Linha de Tendência Adaptativa Rápida (FATL) suavizada com uma Média Móvel Jurik (JMA). O indicador original publica três cores (cima, plano, baixo). A estratégia reage quando a cor na última barra finalizada muda e alinha a posição com a nova inclinação.

A implementação no StockSharp mantém o comportamento de alto nível da versão MQL: as ordens são geradas em velas fechadas, saídas baseadas em tempo são opcionais, e a entrada de dimensionamento de lote é representada pelo parâmetro `TradeVolume`.

## Lógica de sinais

1. **Cálculo do indicador**
   - Os preços são alimentados pelo filtro digital FATL de 39 taps fornecido com o indicador original.
   - A série filtrada é suavizada com uma Média Móvel Jurik. O comprimento, o preço aplicado e a precisão de arredondamento podem ser personalizados via parâmetros.
   - O estado de cor é determinado pelo sinal da diferença entre os valores suavizados atual e anterior: `2` para inclinação de alta, `0` para inclinação de baixa e `1` para neutro/sem alteração.

2. **Condições de entrada**
   - **Entrada comprada** – habilitada por `EnableBuyEntries`. Ativada quando a cor da barra atual torna-se `2` enquanto a cor da barra anterior era menor que `2`. Qualquer posição vendida existente é fechada primeiro quando `EnableSellExits` é true.
   - **Entrada vendida** – habilitada por `EnableSellEntries`. Ativada quando a cor da barra atual torna-se `0` enquanto a cor anterior era maior que `0`. Qualquer posição comprada existente é fechada primeiro quando `EnableBuyExits` é true.
   - Apenas uma posição pode estar aberta por vez. As ordens são enviadas no fechamento da vela de confirmação.

3. **Condições de saída**
   - **Saídas por reversão de inclinação** – quando a inclinação vira na direção oposta, o flag correspondente `EnableBuyExits` ou `EnableSellExits` fechará a posição aberta.
   - **Saída baseada em tempo** – se `UseTimeExit` estiver habilitado, uma posição é fechada após mantê-la por `HoldingMinutes` minutos.
   - **Níveis de proteção** – `StopLossPoints` e `TakeProfitPoints` são expressos em passos de preço. São avaliados em cada vela finalizada comparando a máxima/mínima da sessão com o preço de entrada.

## Parâmetros

| Parâmetro | Descrição |
|-----------|-----------|
| `TradeVolume` | Quantidade usada para entradas de mercado. |
| `StopLossPoints` | Distância do stop de proteção em passos de preço. Definir como `0` para desabilitar. |
| `TakeProfitPoints` | Distância do alvo de lucro em passos de preço. Definir como `0` para desabilitar. |
| `EnableBuyEntries` / `EnableSellEntries` | Habilitar ou desabilitar entradas compradas/vendidas. |
| `EnableBuyExits` / `EnableSellExits` | Habilitar ou desabilitar saídas baseadas em inclinação. |
| `UseTimeExit` | Habilita a lógica de saída temporizada. |
| `HoldingMinutes` | Período de manutenção em minutos quando a saída temporizada está ativa. |
| `CandleType` | Período usado para cálculos (padrão 4 horas). |
| `JmaLength` | Comprimento de suavização da Média Móvel Jurik aplicado à saída FATL. |
| `AppliedPrices` | Fonte de preço para o filtro digital (fechamento, abertura, mediana, Demark, etc.). |
| `RoundingDigits` | Número de dígitos usados ao arredondar a linha suavizada. |
| `SignalBar` | Offset da barra finalizada usada para avaliar o estado do indicador. |

## Notas

- A estratégia processa apenas velas completamente finalizadas e portanto funciona bem com backtests históricos.
- `AppliedPrices.Demark` reproduz o mesmo cálculo que o indicador MQL original.
- Como o StockSharp trata a execução de ordens de forma assíncrona, o rastreamento interno do preço de entrada é atualizado sempre que uma nova posição é aberta e limpo sempre que uma ordem de saída é enviada.
