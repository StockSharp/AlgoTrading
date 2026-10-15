# Estratégia de Cores
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que opera com base na luminosidade percebida de uma cor configurada.
Se a cor for clara (luminância > 0,5) a estratégia compra, caso contrário vende.

## Detalhes

- **Critérios de entrada**:
  - Comprado: `Color luminance > 0.5`
  - Vendido: `Color luminance <= 0.5`
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: Sinal oposto
- **Stops**: Não
- **Valores padrão**:
  - `ColorHex` = "#f23645"
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filtros**:
  - Categoria: Outro
  - Direção: Ambos
  - Indicadores: Nenhum
  - Stops: Não
  - Complexidade: Básico
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Baixo
