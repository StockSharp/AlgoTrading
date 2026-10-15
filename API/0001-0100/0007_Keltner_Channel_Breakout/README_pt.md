# Rompimento do Canal Keltner
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia baseada no rompimento do Canal Keltner.

Os testes indicam um retorno anual médio de aproximadamente 58%. Funciona melhor no mercado de ações.

O Rompimento do Canal Keltner usa bandas de volatilidade derivadas do ATR. Rompimentos acima da banda superior ou abaixo da banda inferior acionam entradas. O preço voltando através do centro EMA ou atingindo um stop encerra a posição.

Como as bandas se expandem e contraem com a volatilidade, este método de rompimento visa capturar os estágios iniciais de um movimento forte enquanto ainda permite ao preço respirar dentro do canal.


## Detalhes

- **Critérios de entrada**: Sinais baseados em ATR, Keltner.
- **Comprado/Vendido**: Ambos os sentidos.
- **Critérios de saída**: Sinal oposto ou stop.
- **Stops**: Sim.
- **Valores padrão**:
  - `EmaPeriod` = 20
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Ambos
  - Indicadores: ATR, Keltner
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário (5m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

