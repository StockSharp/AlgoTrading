# Estratégia Bruno
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

O assessor especialista Bruno é um sistema de seguidor de tendência originalmente escrito para MetaTrader 5. O porte mantém a mesma cadeia de confirmação: Average Directional Index (ADX) com movimento direcional, um par de médias móvies exponenciais (EMA 8/21), MACD (13, 34, 8), um Oscilador Estocástico (21, 3, 3) e a inclinação de um Parabolic SAR (0.055, 0.21). Cada filtro que concorda com a direção multiplica o tamanho da ordem por um fator configurável. Se tanto os sinais comprados quanto os vendidos forem amplificados na mesma vela, o trading é ignorado para evitar ordens conflitantes.

### Lógica de trading

- **Viés direcional**
  - A pressão comprada é fortalecida quando `+DI > -DI` e `+DI > 20`.
  - A pressão vendida é fortalecida quando `+DI < -DI` e `+DI < 40`.
- **Alinhamento de momentum**
  - A preferência comprada requer EMA(8) acima de EMA(21), Estocástico %K acima de %D e %K abaixo do limite de sobrecomprado (padrão 80).
  - A preferência vendida requer EMA(8) abaixo de EMA(21), Estocástico %K abaixo de %D e %K acima do limite de sobrevendido (padrão 20).
- **Filtro MACD**
  - Viés comprado: histograma MACD acima de zero e linha principal MACD acima da linha de sinal.
  - Viés vendido: histograma MACD abaixo de zero e linha principal MACD abaixo da linha de sinal.
- **Inclinação do Parabolic SAR**
  - O viés comprado é reforçado quando os valores anteriores do SAR estão subindo enquanto EMA(8) > EMA(21).
  - O viés vendido é reforçado quando os valores anteriores do SAR estão caindo enquanto EMA(8) < EMA(21).

Cada condição satisfeita multiplica o tamanho do lote base por `SignalMultiplier` (padrão 1.6). Apenas um lado pode estar ativo de cada vez. Quando um sinal final é gerado, a estratégia fecha qualquer posição oposta, envia a ordem a mercado com o volume multiplicado e armazena o fechamento atual como preço de entrada.

### Gestão de posições

- **Stop-loss / take-profit** – distâncias fixas expressas em pips ajustados, correspondendo à versão MetaTrader. Se qualquer nível for atingido intrabarra, a posição é fechada imediatamente.
- **Trailing stop** – ativa quando o lucro flutuante excede `TrailingStop + TrailingStep` pips. O stop é então colocado `TrailingStop` pips atrás do preço e só avança quando o ganho aumenta pelo menos mais `TrailingStep` pips.
- **Tratamento de conflito** – se ambos os filtros comprado e vendido dispararem na mesma vela, nenhuma nova operação é tomada.

### Parâmetros

| Grupo | Nome | Descrição |
| --- | --- | --- |
| Trading | `BaseVolume` | Tamanho inicial do lote antes dos multiplicadores. |
| Trading | `SignalMultiplier` | Multiplicador de volume aplicado por cada filtro coincidente. |
| Gestão de risco | `StopLossPips` / `TakeProfitPips` | Distâncias de proteção em pips ajustados. Defina como zero para desabilitar. |
| Gestão de risco | `TrailingStopPips` / `TrailingStepPips` | Distância de trailing e passo mínimo em pips ajustados. |
| Indicadores | `AdxPeriod`, `AdxPositiveThreshold`, `AdxNegativeThreshold` | Comprimento do ADX e limites do DI. |
| Indicadores | `FastEmaPeriod`, `SlowEmaPeriod` | Comprimentos de EMA usados na confirmação de tendência. |
| Indicadores | `MacdFastPeriod`, `MacdSlowPeriod`, `MacdSignalPeriod` | Configuração do MACD. |
| Indicadores | `StochasticPeriod`, `StochasticKsmoothing`, `StochasticDsmoothing`, `StochasticOverbought`, `StochasticOversold` | Configurações do oscilador estocástico. |
| Geral | `CandleType` | Período usado para toda a cadeia de sinais (padrão 1 hora). |

### Notas

- O tamanho de pip ajustado segue a convenção do MetaTrader: instrumentos com 3 ou 5 dígitos decimais são multiplicados por 10.
- O Parabolic SAR opera com passo de aceleração `0.055` e máximo `0.21`, espelhando os padrões do assessor especialista.
- O porte mantém o estilo de gerenciamento de capital original (empilhamento de volume) mas agrega a exposição em uma única posição do StockSharp.
