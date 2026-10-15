# Estratégia MACD em Modo Tendência
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que opera usando o indicador MACD com três modos de detecção de tendência selecionáveis: inclinação do histograma, cruzamento de nuvem ou cruzamento de linha zero.

## Detalhes

- **Critérios de entrada**:
  - *Histograma*: o histograma estava caindo e então vira para cima para comprados; subindo e então vira para baixo para vendidos.
  - *Nuvem*: a linha MACD estava previamente acima da linha de sinal e cruza abaixo para abrir comprado; o cruzamento oposto abre vendido.
  - *Zero*: o histograma cruza a linha zero na direção oposta.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Condições opostas fecham posições.
- **Stops**: Não.
- **Valores padrão**:
  - `FastLength` = 12
  - `SlowLength` = 26
  - `SignalLength` = 9
  - `TrendMode` = TrendMode.Cloud
  - `Volume` = 1m
  - `CandleType` = TimeSpan.FromHours(4)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: MACD
  - Stops: Não
  - Complexidade: Intermediário
  - Período: 4h
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Sim (histograma)
  - Nível de risco: Médio
