# Estratégia Exp Sinewave2 X2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Exp Sinewave2 X2 é uma estratégia de seguimento de tendência multi-período inspirada na análise Sinewave de John Ehlers. O filtro de período superior define a direção dominante, enquanto o período inferior fornece gatilhos precisos de entrada e saída. Todos os cálculos usam o indicador Sinewave2 reconstruído, que internamente depende do módulo adaptativo CyclePeriod.

## Indicadores
- **Sinewave2 de período superior (linha lead vs. linha sine)** – detecta viés altista ou baixista usando o cruzamento do lead sine sobre o componente sine principal.
- **Sinewave2 de período inferior** – monitora os eventos de cruzamento mais recentes para acionar trades alinhados com a direção do período superior.

## Lógica de trading
1. **Filtro de tendência**
   - Calcular Sinewave2 no período superior.
   - Avaliar as linhas lead e main `SignalBarHigh` barras atrás.
   - A tendência é altista se `Lead > Sine`, baixista se `Lead < Sine`, caso contrário neutro.
2. **Sinais de entrada**
   - Aguardar uma vela finalizada no período inferior.
   - Recuperar os valores lead e sine nos deslocamentos definidos por `SignalBarLow` (atual) e `SignalBarLow + 1` (anterior).
   - Entrada comprada: o cruzamento anterior foi para baixo (`Lead > Sine` anteriormente, `Lead <= Sine` agora) enquanto a tendência do período superior é altista e `EnableBuyOpen` está habilitado.
   - Entrada vendida: o cruzamento anterior foi para cima (`Lead < Sine` anteriormente, `Lead >= Sine` agora) enquanto a tendência do período superior é baixista e `EnableSellOpen` está habilitado.
3. **Regras de saída**
   - Os booleanos de saída do período inferior `EnableBuyCloseLower` e `EnableSellCloseLower` fecham posições em cruzamentos opostos.
   - Os booleanos de saída do período superior `EnableBuyCloseTrend` e `EnableSellCloseTrend` fecham posições imediatamente sempre que a tendência principal vira contra a direção aberta.
   - O stop loss protetor e o take profit são avaliados a cada vela usando as máximas/mínimas intrabar e as distâncias `StopLossPoints` / `TakeProfitPoints` expressas em passos de preço.
4. **Gestão de risco**
   - As reversões de posição dimensionam as novas ordens como `Volume + |Position|` para achatar a posição existente antes de estabelecer a nova.
   - Após cada entrada `SetRiskLevels` recalcula preços absolutos de stop/alvo usando `Security.PriceStep` (fallback 1 quando não disponível).

## Parâmetros
| Nome | Descrição |
| --- | --- |
| `AlphaHigh` | Fator alpha para o filtro Sinewave2 de período superior. |
| `AlphaLow` | Fator alpha para o gatilho Sinewave2 de período inferior. |
| `SignalBarHigh` | Número de barras atrás no período superior usadas para ler o estado de tendência. |
| `SignalBarLow` | Número de barras atrás no período inferior usadas para ler estados de cruzamento. |
| `EnableBuyOpen` / `EnableSellOpen` | Permitir entradas compradas/vendidas de sinais do período inferior. |
| `EnableBuyCloseTrend` / `EnableSellCloseTrend` | Forçar saídas quando o período superior vira contra a posição. |
| `EnableBuyCloseLower` / `EnableSellCloseLower` | Fechar posições em cruzamentos opostos do período inferior. |
| `StopLossPoints` | Distância do stop-loss expressa em passos de preço do instrumento. |
| `TakeProfitPoints` | Distância do take-profit expressa em passos de preço do instrumento. |
| `HigherCandleType` / `LowerCandleType` | Tipos de dados de velas (períodos) para os fluxos de filtro e gatilho. |

## Notas
- A estratégia processa apenas velas finalizadas e ignora atualizações parciais.
- A implementação adaptativa do Sinewave2 usa o algoritmo CyclePeriod original para permanecer fiel à versão MQL.
- Quando os tipos de vela superior e inferior são idênticos, ambos os indicadores compartilham uma única subscrição de velas para evitar solicitações de dados redundantes.
- Ajuste `Volume` na `Strategy` base para controlar o tamanho do trade antes do implantação.
