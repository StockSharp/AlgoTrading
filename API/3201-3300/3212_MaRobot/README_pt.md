# Estratégia de MaRobot
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Resumo
- Implementa um sistema de cruzamento de médias móvies baseado em barras que opera em um período intradiário configurável enquanto usa filtros diários de ADX e RSI.
- Usa bindings de alto nível do StockSharp para calcular duas médias móveis simples juntamente com detectores de oscilação `Lowest`/`Highest` e indicadores diários `AverageDirectionalIndex` e `RelativeStrengthIndex`.
- Recria a lógica de proteção original do MT4: take-profit por percentual, stop-loss baseado em oscilação e um stop de break-even opcional assim que um lucro mínimo é alcançado.

## Indicadores
- `SimpleMovingAverage` (rápida e lenta) no período principal.
- `Lowest` / `Highest` para capturar os preços extremos das últimas `BackClose` velas para posicionamento do stop.
- Valores diários de `AverageDirectionalIndex` e `RelativeStrengthIndex` para filtros de força de tendência e momentum.

## Parâmetros
- `CandleType` – período principal (padrão: velas de 15 minutos).
- `FastPeriod`, `SlowPeriod` – comprimentos das linhas SMA rápida e lenta.
- `AdxThreshold` – valor máximo permitido do ADX diário para habilitar novas entradas.
- `RsiThreshold` – nível de RSI diário para entradas longas (a entrada curta usa `100 - RsiThreshold`).
- `TakeProfitRatio` – distância fracional entre o preço de entrada e o alvo de lucro.
- `StopLossPoints` – distância do stop de proteção (em pontos de instrumento) que é ativado ao atingir `ProtectThreshold`.
- `ProtectThreshold` – razão mínima de lucro aberto que ativa o stop de proteção.
- `BackClose` – número de velas concluídas usadas para o cálculo do stop de máxima/mínima de oscilação.
- `DailyAdxPeriod`, `DailyRsiPeriod` – comprimentos dos indicadores diários.

## Regras de trading
1. Trabalhar apenas em velas terminadas para corresponder ao consultor especialista MT4.
2. Aguardar até que todos os indicadores estejam totalmente formados e os valores diários estejam disponíveis.
3. **Filtros de entrada**:
   - Rejeitar novas posições quando o ADX diário excede `AdxThreshold`.
   - A entrada longa requer que a SMA rápida cruze acima da SMA lenta e o RSI diário esteja abaixo de `RsiThreshold`.
   - A entrada curta requer que a SMA rápida cruze abaixo da SMA lenta e o RSI diário esteja acima de `100 - RsiThreshold`.
4. Na entrada, armazenar o extremo de oscilação (`Lowest` para longos, `Highest` para curtos) como referência de stop manual.
5. **Lógica de saída** enquanto uma posição está ativa:
   - Fechar com `TakeProfitRatio` de lucro medido desde o preço de entrada armazenado.
   - Fechar se o fechamento da vela romper o nível de stop de oscilação armazenado.
   - Fechar em um cruzamento de média móvel oposto.
   - Após o lucro exceder `ProtectThreshold`, armar um stop estilo break-even deslocado por `StopLossPoints` (arredondado para o tamanho do tick) e fechar se o preço recuar através dele.
6. Redefinir todo o estado interno quando a posição líquida retornar a zero.

## Notas
- Todos os comentários no código C# são mantidos em inglês conforme as diretrizes do repositório.
- A estratégia depende exclusivamente das subscrições de alto nível do StockSharp via `Bind`, evitando buffers de indicadores manuais.
- A tradução para Python é intencionalmente omitida conforme as instruções da tarefa.
