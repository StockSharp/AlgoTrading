# Estratégia de Estimador de Quantis Harrell-Davis Ponderado com DesvioAbsoluto
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia utiliza um estimador de quantis baseado na mediana com bandas de desvio absoluto para detectar valores atípicos de preço.
Compra quando o fechamento cai abaixo da banda inferior e vende quando sobe acima da banda superior.

## Detalhes

- **Critérios de entrada**: fechamento abaixo da banda de desvio inferior ou acima da banda superior
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: cruzamento da banda oposta
- **Stops**: Não
- **Valores padrão**:
  - `Length` = 39
  - `DeviationMultiplier` = 1.213
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Ambos
  - Indicadores: Median
  - Stops: Não
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
