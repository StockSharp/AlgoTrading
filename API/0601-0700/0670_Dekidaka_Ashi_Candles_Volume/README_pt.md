# Estratégia de Volume de Velas Dekidaka-Ashi
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia combina o corpo do candle com o volume suavizado usando a abordagem Dekidaka-Ashi. Compra em sinais de alta e vende em sinais de baixa. Candles que abrangem ambos os intervalos fecham as posições abertas.

## Detalhes

- **Critérios de entrada**:
  - Sinal de alta forte ou fraco: máxima acima do intervalo superior e mínima acima do intervalo inferior.
  - Sinal de baixa forte ou fraco: máxima abaixo do intervalo superior e mínima abaixo do intervalo inferior.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**:
  - Sinal oposto ou candle que abrange ambos os intervalos (incerteza).
- **Stops**: Não.
- **Valores padrão**:
  - `BodySize` = 1
  - `VolumeSmooth` = 1
  - `CandleType` = período de 5 minutos
- **Filtros**:
  - Categoria: Padrão
  - Direção: Comprado e Vendido
  - Indicadores: EMA, Volume
  - Stops: Não
  - Complexidade: Baixo
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
