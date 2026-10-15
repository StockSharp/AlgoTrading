# Estratégia Multi-Passo FlexiSuperTrend
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Um filtro SuperTrend combinado com um oscilador de desvio suavizado.
A estratégia inclui três níveis de take profit configuráveis.

## Detalhes

- **Critérios de entrada**:
  - Preço abaixo do SuperTrend e desvio (SMA do preço menos SuperTrend) > 0 → comprar.
  - Preço acima do SuperTrend e desvio < 0 → vender.
- **Comprado/Vendido**: Comprado, vendido ou ambas as direções.
- **Critérios de saída**:
  - Take profit parcial em 3 níveis.
  - Posição restante fechada na reversão de tendência quando o preço cruza o SuperTrend.
- **Stops**: Sem lógica de stop por padrão.
- **Valores padrão**:
  - Período ATR = 10.
  - Fator ATR = 3.0.
  - Comprimento SMA = 10.
  - Níveis de take profit = 2%, 8%, 18%.
  - Percentuais de take profit = 30%, 20%, 15%.
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: SuperTrend, SMA
  - Stops: Take profit
  - Complexidade: Moderado
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
