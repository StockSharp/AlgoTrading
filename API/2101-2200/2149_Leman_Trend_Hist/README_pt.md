# Estratégia LeMan Trend Hist
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia é uma conversão simplificada do especialista MQL5 original "LeManTrendHist". Ela se baseia em um histograma baseado em EMA para gerar sinais de trading.

## Ideia

O algoritmo original calcula um histograma personalizado derivado de extremos de preço e intervalos suavizados. Para este exemplo, o histograma é aproximado por uma média móvel exponencial dos intervalos das velas.

## Lógica da estratégia

1. Calcular o valor EMA para cada vela concluída.
2. Comparar os três últimos valores EMA.
3. Quando o valor do meio é menor que o mais antigo e o valor mais recente sobe acima dele, uma posição comprada é aberta e as posições vendidas são fechadas.
4. Quando o valor do meio é maior que o mais antigo e o valor mais recente cai abaixo dele, uma posição vendida é aberta e as posições compradas são fechadas.

## Parâmetros

- **Candle Type** – período das velas processadas.
- **EMA Period** – comprimento do EMA usado no histograma de espaço reservado.
- **Signal Bar** – deslocamento histórico para valores do indicador (mantido para compatibilidade, não usado na lógica simplificada).
- **Buy/Sell Open** – habilitar entradas compradas ou vendidas.
- **Buy/Sell Close** – habilitar o fechamento de posições existentes.

## Notas

O verdadeiro indicador LeManTrendHist usa algoritmos de suavização complexos que ainda não foram implementados. A implementação atual serve como espaço reservado e deve ser substituída pelo indicador completo para uso em produção.
