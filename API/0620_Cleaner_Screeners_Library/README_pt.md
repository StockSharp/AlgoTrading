# Biblioteca de Screeners Limpa
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia de screener simples que avalia o RSI em múltiplos símbolos e imprime classificações de compra ou venda. Serve como base para construir screeners personalizados de múltiplos ativos.

## Detalhes

- **Critérios de entrada**: Os valores de RSI são verificados contra limites para cada símbolo.
- **Comprado/Vendido**: Nenhum (apenas sinais)
- **Critérios de saída**: Nenhum
- **Stops**: Nenhum
- **Valores padrão**:
  - `RsiLength` = 14
  - `StrongThreshold` = 70m
  - `WeakThreshold` = 60m
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filtros**:
  - Categoria: Screener
  - Direção: N/A
  - Indicadores: RSI
  - Stops: Não
  - Complexidade: Básico
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: N/A
