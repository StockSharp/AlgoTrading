# Estratégia TripleStochasticMTF
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia executa três Stochastic Oscillators em diferentes períodos de tempo e opera quando o menor período cruza sua linha de sinal na direção confirmada pelos períodos superiores. É projetada para capturar reversões de curto prazo dentro de um contexto de tendência maior.

O período primário (padrão 30 minutos) e o secundário (padrão 15 minutos) determinam o viés do mercado. O período de entrada (padrão 5 minutos) aguarda um cruzamento de %K e %D oposto à barra anterior, sinalizando um pullback. As posições são fechadas quando qualquer um dos períodos monitorados sinaliza uma mudança de tendência contra a operação ativa.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: %K anterior > %D no gráfico de 5 minutos, %K atual ≤ %D, e ambos os períodos superiores mostram %K > %D.
  - **Vendido**: %K anterior < %D no gráfico de 5 minutos, %K atual ≥ %D, e ambos os períodos superiores mostram %K < %D.
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**:
  - **Comprado**: Qualquer período muda para tendência de baixa (%K < %D).
  - **Vendido**: Qualquer período muda para tendência de alta (%K > %D).
- **Stops**: Não implementados por padrão.
- **Valores padrão**:
  - `Timeframe 1` = 30 minutos.
  - `Timeframe 2` = 15 minutos.
  - `Timeframe 3` = 5 minutos.
  - `%K Period` = 5.
  - `%D Period` = 3.
  - `Slowing` = 3.
- **Filtros**:
  - Categoria: Seguidor de tendência / Pullback
  - Direção: Ambos
  - Indicadores: Stochastic Oscillator
  - Stops: Não
  - Complexidade: Médio
  - Período: Curto plazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Moderado
