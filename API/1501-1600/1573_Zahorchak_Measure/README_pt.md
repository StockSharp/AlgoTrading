# Estratégia Zahorchak Measure
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Calcula uma pontuação ponderada usando múltiplas médias móveis. Compra quando a pontuação se torna positiva e vende quando se torna negativa.

## Detalhes

- **Critérios de entrada**: A pontuação cruza acima de zero
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: Sinal oposto
- **Stops**: Não
- **Valores padrão**:
  - `Points` = 1
  - `EmaLength` = 10
- **Filtros**:
  - Categoria: Amplitude de mercado
  - Direção: Ambos
  - Indicadores: SMA, EMA
  - Stops: Não
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
