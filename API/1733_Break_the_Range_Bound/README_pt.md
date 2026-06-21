# Estratégia de Rompimento do Intervalo Lateral
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia detecta fases de mercado tranquilas onde três médias móveis convergem dentro de uma banda estreita. Quando o preço finalmente rompe acima ou abaixo deste intervalo, a estratégia entra na direção do rompimento e visa capturar a tendência emergente.

O sistema observa a diferença entre as SMAs Rápida, Média e Lenta. Se a diferença máxima entre essas médias permanecer abaixo do limiar configurado por um número específico de barras, o mercado é considerado "em intervalo". A máxima mais alta e a mínima mais baixa desse período definem os níveis de rompimento.

As operações são abertas quando o preço fecha além dessas extremidades. As posições são protegidas por condições inversas: se o preço retornar ao intervalo ou atingir um múltiplo da largura do intervalo em lucro, a posição é fechada.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: Após um intervalo de `RangeLength` barras onde a diferença da SMA esteja abaixo de `ShakeThreshold`, entrar quando o preço fechar acima da máxima mais alta do intervalo.
  - **Vendido**: Sob as mesmas condições de intervalo, entrar quando o preço fechar abaixo da mínima mais baixa do intervalo.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**:
  - **Comprado**: Fechar se o preço retornar abaixo da mínima do intervalo ou o lucro superar `4 * (máxima do intervalo - mínima do intervalo)`.
  - **Vendido**: Fechar se o preço retornar acima da máxima do intervalo ou o lucro superar `4 * (máxima do intervalo - mínima do intervalo)`.
- **Stops**: Saídas implícitas baseadas nos limites do intervalo e no múltiplo de lucro.
- **Valores padrão**:
  - `FastSma` = 38
  - `MidSma` = 140
  - `SlowSma` = 210
  - `ShakeThreshold` = 250
  - `RangeLength` = 200
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Ambos
  - Indicadores: SMA, Highest, Lowest
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
