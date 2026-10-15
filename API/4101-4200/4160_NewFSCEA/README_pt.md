# Nova Estratégia FSCEA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A nova estratégia FSCEA é um sistema de acompanhamento de tendências baseado em MACD que foi transferido do MetaTrader 4 consultor especialista original `new_fscea.mq4`. A estratégia combina uma confirmação cruzada clássica MACD com um filtro de inclinação EMA, metas estáticas de lucro e um trailing stop para gerenciar posições abertas. Negocia um único símbolo por vez e abre apenas uma posição no mercado.

## Lógica de negociação
### Entrada longa
- A linha principal MACD está abaixo de zero, mas cruza acima da linha de sinal na vela fechada atual.
- A vela anterior ainda tinha a linha MACD abaixo da linha de sinal (confirma o cruzamento).
- O valor absoluto da linha MACD excede o limite `OpenLevelPoints` (escalonado por etapa de preço).
- A inclinação EMA deslocada é positiva (`EMA_shifted_now > EMA_shifted_previous`).
- Nenhuma posição está aberta no momento.

### Entrada curta
- A linha principal MACD está acima de zero, mas cruza abaixo da linha de sinal na vela fechada atual.
- A vela anterior ainda tinha a linha MACD acima da linha de sinal.
- A linha principal MACD excede o limite `OpenLevelPoints` (escalonado por etapa de preço).
- A inclinação EMA deslocada é negativa (`EMA_shifted_now < EMA_shifted_previous`).
- Nenhuma posição está aberta no momento.

### Saída Longa
- Acionado quando MACD cruza abaixo da linha de sinal enquanto permanece acima de zero e o valor MACD excede o limite de `CloseLevelPoints`.
- Ou quando a vela atinge o nível de lucro virtual (`entry + TakeProfitPoints * priceStep`).
- Ou quando a mínima da vela atinge o nível do trailing stop (atualizado dinamicamente conforme o preço se move a favor).

### Saída curta
- Acionado quando MACD cruza acima da linha de sinal enquanto permanece abaixo de zero e o valor absoluto de MACD excede o limite de `CloseLevelPoints`.
- Ou quando a mínima da vela atinge o nível de lucro virtual (`entry - TakeProfitPoints * priceStep`).
- Ou quando a máxima da vela atinge o nível do trailing stop (atualizado dinamicamente conforme o preço se move a favor).

## Gestão de risco
- O Take Profit é expresso em pontos de instrumento e convertido em preço multiplicando por `Security.PriceStep`.
- O trailing stop funciona em pontos e se estreita quando o lucro flutuante é maior que a distância final.
- Apenas uma posição pode ser aberta por vez, refletindo o comportamento do consultor especialista MT4.
- A proteção de posição é ativada por meio do auxiliar `StartProtection()` integrado.

## Indicadores
- **MACD (12, 26, 9)** – o principal mecanismo de crossover. A magnitude do histograma fornece os limites de entrada e saída.
- **EMA (TrendPeriod)** – aplicado a preços de fechamento. A comparação de inclinação usa um deslocamento configurável (`TrendShift`) para emular o parâmetro MT4 `ma_shift`.

## Parâmetros
| Parâmetro | Padrão | Descrição |
|-----------|---------|-------------|
| `TakeProfitPoints` | 300 | Distância até a meta de lucro em pontos. Convertido em preço usando a etapa de preço do símbolo. |
| `TrailingStopPoints` | 20 | Tamanho do trailing stop em pontos. Ativado somente após a negociação se mover a favor por mais do que essa distância. |
| `OpenLevelPoints` | 3 | Magnitude mínima de MACD (pontos) necessária antes que uma nova negociação seja permitida. |
| `CloseLevelPoints` | 2 | Magnitude de MACD (pontos) necessária para fechar uma negociação por meio de cruzamento de MACD. |
| `TrendPeriod` | 10 | Comprimento do filtro de tendência EMA. |
| `TrendShift` | 2 | Deslocamento horizontal (em barras) aplicado ao EMA ao avaliar sua inclinação. Valores mais altos atrasam a confirmação da tendência. |
| `TradeVolume` | 0,1 | Volume de ordens padrão enviado com ordens de mercado. |
| `CandleType` | Período de 1 hora | Tipo de vela usado para cálculos de indicadores; pode ser alterado para corresponder ao período de tempo desejado. |

## Notas de implementação
- A estratégia processa apenas velas finalizadas para manter a lógica próxima da versão MT4.
- O deslocamento EMA é emulado armazenando em buffer as saídas do indicador e comparando valores separados por `TrendShift` barras.
- O trailing stop e o take-profit são implementados virtualmente (sem ordens reais de stop/limit) para permanecer dentro dos requisitos de alto nível API.
- O código depende exclusivamente da assinatura de vela de alto nível API (`SubscribeCandles().BindEx(...)`) para cumprir as diretrizes do repositório.
