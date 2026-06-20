# Estratégia de Reversão Heikin-Ashi
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Os candles Heikin-Ashi suavizam o ruído e destacam a direção da tendência. Uma mudança de uma série de candles HA de baixa para um de alta, ou vice-versa, pode indicar uma mudança de momentum. Esta estratégia opera essas mudanças de cor e utiliza um stop percentual para proteção.

Os testes indicam um retorno anual médio de aproximadamente 145%. Funciona melhor no mercado de criptomoedas.

A lógica calcula os valores Heikin-Ashi a partir dos candles regulares. Quando o fechamento HA cruza acima da abertura HA após uma sequência de baixa, uma posição comprada é assumida. Um cruzamento abaixo após uma sequência de alta abre uma posição vendida. O stop é colocado a uma porcentagem fixa a partir da entrada.

O método é simples mas eficaz durante oscilações instáveis quando os gráficos de candles tradicionais são ruidosos.

## Detalhes

- **Critérios de entrada**: O candle Heikin-Ashi muda de cor.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Stop-loss.
- **Stops**: Sim, baseado em percentual.
- **Valores padrão**:
  - `CandleType` = 15 minute
  - `StopLoss` = 2%
- **Filtros**:
  - Categoria: Reversão
  - Direção: Ambos
  - Indicadores: Heikin-Ashi
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

