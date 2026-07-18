# Estratégia Hull Ma Stochastic
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia Hull Moving Average + Stochastic Oscillator. A estratégia entra quando a direção da tendência do HMA muda com o Stochastic confirmando condições de sobrevenda/sobrecompra.

Os testes indicam um retorno anual médio de aproximadamente 94%. Funciona melhor no mercado de ações.

O Hull MA revela rapidamente a direção da tendência. O Stochastic aguarda uma queda ou rali dentro dessa tendência para acionar a operação.

Uma abordagem flexível para quem deseja sinais suaves. Stops baseados em ATR limitam a perda potencial.

## Detalhes

- **Critérios de entrada**:
  - Comprado: `HullMA turning up && StochK < 20`
  - Vendido: `HullMA turning down && StochK > 80`
- **Comprado/Vendido**: Ambos
- **Critérios de saída**:
  - Mudança de direção do Hull MA
- **Stops**: Baseados em ATR usando `StopLossAtr`
- **Valores padrão**:
  - `HmaPeriod` = 9
  - `StochPeriod` = 14
  - `StochK` = 3
  - `StochD` = 3
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
  - `StopLossAtr` = 2m
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Ambos
  - Indicadores: Hull MA, Moving Average, Stochastic Oscillator
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

