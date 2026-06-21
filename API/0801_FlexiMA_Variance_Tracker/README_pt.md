# Estratégia FlexiMA de Rastreamento de Variância
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Rastreia o desvio do preço em torno de uma média móvel e abre operações quando o desvio supera um limiar de volatilidade enquanto a direção do Supertrend confirma.

## Detalhes

- **Critérios de entrada**:
  - Preço acima do Supertrend e desvio > média + desvio padrão × multiplicador → compra.
  - Preço abaixo do Supertrend e desvio < -(média + desvio padrão × multiplicador) → venda.
- **Comprado/Vendido**: Ambas as direções podem ser habilitadas.
- **Critérios de saída**:
  - Desvio oposto ou reversão do Supertrend.
- **Stops**: Sem lógica de stop por padrão.
- **Valores padrão**:
  - Comprimento da MA = 20.
  - Comprimento do StdDev = 20.
  - Multiplicador StdDev = 1.0.
  - Período ATR = 10.
  - Fator ATR = 3.0.
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: SMA, StandardDeviation, SuperTrend
  - Stops: Nenhum
  - Complexidade: Moderado
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
