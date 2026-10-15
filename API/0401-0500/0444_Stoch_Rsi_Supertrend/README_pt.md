# Estratégia Stochastic RSI SuperTrend
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Este sistema combina as oscilações rápidas do Stochastic RSI com um filtro de
tendência e um modelo SuperTrend simplificado. O oscilador destaca os extremos de
momentum de curto prazo, enquanto a média móvel e as bandas ATR definem a tendência
dominante. As operações são abertas apenas quando a linha %K cruza %D dentro da zona
relevante e a tendência mais ampla está alinhada, reduzindo os falsos sinais em
condições laterais.

A configuração padrão foca em operações compradas, mas pode opcionalmente habilitar
entradas vendidas. A estratégia é projetada para períodos intradiários a swing, onde
os sinais do Stochastic RSI aparecem frequentemente e as bandas baseadas em ATR
fornecem um viés adaptativo à volatilidade. As saídas ocorrem em cruzamentos opostos,
permitindo que o mercado rode até que o momentum se esgote.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: fechamento acima da MA de tendência, %K < 20, %K cruza acima de %D, SuperTrend mostra tendência de alta.
  - **Vendido**: fechamento abaixo da MA de tendência, %K > 80, %K cruza abaixo de %D, SuperTrend mostra tendência de baixa.
- **Comprado/Vendido**: Comprado por padrão, vendido opcional.
- **Critérios de saída**:
  - **Comprado**: %K > 80 e cruza abaixo de %D.
  - **Vendido**: %K < 20 e cruza acima de %D.
- **Stops**: Nenhum por padrão; podem ser adicionados externamente.
- **Valores padrão**:
  - Período RSI = 14, comprimento Stochastic = 14.
  - Tipo MA = EMA, comprimento MA = 100.
  - Período ATR = 10, fator ATR = 3.0.
- **Filtros**:
  - Categoria: Momentum + Tendência
  - Direção: Principalmente comprado
  - Indicadores: RSI, ATR, Média Móvel
  - Stops: Não
  - Complexidade: Moderado
  - Período: Curto/médio
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
