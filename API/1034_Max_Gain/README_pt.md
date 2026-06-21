# Ganho Máximo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Max Gain compara a distância percentual da mínima mais baixa até a máxima atual e da máxima mais alta até a mínima atual ao longo de um período de retrocesso. Vai comprado quando o ganho potencial supera a perda ajustada; caso contrário, vai vendido.

## Detalhes
- **Dados**: Velas de preço.
- **Critérios de entrada**:
  - **Comprado**: Max gain > adjusted max loss.
  - **Vendido**: Adjusted max loss > max gain.
- **Critérios de saída**: Sinal reverso.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `PeriodLength` = 30
- **Filtros**:
  - Categoria: Momentum
  - Direção: Comprado e vendido
  - Indicadores: Highest, Lowest
  - Complexidade: Baixo
  - Nível de risco: Médio
