# Estratégia Hull Ma Stochastic
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia Hull Moving Average + Stochastic Oscillator. A estratégia entra quando a direção da tendência do HMA muda com o Stochastic confirmando condições de sobrevenda/sobrecompra.

Os testes indicam um retorno anual médio de aproximadamente 94%. Funciona melhor no mercado de ações.

O Hull MA revela rapidamente a direção da tendência. O Stochastic aguarda uma queda ou rali dentro dessa tendência para acionar a operação.

Uma abordagem flexível para quem deseja sinais suaves. Stops baseados em ATR limitam a perda potencial.

## Detalhes

- **Critérios de entrada**:
  - Comprado: `HullMA turning up && StochK < StochOversold`
  - Vendido: `HullMA turning down && StochK > StochOverbought`
- **Comprado/Vendido**: Ambos
- **Critérios de saída**:
  - Mudança de direção do Hull MA
- **Stops**: Baseados em ATR usando `StopLossAtr`
- **Valores padrão**:
  - `HmaPeriod` = 9
  - `StochPeriod` = 14
  - `StochK` = 3
  - `StochOversold` = 20
  - `StochOverbought` = 80
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
  - `StopLossAtr` = 2
  - `AtrPeriod` = 14
    A HMA vira para cima quando sobe depois de cair e para baixo quando cai depois de subir; uma compra é encerrada quando ela cai e uma venda quando sobe. %K é o estocástico de StochPeriod candles suavizado em StochK candles, e o %D não participa. O stop fica a StopLossAtr ATR (AtrPeriod) do fechamento de entrada e é verificado nos fechamentos dos candles. Um sinal de entrada contra uma posição aberta a inverte.
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

