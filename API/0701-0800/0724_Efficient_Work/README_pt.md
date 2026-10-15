# Estratégia Efficient Work
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia usa médias móveis em horizontes curto, médio e longo. Uma posição comprada é aberta quando a média rápida está acima de ambas as médias mais lentas, e uma posição vendida quando está abaixo delas.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: `fast MA > medium MA` e `fast MA > high MA`.
  - **Vendido**: `fast MA < medium MA` e `fast MA < high MA`.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**:
  - Um sinal contrário desencadeia uma reversão.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `MA Period` = 20
  - `Medium TF Multiplier` = 5
  - `High TF Multiplier` = 10
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: Múltiplos
  - Stops: Não
  - Complexidade: Básico
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
