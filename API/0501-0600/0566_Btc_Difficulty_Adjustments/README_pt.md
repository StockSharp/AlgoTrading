# Estratégia de Ajustes de Dificuldade BTC
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia de Ajustes de Dificuldade BTC opera com base nas mudanças na dificuldade de mineração do Bitcoin. Quando o modo de limite está ativado, as operações são abertas somente se a variação percentual exceder o limite especificado. Uma posição comprada é aberta em ajustes de dificuldade positivos, enquanto uma posição vendida é aberta em ajustes negativos.

## Detalhes

- **Critérios de entrada**:
  - Modo de limite: `abs(change) >= Threshold` e `change < 0` → entrar comprado.
  - Modo de limite: `abs(change) >= Threshold` e `change > 0` → entrar vendido.
  - Sem modo de limite: `difficulty > difficulty anterior` → entrar comprado.
  - Sem modo de limite: `difficulty < difficulty anterior` → entrar vendido.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**:
  - O sinal oposto fecha e reverte as posições.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `CandleType` = 1 dia
  - `ThresholdMode` = false
  - `Threshold` = 10
- **Filtros**:
  - Categoria: Fundamental
  - Direção: Comprado e Vendido
  - Indicadores: Nenhum
  - Stops: Não
  - Complexidade: Baixo
  - Período: Diário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
