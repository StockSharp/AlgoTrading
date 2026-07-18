# Estratégia de Canal de Bandas de Regressão Polinomial
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia ajusta uma linha de regressão polinomial aos preços recentes e constrói bandas superior e inferior a partir do desvio padrão dos resíduos. Posições compradas são abertas quando o preço cai abaixo da banda inferior e posições vendidas quando o preço sobe acima da banda superior.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: `Close < LowerBand`.
  - **Vendido**: `Close > UpperBand`.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**:
  - Sinal oposto.
- **Stops**: Não.
- **Valores padrão**:
  - `Length` = 100.
  - `Degree` = 2.
  - `Std Dev Multiplier` = 2.
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Ambos
  - Indicadores: Regressão polinomial
  - Stops: Não
  - Complexidade: Moderado
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
