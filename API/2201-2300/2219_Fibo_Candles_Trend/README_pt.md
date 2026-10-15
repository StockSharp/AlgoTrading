# Estratégia de Tendência Fibo Candles
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia utiliza a técnica personalizada **Fibo Candles** para determinar a direção da tendência.
O indicador pinta cada candle em uma de duas cores com base em uma comparação de razão de Fibonacci
entre o fechamento atual e o intervalo máximo/mínimo recente. Uma mudança de cor sinaliza uma possível
reversão. Quando a cor se torna altista, a estratégia fecha qualquer posição vendida e abre uma comprada.
Quando a cor se torna baixista, fecha qualquer posição comprada e abre uma vendida.

O método se adapta à volatilidade do mercado por meio de um período de lookback e nível de Fibonacci selecionável.
Um stop loss e take profit em pontos absolutos protegem cada operação.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: A cor do candle atual muda de baixista para altista.
  - **Vendido**: A cor do candle atual muda de altista para baixista.
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**:
  - As posições existentes são fechadas quando a cor oposta aparece.
- **Stops**: Stop loss e take profit fixos em pontos via `StartProtection`.
- **Valores padrão**:
  - `Period` = 10 (candles utilizados para medir o intervalo máximo/mínimo).
  - `Fibo Level` = 0.236 (razão utilizada para a decisão de tendência).
  - `Stop Loss` = 1000 pontos.
  - `Take Profit` = 2000 pontos.
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: Highest, Lowest
  - Stops: Sim
  - Complexidade: Médio
  - Período: Horário por padrão
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Moderado
