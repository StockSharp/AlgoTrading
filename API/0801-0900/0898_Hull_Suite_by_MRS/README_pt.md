# Estratégia Hull Suite by MRS
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia de seguidor de tendência que compara a média móvel do tipo Hull selecionada com seu valor de duas barras atrás. Posições compradas são abertas quando a média sobe acima do valor de duas barras atrás, e posições vendidas quando cai abaixo.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: `MA > MA[2]`.
  - **Vendido**: `MA < MA[2]`.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Reversão ao sinal oposto.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `Length` = 55
  - `Mode` = Hma
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: Hull MA
  - Stops: Nenhum
  - Complexidade: Simples
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
