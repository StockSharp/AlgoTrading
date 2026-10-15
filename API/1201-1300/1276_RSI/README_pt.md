# Estratégia RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia simples baseada no Índice de Força Relativa. Compra quando o RSI cruza acima do nível de sobrevenda e vende quando cruza abaixo do nível de sobrecompra.

## Detalhes

- **Critérios de entrada**:
  - Comprado: RSI cruza acima de `OverSold`
  - Vendido: RSI cruza abaixo de `OverBought`
- **Comprado/Vendido**: Ambos
- **Critérios de saída**:
  - Sinal oposto
- **Stops**: Não
- **Valores padrão**:
  - `RsiLength` = 14
  - `OverSold` = 25m
  - `OverBought` = 75m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoria: Oscilador
  - Direção: Ambos
  - Indicadores: RSI
  - Stops: Não
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Baixo
