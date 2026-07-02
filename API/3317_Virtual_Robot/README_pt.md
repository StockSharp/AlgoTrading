# Estratégia Virtual Robot
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A estratégia Virtual Robot recria a abordagem de média baseada em grid do expert advisor MetaTrader original. O algoritmo mantém duas grades virtuais independentes (compra e venda) em um timeframe de candles configurável. Somente quando o número de níveis virtuais atinge o limiar definido é que ordens reais a mercado são enviadas. Isso permite à estratégia simular o comportamento MT4 em que níveis virtuais guiam a gestão real de posições.

## Lógica de negociação

1. **Criação da escada virtual:** em cada candle concluído a estratégia compara o fechamento com o preço de abertura.
   - Se o candle fecha acima da abertura, um novo nível virtual comprado é anexado quando a distância do nível comprado anterior excede o passo em pips.
   - Se o candle fecha abaixo, a mesma lógica é aplicada à escada virtual vendida.
   - As primeiras `VirtualStepper` operações virtuais usam o lote base; níveis posteriores escalam o tamanho por `Multiplier`.
2. **Promoção para ordens reais:** depois que pelo menos `StartingRealOrders` níveis virtuais existem para um lado (ou uma cesta existente entra em drawdown de pelo menos um passo em pips), a estratégia abre uma ordem real a mercado com volume calculado pelo multiplicador martingale (`Multiplier * distance / PipStep`).
3. **Gestão da cesta:** a estratégia acompanha:
   - O último preço de execução e volume de cada lado.
   - A média ponderada da cesta aberta (real ou virtual, dependendo de `RealAverageThreshold`).
4. **Lógica de take-profit:** posições são fechadas quando qualquer uma das condições abaixo é atendida:
   - O preço se move `MinTakeProfitPips` a partir da primeira ordem virtual (take-profit de nível único).
   - O preço retorna à média virtual ponderada mais/menos `AverageTakeProfitPips` para grids multinível.
   - O nível calculado de take-profit de ordem única ou médio (derivado de `TakeProfitPips` / `AverageTakeProfitPips`) é alcançado.
5. **Lógica de stop-loss:** um stop suave é derivado da última ordem executada usando `StopLossPips`. Quando o preço cruza o nível protetor, a cesta é liquidada.
6. **Segurança de volume:** tamanhos de lote são normalizados contra os metadados do ativo (`VolumeStep`, `MinVolume`, `MaxVolume`) e limitados por `MaxVolume`.

## Parâmetros

| Parâmetro | Descrição |
|-----------|-------------|
| `CandleType` | Série de candles usada para formar a escada virtual (padrão: candles de 60 minutos). |
| `StopLossPips` | Distância do stop em pips a partir da última ordem executada. |
| `TakeProfitPips` | Distância de take-profit para cestas de ordem única. |
| `MinTakeProfitPips` | Lucro mínimo exigido para fechar um único nível virtual. |
| `AverageTakeProfitPips` | Meta de lucro aplicada à média ponderada da cesta. |
| `BaseVolume` | Tamanho base de lote para as primeiras ordens do grid. |
| `MaxVolume` | Tamanho máximo de lote permitido. |
| `Multiplier` | Multiplicador de lote para entradas médias. |
| `RealStepper` | Número de ordens reais executadas antes do multiplicador entrar. |
| `VirtualStepper` | Ordens virtuais preenchidas com lote base antes da escala. |
| `PipStepPips` | Excursão adversa mínima (em pips) entre níveis sucessivos do grid. |
| `MaxTrades` | Limite rígido do número de ordens reais por lado. |
| `StartingRealOrders` | Número de ordens virtuais exigido antes de colocar a primeira ordem real. |
| `RealAverageThreshold` | Troca o preço médio de virtual para real quando esse número de ordens é executado. |
| `VisualMode` | Mantido por paridade com a entrada MT4 (sem efeito no StockSharp). |

## Notas de implementação

- A estratégia usa posições líquidas (modelo de carteira StockSharp) e portanto não consegue manter cestas compradas e vendidas simultâneas independentes como no modo hedging do MT4. Quando as duas escadas virtuais disparam, o sinal mais recente inverterá a posição líquida.
- O desenho de gráfico do EA original é intencionalmente omitido; todos os níveis virtuais são mantidos internamente.
- Passos de preço são derivados de `Security.PriceStep` (com ajuste 10x para instrumentos forex de três/cinco dígitos) para espelhar a lógica de conversão de pips do MT4.
- Ordens protetoras são modeladas monitorando preços no manipulador de candles e enviando saídas a mercado, em vez de anexar stops/limits do lado da corretora.

## Dicas de uso

1. Garanta que os metadados do instrumento (`PriceStep`, `VolumeStep`, `MinVolume`, `MaxVolume`) estejam preenchidos para que conversão de pips e normalização de lotes correspondam às regras da corretora.
2. Comece em simulação ou com volume pequeno para validar que distâncias do grid e multiplicadores se alinham com a corretora planejada.
3. Ajuste `StartingRealOrders` e `RealStepper` para controlar a agressividade da escala martingale.
